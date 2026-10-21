using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class LoopStoreConcurrencyTests
    {
        static readonly TimeSpan Timeout=TimeSpan.FromSeconds(10);
        string directory;
        string LocalPath => Path.Combine(directory,"checkpoint.json");
        [SetUp] public void Setup() => directory=Path.Combine(Path.GetTempPath(),"DiceWarriorStoreSafety",Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup()
        {
            string allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"DiceWarriorStoreSafety"))+Path.DirectorySeparatorChar;
            if(directory!=null && Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                Directory.Delete(directory,true);
        }
        static Exception Capture(Action action)
        {
            try { action(); return null; }
            catch(Exception exception) { return exception; }
        }
        static Exception[] RunTogether(params Action[] actions)
        {
            using(var cancel=new CancellationTokenSource())
            using(var ready=new Barrier(actions.Length+1))
            {
                var tasks=actions.Select(action=>Task.Run(()=>Capture(()=> {
                    if(!ready.SignalAndWait(Timeout,cancel.Token)) throw new TimeoutException("Worker start barrier timed out");
                    action();
                }))).ToArray();
                try
                {
                    Assert.That(ready.SignalAndWait(Timeout,cancel.Token),Is.True,"Main start barrier timed out");
                    Assert.That(Task.WaitAll(tasks,Timeout),Is.True,"Concurrent storage work timed out");
                    return tasks.Select(task=>task.Result).ToArray();
                }
                finally
                {
                    // Also release workers when a main-thread assertion fails before all participants arrive.
                    cancel.Cancel();
                    Assert.That(Task.WaitAll(tasks,Timeout),Is.True,"Storage workers must stop before fixture cleanup");
                }
            }
        }
        static PlayerDocument Next(PlayerDocument current,int currency)
        {
            var candidate=LoopJson.Copy(current);
            candidate.revision++; candidate.profile.currency=currency;
            return candidate;
        }
        static Command Pending(string id) => new Command {id=id,kind="loadout",arg="die:arcane"};

        [Test] public void SameRevisionConcurrentCommitsHaveOneWinnerAndReloadCanContinue()
        {
            var first=new LoopStore(LocalPath); var second=new LoopStore(LocalPath);
            var initial=first.Read(); var a=Next(initial,10); var b=Next(initial,20);
            var failures=RunTogether(()=>first.Commit(a,0,0),()=>second.Commit(b,0,0));
            Assert.That(failures.Count(error=>error==null),Is.EqualTo(1));
            Assert.That(failures.Count(error=>error is InvalidOperationException),Is.EqualTo(1));
            var winner=new LoopStore(LocalPath).Read();
            Assert.That(winner.revision,Is.EqualTo(1));
            Assert.That(new[]{10,20},Does.Contain(winner.profile.currency));
            int loser=failures[0]!=null?0:1;
            var reloaded=(loser==0?first:second).Read();
            (loser==0?first:second).Commit(Next(reloaded,reloaded.profile.currency+1),reloaded.revision,reloaded.generation);
            var final=new LoopStore(LocalPath).Read();
            Assert.That(final.revision,Is.EqualTo(2));
            Assert.That(final.profile.currency,Is.EqualTo(winner.profile.currency+1));
        }

        [Test] public void BackupRestoreAdvancesGenerationAndRejectsSameRevisionOldCandidate()
        {
            var disk=new LoopStore(LocalPath); var other=new LoopStore(LocalPath);
            disk.Commit(Next(disk.Read(),10),0,0);
            disk.Commit(Next(disk.Read(),20),1,0);
            byte[] before=File.ReadAllBytes(LocalPath);
            var stale=Next(other.Read(),99);
            Assert.That(other.InspectBackup().profile.currency,Is.EqualTo(10));
            var restored=disk.RestoreBackup();
            Assert.That(restored.revision,Is.EqualTo(2),"Equal revision alone must not authorize an old generation");
            Assert.That(restored.generation,Is.EqualTo(1));
            Assert.That(File.ReadAllBytes(Directory.GetFiles(directory,"*.damaged-*").Single()),Is.EqualTo(before));
            Assert.Throws<InvalidOperationException>(()=>other.Commit(stale,2,0));
            var current=other.Read();
            other.Commit(Next(current,11),current.revision,current.generation);
            Assert.That(disk.Read().profile.currency,Is.EqualTo(11));
            Assert.That(other.RestoreBackup().generation,Is.EqualTo(2));
            Assert.That(Directory.GetFiles(directory,"*.tmp"),Is.Empty);
        }

        [Test] public void LateClearAndDiscardCannotRemoveANewerPendingRequest()
        {
            var first=new LoopStore(LocalPath); var second=new LoopStore(LocalPath);
            first.WritePending(Pending("old-request"));
            Exception lateClear=null,lateDiscard=null;
            using(var newerReady=new ManualResetEventSlim(false))
            {
                var failures=RunTogether(()=> {
                    try
                    {
                        first.ClearPending("old-request");
                        first.WritePending(Pending("new-request"));
                    }
                    finally { newerReady.Set(); }
                },()=> {
                    if(!newerReady.Wait(Timeout)) throw new TimeoutException("New pending request was not published");
                    lateClear=Capture(()=>second.ClearPending("old-request"));
                    lateDiscard=Capture(()=>second.DiscardPending("old-request"));
                });
                Assert.That(failures,Is.All.Null);
            }
            Assert.That(lateClear,Is.TypeOf<IOException>());
            Assert.That(lateDiscard,Is.TypeOf<IOException>());
            Assert.That(second.ReadPending().id,Is.EqualTo("new-request"));
            Assert.That(Directory.GetFiles(directory,"*.pending-discarded-*"),Is.Empty);
            second.DiscardPending("new-request");
            Assert.That(first.ReadPending(),Is.Null);
            Assert.That(LoopJson.Decode<Command>(File.ReadAllText(Directory.GetFiles(directory,"*.pending-discarded-*").Single())).id,
                Is.EqualTo("new-request"));
        }

        [Test] public void ConcurrentIdenticalPendingWritesAreIdempotentAndChangedBodyIsRejected()
        {
            var first=new LoopStore(LocalPath); var second=new LoopStore(LocalPath);
            var command=Pending("shared-request");
            var copy=LoopJson.Copy(command);
            Assert.That(RunTogether(()=>first.WritePending(command),()=>second.WritePending(copy)),Is.All.Null);
            byte[] original=File.ReadAllBytes(LocalPath+".pending");
            Assert.That(first.ReadPending().id,Is.EqualTo(command.id));
            copy.arg="die:steel";
            Assert.Throws<IOException>(()=>second.WritePending(copy));
            Assert.That(File.ReadAllBytes(LocalPath+".pending"),Is.EqualTo(original));
            Assert.That(Directory.GetFiles(directory,"*.tmp"),Is.Empty);
        }

        [Test] public void PreservedCheckpointIsUniqueAndRetainsTheExactRawBytes()
        {
            var disk=new LoopStore(LocalPath);
            Assert.That(disk.PreserveCheckpoint(),Is.Null);
            Directory.CreateDirectory(directory);
            byte[] original={0xef,0xbb,0xbf,0x20,0x7b,0xff,0x00,0x0d,0x0a};
            File.WriteAllBytes(LocalPath,original);
            string first=disk.PreserveCheckpoint();
            string second=new LoopStore(LocalPath).PreserveCheckpoint();
            Assert.That(first,Does.StartWith(LocalPath+".reconciled-"));
            Assert.That(first,Does.EndWith(".json")); Assert.That(first,Is.Not.EqualTo(second));
            Assert.That(File.ReadAllBytes(first),Is.EqualTo(original));
            Assert.That(File.ReadAllBytes(second),Is.EqualTo(original));
            Assert.That(File.ReadAllBytes(LocalPath),Is.EqualTo(original));
            Assert.That(Directory.GetFiles(directory,"*.tmp"),Is.Empty);
        }

        sealed class FailureRecord
        {
            public Command command;
            public BackendReply reply;
            public string diagnostic;
        }
        [Test] public void BackendFailureKeepsTheOriginalRequestReplyAndExceptionDiagnostic()
        {
            var disk=new LoopStore(LocalPath); var command=Pending("failed-request");
            var reply=new BackendReply {status=BackendStatus.UnknownAfterTimeout,code="TRANSPORT_UNKNOWN",message="결과 확인 중"};
            string diagnostic="System.IO.IOException: transport disconnected\r\n   at Transport.Send()\r\n원문 보존";
            disk.WritePending(command); disk.RecordBackendFailure(command,reply,diagnostic);
            var record=LoopJson.Decode<FailureRecord>(File.ReadAllText(Directory.GetFiles(directory,"*.backend-error-*.json").Single()));
            Assert.That(LoopJson.Encode(record.command),Is.EqualTo(LoopJson.Encode(command)));
            Assert.That(LoopJson.Encode(record.reply),Is.EqualTo(LoopJson.Encode(reply)));
            Assert.That(record.diagnostic,Is.EqualTo(diagnostic));
            Assert.That(disk.ReadPending().id,Is.EqualTo(command.id));
        }
    }
}
