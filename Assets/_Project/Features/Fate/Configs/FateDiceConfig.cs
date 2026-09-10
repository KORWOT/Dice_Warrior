using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FateDice
{
    [Serializable] public sealed class RollPresentationSettings
    {
        [Tooltip("굴림 연출 시간. 결과는 이미 저장되어 있으며 0은 즉시 표시합니다.")] [Min(0)] public float rollSeconds = .65f;
        [Tooltip("확정된 여섯 면과 조합을 보여주는 시간. 0은 바로 카드를 표시합니다.")] [Min(0)] public float resultHoldSeconds = .9f;
    }
    [Serializable] public sealed class PresentationSettings
    {
        [Tooltip("Cosmetic input lock duration, seconds. Does not consume rules RNG.")] [Range(0,2)] public float actionSeconds;
        // Retained only to read schema1 checkpoints authored before separate phase settings.
        [HideInInspector] public float rollSeconds;
        public RollPresentationSettings explorationDice = new RollPresentationSettings();
        public RollPresentationSettings combatDice = new RollPresentationSettings();

        public PresentationSettings DeepCopy() => new PresentationSettings
        {
            actionSeconds=actionSeconds, rollSeconds=rollSeconds,
            explorationDice=CopyTiming(DiceTiming(false)), combatDice=CopyTiming(DiceTiming(true)),
            referenceResolution=referenceResolution, bodyFontSize=bodyFontSize, titleFontSize=titleFontSize,
            buttonHeight=buttonHeight, background=background, panel=panel, accent=accent, text=text, danger=danger,
            gradeColors=gradeColors==null?null:(Color[])gradeColors.Clone()
        };
        private static RollPresentationSettings CopyTiming(RollPresentationSettings value) =>
            new RollPresentationSettings {rollSeconds=value.rollSeconds,resultHoldSeconds=value.resultHoldSeconds};

        public RollPresentationSettings DiceTiming(bool combat)
        {
            return (combat ? combatDice : explorationDice) ?? new RollPresentationSettings
            {
                rollSeconds = Math.Max(.65f, rollSeconds), resultHoldSeconds = .9f
            };
        }
        [Tooltip("Portrait logical canvas dimensions in pixels.")] public Vector2 referenceResolution;
        [Tooltip("Main text pixel sizes at reference resolution.")] [Range(16,64)] public int bodyFontSize, titleFontSize;
        [Tooltip("Button height in reference pixels.")] [Range(48,180)] public float buttonHeight;
        public Color background, panel, accent, text, danger;
        [Tooltip("One color per grade, alongside grade text.")] public Color[] gradeColors;
    }
    // The authored/save DTO retains schema1 flat fields; the core copies only RunRulesCatalog.
    [Serializable] public sealed class GameConfigData : RunRulesCatalog
    {
        public PresentationSettings presentation;
        public new GameConfigData DeepCopy()
        {
            var copy=new GameConfigData {presentation=presentation?.DeepCopy()};
            CopyRulesTo(copy);
            return copy;
        }
        public override string[] Validate()
        {
            var errors=base.Validate().ToList();
            bool Check(bool ok,string path) { if(!ok)errors.Add(path);return ok; }
            bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
            bool Nonnegative(float x)=>Finite(x)&&x>=0;
            bool Positive(float x)=>Finite(x)&&x>0;
            if(!Check(presentation!=null,"presentation: required"))return errors.ToArray();
            Check(Nonnegative(presentation.actionSeconds)&&presentation.actionSeconds<=2&&Nonnegative(presentation.rollSeconds)&&presentation.rollSeconds<=2,"presentation.timings: 0..2 seconds");
            foreach (bool combat in new[] { false, true })
            {
                var timing = presentation.DiceTiming(combat);
                Check(Nonnegative(timing.rollSeconds)&&Nonnegative(timing.resultHoldSeconds),
                    "presentation."+(combat?"combatDice":"explorationDice")+": finite nonnegative times required");
            }
            Check(Positive(presentation.referenceResolution.x)&&Positive(presentation.referenceResolution.y)&&presentation.referenceResolution.y>presentation.referenceResolution.x,"presentation.referenceResolution: positive portrait dimensions required");
            Check(presentation.bodyFontSize>=16&&presentation.bodyFontSize<=64&&presentation.titleFontSize>=16&&presentation.titleFontSize<=64,"presentation.fonts: 16..64");
            Check(Finite(presentation.buttonHeight)&&presentation.buttonHeight>=48&&presentation.buttonHeight<=180,"presentation.buttonHeight: 48..180");
            Check(presentation.gradeColors!=null&&presentation.gradeColors.Length==5,"presentation.gradeColors: five required");
            return errors.ToArray();
        }
    }
    [CreateAssetMenu(menuName="Fate Dice/Prototype Config", fileName="DefaultFateDice")]
    public sealed class FateDiceConfig : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/_Project/Features/Fate/Configs/DefaultFateDice.asset";
        public GameConfigData data;
        public GameConfigData Snapshot()
        {
            if (data == null) throw new InvalidOperationException("Missing config data: " + DefaultAssetPath);
            var errors = data.Validate();
            if (errors.Length != 0) throw new InvalidOperationException(DefaultAssetPath + ": " + string.Join("; ", errors));
            return data.DeepCopy();
        }
    }
}
