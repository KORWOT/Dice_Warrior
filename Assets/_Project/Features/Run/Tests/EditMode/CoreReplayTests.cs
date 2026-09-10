using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    public sealed class CoreReplayTests
    {
        static JToken Canonical(JToken token)
        {
            if(token is JObject obj)return new JObject(obj.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Canonical(p.Value))));
            if(token is JArray array)return new JArray(array.Select(Canonical));
            return token.DeepClone();
        }
        static string Hash(RunState state)
        {
            // D adds optional fields. Require legacy semantics before projecting ONLY those new fields.
            Assert.That(state.config.combat.actions.All(a=>a.effects==null||a.effects.Length==0),Is.True);
            Assert.That(state.config.world.shopPriceMultipliers==null||state.config.world.shopPriceMultipliers.Length==0,Is.True);
            if(state.shopOffers!=null&&state.shopOffers.Count>0)
            {
                Assert.That(ShopRules.IsShopContext(state),Is.True);
                Assert.That(state.shopOffers.Select(o=>o.productId),Is.EqualTo(state.config.world.shop.Select(p=>p.id)));
                Assert.That(state.shopOffers.Select(o=>o.price),Is.EqualTo(state.config.world.shop.Select(p=>p.price)));
            }
            var json=JObject.Parse(JsonUtility.ToJson(state));
            foreach(var field in json.Descendants().OfType<JProperty>().Where(p=>p.Name=="addActionId").ToArray())
            { Assert.That(string.IsNullOrEmpty((string)field.Value),Is.True);field.Remove(); }
            foreach(var action in json["config"]["combat"]["actions"].Cast<JObject>())action.Property("effects")?.Remove();
            ((JObject)json["config"]["world"]).Property("shopPriceMultipliers")?.Remove();json.Property("shopOffers")?.Remove();
            // Every pre-D field, ID, RNG and time remains in the original complete-state hash.
            var text=Canonical(json).ToString(Formatting.None);
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");
        }
        static bool Apply(RunSession run,string command,string argument)
        {
            var token=run.CaptureCommandToken();
            switch(command)
            {
                case "node":return run.ChooseNode(argument,token);
                case "roll":return run.Roll(token);
                case "reroll":return run.Reroll(int.Parse(argument),token);
                case "fate":return run.ChooseFate(argument,token);
                case "action":return run.ChooseAction(argument,token);
                case "encounter":return run.ResolveEncounter(argument=="true",token);
                case "buy":return run.Buy(argument,token);
                case "leave":return run.LeaveShop(token);
                case "claim":return run.ClaimReward(token);
                case "equip":return run.Equip(argument=="true",token);
                case "die":return run.ReplaceDie(int.Parse(argument),token);
                default:throw new ArgumentException(command);
            }
        }
        [TestCase(0,false)][TestCase(1,false)][TestCase(2,false)][TestCase(3,false)][TestCase(4,false)][TestCase(5,false)][TestCase(6,false)]
        [TestCase(0,true)][TestCase(1,true)][TestCase(2,true)][TestCase(3,true)][TestCase(4,true)][TestCase(5,true)][TestCase(6,true)]
        public void PreExtractionCommandsKeepEveryStateFieldAndChoiceId(int scenario,bool resumeEveryCommand)
        {
            var fixture=JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"_Project/Features/Run/Tests/EditMode/Fixtures/ReplayBaseline.json")));
            var row=fixture["cases"][scenario];
            var initial=JsonUtility.FromJson<RunState>((string)row["initialJson"]);
            string directory=Path.Combine(Path.GetTempPath(),"FateDiceCoreReplay",Guid.NewGuid().ToString("N"));
            var store=new LocalRunStore(Path.Combine(directory,"run.json"));
            try
            {
                var run=new RunSession(initial,resumeEveryCommand?store:null);
                foreach(var step in row["steps"])
                {
                    var context=(string)row["name"]+" sequence="+step["sequence"]+" "+step["command"]+" "+step["argument"];
                    run.RecordElapsed((double)step["elapsed"]);
                    Assert.That(Apply(run,(string)step["command"],(string)step["argument"]),Is.True,context);
                    var current=run.ReadSnapshot();
                    Assert.That(current.sequence,Is.EqualTo((int)step["sequence"]),context);
                    Assert.That(current.rngState,Is.EqualTo((uint)step["rngState"]),context);
                    Assert.That(current.phase.ToString(),Is.EqualTo((string)step["phase"]),context);
                    Assert.That(Hash(current),Is.EqualTo((string)step["expectedHash"]),context+" complete state differs");
                    if(resumeEveryCommand)
                    {
                        var restored=store.Load();
                        Assert.That(Hash(restored),Is.EqualTo((string)step["expectedHash"]),context+" stored snapshot differs");
                        run=new RunSession(restored,store);
                    }
                }
                Assert.That(run.Phase,Is.EqualTo(RunPhase.Result));
                Assert.That(run.State.won,Is.EqualTo((bool)row["won"]));
            }
            finally
            {
                var allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"FateDiceCoreReplay"))+Path.DirectorySeparatorChar;
                if(Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
    }
}
