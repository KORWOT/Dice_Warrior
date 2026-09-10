using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class SelectionFeedback : MonoBehaviour
    {
        public MMF_Player player;
        public Image surface;
        [Tooltip("선택 연출 재생 배율. 실제 FEEL 종료를 기다립니다.")]
        [Min(0)] public float playbackSpeed = 1;
        Vector3 originalScale;
        Color originalColor;
        bool captured;

        public IEnumerator Play()
        {
            ResetFeedback();
            if (!player) yield break;
            if (float.IsNaN(playbackSpeed) || float.IsInfinity(playbackSpeed) || playbackSpeed < 0)
                throw new System.InvalidOperationException("Selection playback speed must be finite and nonnegative.");
            if (playbackSpeed == 0) yield break;
            originalScale = transform.localScale;
            if (surface) originalColor = surface.color;
            captured = true;
            foreach (var item in player.FeedbacksList)
                if (item is MMF_Graphic graphic)
                {
                    graphic.ColorOverTime = new Gradient();
                    graphic.ColorOverTime.SetKeys(new[] {
                        new GradientColorKey(originalColor, 0),
                        new GradientColorKey(Color.Lerp(originalColor, Color.white, .45f), .35f),
                        new GradientColorKey(originalColor, 1)
                    }, new[] { new GradientAlphaKey(originalColor.a, 0), new GradientAlphaKey(originalColor.a, 1) });
                }
            player.Initialization();
            player.DurationMultiplier = 1f / playbackSpeed;
            player.PlayFeedbacks();
            try { while (captured && player.IsPlaying) yield return null; }
            finally { ResetFeedback(); }
        }

        public void ResetFeedback()
        {
            if (player) { player.StopFeedbacks(true); if (captured) player.RestoreInitialValues(); }
            if (!captured) return;
            transform.localScale = originalScale;
            if (surface) surface.color = originalColor;
            captured = false;
        }

        void OnDisable() => ResetFeedback();
    }
}
