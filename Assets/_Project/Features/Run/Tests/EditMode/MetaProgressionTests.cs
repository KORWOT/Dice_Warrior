using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using FateDice.Editor;
using UnityEngine;
using NUnit.Framework;

namespace FateDice.Tests
{
    public sealed class MetaProgressionTests
    {
        string directory;
        MetaProgressionConfig config;
        GameConfigData rules;
        LocalPlayerDataStore disk;
        ProbeStore probe;
        LocalMetaProgressionService service;
        sealed class ProbeStore : IPlayerDataStore
        {
            readonly IPlayerDataStore inner;
            public bool failBefore, failAfter;
            public ProbeStore(IPlayerDataStore inner) { this.inner = inner; }
            public string ProfileId => inner.ProfileId;
            public bool Exists => inner.Exists;
            public PlayerSaveDocument Read() => inner.Read();
            public void Write(long revision, PlayerSaveDocument candidate)
            {
                if (failBefore) throw new IOException("controlled commit failure");
                inner.Write(revision, candidate);
                if (failAfter) throw new IOException("controlled lost response after commit");
            }
        }
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "FateDiceMetaTests", Guid.NewGuid().ToString("N"));
            config = ScriptableObject.CreateInstance<MetaProgressionConfig>();
            config.startingEquipmentIds = new[] { "ember_blade", "traveler_charm" }; config.extraDieIds = new[] { "ember" };
            rules = PrototypeAuthoring.CreateDefaults(); rules.world.eventsToBoss = 1;
            rules.fate.nodeWeights = new float[] { 0, 0, 0, 0, 1 }; rules.growth.startingPower = 500;
            disk = new LocalPlayerDataStore(Path.Combine(directory, "player.json"), "test-player"); probe = new ProbeStore(disk);
            service = NewService();
        }
        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
            var allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceMetaTests")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(allowed, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        LocalMetaProgressionService NewService(IRunStore legacy = null) => new LocalMetaProgressionService(probe, () => rules.DeepCopy(), config, new FixedSeedSource(33), legacy);
        static T Done<T>(Task<T> task) => task.GetAwaiter().GetResult();
        StartRunRequest StartRequest() => new StartRunRequest { requestId = Guid.NewGuid().ToString("N"), expectedRevision = service.Profile.revision };
        RunSession Start(StartRunRequest request = null) => new RunSession(Done(service.StartRunAsync(request ?? StartRequest())), service.RunStore);
        SettlementRequest SettleRequest(string runId) => new SettlementRequest { requestId = "settle-" + runId, runId = runId, expectedRevision = service.Profile.revision };
        static void Finish(RunSession run)
        {
            for (int i = 0; i < 300 && run.Phase != RunPhase.Result; i++)
            {
                var s = run.ReadSnapshot(); bool changed;
                switch (s.phase)
                {
                    case RunPhase.Map: changed = run.ChooseNode(s.availableNodeIds[0]); break;
                    case RunPhase.ExplorationRoll: case RunPhase.CombatRoll: changed = run.Roll(); break;
                    case RunPhase.ExplorationCards: changed = run.ChooseFate(s.cards[0].id); break;
                    case RunPhase.CombatCards: changed = run.ChooseAction(s.cards.OrderByDescending(x => CombatRules.Evaluate(s, x).damage).First().id); break;
                    case RunPhase.Encounter: changed = run.ResolveEncounter(false); break;
                    case RunPhase.Reward: changed = run.ClaimReward(); break;
                    case RunPhase.EquipmentChoice: changed = !string.IsNullOrEmpty(s.pendingEquipmentId) ? run.Equip(true) : run.ReplaceDie(0); break;
                    case RunPhase.Shop: changed = run.LeaveShop(); break;
                    default: throw new Exception("Unexpected phase " + s.phase);
                }
                Assert.That(changed, Is.True, s.phase.ToString());
            }
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Result));
        }
        [Test] public void ProfileReadsDoNotWriteAndCopiesNeverAliasOwnership()
        {
            var p = service.Profile; Assert.That(disk.Exists, Is.False);
            p.dice[0].definitionId = "changed"; p.loadout.dice[0] = "changed"; p.characters[0].rank = 99;
            Assert.That(service.Profile.dice[0].definitionId, Is.EqualTo("plain"));
            Assert.That(service.Profile.characters[0].rank, Is.Zero);
            Assert.That(LocalPlayerDataStore.PlayerPath(directory, "../other/user"), Does.StartWith(Path.GetFullPath(directory)));
            Assert.That(LocalPlayerDataStore.PlayerPath(directory, "a"), Is.Not.EqualTo(LocalPlayerDataStore.PlayerPath(directory, "b")));
        }
        [Test] public void OwnedLoadoutPersistsAndStartsAnIndependentFrozenRun()
        {
            var p = service.Profile; var l = p.loadout.Copy();
            l.equipment[0] = p.equipment[0].instanceId; l.equipment[2] = p.equipment[1].instanceId;
            l.dice[0] = p.dice[6].instanceId; l.wildcardId = "bastion"; l.cap = Grade.Rare;
            var request = new LoadoutRequest { requestId = "loadout", expectedRevision = p.revision, loadout = l };
            Done(service.SaveLoadoutAsync(request)); l.dice[0] = "external mutation";
            service = NewService(); var run = Start(); var s = run.ReadSnapshot();
            Assert.That(s.profileId, Is.EqualTo("test-player")); Assert.That(s.dieIds[0], Is.EqualTo("ember"));
            Assert.That(s.equipmentIds[0], Is.EqualTo("ember_blade")); Assert.That(s.equipmentIds[2], Is.EqualTo("traveler_charm"));
            Assert.That(s.hp, Is.EqualTo(95)); Assert.That(s.actionIds, Does.Contain("bastion"));
            Assert.That(s.explorationCap, Is.EqualTo(Grade.Rare));
            var frozen = MetaJson.Encode(disk.Read().start); var profile = MetaJson.Encode(service.Profile);
            rules.growth.startingMaxHp = 444; config.policy.hpPerRank = 80;
            run.ChooseNode(s.availableNodeIds[0]); run.Roll();
            Assert.That(MetaJson.Encode(service.Profile), Is.EqualTo(profile));
            Assert.That(MetaJson.Encode(disk.Read().start), Is.EqualTo(frozen));
            Assert.That(NewService().RunStore.Load().config.growth.startingMaxHp, Is.EqualTo(90));
            disk.Read().start.loadout.dice[0] = "mutated copy";
            Assert.That(disk.Read().start.loadout.dice[0], Is.EqualTo("starter-die-6"));
        }
        [TestCase("duplicate-die")] [TestCase("missing-die")] [TestCase("wrong-slot")]
        [TestCase("missing-wildcard")] [TestCase("missing-character")] [TestCase("short-dice")]
        public void InvalidLoadoutsCannotCreateOrOverwriteAProfile(string error)
        {
            var p = service.Profile; var l = p.loadout.Copy();
            switch (error)
            {
                case "duplicate-die": l.dice[1] = l.dice[0]; break;
                case "missing-die": l.dice[0] = "not-owned"; break;
                case "wrong-slot": l.equipment[1] = p.equipment[0].instanceId; break;
                case "missing-wildcard": l.wildcardId = "strike"; break;
                case "missing-character": l.characterId = "not-owned"; break;
                case "short-dice": l.dice = l.dice.Take(5).ToArray(); break;
            }
            Assert.Throws<MetaCommandException>(() => Done(service.SaveLoadoutAsync(new LoadoutRequest { requestId = error, loadout = l })));
            Assert.That(disk.Exists, Is.False);
        }
        [Test] public void SettlementUsesFrozenPolicyAndNeverExportsRunItemsOrGold()
        {
            var run = Start(); var original = service.Profile; Finish(run);
            Assert.That(run.ReadSnapshot().won, Is.True); Assert.That(service.Profile.growthCurrency, Is.Zero);
            config.policy.currencyPerEvent = 999; config.policy.victoryBonus = 999;
            var receipt = Done(service.SettleAsync(SettleRequest(run.ReadSnapshot().runId)));
            Assert.That(receipt.currency, Is.EqualTo(11)); Assert.That(service.Profile.growthCurrency, Is.EqualTo(11));
            Assert.That(MetaJson.Encode(service.Profile.equipment), Is.EqualTo(MetaJson.Encode(original.equipment)));
            Assert.That(MetaJson.Encode(service.Profile.dice), Is.EqualTo(MetaJson.Encode(original.dice)));
            Assert.That(run.ReadSnapshot().gold, Is.GreaterThan(rules.growth.startingGold));
        }
        [Test] public void UnsettledResultsBlockReplacementAndReplaySurvivesRestartAndResponseLoss()
        {
            var run = Start(); Finish(run); string id = run.ReadSnapshot().runId;
            Assert.Throws<MetaCommandException>(() => Start(new StartRunRequest { requestId = "blocked", expectedRevision = service.Profile.revision, abandonActiveRun = true }));
            var request = SettleRequest(id); var before = File.ReadAllBytes(disk.Path);
            probe.failBefore = true; Assert.Throws<IOException>(() => Done(service.SettleAsync(request)));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(disk.Path)); Assert.That(service.Profile.growthCurrency, Is.Zero);
            probe.failBefore = false; probe.failAfter = true; Assert.Throws<IOException>(() => Done(service.SettleAsync(request)));
            probe.failAfter = false; service = NewService();
            Assert.That(Done(service.SettleAsync(request)).currency, Is.EqualTo(11));
            Assert.That(Done(service.SettleAsync(new SettlementRequest { requestId = "another-request", runId = id, expectedRevision = 0 })).currency, Is.EqualTo(11));
            Assert.That(service.Profile.growthCurrency, Is.EqualTo(11)); Assert.That(disk.Read().settlements.Count, Is.EqualTo(1));
            Start(); Assert.That(Done(service.SettleAsync(request)).currency, Is.EqualTo(11)); Assert.That(service.Profile.growthCurrency, Is.EqualTo(11));
        }
        [Test] public void GrowthChargesOnceAndOnlyChangesFutureRunStats()
        {
            var run = Start(); Finish(run); Done(service.SettleAsync(SettleRequest(run.ReadSnapshot().runId)));
            var active = Start(); string frozen = MetaJson.Encode(active.ReadSnapshot());
            var request = new GrowthRequest { requestId = "growth", expectedRevision = service.Profile.revision, characterId = "wanderer" };
            probe.failBefore = true; Assert.Throws<IOException>(() => Done(service.GrowAsync(request))); Assert.That(service.Profile.growthCurrency, Is.EqualTo(11));
            probe.failBefore = false; probe.failAfter = true; Assert.Throws<IOException>(() => Done(service.GrowAsync(request)));
            probe.failAfter = false; service = NewService(); Done(service.GrowAsync(request));
            Assert.That(service.Profile.growthCurrency, Is.EqualTo(1)); Assert.That(service.Profile.characters[0].rank, Is.EqualTo(1));
            Assert.That(MetaJson.Encode(service.RunStore.Load()), Is.EqualTo(frozen));
            var next = Start(new StartRunRequest { requestId = "grown-start", expectedRevision = service.Profile.revision, abandonActiveRun = true });
            Assert.That(next.ReadSnapshot().baseMaxHp, Is.EqualTo(92)); Assert.That(next.ReadSnapshot().basePower, Is.EqualTo(501));
            Assert.That(disk.Read().start.characterRank, Is.EqualTo(1));
            Assert.Throws<MetaCommandException>(() => Done(service.GrowAsync(new GrowthRequest { requestId = "too-poor", characterId = "wanderer", expectedRevision = service.Profile.revision })));
        }
        [Test] public void StartRetryDoesNotAbandonOrGenerateASecondRunAndFailureDoesNotPublish()
        {
            var request = StartRequest(); probe.failBefore = true;
            Assert.Throws<IOException>(() => Start(request)); Assert.That(disk.Exists, Is.False);
            probe.failBefore = false; probe.failAfter = true; Assert.Throws<IOException>(() => Start(request));
            var committed = disk.Read().run.runId; probe.failAfter = false;
            Assert.That(Start(request).ReadSnapshot().runId, Is.EqualTo(committed));
            Assert.That(disk.Read().settlements, Is.Empty);
            Assert.Throws<MetaCommandException>(() => Start());
            Start(new StartRunRequest { requestId = "abandon", expectedRevision = service.Profile.revision, abandonActiveRun = true });
            Assert.That(service.Settlement(committed).outcome, Is.EqualTo("abandoned")); Assert.That(service.Settlement(committed).currency, Is.Zero);
        }
        [Test] public void RequestReuseStaleRevisionAndForeignCheckpointAreRejected()
        {
            var p = service.Profile; Done(service.SaveLoadoutAsync(new LoadoutRequest { requestId = "same", loadout = p.loadout }));
            var other = p.loadout.Copy(); other.wildcardId = "bastion";
            Assert.Throws<MetaCommandException>(() => Done(service.SaveLoadoutAsync(new LoadoutRequest { requestId = "same", loadout = other })));
            Assert.Throws<MetaCommandException>(() => Done(service.GrowAsync(new GrowthRequest { requestId = "same", characterId = "wanderer" })));
            Assert.Throws<MetaCommandException>(() => Start(new StartRunRequest { requestId = "stale", expectedRevision = 0 }));
            var run = Start(); var state = run.ReadSnapshot(); state.profileId = "foreign";
            Assert.Throws<MetaCommandException>(() => service.RunStore.Save(state));
            state = run.ReadSnapshot(); state.config.growth.startingPower++;
            Assert.Throws<MetaCommandException>(() => service.RunStore.Save(state));
            var stale = disk.Read(); run.ChooseNode(run.ReadSnapshot().availableNodeIds[0]);
            Assert.Throws<MetaCommandException>(() => disk.Write(stale.revision, stale));
        }
        [Test] public void LegacyMigrationPreservesSourceBytesAndAwardsNoPermanentCurrency()
        {
            var legacy = new LocalRunStore(Path.Combine(directory, "legacy.json")); var old = RunSession.New(rules, 33, "fireball", Grade.Common, legacy);
            Finish(old); var bytes = File.ReadAllBytes(legacy.Path); service = NewService(legacy);
            Assert.That(service.RunStore.Load().phase, Is.EqualTo(RunPhase.Result)); Assert.That(disk.Exists, Is.False);
            var receipt = Done(service.SettleAsync(SettleRequest(service.RunStore.Load().runId)));
            Assert.That(receipt.outcome, Is.EqualTo("legacy")); Assert.That(receipt.currency, Is.Zero);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(legacy.Path));
            Assert.That(NewService().RunStore.Load().profileId, Is.EqualTo("test-player"));
        }
        [Test] public void DefeatOnlyAwardsCompletedEvents()
        {
            rules.growth.startingPower = 0; rules.growth.startingMaxHp = 1; rules.fate.nodeWeights = new float[] { 1, 0, 0, 0, 0 };
            foreach (var enemy in rules.combat.enemies)
            { enemy.maxHp = 10000; enemy.power = 10000; foreach (var intent in enemy.intents) intent.weight = intent.kind == IntentKind.Attack ? 1 : 0; }
            var run = Start(); Finish(run); Assert.That(run.ReadSnapshot().won, Is.False);
            var receipt = Done(service.SettleAsync(SettleRequest(run.ReadSnapshot().runId)));
            Assert.That(receipt.outcome, Is.EqualTo("lost")); Assert.That(receipt.currency, Is.EqualTo(run.ReadSnapshot().eventsResolved));
        }
        [Test] public void CorruptOrForeignDocumentsNeverGetOverwrittenByCommands()
        {
            Start(); var valid = File.ReadAllText(disk.Path);
            Assert.Throws<InvalidDataException>(() => new LocalPlayerDataStore(disk.Path, "foreign").Read());
            File.WriteAllText(disk.Path, valid.Replace("checksum", "broken_checksum")); var damaged = File.ReadAllBytes(disk.Path);
            Assert.Throws<InvalidDataException>(() => Done(service.StartRunAsync(new StartRunRequest { requestId = "corrupt", abandonActiveRun = true })));
            CollectionAssert.AreEqual(damaged, File.ReadAllBytes(disk.Path));
        }
        [Test] public void ConcurrentSessionCandidatesCannotOverwriteACommittedCommand()
        {
            var first = Start(); var stale = new RunSession(service.RunStore.Load(), service.RunStore);
            Assert.That(first.SetExplorationCap(Grade.Common), Is.True);
            var committed = File.ReadAllBytes(disk.Path);
            Assert.Throws<MetaCommandException>(() => stale.SetExplorationCap(Grade.Rare));
            CollectionAssert.AreEqual(committed, File.ReadAllBytes(disk.Path));
            Assert.That(stale.ReadSnapshot().explorationCap, Is.EqualTo(Grade.Legendary));
            Assert.That(service.RunStore.Load().explorationCap, Is.EqualTo(Grade.Common));
        }
        [Test] public void SameCommandRetryAndElapsedAutosaveKeepTheirLegalSequenceSemantics()
        {
            var run = Start(); probe.failAfter = true;
            Assert.Throws<IOException>(() => run.SetExplorationCap(Grade.Common));
            Assert.That(run.ReadSnapshot().sequence, Is.Zero);
            Assert.That(service.RunStore.Load().sequence, Is.EqualTo(1));
            probe.failAfter = false; Assert.That(run.SetExplorationCap(Grade.Common), Is.True);
            Assert.That(run.ReadSnapshot().sequence, Is.EqualTo(1));
            run.RecordElapsed(1.25); Assert.That(run.SaveCheckpoint(), Is.True);
            Assert.That(service.RunStore.Load().sequence, Is.EqualTo(1));
            Assert.That(service.RunStore.Load().playedSeconds, Is.EqualTo(1.25));
            var backwards = service.RunStore.Load(); backwards.playedSeconds = 0;
            Assert.Throws<MetaCommandException>(() => service.RunStore.Save(backwards));
        }
        [Test] public void PermanentCommandsExposeIntentInsteadOfClientSelectedAmounts()
        {
            var assembly = typeof(RunSession).Assembly;
            foreach (var name in new[] { "IMetaProgressionService", "LocalMetaProgressionService", "SettlementRequest", "GrowthRequest" })
                Assert.That(assembly.GetType("FateDice." + name), Is.Not.Null, "Missing meta boundary: " + name);
            foreach (var name in new[] { "SettlementRequest", "GrowthRequest" })
                Assert.That(assembly.GetType("FateDice." + name).GetFields().Select(x => x.Name),
                    Has.None.Matches<string>(x => x == "amount" || x == "balance" || x == "cost" || x == "profileId"));
        }
    }
}
