#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DiceWarrior.SymbolLoop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DiceWarrior.SharedWorld.Tests
{
    // Run these names individually against the explicitly owned Q1 server.
    [Category("Q1Http"), Explicit("Run each exact Q1 test name with an owned isolated HTTP server; Prepare/Resume require a coordinator restart.")]
    public sealed class CompetitionHttpFlowTests
    {
        CompetitionHttpFixture f;
        [UnityTearDown] public IEnumerator Cleanup() { f?.Dispose(); f=null; yield return null; }

        [UnityTest] public IEnumerator Q1_HttpJourneyPreservesFrozenMatchAndPaysNothing()
        {
            f=new CompetitionHttpFixture("journey");
            var metricsBefore=f.Json("/dev/metrics"); yield return CompetitionHttpFixture.Await(metricsBefore);
            yield return f.Open(true); // Exercise the public HTTP entry with authored JSON/config and isolated root.
            Assert.That(f.App.Account.Ready,Is.True); Assert.That(f.App.Account.Profile.currency,Is.EqualTo(20));
            var initial=f.App.Account.Session.Snapshot.authority;
            yield return f.Open(); // Same endpoint/account/path; explicit seed only affects the local v4 match.
            yield return f.Press("출전 선택"); yield return f.Press("비전 주사위");
            yield return f.Press("출전 선택"); yield return f.Press("불꽃 지팡이");
            foreach(string id in new[]{"wanderer","vanguard","scholar"})
            {
                string previous=f.App.Session.Snapshot.state==null?null:LoopJson.Encode(f.App.Session.Snapshot);
                string raw=File.Exists(f.CompetitionPath)?File.ReadAllText(f.CompetitionPath):null;
                if(!f.App.InLobby) yield return f.Press("모험 종료");
                yield return f.SelectAdventurer(id);
                if(previous!=null)
                {
                    yield return f.Press("경기 이어하기");
                    Assert.That(LoopJson.Encode(f.App.Session.Snapshot),Is.EqualTo(previous),"A new preference cannot alter the current match.");
                    yield return f.Press("모험 종료"); yield return f.Press("새 게임");
                    f.Capture("restart-warning"); yield return f.Press("취소");
                    Assert.That(File.ReadAllText(f.CompetitionPath),Is.EqualTo(raw));
                }
                yield return f.Press("새 게임");
                if(previous!=null) yield return f.Press("새 게임 시작 확정");
                var state=f.App.Session.Snapshot.state;
                Assert.That(state.definition.rulesVersion,Is.EqualTo(CompetitionV4Content.Version));
                Assert.That(state.participants[0].adventurerId,Is.EqualTo(id));
                Assert.That(state.startingProfile.startingDie,Is.EqualTo("arcane"));
                Assert.That(state.startingProfile.startingWeapon,Is.EqualTo("wand"));
                Assert.That(state.participants.All(p=>p.v4.accountHp==0),Is.True);
                if(raw!=null) Assert.That(Directory.GetFiles(Path.GetDirectoryName(f.CompetitionPath),"checkpoint.json.restarted-*")
                    .Select(File.ReadAllText),Does.Contain(raw));
            }
            Assert.That(f.App.Session.Snapshot.state.definition.world.places.Length,Is.EqualTo(12));
            bool resumedCombat=false,sawAi=false,sawOffer=false;
            var types=new HashSet<DamageType>(); int actions=0;
            while(!f.App.Session.Snapshot.state.finished && actions++<300)
            {
                var before=f.App.Session.Snapshot; var human=before.state.participants[0];
                if(human.phase==CompetitionPhase.Encounter)
                {
                    sawOffer=true; Assert.That(human.offer.choices.Length,Is.InRange(2,3));
                    if(human.offer.choices.Any(c=>c.kind==EncounterKind.Combat))
                    { Assert.That(f.Screen.detail.text,Does.Contain("약점")); f.Capture("encounter-affinity"); }
                }
                if(human.phase==CompetitionPhase.Combat && !resumedCombat)
                {
                    string saved=LoopJson.Encode(before),raw=File.ReadAllText(f.CompetitionPath);
                    Assert.That(f.Screen.detail.text,Does.Contain("약점")); f.Capture("combat-before-resume");
                    yield return f.Press("모험 종료"); yield return f.Open(); yield return f.Press("경기 이어하기");
                    Assert.That(LoopJson.Encode(f.App.Session.Snapshot),Is.EqualTo(saved));
                    Assert.That(File.ReadAllText(f.CompetitionPath),Is.EqualTo(raw)); resumedCombat=true;
                    yield return f.Step(true); f.Capture("combat-after-roll");
                }
                else yield return f.Step(actions==1);
                var after=f.App.Session.Snapshot.state;
                sawAi|=after.lastRound.Count>0;
                foreach(var p in after.participants)
                    if(p.v4.lastTurn!=null) foreach(var result in p.v4.lastTurn.skills.Where(s=>s.status=="applied" && s.damageBeforeShield>0)) types.Add(result.damageType);
            }
            Assert.That(f.App.Session.Snapshot.state.finished,Is.True,"The unchanged v4 rules must reach a natural terminal condition.");
            Assert.That(resumedCombat && sawAi && sawOffer,Is.True);
            Assert.That(f.Screen.detail.text,Does.Contain("영구 재화 지급 0")); f.Capture("natural-terminal");
            var profile=f.Backend.GetProfileAsync(CancellationToken.None); yield return CompetitionHttpFixture.Await(profile);
            Assert.That(LoopJson.Encode(profile.Result.profile),Is.EqualTo(LoopJson.Encode(initial)));
            f.AssertNoGameWrites();
            var metricsAfter=f.Json("/dev/metrics"); yield return CompetitionHttpFixture.Await(metricsAfter);
            foreach(string kind in new[]{"start","settle","grow"})
                Assert.That(CompetitionHttpFixture.Requests(metricsAfter.Result,kind),Is.EqualTo(CompetitionHttpFixture.Requests(metricsBefore.Result,kind)),kind);
            Assert.That((bool)metricsAfter.Result["estimatedFirestore"]["isActualFirestoreUsage"],Is.False);
            Assert.That((long)metricsAfter.Result["overview"]["requests"],Is.GreaterThan((long)metricsBefore.Result["overview"]["requests"]));
            var cost=f.Cost(); yield return CompetitionHttpFixture.Await(cost);
            Assert.That((bool)cost.Result["isActualBilling"],Is.False);
            f.Write("journey-evidence.json",new {completed=true,actions,resumedCombat,sawAi,sawOffer,
                observedDamageTypes=types.Select(t=>t.ToString()).OrderBy(x=>x).ToArray(),serverProfileUnchanged=true,
                actualHttp=f.Wire.Summary(),firestoreIsHypothetical=true,costIsActualBilling=false,
                finalMatchHash=LoopJson.Hash(LoopJson.Encode(f.App.Session.Snapshot))});
            f.Write("metrics-after.json",metricsAfter.Result); f.Write("hypothetical-cost.json",cost.Result);
        }

        [UnityTest] public IEnumerator Q1_ProfileFaultsPreserveCheckpointAndRetry()
        {
            f=new CompetitionHttpFixture("profile-fault",timeoutSeconds:1); f.Wire.Arm("GET","/v1/profile","before-abort");
            yield return f.Open();
            Assert.That(f.App.Account.Ready,Is.False); Assert.That(f.Button("새 게임").button.interactable,Is.False);
            Assert.That(f.App.Session.Snapshot.state,Is.Null);
            yield return f.Press("계정 관리"); yield return f.Press("프로필 다시 확인"); yield return f.Press("로비로");
            Assert.That(f.App.Account.Ready,Is.True); yield return f.Press("새 게임");
            string saved=LoopJson.Encode(f.App.Session.Snapshot),raw=File.ReadAllText(f.CompetitionPath);
            yield return f.Press("모험 종료"); yield return f.Press("새 게임");
            f.Wire.Arm("GET","/v1/profile","after-delay"); yield return f.Press("새 게임 시작 확정");
            Assert.That(f.App.Account.Ready,Is.False); Assert.That(f.App.InLobby,Is.True);
            Assert.That(File.ReadAllText(f.CompetitionPath),Is.EqualTo(raw));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(f.CompetitionPath),"checkpoint.json.restarted-*"),Is.Empty);
            yield return f.Press("경기 이어하기"); Assert.That(LoopJson.Encode(f.App.Session.Snapshot),Is.EqualTo(saved));
            yield return f.Press("모험 종료"); yield return f.Press("계정 관리"); yield return f.Press("프로필 다시 확인");
            Assert.That(f.App.Account.Ready,Is.True); Assert.That(f.App.Account.Profile.currency,Is.EqualTo(20));
            f.AssertNoGameWrites(); f.Capture("profile-recovered");
            Assert.That(f.Wire.Records.Count(r=>r.fault!=null),Is.EqualTo(2));
            Assert.That(f.Wire.Records.Where(r=>r.fault!=null).All(r=>r.transportError),Is.True);
            f.Write("profile-fault-evidence.json",new {realSocketFaults=new[]{"before-abort","after-delay"},checkpointUnchanged=true,
                recovered=true,http=f.Wire.Summary(),http503="Separate existing actual HTTP storage-failure test; not simulated here"});
        }

        [UnityTest] public IEnumerator Q1_LegacyActiveRunBlocksGrowthWithoutSubmitting()
        {
            f=new CompetitionHttpFixture("legacy");
            var profile=f.Backend.GetProfileAsync(CancellationToken.None); yield return CompetitionHttpFixture.Await(profile);
            var start=f.Backend.StartRunAsync(new BackendRequest {requestId=Guid.NewGuid().ToString("N"),kind="start",expectedRevision=profile.Result.profile.revision},CancellationToken.None);
            yield return CompetitionHttpFixture.Await(start); Assert.That(start.Result.status,Is.EqualTo(BackendStatus.Approved));
            yield return f.Open(); Assert.That(f.App.Account.HasLegacyRun,Is.True);
            string original=File.ReadAllText(f.AccountPath),ticket=LoopJson.Encode(f.App.Account.Session.Snapshot.ticket);
            yield return f.Press("계정 관리"); Assert.That(f.Button("영구 성장").button.interactable,Is.False);
            var denied=f.App.Account.GrowAsync(CancellationToken.None);
            while(!denied.IsCompleted) yield return null;
            Assert.That(denied.IsFaulted,Is.True); Assert.That(denied.Exception.GetBaseException(),Is.TypeOf<InvalidOperationException>());
            f.Capture("legacy-growth-held"); yield return f.Press("로비로");
            yield return f.Press("출전 선택"); yield return f.Press("비전 주사위"); yield return f.Press("새 게임");
            Assert.That(f.App.Session.Snapshot.state.definition.rulesVersion,Is.EqualTo(CompetitionV4Content.Version));
            Assert.That(File.ReadAllText(f.AccountPath),Is.EqualTo(original));
            Assert.That(LoopJson.Encode(f.App.Account.Session.Snapshot.ticket),Is.EqualTo(ticket));
            var server=f.Backend.GetProfileAsync(CancellationToken.None); yield return CompetitionHttpFixture.Await(server);
            Assert.That(LoopJson.Encode(server.Result.profile),Is.EqualTo(LoopJson.Encode(start.Result.profile)));
            Assert.That(server.Result.run.serverRunId,Is.EqualTo(start.Result.run.serverRunId));
            // A legal old v1 abandonment creates an unsettled local result. Merely reopening must not submit it.
            var old=f.App.Account.Session; old.Execute(old.Prepare("abandon"));
            var pending=old.Prepare("settle"); var store=new LoopStore(f.AccountPath); store.WritePending(pending);
            string pendingJson=LoopJson.Encode(pending),pendingCheckpoint=File.ReadAllText(f.AccountPath);
            int requests=f.Wire.Records.Length; yield return f.Open();
            Assert.That(LoopJson.Encode(f.App.Account.Pending),Is.EqualTo(pendingJson));
            Assert.That(File.ReadAllText(f.AccountPath),Is.EqualTo(pendingCheckpoint));
            Assert.That(f.Wire.Records.Length,Is.EqualTo(requests),"Reopening with an old pending settlement must not send even a profile request.");
            Assert.That(f.Button("새 게임").button.interactable,Is.False);
            Assert.That(f.Wire.Count("POST","/v1/runs"),Is.EqualTo(1),"Only the explicit v1 setup request is permitted.");
            Assert.That(f.Wire.Records.Any(r=>r.path.EndsWith("/submit",StringComparison.Ordinal)),Is.False);
            Assert.That(f.Wire.Count("POST","/v1/profile/upgrades"),Is.Zero);
            f.Write("legacy-evidence.json",new {activeTicketPreserved=true,growthBlocked=true,localV4Started=true,
                oldSettlementPendingPreserved=true,automaticRequestsWithPending=0,http=f.Wire.Summary()});
        }

        [UnityTest] public IEnumerator Q1_PrepareLostGrowthResponseForServerRestart()
        {
            f=new CompetitionHttpFixture("restart",restart:true);
            Assert.That(File.Exists(f.RestartEvidencePath),Is.False,"Use a fresh Q1 root/user pair for a new Prepare/Resume run.");
            yield return f.Open(); Assert.That(f.App.Account.Profile.currency,Is.EqualTo(20));
            yield return f.SelectAdventurer("scholar"); yield return f.Press("출전 선택"); yield return f.Press("비전 주사위");
            yield return f.Press("새 게임");
            for(int i=0;i<80 && f.App.Session.Snapshot.state.participants[0].phase!=CompetitionPhase.Combat;i++) yield return f.Step(false);
            Assert.That(f.App.Session.Snapshot.state.participants[0].phase,Is.EqualTo(CompetitionPhase.Combat));
            yield return f.Press("모험 종료"); yield return f.Press("계정 관리");
            f.Wire.Arm("POST","/v1/profile/upgrades","after-abort"); yield return f.Press("영구 HP");
            var pending=f.App.Account.Pending; Assert.That(pending,Is.Not.Null);
            Assert.That(f.App.Account.Profile.growth,Is.Zero); Assert.That(f.App.Account.Ready,Is.False);
            var server=f.Backend.GetProfileAsync(CancellationToken.None); yield return CompetitionHttpFixture.Await(server);
            Assert.That(server.Result.profile.values.growth,Is.EqualTo(1)); Assert.That(server.Result.profile.values.currency,Is.Zero);
            var evidence=new Q1RestartEvidence {endpoint=f.Settings.endpoint,userHash=LoopJson.Hash(f.Settings.userId),pendingId=pending.id,
                pendingHash=LoopJson.Hash(LoopJson.Encode(pending)),bodyHash=LoopJson.Hash(LoopJson.Encode(pending.backend)),
                accountHash=CompetitionHttpFixture.FileHash(f.AccountPath),competitionHash=CompetitionHttpFixture.FileHash(f.CompetitionPath),
                adventurerHash=CompetitionHttpFixture.FileHash(f.SelectionPath),loadoutHash=CompetitionHttpFixture.FileHash(f.LoadoutPath),
                snapshotHash=LoopJson.Hash(LoopJson.Encode(f.App.Session.Snapshot)),accountRevision=f.App.Account.Session.Snapshot.revision,
                competitionRevision=f.App.Session.Snapshot.revision,serverRevision=server.Result.profile.revision};
            f.Write("restart-evidence.json",evidence); f.Capture("restart-pending");
            f.Write("prepare-http.json",f.Wire.Summary());
            TestContext.WriteLine("Q1 Prepare retained an approved growth response loss. Restart the owned server at the SAME endpoint/DB, then invoke Q1_ResumeLostGrowthResponseAfterServerRestart.");
        }

        [UnityTest] public IEnumerator Q1_ResumeLostGrowthResponseAfterServerRestart()
        {
            f=new CompetitionHttpFixture("restart",restart:true);
            Assert.That(File.Exists(f.RestartEvidencePath),Is.True,"Prepare must finish before the coordinator restarts the owned server.");
            var evidence=JsonConvert.DeserializeObject<Q1RestartEvidence>(File.ReadAllText(f.RestartEvidencePath));
            Assert.That(f.Settings.endpoint,Is.EqualTo(evidence.endpoint)); Assert.That(LoopJson.Hash(f.Settings.userId),Is.EqualTo(evidence.userHash));
            Assert.That(CompetitionHttpFixture.FileHash(f.AccountPath),Is.EqualTo(evidence.accountHash));
            Assert.That(CompetitionHttpFixture.FileHash(f.CompetitionPath),Is.EqualTo(evidence.competitionHash));
            Assert.That(CompetitionHttpFixture.FileHash(f.SelectionPath),Is.EqualTo(evidence.adventurerHash));
            Assert.That(CompetitionHttpFixture.FileHash(f.LoadoutPath),Is.EqualTo(evidence.loadoutHash));
            yield return f.Open();
            Assert.That(f.Wire.Records,Is.Empty,"A durable pending request must fence automatic profile refresh on re-entry.");
            Assert.That(LoopJson.Hash(LoopJson.Encode(f.App.Account.Pending)),Is.EqualTo(evidence.pendingHash));
            Assert.That(f.App.Account.Session.Snapshot.revision,Is.EqualTo(evidence.accountRevision));
            Assert.That(LoopJson.Hash(LoopJson.Encode(f.App.Session.Snapshot)),Is.EqualTo(evidence.snapshotHash));
            yield return f.Press("경기 이어하기"); Assert.That(f.App.Session.Snapshot.state.participants[0].phase,Is.EqualTo(CompetitionPhase.Combat));
            yield return f.Press("모험 종료"); yield return f.Press("계정 관리"); yield return f.Press("같은 요청 다시 확인");
            Assert.That(f.App.Account.Pending,Is.Null); Assert.That(f.App.Account.Ready,Is.True);
            Assert.That(f.App.Account.Profile.growth,Is.EqualTo(1)); Assert.That(f.App.Account.Profile.currency,Is.Zero);
            var post=f.Wire.Records.Single(r=>r.method=="POST");
            Assert.That(post.path,Is.EqualTo("/v1/profile/upgrades")); Assert.That(post.requestId,Is.EqualTo(evidence.pendingId));
            Assert.That(post.bodyHash,Is.EqualTo(evidence.bodyHash));
            Assert.That(f.App.Account.Session.Snapshot.authority.revision,Is.EqualTo(evidence.serverRevision),"An idempotent retry cannot purchase growth twice.");
            Assert.That(CompetitionHttpFixture.FileHash(f.CompetitionPath),Is.EqualTo(evidence.competitionHash));
            Assert.That(CompetitionHttpFixture.FileHash(f.SelectionPath),Is.EqualTo(evidence.adventurerHash));
            Assert.That(CompetitionHttpFixture.FileHash(f.LoadoutPath),Is.EqualTo(evidence.loadoutHash));
            yield return f.Press("로비로"); yield return f.Press("경기 이어하기");
            Assert.That(f.App.Session.Snapshot.state.startingProfile.growth,Is.Zero,"Existing v4 participants retain their frozen pre-purchase account growth.");
            Assert.That(f.App.Session.Snapshot.revision,Is.EqualTo(evidence.competitionRevision));
            f.Capture("restart-resumed"); f.Write("resume-evidence.json",new {sameEndpointAndClientRoot=true,
                samePendingIdAndBody=true,appliedGrowth=1,currency=0,competitionAndPreferencesUnchanged=true,http=f.Wire.Summary()});
        }
    }

    [Serializable] public sealed class Q1RestartEvidence
    {
        public string endpoint,userHash,pendingId,pendingHash,bodyHash,accountHash,competitionHash,adventurerHash,loadoutHash,snapshotHash;
        public long accountRevision,competitionRevision,serverRevision;
    }
}
#endif
