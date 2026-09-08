using System;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class DiceRollUI : BaseUI<DiceRollUIData>
    {
        public Text title, detail, result;
        public DiceFaceView[] dice;
        public CommonButtonView rollButton;
        [Range(0, 3)] public float resultHoldSeconds = .9f;
        public int feedbackVersion;
        public bool IsRolling { get; private set; }
        float began;
        Vector3[] positions;
        Vector3 resultScale;
        float resultRevealedAt = -1;

        protected override void OnBind(DiceRollUIData data)
        {
            if (!title || !detail || !result || !rollButton || dice == null || dice.Length != 6)
                throw new InvalidOperationException("DiceRollUI requires six authored dice and its text/button references.");
            if (data.values != null && data.values.Length != 6)
                throw new ArgumentException("A dice presentation requires exactly six results.");
            title.text = data.title ?? "주사위 굴리기";
            detail.text = data.detail ?? "";
            result.text = data.rolling ? "굴리는 중…" : data.result ?? "";
            resultScale = result.rectTransform.localScale;
            resultRevealedAt = -1;
            positions ??= new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                if (!dice[i]) throw new InvalidOperationException("A die reference is missing.");
                positions[i] = dice[i].rectTransform.localPosition;
                dice[i].Render(data.values == null ? 0 : data.values[i]);
            }
            rollButton.Unbind();
            rollButton.gameObject.SetActive(data.roll != null && !data.rolling);
            if (data.roll != null && !data.rolling)
            {
                var choice = data.roll;
                rollButton.Bind(choice.key, choice.text, null, null, data.appearance, choice.interactable, false,
                    _ => choice.clicked?.Invoke());
            }
            IsRolling = data.rolling;
            began = Time.unscaledTime;
        }

        void Update()
        {
            if (resultRevealedAt >= 0 && result)
            {
                float remaining = 1 - Mathf.Clamp01((Time.unscaledTime - resultRevealedAt) / .3f);
                result.rectTransform.localScale = resultScale * (1 + .055f * remaining);
                if (remaining <= 0) resultRevealedAt = -1;
            }
            if (!IsRolling || Data == null) return;
            var progress = Mathf.Clamp01((Time.unscaledTime - began) / Mathf.Max(.01f, Data.duration));
            if (progress >= 1)
            {
                FinishFaces();
                return;
            }
            for (int i = 0; i < dice.Length; i++)
            {
                // Cosmetic cycling is deliberately independent of both Unity Random and the rules RNG.
                dice[i].Render(1 + ((int)((Time.unscaledTime - began) * 24) + i * 5) % 6);
                var decay = 1 - progress;
                dice[i].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(progress * 29 + i) * 24 * decay);
                dice[i].rectTransform.localPosition = positions[i] + Vector3.up * Mathf.Abs(Mathf.Sin(progress * 19 + i)) * 18 * decay;
            }
        }

        void FinishFaces()
        {
            for (int i = 0; i < dice.Length; i++)
            {
                dice[i].rectTransform.localPosition = positions[i];
                dice[i].rectTransform.localRotation = Quaternion.identity;
                dice[i].Render(Data?.values == null ? 0 : Data.values[i]);
            }
            result.text = Data?.result ?? "";
            if (IsRolling) resultRevealedAt = Time.unscaledTime;
            IsRolling = false;
        }

        protected override void OnUnbind()
        {
            if (positions != null && dice != null && dice.Length == positions.Length) FinishFaces();
            IsRolling = false;
            resultRevealedAt = -1;
            if (result) result.rectTransform.localScale = resultScale;
            if (rollButton) rollButton.Unbind();
            if (title) title.text = "";
            if (detail) detail.text = "";
            if (result) result.text = "";
        }
    }
}
