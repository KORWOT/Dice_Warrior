using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FateDice.Editor
{
    // Sample numbers live here only for initial authoring. Existing assets are never reset automatically.
    public static class PrototypeAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/FateDicePrototype.unity";

        public static GameConfigData CreateDefaults()
        {
            var kinds = new[] { HandKind.Pair, HandKind.TwoPairs, HandKind.Triple, HandKind.Straight, HandKind.FullHouse,
                HandKind.ThreePairs, HandKind.FourKind, HandKind.FullStraight, HandKind.FiveKind, HandKind.SixKind };
            var powers = new[] { 1, 2, 3, 4, 5, 5, 6, 7, 8, 9 };
            var data = new GameConfigData
            {
                version = "prototype-1",
                dice = new DiceSettings
                {
                    basicDieId = "plain", replacementDieId = "ember", minimumFatePower = 1, maximumFatePower = 9,
                    dice = new[] {
                        new DieDefinition { id="plain", label="Plain D6", values=new[]{1,2,3,4,5,6}, weights=new[]{1f,1f,1f,1f,1f,1f}},
                        new DieDefinition { id="ember", label="Ember D6", values=new[]{1,2,3,4,5,6}, weights=new[]{1f,1f,1f,1f,2f,3f}}
                    },
                    hands = kinds.Select((x,i) => new HandDefinition { kind=x, label=HandLabel(x), priority=i, fatePower=powers[i] }).ToArray()
                },
                fate = new FateSettings
                {
                    exploration = new[] { Row(1,65,25,8,1.8f,.2f), Row(3,45,32,18,4,1), Row(5,25,32,28,12,3), Row(7,10,20,35,25,10), Row(9,3,12,30,35,20) },
                    combat = new[] { Row(1,58,31,9,1.8f,.2f), Row(3,43,34,18,4,1), Row(5,25,32,30,10,3), Row(7,12,23,35,22,8), Row(9,5,15,35,30,15) },
                    gradeMultipliers = new[]{1f,1.2f,1.5f,1.9f,2.4f},
                    nodeWeights = new[]{34f,22f,17f,12f,15f},
                    selectedTypeBias=3f,
                    capRewardMultipliers=new[]{1.35f,1.25f,1.15f,1.05f,1f}
                },
                combat = new CombatSettings
                {
                    minimumEffect=1, bossEnemyId="fate_keeper",
                    startingActionIds=new[]{"strike","guard","heavy"},
                    trialActionIds=new[]{"fireball","bastion"},
                    actions=new[]{
                        Action("strike","Strike",Grade.Common,1.1f,0,"attack","physical"),
                        Action("guard","Guard",Grade.Common,0,1.5f,"defense"),
                        Action("heavy","Heavy strike",Grade.Uncommon,1.8f,0,"attack","physical"),
                        Action("fireball","Fireball",Grade.Rare,2.3f,0,"attack","fire","magic","projectile"),
                        Action("bastion","Bastion",Grade.Rare,0,2.5f,"defense","magic"),
                        new ActionDefinition{id="ember_slash",label="잔불 베기",grade=Grade.Uncommon,tags=new[]{"attack","fire","physical"},
                            effects=new[]{new ActionEffectDefinition{kind=ActionEffectKind.Damage,coefficient=1.6f}}}
                    },
                    enemies = new[]{
                        Enemy("road_bandit","Road bandit",26,6,5, new[]{55f,25f,20f}),
                        Enemy("stone_sentry","Stone sentry",36,7,8, new[]{40f,35f,25f}),
                        Enemy("fate_keeper","Fate keeper",130,13,9, new[]{50f,20f,30f})
                    }
                },
                growth = new GrowthSettings
                {
                    characterId="wanderer",characterName="Wanderer",startingMaxHp=90,startingPower=12,startingGuard=10,startingGold=12,
                    rerollCost=1,
                    levels = new[]{Level(12,8,2,1),Level(18,8,2,1),Level(26,10,2,2),Level(40,10,3,2),Level(60,12,3,2)},
                    tags = new[]{"attack","physical","defense","fire","magic","projectile","steel","luck"}.Select(x=>new TagDefinition{id=x,label=x}).ToArray(),
                    equipment = new[]{
                        new EquipmentDefinition {id="ember_blade",label="Ember blade",slot=EquipmentSlot.Weapon,power=3,tags=new[]{"fire"},modifiers=new[]{Modifier(MatchMode.Any,TagSource.Card,ModifiedValue.Damage,.25f,"fire")}},
                        new EquipmentDefinition {id="guard_plate",label="Guard plate",slot=EquipmentSlot.Armor,guard=3,tags=new[]{"steel"},modifiers=new[]{Modifier(MatchMode.All,TagSource.Card,ModifiedValue.Block,.2f,"defense")}},
                        new EquipmentDefinition {id="traveler_charm",label="Traveler charm",slot=EquipmentSlot.Accessory,maxHp=5,tags=new[]{"luck"},modifiers=new[]{Modifier(MatchMode.None,TagSource.Card,ModifiedValue.Damage,.1f,"magic"),Modifier(MatchMode.All,TagSource.Equipment,ModifiedValue.Block,.1f,"luck")}}
                    }
                },
                world = new WorldSettings
                {
                    eventsToBoss=10,offeredCards=3,previewDepth=2,branchCount=3,
                    bossReward = new RewardDefinition {gold=50,xp=30},
                    restTraining = new RewardDefinition {xp=12,rerollCharges=1},
                    shopPriceMultipliers=new[]{1f,.95f,.9f,.85f,.8f},
                    shop = new[]{
                        new ShopProduct{id="potion",label="Healing draught",price=10,reward=new RewardDefinition{health=25}},
                        new ShopProduct{id="reroll",label="Reroll training + 3 charges",price=12,reward=new RewardDefinition{rerollCharges=3}},
                        new ShopProduct{id="die",label="Ember die (replace one)",price=16,reward=new RewardDefinition{dieId="ember"}},
                        new ShopProduct{id="blade",label="Ember blade",price=18,reward=new RewardDefinition{equipmentId="ember_blade"}}
                    }
                },
                presentation = new PresentationSettings
                {
                    actionSeconds=.18f,rollSeconds=.35f,
                    explorationDice=new RollPresentationSettings(),combatDice=new RollPresentationSettings(),
                    referenceResolution=new Vector2(720,1280),
                    bodyFontSize=25,titleFontSize=38,buttonHeight=102,
                    background=new Color(.035f,.047f,.075f),panel=new Color(.09f,.12f,.18f),
                    accent=new Color(.34f,.84f,.72f),text=new Color(.92f,.94f,.97f),danger=new Color(.96f,.39f,.39f),
                    gradeColors=new[]{new Color(.72f,.76f,.81f),new Color(.42f,.8f,.5f),new Color(.42f,.66f,1f),new Color(.75f,.48f,1f),new Color(1f,.72f,.28f)}
                }
            };
            data.world.events = Enumerable.Range(0,5).SelectMany(type => Enumerable.Range(0,5).Select(grade =>
            {
                var factor = data.fate.gradeMultipliers[grade];
                var reward = new RewardDefinition { gold=Mathf.RoundToInt(5*factor), xp=Mathf.RoundToInt(4*factor) };
                var name = ""; var description = "";
                switch ((NodeType)type)
                {
                    case NodeType.Combat: name="Road encounter"; description="Defeat the enemy to claim the spoils."; reward.xp=Mathf.RoundToInt(8*factor); break;
                    case NodeType.Event: name="The wishing stone"; description="A sharp toll buys insight and the power to reroll."; reward.health=-4; reward.xp=Mathf.RoundToInt(8*factor);reward.rerollCharges=2;break;
                    case NodeType.Treasure: name="Abandoned cache";description="Take the coins and inspect a piece of equipment.";reward.gold=Mathf.RoundToInt(12*factor);reward.equipmentId=data.growth.equipment[grade%3].id; if(grade>=2)reward.dieId="ember";break;
                    case NodeType.Shop: name="Wayfarer's stall";description="Spend run coins, or leave when ready.";reward.gold=0;reward.xp=0;break;
                    case NodeType.Rest: name="Quiet camp";description="Recover health or train for experience and a reroll charge.";reward.health=Mathf.RoundToInt(24*factor);reward.gold=0;break;
                }
                return new EventDefinition{id=((NodeType)type).ToString().ToLowerInvariant()+"_"+grade,label=name,description=description,type=(NodeType)type,grade=(Grade)grade,
                    enemyId=type==0?(grade>=2?"stone_sentry":"road_bandit"):"",reward=reward};
            })).ToArray();
            data.world.events.Single(x=>x.id=="treasure_0").reward.addActionId="ember_slash";
            return data;
        }

        [MenuItem("Fate Dice/Create prototype assets (new only)")]
        public static string CreateAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring.");
            var config = AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            if (!config)
            {
                EnsureFolder("Assets/_Project/Features/Fate/Configs");
                config = ScriptableObject.CreateInstance<FateDiceConfig>();
                config.data = CreateDefaults();
                AssetDatabase.CreateAsset(config, FateDiceConfig.DefaultAssetPath);
                AssetDatabase.SaveAssets();
            }
            config.Snapshot();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
            {
                if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
                    throw new InvalidOperationException("An open scene is dirty. Save it yourself before creating the prototype scene.");
                EnsureFolder("Assets/_Project/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera = new GameObject("Prototype Camera").AddComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=config.data.presentation.background;
                camera.orthographic=true;
                var screen = new GameObject("Fate Dice").AddComponent<FateDiceScreen>();
                screen.config=config;
                screen.uiFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            return "Config validated; assets created only if missing: "+ScenePath;
        }

        public static string VerifyConfigEdits(string savePath)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run data-edit verification outside Play Mode.");
            var asset=AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            var original=JsonUtility.ToJson(asset.Snapshot());
            var result=new DataEditEvidence();var store=new LocalRunStore(savePath);
            try
            {
                var start=RunSession.New(asset.Snapshot(),33,"fireball",Grade.Common);store.Save(start.State);
                var baseline=new RunSession(store.Load());EnterSampleCombat(baseline);
                result.beforeEnemyHp=baseline.State.enemyHp;
                result.beforeDamage=CombatRules.Evaluate(baseline.State,new OfferedCard{contentId="strike",grade=Grade.Common}).damage;
                baseline.Roll();result.beforeGrades=string.Join(",",baseline.State.cards.Select(x=>x.grade));
                result.beforeThreshold=asset.data.world.eventsToBoss;

                asset.data.Enemy("road_bandit").maxHp*=2;asset.data.Action("strike").damageCoefficient*=2;
                foreach(var row in asset.data.fate.exploration.Concat(asset.data.fate.combat))row.weights=new float[]{0,0,0,0,1};
                asset.data.world.eventsToBoss=1;SaveConfigAsset(asset);
                asset=AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
                var changed=RunSession.New(asset.Snapshot(),33,"fireball",Grade.Common);EnterSampleCombat(changed);
                result.afterEnemyHp=changed.State.enemyHp;
                result.afterDamage=CombatRules.Evaluate(changed.State,new OfferedCard{contentId="strike",grade=Grade.Common}).damage;
                changed.Roll();result.afterGrades=string.Join(",",changed.State.cards.Select(x=>x.grade));
                RequireEvidence(result.afterEnemyHp>result.beforeEnemyHp&&result.afterDamage>result.beforeDamage,"Enemy/card SO edits did not change actual combat.");
                RequireEvidence(changed.State.cards.All(x=>x.grade==Grade.Legendary)&&result.afterGrades!=result.beforeGrades,"Grade SO weights did not change actual draws.");

                var next=RunSession.New(asset.Snapshot(),33,"fireball",Grade.Common);ResolveSampleEvent(next);
                result.afterThreshold=next.State.config.world.eventsToBoss;
                result.newRunOffersBoss=next.State.nodes.Where(x=>next.State.availableNodeIds.Contains(x.id)).All(x=>x.type==NodeType.Boss);
                var continued=new RunSession(store.Load());EnterSampleCombat(continued);continued.Roll();
                result.savedSnapshotUnchanged=JsonUtility.ToJson(continued.State.config)==original;
                result.savedReplayUnchanged=JsonUtility.ToJson(continued.State)==JsonUtility.ToJson(baseline.State);
                var oldProgress=new RunSession(store.Load());ResolveSampleEvent(oldProgress);
                result.oldRunKeepsNormalPaths=oldProgress.State.nodes.Where(x=>oldProgress.State.availableNodeIds.Contains(x.id)).All(x=>x.type!=NodeType.Boss);
                RequireEvidence(result.newRunOffersBoss&&result.oldRunKeepsNormalPaths&&result.savedSnapshotUnchanged&&result.savedReplayUnchanged,"Snapshot/progression changed in an existing saved run.");
            }
            finally
            {
                asset.data=JsonUtility.FromJson<GameConfigData>(original);SaveConfigAsset(asset);
                result.originalAssetRestored=JsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath).data)==original;
            }
            RequireEvidence(result.originalAssetRestored,"The original data asset was not restored.");
            return JsonUtility.ToJson(result);
        }
        private static void SaveConfigAsset(FateDiceConfig asset)
        {
            asset.Snapshot();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);
            AssetDatabase.ImportAsset(FateDiceConfig.DefaultAssetPath,ImportAssetOptions.ForceUpdate);
        }
        private static void EnterSampleCombat(RunSession run)
        {
            var node=run.State.nodes.First(x=>run.State.availableNodeIds.Contains(x.id)&&x.type==NodeType.Combat);
            RequireEvidence(run.ChooseNode(node.id)&&run.Roll()&&run.ChooseFate(run.State.cards[0].id),"Sample combat did not start.");
        }
        private static void ResolveSampleEvent(RunSession run)
        {
            var node=run.State.nodes.First(x=>run.State.availableNodeIds.Contains(x.id)&&x.type==NodeType.Event);
            RequireEvidence(run.ChooseNode(node.id)&&run.Roll()&&run.ChooseFate(run.State.cards[0].id)&&run.ResolveEncounter(false)&&run.ClaimReward(),"Sample event did not resolve.");
        }
        private static void RequireEvidence(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
        [Serializable] private sealed class DataEditEvidence
        {
            public int beforeEnemyHp,afterEnemyHp,beforeDamage,afterDamage,beforeThreshold,afterThreshold;
            public string beforeGrades,afterGrades;
            public bool newRunOffersBoss,oldRunKeepsNormalPaths,savedSnapshotUnchanged,savedReplayUnchanged,originalAssetRestored;
        }

        public static string InspectAssets()
        {
            var config=AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            if(!config) throw new InvalidOperationException("Missing "+FateDiceConfig.DefaultAssetPath);
            config.Snapshot();
            return JsonUtility.ToJson(new AssetInspection { config=FateDiceConfig.DefaultAssetPath, scene=ScenePath,
                version=config.data.version,eventCount=config.data.world.events.Length,actions=config.data.combat.actions.Length,valid=true});
        }
        [Serializable] private sealed class AssetInspection {public string config,scene,version;public int eventCount,actions;public bool valid;}
        private static void EnsureFolder(string path)
        {
            var parts=path.Split('/');var current=parts[0];
            for(var i=1;i<parts.Length;i++){var next=current+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);current=next;}
        }
        private static GradeRow Row(int min,params float[] weights)=>new GradeRow{minimumPower=min,weights=weights};
        private static LevelStep Level(int xp,int hp,int power,int guard)=>new LevelStep{xpRequired=xp,maxHp=hp,power=power,guard=guard};
        private static ActionDefinition Action(string id,string label,Grade grade,float damage,float block,params string[] tags)=>new ActionDefinition{id=id,label=label,grade=grade,damageCoefficient=damage,blockCoefficient=block,tags=tags};
        private static TagModifier Modifier(MatchMode mode,TagSource source,ModifiedValue value,float bonus,params string[] tags)=>new TagModifier{match=mode,source=source,value=value,bonus=bonus,requiredTags=tags};
        private static EnemyDefinition Enemy(string id,string label,int hp,int power,int guard,float[] weights)=>new EnemyDefinition{id=id,label=label,maxHp=hp,power=power,guard=guard,intents=new[]{
            new EnemyIntent{id="attack",label="Attack",kind=IntentKind.Attack,weight=weights[0],coefficient=1f},
            new EnemyIntent{id="defend",label="Defend",kind=IntentKind.Defend,weight=weights[1],coefficient=1.5f},
            new EnemyIntent{id="heavy",label="Heavy attack",kind=IntentKind.Heavy,weight=weights[2],coefficient=1.5f}}};
        private static string HandLabel(HandKind kind)
        {
            switch(kind){case HandKind.TwoPairs:return "Two pairs";case HandKind.ThreePairs:return "Three pairs";case HandKind.FullHouse:return "Full house";case HandKind.FourKind:return "Four of a kind";case HandKind.FullStraight:return "Full straight";case HandKind.FiveKind:return "Five of a kind";case HandKind.SixKind:return "Six of a kind";default:return kind.ToString();}
        }
    }
}
