using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    [Serializable]
    public sealed class UiPrefabReferences
    {
        public CommonButtonView commonButton;
        public ExplorationNodeView explorationNode;
        public ActionCardView actionCard;
        public FateCardView fateCard;
        public void Validate()
        {
            if (!commonButton || !explorationNode || !actionCard || !fateCard)
                throw new InvalidOperationException("Four reusable UI prefab references are required.");
        }
    }

    // Owns repeated items and the variable graph only. All screen scaffolding is prefab-authored.
    public sealed class FateDiceWidgets
    {
        public readonly Dictionary<string,Button> Buttons = new Dictionary<string,Button>();
        public RectTransform SafeRoot => context.manager.Root.safeArea;
        public RectTransform Body => layout.body;
        public Text Header => layout.header;
        public Text Stats => layout.stats;
        public Text Situation => layout.situation;
        public Text Fate => layout.fate;
        public Text Notice => layout.notice;
        readonly RunScreenLayout layout;
        RunUIContext context;
        readonly List<GameObject> items = new List<GameObject>();
        CampaignMapView campaignMap;
        RectTransform pulsingCard;
        Vector3 originalCardScale;
        Image pulsingImage;
        Color originalCardColor;

        public FateDiceWidgets(RunScreenLayout layout, RunUIContext context)
        {
            if (!layout) throw new ArgumentNullException(nameof(layout));
            layout.Validate(); this.layout = layout; BindContext(context);
        }
        public void BindContext(RunUIContext value)
        {
            if (value == null || !value.manager || !value.manager.Root || value.prefabs == null ||
                !value.visuals || value.presentation == null)
                throw new InvalidOperationException("Run UI requires its injected manager/root, four prefab originals, visual catalog and presentation settings.");
            value.prefabs.Validate(); context = value;
        }
        public void UsePresentation(PresentationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            context.presentation = settings;
        }
        public void UpdateSafeArea() => context.manager.Root.ApplySafeArea();
        public void SetLocked(bool locked) => context.manager.SetInputLocked(locked);

        public void Clear(bool preserveMap = false)
        {
            ResetSelectionFeedback();
            foreach (var item in items) DestroyOwned(item);
            items.Clear(); Buttons.Clear();
            if (!preserveMap) ClearMap();
        }

        public Button Choice(RectTransform parent, UIChoiceData choice, CommonButtonView prefab = null, ButtonAppearance appearance = null)
        {
            if (!parent) throw new InvalidOperationException("An authored choice container is missing.");
            if (choice == null) return null;
            parent.gameObject.SetActive(true);
            var view = UnityEngine.Object.Instantiate(prefab ? prefab : context.prefabs.commonButton, parent, false);
            items.Add(view.gameObject);
            view.Bind(choice.key, choice.text, null, "", appearance ?? context.visuals.ResolveButton(choice.purpose),
                choice.interactable, choice.selected, _ => choice.clicked?.Invoke());
            if (choice.purpose == ButtonPurpose.Die || choice.purpose == ButtonPurpose.Grade)
            {
                // Only compact purpose-specific label placement changes; authored font/style and right/top/bottom padding survive.
                view.icon.transform.parent.gameObject.SetActive(false);
                var inset = view.label.rectTransform.offsetMin;
                inset.x = -view.label.rectTransform.offsetMax.x;
                view.label.rectTransform.offsetMin = inset;
                view.label.alignment = TextAnchor.MiddleCenter;
                view.label.fontSize = Math.Min(view.label.fontSize, choice.purpose == ButtonPurpose.Die ? 22 : 21);
            }
            Buttons[choice.key] = view.button;
            return view.button;
        }

        public void Choices(RectTransform parent, UIChoiceData[] choices)
        {
            if (!parent) throw new InvalidOperationException("An authored choices container is missing.");
            bool present = choices != null && choices.Any(x => x != null);
            parent.gameObject.SetActive(present);
            if (present) foreach (var choice in choices) Choice(parent, choice);
        }

        public void ShowDice(int[] values, Action<int> clicked = null, CommonButtonView prefab = null, ButtonAppearance appearance = null)
        {
            // Also supports refreshing a die choice within the same binding without touching authored children.
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item && item.transform.parent == layout.diceRow)
                { DestroyOwned(item); items.RemoveAt(i); }
            }
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                int value = values != null && values.Length == 6 ? values[i] : 0;
                Choice(layout.diceRow, new UIChoiceData {
                    key = "die-" + i, text = value == 0 ? "?" : Pips(value),
                    clicked = () => clicked?.Invoke(index), interactable = clicked != null, purpose = ButtonPurpose.Die
                }, prefab, appearance);
            }
        }

        // Membership is supplied by the controller: the map displays public IDs and edges only.
        public void ShowMap(RectTransform parent, IReadOnlyList<NodeState> active, IReadOnlyList<string> available,
            string selected, Action<string> choose, IReadOnlyList<NodeState> completed = null)
        {
            if (!parent) throw new InvalidOperationException("The authored map container is missing.");
            var authored = parent.GetComponent<CampaignMapView>();
            if (!authored) throw new InvalidOperationException("The map container requires its authored CampaignMapView.");
            if (campaignMap && campaignMap != authored) ClearMap();
            campaignMap = authored;
            campaignMap.Bind(active, available, selected, context.prefabs.explorationNode, context.visuals, choose, completed);
            foreach (var id in available)
                if (campaignMap.Nodes.TryGetValue(id, out var view)) Buttons["node-" + id] = view.frame.button;
        }

        public IEnumerator AnimateNodeArrival(string id, float seconds)
        {
            if (campaignMap) yield return campaignMap.AnimateNodeArrival(id, seconds);
        }
        public IEnumerator AnimateCardSelection(string key, float seconds)
        {
            ResetSelectionFeedback();
            if (!Buttons.TryGetValue(key, out var button) || !button) yield break;
            pulsingCard = button.transform as RectTransform;
            if (!pulsingCard) yield break;
            originalCardScale = pulsingCard.localScale;
            pulsingImage = button.GetComponent<CommonButtonView>()?.background;
            if (pulsingImage) originalCardColor = pulsingImage.color;
            float began = Time.unscaledTime;
            try
            {
                while (pulsingCard && Time.unscaledTime - began < seconds)
                {
                    float pulse = Mathf.Sin(Mathf.Clamp01((Time.unscaledTime - began) / Mathf.Max(.01f, seconds)) * Mathf.PI);
                    pulsingCard.localScale = originalCardScale * (1 + .07f * pulse);
                    if (pulsingImage) pulsingImage.color = Color.Lerp(originalCardColor, Color.white, .35f * pulse);
                    yield return null;
                }
            }
            finally { ResetSelectionFeedback(); }
        }
        public void ResetSelectionFeedback()
        {
            if (pulsingCard) pulsingCard.localScale = originalCardScale;
            if (pulsingImage) pulsingImage.color = originalCardColor;
            pulsingCard = null; pulsingImage = null;
        }
        public Button ActionCard(RectTransform parent, ActionOfferUIData card, ButtonAppearance appearance = null)
        {
            if (!parent || card == null) throw new ArgumentException("An authored action container and display offer are required.");
            var view = UnityEngine.Object.Instantiate(context.prefabs.actionCard, parent, false); items.Add(view.gameObject);
            view.Bind(card.id, card.originalId, card.label, card.grade, card.effect, string.Join(" • ", (card.tags ?? Array.Empty<string>()).Select(KoreanText.Tag)),
                card.visual, context.visuals.ResolveGrade(card.grade), GradeColor(card.grade),
                appearance ?? context.visuals.ResolveButton(ButtonPurpose.Primary), card.clicked);
            Buttons["card-" + card.id] = view.frame.button; return view.frame.button;
        }
        public Button FateCard(RectTransform parent, FateOfferUIData card)
        {
            if (!parent || card == null) throw new ArgumentException("An authored fate container and public offer are required.");
            var view = UnityEngine.Object.Instantiate(context.prefabs.fateCard, parent, false); items.Add(view.gameObject);
            view.Bind(card.id, card.type, card.grade, context.visuals.ResolveFate(card.type), context.visuals.ResolveGrade(card.grade),
                GradeColor(card.grade), context.visuals.ResolveButton(ButtonPurpose.Primary), card.clicked);
            Buttons["fate-" + card.id] = view.frame.button; return view.frame.button;
        }
        public Color GradeColor(Grade grade) => context.presentation.gradeColors[(int)grade];

        void ClearMap()
        {
            if (campaignMap) campaignMap.Clear();
            campaignMap = null;
        }
        static void DestroyOwned(GameObject target)
        {
            if (!target) return;
            foreach (var view in target.GetComponentsInChildren<ExplorationNodeView>(true)) view.Unbind();
            foreach (var view in target.GetComponentsInChildren<ActionCardView>(true)) view.Unbind();
            foreach (var view in target.GetComponentsInChildren<FateCardView>(true)) view.Unbind();
            foreach (var view in target.GetComponentsInChildren<CommonButtonView>(true)) view.Unbind();
            target.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
        static string Pips(int value)
        {
            switch (value) {
                case 1: return "  •  "; case 2: return "•    \n    •"; case 3: return "•    \n  •  \n    •";
                case 4: return "•   •\n\n•   •"; case 5: return "•   •\n  •  \n•   •"; case 6: return "•   •\n•   •\n•   •";
                default: return "?";
            }
        }
    }
}
