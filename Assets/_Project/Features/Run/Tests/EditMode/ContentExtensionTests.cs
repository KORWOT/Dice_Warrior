using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections.Generic;
using FateDice.Editor;
using NUnit.Framework;

namespace FateDice.Tests
{
    public sealed class ContentExtensionTests
    {
        [Test] public void DamageAndBlockUseTheExplicitCoreEffectResolver()
        {
            Assert.That(typeof(CombatRules).Assembly.GetType("FateDice.EffectResolver"),Is.Not.Null);
            Assert.That(typeof(ActionDefinition).GetField("effects"),Is.Not.Null);
        }
        [Test] public void OneNewActionIsAcquiredFromTheExistingTreasureReward()
        {
            var data=PrototypeAuthoring.CreateDefaults();
            var action=data.combat.actions.SingleOrDefault(a=>a.id=="ember_slash");
            Assert.That(action,Is.Not.Null);
            Assert.That(data.combat.actions.Length,Is.EqualTo(6));
            Assert.That(typeof(RewardDefinition).GetField("addActionId"),Is.Not.Null);
        }
        [Test] public void ShopPricePolicyAndFixedOffersHaveExplicitDataFields()
        {
            Assert.That(typeof(WorldSettings).GetField("shopPriceMultipliers"),Is.Not.Null);
            Assert.That(typeof(RunStateData).GetField("shopOffers"),Is.Not.Null);
        }

