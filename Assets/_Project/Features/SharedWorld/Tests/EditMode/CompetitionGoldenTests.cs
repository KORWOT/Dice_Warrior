using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionGoldenTests
    {
        [Serializable] public sealed class GoldenFile
        {
            public int format = 1;
            public string rulesVersion = CompetitionContent.RulesVersion;
            public List<GoldenVector> vectors = new List<GoldenVector>();
        }
        [Serializable] public sealed class GoldenVector
        {
            public string name, matchId;
            public uint seed;
            public Profile profile;
            public int resumeAfter;
            public List<CompetitionCommandEntry> commands = new List<CompetitionCommandEntry>();
            public List<string> stateHashes = new List<string>(), randomStates = new List<string>();
        }

        [Test] public void FrozenHumanCommandsReproduceAllParticipantsAndRngOnBothRuntimes()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "CompetitionGoldenVectors.json");
            if (!File.Exists(path)) path = Path.Combine("Assets", "_Project", "Features", "SharedWorld", "Tests", "EditMode", "CompetitionGoldenVectors.json");
            if (!File.Exists(path))
            {
                // Missing expected fixtures fail. The observation is separate and never overwrites a frozen vector.
                var observed = new GoldenFile();
                observed.vectors.Add(Capture("normal", 456, false));
                observed.vectors.Add(Capture("abandon-spectate", 789, true));
                File.WriteAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "competition-golden-observation.json"), LoopJson.Encode(observed));
                Assert.Fail("Frozen competition vectors are missing; a separate observation was captured for initial inspection.");
            }
            var file = LoopJson.Decode<GoldenFile>(File.ReadAllText(path));
            Assert.That(file.format, Is.EqualTo(1)); Assert.That(file.rulesVersion, Is.EqualTo(CompetitionContent.RulesVersion));
            Assert.That(file.vectors.Count, Is.EqualTo(2));
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                foreach (string culture in new[] { "en-US", "ko-KR", "tr-TR", "fr-FR" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                    foreach (var vector in file.vectors)
                    {
                        var match = CompetitionRules.Start(CompetitionRulesTests.Definition(), vector.profile, vector.seed, vector.matchId);
                        Check(vector, match, 0);
                        for (int i = 0; i < vector.commands.Count; ++i)
                        {
                            var command = vector.commands[i];
                            Assert.That(command.sequence, Is.EqualTo(i + 1));
                            Assert.That(command.targetId, Is.EqualTo(CompetitionCommandRules.Target(match)));
                            CompetitionCommandRules.Apply(match, command.kind, command.arg);
                            if (i == vector.resumeAfter) match = LoopJson.Copy(match);
                            Check(vector, match, i + 1);
                        }
                        Assert.That(match.finished, Is.True);
                    }
                }
            }
            finally { CultureInfo.CurrentCulture = previousCulture; }
        }
        static GoldenVector Capture(string name, uint seed, bool abandon)
        {
            var vector = new GoldenVector { name = name, matchId = "golden-" + name, seed = seed, profile = new Profile { growth = 2 }, resumeAfter = 12 };
            var match = CompetitionRules.Start(CompetitionRulesTests.Definition(), vector.profile, seed, vector.matchId);
            Observe(vector, match);
            for (int i = 0; i < 2048 && !match.finished; ++i)
            {
                var command = CompetitionRulesTests.NextCommand(match);
                if (abandon && i == 2) { command.kind = "abandon"; command.arg = ""; }
                vector.commands.Add(command);
                CompetitionCommandRules.Apply(match, command.kind, command.arg);
                Observe(vector, match);
            }
            Assert.That(match.finished, Is.True);
            return vector;
        }
        static void Observe(GoldenVector vector, CompetitionState match)
        {
            vector.stateHashes.Add(LoopJson.Hash(LoopJson.Encode(match))); vector.randomStates.Add(Randoms(match));
        }
        static string Randoms(CompetitionState match) => string.Join("/", match.participants.Select(p =>
            FormattableString.Invariant($"{p.travelRng}:{p.combatRng}:{p.eventRng}:{p.aiRng}")));
        static void Check(GoldenVector vector, CompetitionState match, int index)
        {
            Assert.That(LoopJson.Hash(LoopJson.Encode(match)), Is.EqualTo(vector.stateHashes[index]), vector.name + " state " + index);
            Assert.That(Randoms(match), Is.EqualTo(vector.randomStates[index]), vector.name + " RNG " + index);
        }
    }
}
