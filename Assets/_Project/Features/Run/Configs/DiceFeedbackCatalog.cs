using System;
using UnityEngine;

namespace FateDice
{
    public enum DiceEffectComplexity { Simple, Runic, Grand, Legendary }

    [Serializable]
    public sealed class DiceEffectTimeline
    {
        public string label;
        [Range(0, 1)] public float maximumStrength;
        public DiceEffectComplexity complexity;
        [Min(0)] public float entranceSeconds;
        [Min(0)] public float flourishSeconds;
        [Min(0)] public float readSeconds;
        [Min(0)] public float exitSeconds;

        public float Duration(float minimumRead)
        {
            foreach (float value in new[] { entranceSeconds, flourishSeconds, readSeconds, exitSeconds, minimumRead })
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                    throw new InvalidOperationException("Dice effect timeline durations must be finite and nonnegative.");
            return entranceSeconds + flourishSeconds + Mathf.Max(readSeconds, minimumRead) + exitSeconds;
        }
    }

    [Serializable]
    public sealed class DiceHandFeedbackStyle
    {
        public HandKind hand;
        public Color color = Color.white;
        [Tooltip("주사위 링과 중앙 오라의 보조색")]
        public Color accentColor = Color.white;
        [Tooltip("조합의 두 번째 묶음을 보조색으로 강조")]
        public bool alternateGroups;
        [Tooltip("설치된 Text Animator 효과 태그. 예: wave, bounce")]
        public string textEffect = "wave";
        [Range(0, .2f)] public float punchScale = .08f;
    }

    [CreateAssetMenu(menuName = "Fate Dice/주사위 조합 연출")]
    public sealed class DiceFeedbackCatalog : ScriptableObject
    {
        public int auraVersion;
        public int timelineVersion;
        [Tooltip("저장된 조합 우선순위의 낮은 단계부터 순서대로 적용. 읽기 시간은 기존 결과 유지 시간 이상입니다.")]
        public DiceEffectTimeline[] tiers = Array.Empty<DiceEffectTimeline>();
        public DiceHandFeedbackStyle[] styles = Array.Empty<DiceHandFeedbackStyle>();

        public DiceEffectTimeline ResolveTimeline(float strength)
        {
            if (float.IsNaN(strength) || float.IsInfinity(strength))
                throw new ArgumentOutOfRangeException(nameof(strength));
            if (tiers == null || tiers.Length == 0)
                throw new InvalidOperationException("Dice feedback requires authored tier timelines.");
            float previous = -1;
            DiceEffectTimeline match = null;
            foreach (var tier in tiers)
            {
                if (tier == null || float.IsNaN(tier.maximumStrength) || tier.maximumStrength <= previous || tier.maximumStrength > 1 ||
                    !Enum.IsDefined(typeof(DiceEffectComplexity), tier.complexity))
                    throw new InvalidOperationException("Dice effect tiers must have increasing thresholds ending at 1.");
                tier.Duration(0);
                previous = tier.maximumStrength;
                if (match == null && Mathf.Clamp01(strength) <= tier.maximumStrength) match = tier;
            }
            if (previous != 1 || match == null) throw new InvalidOperationException("Dice effect tiers must cover every strength through 1.");
            return match;
        }

        public DiceHandFeedbackStyle Resolve(HandKind hand)
        {
            foreach (var style in styles)
                if (style != null && style.hand == hand) return style;
            throw new InvalidOperationException("Missing dice feedback style: " + hand);
        }
    }
}
