using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class DiceRollUI : BaseUI<DiceRollUIData>
    {
        public Text title, detail, result;
        public DiceFaceView[] dice;
        public CommonButtonView rollButton;
        [HideInInspector] public float resultHoldSeconds = .9f; // Legacy prefab data; timing now comes from presentation settings.
        public int feedbackVersion;
        public int presentationVersion;
        public DiceResultFeedback resultFeedback;
        public bool IsRolling { get; private set; }
        public Exception PresentationError { get; private set; }
        int bindingVersion;
        public int BindingVersion => bindingVersion;
        float began;
        Vector3[] positions;
        Quaternion[] rotations;
        bool hasPose;
        Vector3 resultScale;

        protected override void OnBind(DiceRollUIData data)
        {
            bindingVersion++;
            PresentationError = null;
            RestorePose();
            if (resultFeedback) resultFeedback.ResetFeedback();
            if (!title || !detail || !result || !rollButton || dice == null || dice.Length != 6)
                throw new InvalidOperationException("DiceRollUI requires six authored dice and its text/button references.");
            if (data.values != null && data.values.Length != 6)
                throw new ArgumentException("A dice presentation requires exactly six results.");
            if (float.IsNaN(data.duration) || float.IsInfinity(data.duration) || data.duration < 0)
                throw new ArgumentOutOfRangeException(nameof(data.duration));
            title.text = data.title ?? "주사위 굴리기";
            detail.text = data.detail ?? "";
            result.text = data.rolling ? "굴리는 중…" : data.result ?? "";
            resultScale = result.rectTransform.localScale;
            positions ??= new Vector3[6];
            rotations ??= new Quaternion[6];
            for (int i = 0; i < 6; i++)
            {
                if (!dice[i]) throw new InvalidOperationException("A die reference is missing.");
                positions[i] = dice[i].rectTransform.localPosition;
                rotations[i] = dice[i].rectTransform.localRotation;
                dice[i].Render(data.values == null ? 0 : data.values[i]);
            }
            hasPose = true;
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
            if (IsRolling && data.duration == 0) CompleteRoll();
            else if (!IsRolling && resultFeedback) resultFeedback.Show(data);
        }

        void Update()
        {
            if (!IsRolling || Data == null) return;
            var progress = Data.duration == 0 ? 1 : Mathf.Clamp01((Time.unscaledTime - began) / Data.duration);
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
                dice[i].rectTransform.localRotation = rotations[i] * Quaternion.Euler(0, 0, Mathf.Sin(progress * 29 + i) * 15 * decay);
                dice[i].rectTransform.localPosition = positions[i] + Vector3.up * Mathf.Abs(Mathf.Sin(progress * 19 + i)) * 18 * decay;
            }
        }

        public void CompleteRoll()
        {
            if (IsRolling && Data != null) FinishFaces();
        }

        public IEnumerator PlayPresentation()
        {
            int version = bindingVersion;
            while (version == bindingVersion && IsRolling) yield return null;
            if (version != bindingVersion) yield break;
            if (PresentationError != null) throw PresentationError;
            if (!resultFeedback) throw new InvalidOperationException("Dice result presentation is missing.");
            while (version == bindingVersion && resultFeedback.IsPresenting) yield return null;
            if (version == bindingVersion && resultFeedback.Error != null) throw resultFeedback.Error;
        }

        void FinishFaces()
        {
            for (int i = 0; i < dice.Length; i++)
            {
                dice[i].rectTransform.localPosition = positions[i];
                dice[i].rectTransform.localRotation = rotations[i];
                dice[i].Render(Data?.values == null ? 0 : Data.values[i]);
            }
            result.text = Data?.result ?? "";
            IsRolling = false;
            try { if (resultFeedback && Data != null) resultFeedback.Show(Data); }
            catch (Exception error) { PresentationError = error; }
        }

        void RestorePose()
        {
            if (!hasPose) return;
            for (int i = 0; i < dice.Length; i++)
                if (dice[i]) { dice[i].rectTransform.localPosition = positions[i]; dice[i].rectTransform.localRotation = rotations[i]; }
            if (result) result.rectTransform.localScale = resultScale;
            hasPose = false;
        }

        protected override void OnUnbind()
        {
            bindingVersion++;
            RestorePose();
            if (resultFeedback) resultFeedback.ResetFeedback();
            IsRolling = false;
            if (result) result.rectTransform.localScale = resultScale;
            if (rollButton) rollButton.Unbind();
            if (title) title.text = "";
            if (detail) detail.text = "";
            if (result) result.text = "";
        }
    }
}
