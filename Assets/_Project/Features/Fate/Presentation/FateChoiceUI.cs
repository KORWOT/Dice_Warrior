using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class FateChoiceUI : BaseUI<FateChoiceUIData>
    {
        public RectTransform panel, cardContent, diceRow;
        public ScrollRect cardScroll;
        public Text title, instructions, status;
        public FateCardView cardPrefab;
        public CommonButtonView confirmButton, closeButton;
        public CommonButtonView[] rerollButtons;
        public DiceFaceView[] rerollFaces;
        [Min(0)] public float exitSeconds = .2f;
        [HideInInspector] public int layoutVersion;
        public string SelectedId { get; private set; }
        public int BindingVersion { get; private set; }
        public IReadOnlyList<FateCardView> Cards => readOnlyCards ?? (readOnlyCards = cards.AsReadOnly());
        readonly List<FateCardView> cards = new List<FateCardView>();
        ReadOnlyCollection<FateCardView> readOnlyCards;
        bool dispatched, captured;
        int playbackVersion;
        Vector3 originalPosition, originalScale;
        float originalAlpha;
        SelectionFeedback activeFeedback;

        protected override void OnBind(FateChoiceUIData data)
        {
            ResetPresentation();
            BindingVersion++;
            ClearCards();
            SelectedId = null; dispatched = false;
            if (!panel || !cardContent || !cardScroll || !cardPrefab || !title || !instructions || !status ||
                !confirmButton || !closeButton || !diceRow || rerollButtons == null || rerollButtons.Length != 6 ||
                rerollFaces == null || rerollFaces.Length != 6)
                throw new InvalidOperationException("FateChoiceUI requires its authored layout, cards, buttons and six reroll faces.");
            if (data.context == null || !data.context.visuals || data.context.presentation?.gradeColors == null ||
                data.context.presentation.gradeColors.Length != 5 || data.offers == null || data.offers.Length == 0 || data.offers.Length > 5)
                throw new ArgumentException("Fate choice requires public context and one to five offers.");
            if (float.IsNaN(exitSeconds) || float.IsInfinity(exitSeconds) || exitSeconds < 0)
                throw new InvalidOperationException("Fate popup exit seconds must be finite and nonnegative.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var offer in data.offers)
                if (offer == null || string.IsNullOrWhiteSpace(offer.id) || !ids.Add(offer.id))
                    throw new ArgumentException("Fate offer IDs must be present and unique.");

            originalPosition = panel.localPosition;
            originalScale = panel.localScale;
            originalAlpha = group.alpha;
            captured = true;
            title.text = "운명을 선택하세요";
            status.text = data.hud?.fate ?? "";
            instructions.text = data.offers.Length > 3 ? "좌우로 넘겨 살펴보고 하나를 선택하세요" : "카드를 살펴보고 하나를 선택하세요";
            int version = BindingVersion;
            var visuals = data.context.visuals;
            foreach (var offer in data.offers)
            {
                var card = Instantiate(cardPrefab, cardContent, false);
                cards.Add(card);
                card.Bind(offer.id, offer.type, offer.grade, visuals.ResolveFate(offer.type), visuals.ResolveGrade(offer.grade),
                    data.context.presentation.gradeColors[(int)offer.grade], visuals.ResolveButton(ButtonPurpose.Primary),
                    id => Select(id, version));
            }
            cardScroll.horizontalNormalizedPosition = 0;
            BindConfirm(version);
            closeButton.Bind("fate-close", "×", null, "", visuals.ResolveButton(ButtonPurpose.Navigation), data.close != null, false,
                _ => DispatchClose(version));
            bool reroll = data.hud?.dieClicked != null && data.hud.dice != null && data.hud.dice.Length == 6;
            diceRow.gameObject.SetActive(reroll);
            for (int i = 0; i < 6; i++)
            {
                if (!rerollButtons[i] || !rerollFaces[i]) throw new InvalidOperationException("A reroll button or die face is missing.");
                rerollButtons[i].Unbind();
                if (!reroll) continue;
                int index = i;
                rerollButtons[i].Bind("fate-die-" + i, "", null, "", visuals.ResolveButton(ButtonPurpose.Die), true, false,
                    _ => DispatchReroll(index, version));
                rerollButtons[i].label.gameObject.SetActive(false);
                rerollButtons[i].icon.gameObject.SetActive(false);
                rerollButtons[i].iconFallback.gameObject.SetActive(false);
                rerollFaces[i].Render(data.hud.dice[i]);
            }
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (cardContent) LayoutRebuilder.ForceRebuildLayoutImmediate(cardContent);
        }

        void Select(string id, int version)
        {
            if (!IsCurrent(version) || dispatched || !cards.Exists(card => card.OfferedId == id)) return;
            SelectedId = id;
            foreach (var card in cards) card.SetSelected(card.OfferedId == id);
            instructions.text = "선택한 운명으로 진행할 준비가 되었습니다";
            BindConfirm(version);
        }

        void BindConfirm(int version)
        {
            confirmButton.Bind("fate-confirm", "선택한 운명으로 진행", null, "", Data.context.visuals.ResolveButton(ButtonPurpose.Primary),
                !dispatched && SelectedId != null && Data.confirm != null, SelectedId != null, _ => DispatchConfirm(version));
        }

        bool IsCurrent(int version) => IsOpen && isActiveAndEnabled && version == BindingVersion;
        void DispatchConfirm(int version)
        {
            if (!IsCurrent(version) || dispatched || SelectedId == null || Data.confirm == null) return;
            string selected = SelectedId;
            var callback = Data.confirm;
            LockRequest();
            callback(selected);
        }
        void DispatchClose(int version)
        {
            if (!IsCurrent(version) || dispatched || Data.close == null) return;
            var callback = Data.close;
            LockRequest();
            callback();
        }
        void DispatchReroll(int index, int version)
        {
            if (!IsCurrent(version) || dispatched || Data.hud?.dieClicked == null) return;
            var callback = Data.hud.dieClicked;
            SelectedId = null;
            foreach (var card in cards) card.SetSelected(false);
            LockRequest();
            callback(index);
        }
        void LockRequest()
        {
            dispatched = true;
            foreach (var card in cards) SetInput(card.frame, false);
            SetInput(confirmButton, false); SetInput(closeButton, false);
            foreach (var button in rerollButtons) SetInput(button, false);
        }
        static void SetInput(CommonButtonView view, bool value)
        {
            if (!view) return;
            view.button.interactable = value;
            view.group.interactable = value;
            view.group.blocksRaycasts = value;
        }

        // Capture ownership now, even if the caller starts this iterator on a later frame.
        public IEnumerator PlaySelection(string id)
        {
            if (float.IsNaN(exitSeconds) || float.IsInfinity(exitSeconds) || exitSeconds < 0)
                throw new InvalidOperationException("Fate popup exit seconds must be finite and nonnegative.");
            ResetPresentation();
            if (panel && group)
            {
                originalPosition = panel.localPosition; originalScale = panel.localScale; originalAlpha = group.alpha;
                captured = true;
            }
            return PlaySelectionBound(id, BindingVersion, playbackVersion);
        }
        IEnumerator PlaySelectionBound(string id, int binding, int playback)
        {
            if (binding != BindingVersion || playback != playbackVersion || !IsOpen) yield break;
            var card = cards.Find(value => value.OfferedId == id);
            if (!card) throw new ArgumentException("The selected fate offer is no longer displayed.", nameof(id));
            activeFeedback = card.frame.GetComponent<SelectionFeedback>();
            if (!activeFeedback) throw new InvalidOperationException("The authored fate card requires SelectionFeedback.");
            var sequence = activeFeedback.Play();
            bool completed = false;
            try
            {
                while (binding == BindingVersion && playback == playbackVersion && IsOpen && sequence.MoveNext())
                    yield return sequence.Current;
                if (binding != BindingVersion || playback != playbackVersion || !IsOpen) yield break;
                (sequence as IDisposable)?.Dispose(); sequence = null;
                activeFeedback = null;
                float began = Time.unscaledTime;
                while (binding == BindingVersion && playback == playbackVersion && IsOpen)
                {
                    float progress = exitSeconds <= 0 ? 1 : Mathf.Clamp01((Time.unscaledTime - began) / exitSeconds);
                    panel.localPosition = originalPosition + Vector3.down * (68 * progress);
                    group.alpha = originalAlpha * (1 - progress);
                    if (progress >= 1) { completed = true; yield break; }
                    yield return null;
                }
            }
            finally
            {
                // Reset/rebind already stopped the old player. Its old iterator must not stop a new playback.
                if (binding == BindingVersion && playback == playbackVersion)
                {
                    (sequence as IDisposable)?.Dispose();
                    if (activeFeedback) activeFeedback.ResetFeedback();
                    activeFeedback = null;
                    if (!completed) ResetPresentation();
                }
            }
        }

        public void ResetPresentation()
        {
            playbackVersion++;
            if (activeFeedback) activeFeedback.ResetFeedback();
            activeFeedback = null;
            if (!captured) return;
            if (panel) { panel.localPosition = originalPosition; panel.localScale = originalScale; }
            if (group) group.alpha = originalAlpha;
            captured = false;
        }
        void ClearCards()
        {
            foreach (var card in cards)
            {
                if (!card) continue;
                card.Unbind(); card.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(card.gameObject); else DestroyImmediate(card.gameObject);
            }
            cards.Clear();
        }
        protected override void OnUnbind()
        {
            BindingVersion++;
            ResetPresentation(); ClearCards();
            SelectedId = null; dispatched = false;
            if (confirmButton) confirmButton.Unbind();
            if (closeButton) closeButton.Unbind();
            if (rerollButtons != null) foreach (var button in rerollButtons) if (button) button.Unbind();
        }
        void OnDisable() => ResetPresentation();
        void OnEnable() => RefreshLayout();
    }
}