        static GameConfigData Config()
        {
            var config=PrototypeAuthoring.CreateDefaults();
            foreach(var row in config.fate.combat)row.weights=new float[]{0,1,0,0,0};
            foreach(var enemy in config.combat.enemies){enemy.maxHp=100000;enemy.power=0;}
            return config;
        }
        static RunSession Enter(NodeType type,Grade grade=Grade.Common,GameConfigData config=null)
        {
            config=config??Config();
            foreach(var row in config.fate.exploration){row.weights=new float[5];row.weights[(int)grade]=1;}
            var snapshot=RunSession.New(config,33,"fireball",grade).ReadSnapshot();
            foreach(var node in snapshot.nodes)node.type=NodeType.Combat;
            snapshot.nodes.Single(n=>n.id==snapshot.availableNodeIds[0]).type=type;
            snapshot.gold=1000;
            var run=new RunSession(snapshot);
            Assert.That(run.ChooseNode(snapshot.availableNodeIds[0]),Is.True);
            Assert.That(run.Roll(),Is.True);Assert.That(run.ChooseFate(run.State.cards[0].id),Is.True);
            return run;
        }
        static void WithStore(Action<LocalRunStore> test)
        {
            var directory=Path.Combine(Path.GetTempPath(),"FateDiceContentTests",Guid.NewGuid().ToString("N"));
            try{test(new LocalRunStore(Path.Combine(directory,"run.json")));}
            finally
            {
                var root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"FateDiceContentTests"))+Path.DirectorySeparatorChar;
                if(Path.GetFullPath(directory).StartsWith(root,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        static string Json(RunState state)=>JsonUtility.ToJson(state);
        [Test] public void AuthoredAssetHasExactlyOneAcquirableActionAndPreservesStartingOwnership()
        {
            var data=UnityEditor.AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath).Snapshot();
            Assert.That(data.Validate(),Is.Empty);
            Assert.That(data.combat.actions.Select(x=>x.id),Is.EquivalentTo(new[]{"strike","guard","heavy","fireball","bastion","ember_slash"}));
            Assert.That(data.combat.startingActionIds,Is.EqualTo(new[]{"strike","guard","heavy"}));
            Assert.That(data.combat.trialActionIds,Is.EqualTo(new[]{"fireball","bastion"}));
            Assert.That(data.Event("treasure_0").reward.addActionId,Is.EqualTo("ember_slash"));
            var action=data.Action("ember_slash");Assert.That(action.label,Is.EqualTo("잔불 베기"));
            Assert.That(action.grade,Is.EqualTo(Grade.Uncommon));Assert.That(action.tags,Is.EqualTo(new[]{"attack","fire","physical"}));
            Assert.That(action.effects.Length,Is.EqualTo(1));Assert.That(action.effects[0].kind,Is.EqualTo(ActionEffectKind.Damage));
            Assert.That(action.effects[0].coefficient,Is.EqualTo(1.6f));
            Assert.That(data.world.shopPriceMultipliers,Is.EqualTo(new[]{1f,1.1f,1.25f,1.5f,1.75f}));
        }
        [Test] public void MixedEffectsSharePreviewAndResolutionWithIndependentTagAndShieldOracle()
        {
            var state=Enter(NodeType.Combat).ReadSnapshot();
            var action=state.config.Action("strike");action.damageCoefficient=999;action.blockCoefficient=999;
            action.tags=new[]{"attack","fire","physical","defense"};
            action.effects=new[]{new ActionEffectDefinition{kind=ActionEffectKind.Damage,coefficient=1.6f},new ActionEffectDefinition{kind=ActionEffectKind.Block,coefficient=1.5f}};
            GrowthRules.Equip(state,"ember_blade");GrowthRules.Equip(state,"guard_plate");
            state.phase=RunPhase.CombatCards;state.enemyHp=100;state.enemyShield=5;
            var enemy=state.config.Enemy(state.activeEnemyId);enemy.power=40;
            var intent=enemy.intents.First(x=>x.kind==IntentKind.Attack);intent.coefficient=1;state.activeIntentId=intent.id;
            var card=new OfferedCard{contentId="strike",grade=Grade.Common};
            uint rng=state.rngState;var effect=CombatRules.Evaluate(state,card);
            Assert.That(effect.damage,Is.EqualTo(30),"15 power * 1.6 * 1.25 fire");
            Assert.That(effect.block,Is.EqualTo(23),"13 guard * 1.5 * 1.2 defense, AwayFromZero");
            Assert.That(state.rngState,Is.EqualTo(rng));int hp=state.hp;
            CombatRules.Resolve(state,card);
            Assert.That(state.enemyHp,Is.EqualTo(75));Assert.That(state.hp,Is.EqualTo(hp-17));
            Assert.That(state.shield,Is.Zero);Assert.That(state.enemyShield,Is.Zero);
        }
        [TestCase(-1f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
        public void InvalidExplicitCoefficientsAreRejectedBeforeNewRun(float value)
        {
            var data=Config();data.Action("ember_slash").effects[0].coefficient=value;
            Assert.That(data.Validate(),Is.Not.Empty);Assert.Throws<InvalidOperationException>(()=>RunSession.New(data,33,"fireball",Grade.Common));
        }
        [Test] public void UnknownNullAndAllZeroEffectsAndUnknownRewardActionAreRejected()
        {
            var data=Config();var action=data.Action("ember_slash");
            action.effects=new[]{new ActionEffectDefinition{kind=(ActionEffectKind)99,coefficient=1}};Assert.That(data.Validate(),Is.Not.Empty);
            action.effects=new ActionEffectDefinition[]{null};Assert.That(data.Validate(),Is.Not.Empty);
            action.effects=new[]{new ActionEffectDefinition{kind=ActionEffectKind.Block,coefficient=0}};Assert.That(data.Validate(),Is.Not.Empty);
            data=Config();data.Event("treasure_0").reward.addActionId="missing";Assert.That(data.Validate(),Is.Not.Empty);
            var run=Enter(NodeType.Treasure);var state=run.State;state.pendingReward.addActionId="missing";
            Assert.Throws<RunStateValidationException>(()=>new RunSession(state));
        }
        [Test] public void TreasureAcquisitionActualDrawTagsUseAndEveryCheckpointResumeKeepTheNewCard()
        {
            WithStore(store=>{
                var run=Enter(NodeType.Treasure);Assert.That(run.State.actionIds,Does.Not.Contain("ember_slash"));
                Assert.That(run.ResolveEncounter(false),Is.True);store.Save(run.State);run=new RunSession(store.Load(),store);
                Assert.That(run.ClaimReward(),Is.True);Assert.That(run.State.actionIds.Count(x=>x=="ember_slash"),Is.EqualTo(1));
                Assert.That(run.Equip(true),Is.True);Assert.That(run.State.equipmentIds[0],Is.EqualTo("ember_blade"));
                run=new RunSession(store.Load(),store);Assert.That(run.ChooseNode(run.State.availableNodeIds[0]),Is.True);
                Assert.That(run.Roll(),Is.True);Assert.That(run.ChooseFate(run.State.cards[0].id),Is.True);
                OfferedCard found=null;
                for(int turn=0;turn<32&&found==null;turn++)
                {
                    Assert.That(run.Roll(),Is.True);found=run.State.cards.FirstOrDefault(x=>x.contentId=="ember_slash");
                    if(found==null)Assert.That(run.ChooseAction(run.State.cards[0].id),Is.True);
                }
                Assert.That(found,Is.Not.Null,"Actual RunSession.Roll did not draw the acquired card.");
                string before=Json(run.State);var loaded=store.Load();Assert.That(Json(loaded),Is.EqualTo(before));
                run=new RunSession(loaded,store);var snapshot=run.State;Assert.That(snapshot.cards.Any(c=>c.id==found.id),Is.True);
                Assert.That(CombatRules.Evaluate(snapshot,found).damage,Is.EqualTo(30));
                var expectedHp=Math.Max(0,snapshot.enemyHp-Math.Max(0,30-snapshot.enemyShield));
                Assert.That(run.ChooseAction(found.id),Is.True);Assert.That(run.State.enemyHp,Is.EqualTo(expectedHp));
                Assert.That(Json(store.Load()),Is.EqualTo(Json(run.State)));Assert.That(run.ChooseAction(found.id),Is.False);
            });
        }
        [Test] public void DuplicateAcquisitionRetainsSetOwnershipAndDoesNotBiasActualDraws()
        {
            var run=Enter(NodeType.Treasure);Assert.That(run.ResolveEncounter(false),Is.True);
            var before=run.State;before.actionIds.Add("ember_slash");run=new RunSession(before);
            var rng=run.State.rngState;int gold=run.State.gold,rewardGold=run.State.pendingReward.gold;
            Assert.That(run.ClaimReward(),Is.True);Assert.That(run.State.gold,Is.EqualTo(gold+rewardGold));
            Assert.That(run.State.rngState,Is.EqualTo(rng));Assert.That(run.State.actionIds.Count(x=>x=="ember_slash"),Is.EqualTo(1));
            var single=run.State;var duplicate=single.DeepCopy();GrowthRules.Grant(duplicate,new RewardDefinition{addActionId="ember_slash"});
            for(int i=0;i<64;i++)
            {
                var a=FateCardRules.GenerateActions(single);var b=FateCardRules.GenerateActions(duplicate);
                Assert.That(a.Select(x=>x.contentId),Is.EqualTo(b.Select(x=>x.contentId)));
                Assert.That(a.Select(x=>x.grade),Is.EqualTo(b.Select(x=>x.grade)));Assert.That(single.rngState,Is.EqualTo(duplicate.rngState));
            }
        }
        [Test] public void AcquiringAnActionNeverMutatesTheSourceDefinitionOrAnotherNewRun()
        {
            var config=Config();var run=Enter(NodeType.Treasure,Grade.Common,config);
            string definition=JsonUtility.ToJson(config);Assert.That(run.ResolveEncounter(false),Is.True);Assert.That(run.ClaimReward(),Is.True);
            var fresh=RunSession.New(config,33,"fireball",Grade.Common);
            Assert.That(fresh.State.actionIds,Does.Not.Contain("ember_slash"));Assert.That(run.State.actionIds,Does.Contain("ember_slash"));
            Assert.That(JsonUtility.ToJson(config),Is.EqualTo(definition));Assert.That(config.combat.startingActionIds,Is.EqualTo(new[]{"strike","guard","heavy"}));
        }
        [TestCase(Grade.Common,10,12,16,18)][TestCase(Grade.Uncommon,11,13,18,20)]
        [TestCase(Grade.Rare,13,15,20,23)][TestCase(Grade.Epic,15,18,24,27)][TestCase(Grade.Legendary,18,21,28,32)]
        public void EntrySnapshotsExactGradePricesWithoutRngAndPurchaseResumeReusesThem(Grade grade,int potion,int reroll,int die,int blade)
        {
            WithStore(store=>{
                var run=Enter(NodeType.Shop,grade);var snapshot=run.State;var prices=new[]{potion,reroll,die,blade};
                Assert.That(snapshot.shopOffers.Select(x=>x.price),Is.EqualTo(prices));
                uint rng=snapshot.rngState;ShopRules.Enter(snapshot);Assert.That(snapshot.rngState,Is.EqualTo(rng));
                store.Save(snapshot);run=new RunSession(store.Load(),store);
                int gold=run.State.gold;Assert.That(run.Buy("potion"),Is.True);Assert.That(run.State.gold,Is.EqualTo(gold-potion));
                Assert.That(run.State.rngState,Is.EqualTo(rng));Assert.That(run.State.shopOffers.Select(x=>x.price),Is.EqualTo(prices));
                run=new RunSession(store.Load(),store);Assert.That(run.ClaimReward(),Is.True);Assert.That(run.Phase,Is.EqualTo(RunPhase.Shop));
                Assert.That(run.State.shopOffers.Select(x=>x.price),Is.EqualTo(prices));Assert.That(run.Buy("potion"),Is.False);
                Assert.That(run.LeaveShop(),Is.True);Assert.That(run.State.shopOffers,Is.Empty);
            });
        }
        [Test] public void PriceRoundingZeroAndOverflowAreExplicitAndMalformedOffersRejected()
        {
            var data=Config();Assert.That(ShopRules.Price(data,0,Grade.Legendary),Is.Zero);
            Assert.That(ShopRules.Price(data,2,Grade.Rare),Is.EqualTo(3));Assert.That(ShopRules.Price(data,int.MaxValue,Grade.Legendary),Is.EqualTo(int.MaxValue));
            var snapshot=Enter(NodeType.Shop,Grade.Rare).State;snapshot.shopOffers[0].price--;
            Assert.Throws<RunStateValidationException>(()=>new RunSession(snapshot));
            snapshot=Enter(NodeType.Shop,Grade.Rare).State;snapshot.shopOffers=null;Assert.Throws<RunStateValidationException>(()=>new RunSession(snapshot));
            data.world.shopPriceMultipliers=new[]{1f};Assert.That(data.Validate(),Is.Not.Empty);
        }
        [TestCase(false)][TestCase(true)] public void FailedCardAcquisitionOrPurchaseSaveDoesNotPublishAnyState(bool purchase)
        {
            var run=Enter(purchase?NodeType.Shop:NodeType.Treasure,Grade.Common);
            if(!purchase)Assert.That(run.ResolveEncounter(false),Is.True);
            run.RecordElapsed(.125);string before=Json(run.State);
            run.Checkpoint=_=>throw new IOException("intentional content checkpoint failure");
            Assert.Throws<IOException>(()=>{if(purchase)run.Buy("potion");else run.ClaimReward();});
            Assert.That(Json(run.State),Is.EqualTo(before));run.Checkpoint=null;
            Assert.That(purchase?run.Buy("potion"):run.ClaimReward(),Is.True);
        }
        [Test] public void NewFieldsAreCopiedWithoutAliasesOrNullNormalization()
        {
            var source=Enter(NodeType.Shop,Grade.Rare).State;var copy=source.DeepCopy();
            Assert.That(Json(copy),Is.EqualTo(Json(source)));
            copy.shopOffers[0].price=0;copy.config.world.shopPriceMultipliers[0]=9;
            copy.config.Action("ember_slash").effects[0].coefficient=9;copy.config.Event("treasure_0").reward.addActionId="strike";
            Assert.That(source.shopOffers[0].price,Is.EqualTo(13));Assert.That(source.config.world.shopPriceMultipliers[0],Is.EqualTo(1));
            Assert.That(source.config.Action("ember_slash").effects[0].coefficient,Is.EqualTo(1.6f));
            Assert.That(source.config.Event("treasure_0").reward.addActionId,Is.EqualTo("ember_slash"));
            source.shopOffers=null;source.config.world.shopPriceMultipliers=null;source.config.Action("ember_slash").effects=null;
            copy=source.DeepCopy();Assert.That(copy.shopOffers,Is.Null);Assert.That(copy.config.world.shopPriceMultipliers,Is.Null);Assert.That(copy.config.Action("ember_slash").effects,Is.Null);
        }
        [Test] public void OldJsonShopRestoresOnlyItsOwnPricesAndNeverInjectsNewContentOrRules()
        {
            WithStore(store=>{
                var snapshot=Enter(NodeType.Shop,Grade.Legendary).State;
                snapshot.config.combat.actions=snapshot.config.combat.actions.Where(x=>x.id!="ember_slash").ToArray();
                foreach(var e in snapshot.config.world.events)e.reward.addActionId=null;
                snapshot.config.world.shopPriceMultipliers=null;snapshot.config.world.shop[0].price=7;snapshot.shopOffers=null;
                var payload=JObject.Parse(Json(snapshot));
                payload.Property("shopOffers").Remove();payload["config"]["world"]["shopPriceMultipliers"].Parent.Remove();
                foreach(var action in payload["config"]["combat"]["actions"].Cast<JObject>())action.Property("effects").Remove();
                foreach(var reward in payload.Descendants().OfType<JProperty>().Where(x=>x.Name=="addActionId").ToArray())reward.Remove();
                string text=payload.ToString(Newtonsoft.Json.Formatting.None);string checksum;
                using(var sha=SHA256.Create())checksum=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");
                Directory.CreateDirectory(Path.GetDirectoryName(store.Path));File.WriteAllText(store.Path,JsonUtility.ToJson(new LocalSaveEnvelope{schema=1,payload=text,checksum=checksum}));
                var bytes=File.ReadAllBytes(store.Path);var restored=store.Load();
                Assert.That(restored.config.combat.actions.Length,Is.EqualTo(5));Assert.That(restored.config.combat.actions.Any(x=>x.id=="ember_slash"),Is.False);
                Assert.That(restored.shopOffers.Select(x=>x.price),Is.EqualTo(new[]{7,12,16,18}));
                Assert.That(restored.rngState,Is.EqualTo(snapshot.rngState));Assert.That(restored.config.world.shopPriceMultipliers==null||restored.config.world.shopPriceMultipliers.Length==0,Is.True);
                CollectionAssert.AreEqual(bytes,File.ReadAllBytes(store.Path));var run=new RunSession(restored,store);
                int gold=run.State.gold;Assert.That(run.Buy("potion"),Is.True);Assert.That(run.State.gold,Is.EqualTo(gold-7));
            });
        }
    }
}
