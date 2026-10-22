using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace DiceWarrior.SymbolLoop
{
    public sealed class SqliteAuthorityRepository : IAuthorityRepository
    {
        readonly string connectionString, userId;
        readonly IAuthorityTelemetry telemetry;
        const int MaxAttempts = 128;

        public SqliteAuthorityRepository(string path, string userId, IAuthorityTelemetry telemetry = null)
        {
            if (!AuthorityValidation.ValidId(userId)) throw new ArgumentException("Invalid authority user", nameof(userId));
            string absolutePath = Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            this.userId = userId;
            this.telemetry = telemetry;
            connectionString = new SqliteConnectionStringBuilder { DataSource = absolutePath,
                Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false, DefaultTimeout = 10 }.ToString();
            Initialize();
        }

        public BackendReply Execute(Func<MockAuthorityDocument, MockMutation> operation, CancellationToken ct)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            try
            {
                for (int attempt = 0; attempt < MaxAttempts; ++attempt)
                {
                    ct.ThrowIfCancellationRequested();
                    var snapshot = ReadSnapshot();
                    // Replay and JSON preparation run without a writer transaction or global lock.
                    var before = LoopJson.Copy(snapshot.document);
                    var mutation = operation(snapshot.document) ?? throw new InvalidOperationException("Missing authority mutation");
                    if (!mutation.changed) return LoopJson.Copy(mutation.reply);
                    AuthorityValidation.Validate(snapshot.document, userId);
                    var prepared = Prepare(before, snapshot.document);
                    ct.ThrowIfCancellationRequested();
                    using var connection = Open();
                    using var transaction = BeginTransaction(connection);
                    int changed = ExecuteSql(connection, transaction,
                        "UPDATE profiles SET version=version+1, profile_json=$profile, active_run_id=$active, rate_json=$rate " +
                        "WHERE user_id=$user AND version=$version;", ("$profile", prepared.profile),
                        ("$active", snapshot.document.activeRunId), ("$rate", prepared.rate), ("$version", snapshot.version));
                    if (changed == 0) { AuthorityTelemetry.Emit(telemetry, "cas-retry"); continue; } // Dispose rolls back before retry.
                    foreach (var run in prepared.runs)
                        ExecuteSql(connection, transaction,
                            "INSERT INTO runs(user_id,run_id,ticket_json,commands_json) VALUES($user,$run,$ticket,NULL);",
                            ("$run", run.id), ("$ticket", run.payload));
                    foreach (var submission in prepared.submissions)
                    {
                        ExecuteSql(connection, transaction,
                            "UPDATE runs SET commands_json=$commands WHERE user_id=$user AND run_id=$run;",
                            ("$commands", submission.commands), ("$run", submission.id));
                        ExecuteSql(connection, transaction,
                            "INSERT INTO submissions(user_id,run_id,receipt_id,receipt_json) VALUES($user,$run,$receipt,$json);",
                            ("$run", submission.id), ("$receipt", submission.receiptId), ("$json", submission.payload));
                    }
                    foreach (var receipt in prepared.operations)
                        ExecuteSql(connection, transaction,
                            "INSERT INTO operations(user_id,request_id,fingerprint,reply_json) VALUES($user,$id,$fingerprint,$reply);",
                            ("$id", receipt.id), ("$fingerprint", receipt.fingerprint), ("$reply", receipt.payload));
                    ct.ThrowIfCancellationRequested();
                    transaction.Commit();
                    return LoopJson.Copy(mutation.reply);
                }
                throw new IOException("Authority changed repeatedly; retry the original request.");
            }
            catch (SqliteException error) { throw new IOException("SQLite authority is unavailable; data was not reset.", error); }
            catch (JsonException error) { throw new IOException("SQLite authority JSON is invalid; data was not reset.", error); }
        }

        void Initialize()
        {
            try
            {
                using var connection = Open();
                ExecuteSql(connection, null, "PRAGMA journal_mode=WAL;");
                using var transaction = BeginTransaction(connection);
                using var versionCommand = Command(connection, transaction, "PRAGMA user_version;");
                long version = (long)ExecuteScalar(versionCommand);
                if (version == 0)
                {
                    using var tables = Command(connection, transaction,
                        "SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';");
                    if ((long)ExecuteScalar(tables) != 0) throw new IOException("Unrecognized SQLite authority schema; data was not reset.");
                    ExecuteSql(connection, transaction, @"
CREATE TABLE profiles(user_id TEXT PRIMARY KEY, version INTEGER NOT NULL CHECK(version>=0),
 profile_json TEXT NOT NULL, active_run_id TEXT, rate_json TEXT NOT NULL);
CREATE TABLE runs(user_id TEXT NOT NULL, run_id TEXT NOT NULL, ticket_json TEXT NOT NULL, commands_json TEXT,
 PRIMARY KEY(user_id,run_id), FOREIGN KEY(user_id) REFERENCES profiles(user_id));
CREATE TABLE submissions(user_id TEXT NOT NULL, run_id TEXT NOT NULL, receipt_id TEXT NOT NULL, receipt_json TEXT NOT NULL,
 PRIMARY KEY(user_id,run_id), UNIQUE(user_id,receipt_id), FOREIGN KEY(user_id,run_id) REFERENCES runs(user_id,run_id));
CREATE TABLE operations(user_id TEXT NOT NULL, request_id TEXT NOT NULL, fingerprint TEXT NOT NULL, reply_json TEXT NOT NULL,
 PRIMARY KEY(user_id,request_id), FOREIGN KEY(user_id) REFERENCES profiles(user_id));
PRAGMA user_version=1;");
                }
                else if (version != 1) throw new IOException("Unsupported SQLite authority schema; data was not reset.");
                var fresh = new MockAuthorityDocument(); fresh.profile.userId = userId;
                ExecuteSql(connection, transaction,
                    "INSERT OR IGNORE INTO profiles(user_id,version,profile_json,active_run_id,rate_json) VALUES($user,0,$profile,NULL,$rate);",
                    ("$profile", LoopJson.Encode(fresh.profile)), ("$rate", LoopJson.Encode(new RateState())));
                transaction.Commit();
            }
            catch (SqliteException error) { throw new IOException("SQLite authority initialization failed; data was not reset.", error); }
        }

        (long version, MockAuthorityDocument document) ReadSnapshot()
        {
            using var connection = Open();
            using var transaction = BeginTransaction(connection, deferred: true);
            var document = new MockAuthorityDocument();
            long version;
            using (var command = Command(connection, transaction,
                "SELECT version,profile_json,active_run_id,rate_json FROM profiles WHERE user_id=$user;"))
            using (var reader = ExecuteReader(command))
            {
                if (!reader.Read()) throw new IOException("Authority profile is missing; initial grant was not recreated.");
                version = reader.GetInt64(0); document.profile = LoopJson.Decode<BackendProfile>(reader.GetString(1));
                document.activeRunId = reader.IsDBNull(2) ? null : reader.GetString(2);
                var rate = LoopJson.Decode<RateState>(reader.GetString(3)) ?? throw new IOException("Missing authority rate state");
                document.rateWindowOpen = rate.open; document.rateWindowStartMs = rate.start;
                document.startsInWindow = rate.starts; document.submitsInWindow = rate.submits; document.abandonsInWindow = rate.abandons;
            }
            using (var command = Command(connection, transaction,
                "SELECT r.ticket_json,r.commands_json,s.receipt_json FROM runs r LEFT JOIN submissions s " +
                "ON s.user_id=r.user_id AND s.run_id=r.run_id WHERE r.user_id=$user ORDER BY r.rowid;"))
            using (var reader = ExecuteReader(command))
                while (reader.Read()) document.runs.Add(new MockRunRecord { owner = userId,
                    ticket = LoopJson.Decode<RunTicket>(reader.GetString(0)),
                    commands = reader.IsDBNull(1) ? null : LoopJson.Decode<List<RunCommandEntry>>(reader.GetString(1)),
                    submission = reader.IsDBNull(2) ? null : LoopJson.Decode<SubmissionReceipt>(reader.GetString(2)) });
            using (var command = Command(connection, transaction,
                "SELECT request_id,fingerprint,reply_json FROM operations WHERE user_id=$user ORDER BY rowid;"))
            using (var reader = ExecuteReader(command))
                while (reader.Read()) document.operations.Add(new MockOperationRecord { id = reader.GetString(0),
                    fingerprint = reader.GetString(1), reply = LoopJson.Decode<BackendReply>(reader.GetString(2)) });
            transaction.Commit();
            try { AuthorityValidation.Validate(document, userId); }
            catch (Exception error) when (error is ArgumentException || error is OverflowException || error is NullReferenceException)
            { throw new IOException("SQLite authority content is invalid; data was not reset.", error); }
            return (version, document);
        }

        static Prepared Prepare(MockAuthorityDocument before, MockAuthorityDocument after)
        {
            var result = new Prepared { profile = LoopJson.Encode(after.profile), rate = LoopJson.Encode(new RateState {
                open = after.rateWindowOpen, start = after.rateWindowStartMs, starts = after.startsInWindow,
                submits = after.submitsInWindow, abandons = after.abandonsInWindow }) };
            var oldRuns = before.runs.ToDictionary(r => r.ticket.serverRunId, StringComparer.Ordinal);
            var oldOperations = before.operations.ToDictionary(o => o.id, StringComparer.Ordinal);
            foreach (var run in after.runs)
            {
                string id = run.ticket.serverRunId;
                if (!oldRuns.Remove(id, out var previous)) result.runs.Add(new Row { id = id, payload = LoopJson.Encode(run.ticket) });
                else
                {
                    if (LoopJson.Encode(previous.ticket) != LoopJson.Encode(run.ticket)) throw new InvalidOperationException("Authority ticket cannot change");
                    if (previous.submission != null)
                    {
                        if (LoopJson.Encode(previous) != LoopJson.Encode(run)) throw new InvalidOperationException("Authority receipt cannot change");
                        continue;
                    }
                }
                if (run.submission != null) result.submissions.Add(new Row { id = id, receiptId = run.submission.receiptId,
                    payload = LoopJson.Encode(run.submission), commands = LoopJson.Encode(run.commands) });
            }
            foreach (var operation in after.operations)
            {
                if (oldOperations.Remove(operation.id, out var previous))
                {
                    if (LoopJson.Encode(previous) != LoopJson.Encode(operation)) throw new InvalidOperationException("Authority operation cannot change");
                }
                else result.operations.Add(new Row { id = operation.id, fingerprint = operation.fingerprint, payload = LoopJson.Encode(operation.reply) });
            }
            if (oldRuns.Count != 0 || oldOperations.Count != 0) throw new InvalidOperationException("Authority history cannot be removed");
            return result;
        }

        SqliteConnection Open()
        {
            var connection = new SqliteConnection(connectionString);
            try { connection.Open(); ExecuteSql(connection, null, "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=10000; PRAGMA synchronous=FULL;"); return connection; }
            catch { connection.Dispose(); throw; }
        }
        SqliteCommand Command(SqliteConnection connection, MeasuredTransaction transaction, string sql,
            params (string name, object value)[] parameters)
        {
            var command = connection.CreateCommand(); command.Transaction = transaction?.Value; command.CommandText = sql;
            command.Parameters.AddWithValue("$user", userId);
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.name, parameter.value ?? DBNull.Value);
            return command;
        }
        int ExecuteSql(SqliteConnection connection, MeasuredTransaction transaction, string sql,
            params (string name, object value)[] parameters)
        {
            using var command = Command(connection, transaction, sql, parameters);
            var timer = Stopwatch.StartNew();
            try { int rows = command.ExecuteNonQuery(); AuthorityTelemetry.Emit(telemetry, "sql-rows", Math.Max(0, rows)); return rows; }
            catch (SqliteException error) { Busy(error); throw; }
            finally { AuthorityTelemetry.Emit(telemetry, Kind(sql), 1, timer.Elapsed.TotalMilliseconds); }
        }
        object ExecuteScalar(SqliteCommand command)
        {
            var timer = Stopwatch.StartNew();
            try { var value = command.ExecuteScalar(); AuthorityTelemetry.Emit(telemetry, "sql-rows", value == null ? 0 : 1); return value; }
            catch (SqliteException error) { Busy(error); throw; }
            finally { AuthorityTelemetry.Emit(telemetry, Kind(command.CommandText), 1, timer.Elapsed.TotalMilliseconds); }
        }
        MeasuredReader ExecuteReader(SqliteCommand command)
        {
            var timer = Stopwatch.StartNew();
            try { return new MeasuredReader(command.ExecuteReader(), telemetry, Kind(command.CommandText)); }
            catch (SqliteException error) { Busy(error); throw; }
            finally { AuthorityTelemetry.Emit(telemetry, Kind(command.CommandText), 1, timer.Elapsed.TotalMilliseconds); }
        }
        MeasuredTransaction BeginTransaction(SqliteConnection connection, bool deferred = false)
        {
            var timer = Stopwatch.StartNew();
            try { return new MeasuredTransaction(connection.BeginTransaction(deferred), telemetry); }
            catch (SqliteException error) { Busy(error); throw; }
            finally { AuthorityTelemetry.Emit(telemetry, "transaction-begin", 1, timer.Elapsed.TotalMilliseconds); }
        }
        void Busy(SqliteException error) { if (error.SqliteErrorCode == 5 || error.SqliteErrorCode == 6) AuthorityTelemetry.Emit(telemetry, "sql-busy"); }
        static string Kind(string sql)
        {
            string prefix = sql.TrimStart();
            if (prefix.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) return "sql-select";
            if (prefix.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)) return "sql-insert";
            if (prefix.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)) return "sql-update";
            if (prefix.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)) return "sql-delete";
            return "sql-other";
        }
        sealed class MeasuredTransaction : IDisposable
        {
            public readonly SqliteTransaction Value;
            readonly IAuthorityTelemetry telemetry;
            bool committed, disposed;
            public MeasuredTransaction(SqliteTransaction value, IAuthorityTelemetry telemetry) { Value = value; this.telemetry = telemetry; }
            public void Commit()
            {
                var timer = Stopwatch.StartNew();
                try { Value.Commit(); committed = true; }
                catch (SqliteException error) { if (error.SqliteErrorCode == 5 || error.SqliteErrorCode == 6) AuthorityTelemetry.Emit(telemetry, "sql-busy"); throw; }
                finally { AuthorityTelemetry.Emit(telemetry, "transaction-commit", 1, timer.Elapsed.TotalMilliseconds); }
            }
            public void Dispose()
            {
                if (disposed) return; disposed = true;
                var timer = Stopwatch.StartNew();
                try { Value.Dispose(); }
                finally { if (!committed) AuthorityTelemetry.Emit(telemetry, "transaction-rollback", 1, timer.Elapsed.TotalMilliseconds); }
            }
        }
        sealed class MeasuredReader : IDisposable
        {
            readonly SqliteDataReader value;
            readonly IAuthorityTelemetry telemetry;
            readonly string kind;
            public MeasuredReader(SqliteDataReader value, IAuthorityTelemetry telemetry, string kind) { this.value = value; this.telemetry = telemetry; this.kind = kind; }
            public bool Read()
            {
                var timer = Stopwatch.StartNew();
                try { bool found = value.Read(); if (found) AuthorityTelemetry.Emit(telemetry, "sql-rows"); return found; }
                catch (SqliteException error) { if (error.SqliteErrorCode == 5 || error.SqliteErrorCode == 6) AuthorityTelemetry.Emit(telemetry, "sql-busy"); throw; }
                finally { AuthorityTelemetry.Emit(telemetry, kind, 0, timer.Elapsed.TotalMilliseconds); }
            }
            public long GetInt64(int index) => value.GetInt64(index);
            public string GetString(int index) => value.GetString(index);
            public bool IsDBNull(int index) => value.IsDBNull(index);
            public void Dispose() => value.Dispose();
        }

        sealed class RateState { public bool open; public long start; public int starts, submits, abandons; }
        sealed class Row { public string id, payload, receiptId, commands, fingerprint; }
        sealed class Prepared
        {
            public string profile, rate;
            public readonly List<Row> runs = new List<Row>(), submissions = new List<Row>(), operations = new List<Row>();
        }
    }
}
