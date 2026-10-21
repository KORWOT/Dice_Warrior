using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class SqliteAuthorityTests
    {
        string directory, path;
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "DiceWarriorSqliteTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "authority.sqlite");
        }
        [TearDown] public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        [Test] public async Task UpgradeSurvivesRepositoryReopenWithoutGrantingInitialCurrencyAgain()
        {
            var first = Backend("alice");
            var initial = await first.GetProfileAsync(CancellationToken.None);
            Assert.That(initial.status, Is.EqualTo(BackendStatus.Approved));
            Assert.That(initial.profile.userId, Is.EqualTo("alice"));
            Assert.That(initial.profile.values.currency, Is.EqualTo(20));
            var request = new BackendRequest { requestId = "buy-1", kind = "grow", expectedRevision = 0 };
            var bought = await first.UpgradeAsync(request, CancellationToken.None);
            Assert.That(bought.status, Is.EqualTo(BackendStatus.Approved));
            var reopened = Backend("alice");
            var duplicate = await reopened.UpgradeAsync(request, CancellationToken.None);
            Assert.That(LoopJson.Encode(duplicate), Is.EqualTo(LoopJson.Encode(bought)));
            var current = await reopened.GetProfileAsync(CancellationToken.None);
            Assert.That(current.profile.values.currency, Is.Zero);
            Assert.That(current.profile.values.growth, Is.EqualTo(1));
            Assert.That(current.profile.revision, Is.EqualTo(1));
        }
        [Test] public async Task SeparateUsersCanReuseRequestIdsWithoutSharingProfileRunsOrReceipts()
        {
            var alice = Backend("alice"); var bob = Backend("bob");
            var request = new BackendRequest { requestId = "same-id", kind = "grow" };
            Assert.That((await alice.UpgradeAsync(request, default)).status, Is.EqualTo(BackendStatus.Approved));
            Assert.That((await bob.GetProfileAsync(default)).profile.values.currency, Is.EqualTo(20));
            Assert.That((await bob.GetOperationStatusAsync("same-id", default)).code, Is.EqualTo("OPERATION_NOT_FOUND"));
            Assert.That((await bob.UpgradeAsync(request, default)).status, Is.EqualTo(BackendStatus.Approved));
            var run = await alice.StartRunAsync(new BackendRequest { requestId = "alice-start", kind = "start", expectedRevision = 1 }, default);
            Assert.That((await bob.GetRunStatusAsync(run.run.serverRunId, default)).code, Is.EqualTo("RUN_NOT_FOUND"));
            var changed = LoopJson.Copy(request); changed.expectedRevision = 1;
            Assert.That((await Backend("alice").UpgradeAsync(changed, default)).code, Is.EqualTo("REQUEST_ID_REUSED"));
        }
        [Test] public async Task TenPurchasesAtOneRevisionOnlySpendOnceAcrossConnections()
        {
            var backends = Enumerable.Range(0, 10).Select(_ => Backend("alice")).ToArray();
            var replies = await Task.WhenAll(backends.Select((backend, i) => Task.Run(() => backend.UpgradeAsync(
                new BackendRequest { requestId = "buy-" + i, kind = "grow", expectedRevision = 0 }, default))));
            Assert.That(replies.Count(r => r.status == BackendStatus.Approved), Is.EqualTo(1));
            Assert.That(replies.Count(r => r.code == "REVISION_CONFLICT"), Is.EqualTo(9));
            var profile = (await Backend("alice").GetProfileAsync(default)).profile;
            Assert.That(profile.values.currency, Is.Zero); Assert.That(profile.values.growth, Is.EqualTo(1));
            Assert.That(profile.revision, Is.EqualTo(1));
        }
        [Test] public async Task ABlockedReplayDoesNotHoldWriterLockAndRetriesAfterAnotherConnectionCommits()
        {
            var repository = new SqliteAuthorityRepository(path, "alice");
            var other = Backend("alice");
            using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
            int calls = 0;
            var slow = Task.Run(() => repository.Execute(document => {
                if (Interlocked.Increment(ref calls) == 1) { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException(); }
                document.profile.values.currency++; document.profile.revision++;
                return new MockMutation { changed = true, reply = new BackendReply { status = BackendStatus.Approved, profile = document.profile } };
            }, default));
            try
            {
                Assert.That(entered.Wait(TimeSpan.FromSeconds(10)), Is.True);
                var purchase = Task.Run(() => other.UpgradeAsync(new BackendRequest { requestId = "parallel", kind = "grow" }, default));
                Assert.That(await Task.WhenAny(purchase, Task.Delay(5000)), Is.SameAs(purchase), "Replay must not hold SQLite's writer lock");
                Assert.That((await purchase).status, Is.EqualTo(BackendStatus.Approved));
            }
            finally { release.Set(); await slow; }
            Assert.That(calls, Is.EqualTo(2));
            var profile = (await other.GetProfileAsync(default)).profile;
            Assert.That(profile.values.currency, Is.EqualTo(1)); Assert.That(profile.values.growth, Is.EqualTo(1));
            Assert.That(profile.revision, Is.EqualTo(2));
        }
        [Test] public async Task OneHundredNewSubmissionIdsOnlyPayTheVictoryOnceAndReceiptSurvivesReopen()
        {
            var backend = Backend("alice");
            var start = await backend.StartRunAsync(new BackendRequest { requestId = "start", kind = "start" }, default);
            var commands = GoldenReplayTests.Read().Single(v => v.name == "victory").commands;
            var request = new BackendRequest { kind = "settle", expectedRevision = 1,
                serverRunId = start.run.serverRunId, commands = commands };
            var connections = Enumerable.Range(0, 100).Select(_ => Backend("alice")).ToArray();
            var replies = await Task.WhenAll(connections.Select((connection, i) => Task.Run(() => {
                var copy = LoopJson.Copy(request); copy.requestId = "submit-" + i;
                return connection.SubmitRunAsync(copy, default);
            })));
            Assert.That(replies.All(r => r.status == BackendStatus.Approved), Is.True,
                string.Join(",", replies.Where(r => r.status != BackendStatus.Approved).Select(r => r.code)));
            Assert.That(replies.Select(r => r.submission.receiptId).Distinct().Count(), Is.EqualTo(1));
            var reopened = Backend("alice"); var current = await reopened.GetProfileAsync(default);
            Assert.That(current.profile.values.currency, Is.EqualTo(70)); Assert.That(current.profile.revision, Is.EqualTo(2));
            var status = await reopened.GetRunStatusAsync(start.run.serverRunId, default);
            Assert.That(status.submission.appliedAmount, Is.EqualTo(50)); Assert.That(status.commands.Count, Is.EqualTo(commands.Count));
            using var sql = Open();
            Assert.That(Scalar(sql, "SELECT count(*) FROM submissions;"), Is.EqualTo(1));
            Assert.That(Scalar(sql, "SELECT count(*) FROM operations;"), Is.EqualTo(101));
            using var duplicate = sql.CreateCommand();
            duplicate.CommandText = "INSERT INTO submissions SELECT * FROM submissions;";
            Assert.Throws<SqliteException>(() => duplicate.ExecuteNonQuery());
            duplicate.CommandText = "INSERT INTO operations SELECT * FROM operations;";
            Assert.Throws<SqliteException>(() => duplicate.ExecuteNonQuery());
        }
        [Test] public async Task FailedReceiptInsertRollsBackProfileAndCanRetryOriginalRequestAfterRestart()
        {
            var backend = Backend("alice");
            var start = await backend.StartRunAsync(new BackendRequest { requestId = "start", kind = "start" }, default);
            var request = new BackendRequest { requestId = "settle", kind = "settle", expectedRevision = 1,
                serverRunId = start.run.serverRunId, commands = GoldenReplayTests.Read().Single(v => v.name == "victory").commands };
            using (var sql = Open())
            using (var command = sql.CreateCommand())
            {
                command.CommandText = "CREATE TRIGGER fail_receipt BEFORE INSERT ON operations WHEN NEW.request_id='settle' BEGIN SELECT RAISE(ABORT,'injected'); END;";
                command.ExecuteNonQuery();
            }
            var failed = await backend.SubmitRunAsync(request, default);
            Assert.That(failed.status, Is.EqualTo(BackendStatus.TemporaryFailure));
            var unchanged = await backend.GetProfileAsync(default);
            Assert.That(unchanged.profile.values.currency, Is.EqualTo(20)); Assert.That(unchanged.profile.revision, Is.EqualTo(1));
            Assert.That(unchanged.run.serverRunId, Is.EqualTo(start.run.serverRunId));
            Assert.That((await backend.GetOperationStatusAsync("settle", default)).code, Is.EqualTo("OPERATION_NOT_FOUND"));
            using (var sql = Open())
            using (var command = sql.CreateCommand()) { command.CommandText = "DROP TRIGGER fail_receipt;"; command.ExecuteNonQuery(); }
            var recovered = await Backend("alice").SubmitRunAsync(request, default);
            Assert.That(recovered.status, Is.EqualTo(BackendStatus.Approved)); Assert.That(recovered.profile.values.currency, Is.EqualTo(70));
            Assert.That((await Backend("alice").SubmitRunAsync(request, default)).submission.receiptId, Is.EqualTo(recovered.submission.receiptId));
        }
        [Test] public async Task MalformedCommandLogIsRecordedAsRejectedWithoutPayingOrClosingRun()
        {
            var backend = Backend("alice");
            var start = await backend.StartRunAsync(new BackendRequest { requestId = "start", kind = "start" }, default);
            var commands = GoldenReplayTests.Read().Single(v => v.name == "victory").commands;
            commands[0].arg = "6";
            var rejected = await backend.SubmitRunAsync(new BackendRequest { requestId = "bad", kind = "settle", expectedRevision = 1,
                serverRunId = start.run.serverRunId, commands = commands }, default);
            Assert.That(rejected.status, Is.EqualTo(BackendStatus.Rejected)); Assert.That(rejected.code, Is.EqualTo("REPLAY_COMMAND_INVALID"));
            var current = await Backend("alice").GetProfileAsync(default);
            Assert.That(current.profile.values.currency, Is.EqualTo(20)); Assert.That(current.profile.revision, Is.EqualTo(1));
            Assert.That(current.run.serverRunId, Is.EqualTo(start.run.serverRunId));
        }
        [Test] public void CancellationBeforeCommitLeavesOriginalBalanceAndCorruptDatabaseIsNotReset()
        {
            var repository = new SqliteAuthorityRepository(path, "alice");
            using var cancellation = new CancellationTokenSource();
            Assert.Throws<OperationCanceledException>(() => repository.Execute(document => {
                document.profile.values.currency = 999; cancellation.Cancel();
                return new MockMutation { changed = true, reply = new BackendReply { profile = document.profile } };
            }, cancellation.Token));
            var unchanged = repository.Execute(d => new MockMutation { reply = new BackendReply { profile = d.profile } }, default);
            Assert.That(unchanged.profile.values.currency, Is.EqualTo(20));
            File.WriteAllText(path, "broken database bytes");
            byte[] original = File.ReadAllBytes(path);
            Assert.Throws<IOException>(() => new SqliteAuthorityRepository(path, "alice"));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
        }
        SqliteConnection Open()
        { var sql = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()); sql.Open(); return sql; }
        static long Scalar(SqliteConnection connection, string sql)
        { using var command = connection.CreateCommand(); command.CommandText = sql; return (long)command.ExecuteScalar(); }
        MockGameBackend Backend(string user) => new MockGameBackend(new SqliteAuthorityRepository(path, user), () => 33);
    }
}
