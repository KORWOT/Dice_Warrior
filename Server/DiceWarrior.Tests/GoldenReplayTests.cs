using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DiceWarrior.SymbolLoop.Tests
{
    public sealed class GoldenReplayTests
    {
        public sealed class Vector
        {
            public string name;
            public RunTicket ticket;
            public int payout, resumeAfter;
            public List<RunCommandEntry> commands;
            public List<JObject> states;
        }
        sealed class FileModel { public int format = 0; public string rulesVersion = null; public List<Vector> vectors = null; }
        public static List<Vector> Read()
        {
            var file = LoopJson.Decode<FileModel>(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "ReplayGoldenVectors.json")));
            Assert.That(file.format, Is.EqualTo(1)); Assert.That(file.rulesVersion, Is.EqualTo(BackendProtocol.RulesVersion));
            return file.vectors;
        }
        [TestCase("victory")]
        [TestCase("defeat")]
        [TestCase("abandon-after-region")]
        [TestCase("shop-equipment-resume")]
        public void SharedRulesMatchEveryFrozenUnityState(string name)
        {
            var vector = Read().Single(v => v.name == name);
            var ticket = vector.ticket;
            var run = LoopRules.Start(LoopJson.Copy(ticket.rules), LoopJson.Copy(ticket.startingProfile.values), ticket.seed, ticket.serverRunId);
            AssertState(vector.states[0], run, name + " initial");
            for (int i = 0; i < vector.commands.Count; ++i)
            {
                var command = vector.commands[i]; Assert.That(command.targetId, Is.EqualTo(RunCommandRules.Target(run)));
                RunCommandRules.Apply(run, command.kind, command.arg);
                AssertState(vector.states[i + 1], run, name + " " + (i + 1));
            }
            var verified = MockRunVerifier.Verify(ticket, vector.commands);
            Assert.That(verified.accepted, Is.True, verified.code); Assert.That(verified.payout, Is.EqualTo(vector.payout));
            AssertState(vector.states.Last(), verified.finalRun, name + " verifier final");
        }
        [Test] public void RequestFingerprintUsesSameFieldOrderNullsAndNumbersInDifferentCultures()
        {
            var request = new BackendRequest { requestId = "fingerprint-contract", kind = "grow", expectedRevision = 7 };
            const string expected = "{\"protocolVersion\":1,\"rulesVersion\":\"symbol-loop-mvp-1\",\"requestId\":\"fingerprint-contract\",\"kind\":\"grow\",\"serverRunId\":null,\"expectedRevision\":7,\"startingDie\":\"balanced\",\"startingWeapon\":\"blade\",\"upgrade\":\"hp\",\"commands\":[]}";
            var saved = CultureInfo.CurrentCulture;
            try
            {
                foreach (string culture in new[] { "en-US", "ko-KR", "tr-TR", "fr-FR" })
                { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); Assert.That(LoopJson.Encode(request), Is.EqualTo(expected)); }
            }
            finally { CultureInfo.CurrentCulture = saved; }
            Assert.That(Read().Sum(v => v.commands.Count), Is.EqualTo(121)); Assert.That(Read().Sum(v => v.states.Count), Is.EqualTo(125));
        }
        static void AssertState(JObject expected, Run actual, string context)
        {
            var actualJson = JObject.Parse(LoopJson.Encode(actual));
            foreach (var property in expected.Properties())
                Assert.That(JToken.DeepEquals(property.Value, actualJson[property.Name]), Is.True,
                    context + " " + property.Name + " expected=" + property.Value + " actual=" + actualJson[property.Name]);
        }
    }
}
