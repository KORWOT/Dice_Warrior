using System;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    // Only displays committed data. FEEL owns the banner scale, Text Animator the glyph mesh.
    public sealed class DiceResultFeedback : MonoBehaviour
    {
        public TMP_Text comboName;
        public TextAnimator_TMP textAnimator;
        public MMF_Player reveal;
        public Image ribbon;
        public DiceFeedbackCatalog catalog;
        public DiceAuraGraphic[] dieAuras;
        public DiceAuraGraphic crest;
        [HideInInspector] public float entranceSeconds = .32f; // Legacy prefab compatibility.
        public enum ResultPhase { Idle, Entrance, Flourish, Reading, Exit, Completed }
        public ResultPhase Phase { get; private set; }
        public bool IsPresenting { get; private set; }
        public bool IsPlaying => IsPresenting && Phase != ResultPhase.Reading;
        public Exception Error { get; private set; }
        public double StartedAt { get; private set; }
        public double EndedAt { get; private set; }
        Vector3 originalScale;
        Color originalNameColor, originalRibbonColor;
        bool captured;
        string plainName = "";
        DiceRollUIData pending;
        Material originalMaterial, instanceMaterial;
        double phaseBegan;
        float readDuration;
        DiceEffectTimeline timeline;
        int[] groups;
        DiceHandFeedbackStyle auraStyle;
        float auraStrength;

        public void Show(DiceRollUIData data)
        {
            ResetFeedback();
            if (!comboName || !textAnimator || !reveal || !ribbon || !catalog)
                throw new InvalidOperationException("Dice result feedback requires authored text, player, ribbon and styles.");
            Capture();
            plainName = data.comboName ?? "";
            if (plainName.Length == 0) return;
            var style = catalog.Resolve(data.hand);
            comboName.color = style.color;
            ribbon.color = new Color(style.color.r, style.color.g, style.color.b, .22f);
            groups = data.values == null ? null : DiceComboHighlights.Groups(data.hand, data.values);
            auraStyle = style;
            auraStrength = data.comboStrength;
            timeline = catalog.ResolveTimeline(data.comboStrength);
            SetAuras(1);
            SetPlainText();
            if (!Application.isPlaying || data.holdSeconds <= 0) return;
            timeline.Duration(data.holdSeconds); // Validate before an Update can run.
            readDuration = Mathf.Max(timeline.readSeconds, data.holdSeconds);
            IsPresenting = true;
            StartedAt = Time.realtimeSinceStartupAsDouble;
            EndedAt = double.NaN;
            if (!isActiveAndEnabled) { pending = data; return; }
            PlayMotion(data, style);
        }

        void OnEnable()
        {
            if (pending == null) return;
            var data = pending; pending = null;
            try { PlayMotion(data, catalog.Resolve(data.hand)); }
            catch (Exception error) { Fail(error); }
        }

        void PlayMotion(DiceRollUIData data, DiceHandFeedbackStyle style)
        {

            textAnimator.SetAppearancesActive(false);
            textAnimator.SetBehaviorsActive(true);
            string effect = style.textEffect;
            if (!string.IsNullOrEmpty(effect))
                textAnimator.SetText("<" + effect + ">" + plainName + "</" + effect + ">", false);
            textAnimator.SetVisibilityEntireText(true, false);

            // Initialize against this reveal's current authored pose; never change a shared material.
            reveal.Initialization();
            foreach (var feedback in reveal.FeedbacksList)
                if (feedback is MMF_Scale scale)
                    scale.RemapCurveOne = style.punchScale * Mathf.Lerp(.65f, 1.25f, Mathf.Clamp01(data.comboStrength));
            // The actual player's completion is required; authored extra feedback is never cut off by a guessed delay.
            reveal.DurationMultiplier = timeline.entranceSeconds / .32f;
            if (timeline.entranceSeconds > 0) reveal.PlayFeedbacks();
            originalMaterial = ribbon.material;
            instanceMaterial = new Material(originalMaterial);
            ribbon.material = instanceMaterial;
            instanceMaterial.SetFloat("_ShineLocation", 0);
            EnterPhase(ResultPhase.Entrance);
            SetAuras(0, 0);
        }

        void Update()
        {
            if (!IsPresenting || pending != null || Error != null) return;
            try { Advance(); }
            catch (Exception error) { Fail(error); }
        }

        void Advance()
        {
            // Zero-length stages complete synchronously, while real stages own their animation progress.
            for (int step = 0; step < 4 && IsPresenting; step++)
            {
                float seconds = Phase == ResultPhase.Entrance ? timeline.entranceSeconds :
                    Phase == ResultPhase.Flourish ? timeline.flourishSeconds : Phase == ResultPhase.Reading ? readDuration : timeline.exitSeconds;
                float t = seconds <= 0 ? 1 : Mathf.Clamp01((float)(Time.realtimeSinceStartupAsDouble - phaseBegan) / seconds);
                if (Phase == ResultPhase.Entrance)
                {
                    SetAuras(t * .3f, Mathf.SmoothStep(0, 1, t), true);
                    if (instanceMaterial) instanceMaterial.SetFloat("_ShineLocation", t);
                    if (t < 1 || reveal.IsPlaying) return;
                    StopMotion();
                    EnterPhase(ResultPhase.Flourish);
                }
                else if (Phase == ResultPhase.Flourish)
                {
                    SetAuras(.3f + t * .7f);
                    float wave = Mathf.Sin(t * Mathf.PI * (timeline.complexity == DiceEffectComplexity.Legendary ? 6 : 2));
                    transform.localScale = originalScale * (1 + .025f * (int)timeline.complexity * wave * Mathf.Sin(t * Mathf.PI));
                    if (t < 1) return;
                    transform.localScale = originalScale;
                    SetPlainText();
                    SetAuras(1);
                    EnterPhase(ResultPhase.Reading);
                }
                else if (Phase == ResultPhase.Reading)
                {
                    if (t < 1) return;
                    EnterPhase(ResultPhase.Exit);
                }
                else if (Phase == ResultPhase.Exit)
                {
                    SetAuras(1, 1 - Mathf.SmoothStep(0, 1, t));
                    comboName.color = new Color(auraStyle.color.r, auraStyle.color.g, auraStyle.color.b, auraStyle.color.a * (1 - t));
                    ribbon.color = new Color(auraStyle.color.r, auraStyle.color.g, auraStyle.color.b, .22f * (1 - t));
                    if (t < 1) return;
                    Phase = ResultPhase.Completed;
                    IsPresenting = false;
                    EndedAt = Time.realtimeSinceStartupAsDouble;
                    return;
                }
                else return;
            }
        }

        void EnterPhase(ResultPhase value) { Phase = value; phaseBegan = Time.realtimeSinceStartupAsDouble; }

        void Fail(Exception error)
        {
            Error = error;
            StopMotion();
            IsPresenting = false;
            EndedAt = Time.realtimeSinceStartupAsDouble;
        }

        void SetAuras(float phase, float opacity = 1, bool arriving = false)
        {
            if (auraStyle == null || groups == null) return;
            var complexity = timeline.complexity;
            if (crest)
            {
                if (complexity == DiceEffectComplexity.Simple) crest.Clear();
                else crest.SetVisual(auraStyle.color, auraStyle.accentColor, auraStrength, phase, complexity, opacity);
            }
            if (dieAuras == null) return;
            for (int i = 0; i < dieAuras.Length; i++)
            {
                if (!dieAuras[i]) continue;
                if (i >= groups.Length || groups[i] < 0) { dieAuras[i].Clear(); continue; }
                var primary = auraStyle.alternateGroups && groups[i] % 2 == 1 ? auraStyle.accentColor : auraStyle.color;
                float light = arriving && complexity != DiceEffectComplexity.Simple ? Mathf.Clamp01(opacity * 2 - i * .16f) : opacity;
                dieAuras[i].SetVisual(primary, auraStyle.accentColor, auraStrength, phase, complexity, light);
            }
        }

        void Capture()
        {
            if (captured) return;
            originalScale = transform.localScale;
            originalNameColor = comboName.color;
            originalRibbonColor = ribbon.color;
            captured = true;
        }

        void SetPlainText()
        {
            // Preview binds before its Canvas is active; let TMP build after OnEnable.
            if (!Application.isPlaying)
            {
                if (comboName) { comboName.text = plainName; comboName.renderMode = TextRenderFlags.Render; }
                return;
            }
            if (!textAnimator) return;
            textAnimator.SetBehaviorsActive(false);
            textAnimator.SetAppearancesActive(false);
            textAnimator.SetText(plainName, false);
            textAnimator.SetVisibilityEntireText(true, false);
            textAnimator.ScheduleMeshRefresh();
            textAnimator.Animate(0f);
        }

        void StopMotion()
        {
            bool restore = captured;
            if (reveal) { reveal.StopFeedbacks(true); if (restore) reveal.RestoreInitialValues(); }
            if (captured) transform.localScale = originalScale;
            if (instanceMaterial)
            {
                ribbon.material = originalMaterial;
                Destroy(instanceMaterial);
                instanceMaterial = null;
            }
        }

        public void ResetFeedback()
        {
            StopMotion();
            if (IsPresenting) EndedAt = Time.realtimeSinceStartupAsDouble;
            IsPresenting = false;
            Phase = ResultPhase.Idle;
            Error = null;
            groups = null; auraStyle = null;
            if (crest) crest.Clear();
            if (dieAuras != null) foreach (var aura in dieAuras) if (aura) aura.Clear();
            pending = null;
            plainName = "";
            SetPlainText();
            if (!captured) return;
            comboName.color = originalNameColor;
            ribbon.color = originalRibbonColor;
            captured = false;
        }

        void OnDisable() => ResetFeedback();
    }
}
