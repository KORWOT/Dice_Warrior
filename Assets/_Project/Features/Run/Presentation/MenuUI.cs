using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class MenuUI : RunScreenView<MenuUIData>
    {
        public Text trialHeading, capHeading, seedHeading, error, lastResult;
        public RectTransform trialChoices, capChoices, mainChoices;
        public InputField seedInput;
        public Text characterName, characterDetails, growthDetails;
        public RectTransform characterPanel, settingsPanel, growthPanel;
        public Button characterTab, settingsTab, growthTab;
        public int layoutVersion;
        public int metaLayoutVersion;
        public RectTransform metaCharacters, metaLoadout, metaGrowth;
        public Text legacyGrowthNotice;
        [Header("로비 선택 색상")]
        public Color accent = new Color(.75f, .63f, .39f);
        public Color buttonNormal = new Color(.055f, .078f, .105f);
        public Color buttonSelected = new Color(.23f, .19f, .115f);
        public Color buttonPressed = new Color(.30f, .25f, .15f);
        public Color buttonDisabled = new Color(.04f, .052f, .066f);
        public Color primaryButton = new Color(.18f, .145f, .085f);
        public Color secondaryBorder = new Color(.22f, .28f, .34f);
        public Color unselectedTabText = new Color(.70f, .75f, .80f);

        UnityAction<string> seedListener;
        UnityAction characterListener, settingsListener, growthListener;
        int selectedTab;

        protected override void BindHUD(MenuUIData data)
        {
            SetText(layout.header, data.hud.header);
            SetText(layout.notice, data.hud.notice);
            // The lobby presents preparation information in its own authored panels.
            ClearText(layout.stats); ClearText(layout.situation); ClearText(layout.fate); ClearText(layout.gear);
            layout.diceRow.gameObject.SetActive(false);
            layout.footer.gameObject.SetActive(true);
        }

        protected override void BindScreen(MenuUIData data)
        {
            if (!trialHeading || !capHeading || !seedHeading || !trialChoices || !capChoices || !mainChoices || !seedInput ||
                !characterName || !characterDetails || !growthDetails || !characterPanel || !settingsPanel || !growthPanel ||
                !characterTab || !settingsTab || !growthTab)
                throw new InvalidOperationException("MenuUI requires its authored preparation panels, tabs and existing choice references.");

            DetachListeners();
            seedInput.SetTextWithoutNotify(data.seed ?? "");
            if (data.seedChanged != null)
            {
                var changed = data.seedChanged;
                seedListener = value => changed(value);
                seedInput.onValueChanged.AddListener(seedListener);
            }
            // Keep the serialized field for existing integrations; seed editing belongs to the Editor workbench.
            seedInput.gameObject.SetActive(false);
            seedHeading.gameObject.SetActive(false);
            SetText(characterName, data.characterName);
            SetText(characterDetails, data.characterDetails);
            SetText(growthDetails, data.growthDetails);
            AddChoices(trialChoices, data.trials);
            AddChoices(capChoices, data.caps);
            AddChoices(mainChoices, new[] { data.start, data.resume, data.archive });
            if (data.metaEnabled && (!metaCharacters || !metaLoadout || !metaGrowth))
                throw new InvalidOperationException("MenuUI requires authored meta preparation containers.");
            if (metaCharacters) AddChoices(metaCharacters, data.characters);
            if (metaLoadout) AddChoices(metaLoadout, data.loadoutChoices);
            if (metaGrowth) AddChoices(metaGrowth, data.growthChoices);
            if (legacyGrowthNotice) legacyGrowthNotice.gameObject.SetActive(!data.metaEnabled);
            SetText(error, data.error); SetText(lastResult, data.lastResult);

            characterListener = () => SelectTabFromInput(characterTab, 0);
            settingsListener = () => SelectTabFromInput(settingsTab, 1);
            growthListener = () => SelectTabFromInput(growthTab, 2);
            characterTab.onClick.AddListener(characterListener);
            settingsTab.onClick.AddListener(settingsListener);
            growthTab.onClick.AddListener(growthListener);
            // Rebinding a selected trial/cap stays in Settings; closing the screen resets this index.
            DisplayTab(selectedTab, false);
        }

        void AddChoices(RectTransform parent, UIChoiceData[] choices)
        {
            bool any = false;
            if (choices != null)
            {
                foreach (var choice in choices)
                {
                    if (choice == null) continue;
                    any = true;
                    var appearance = new ButtonAppearance
                    {
                        purpose = choice.purpose,
                        normal = choice.purpose == ButtonPurpose.Primary ? primaryButton : buttonNormal,
                        selected = buttonSelected, pressed = buttonPressed, disabled = buttonDisabled
                    };
                    var button = Widgets.Choice(parent, choice, appearance: appearance);
                    var frame = button.GetComponent<CommonButtonView>();
                    frame.SetBorder(null, choice.selected || choice.purpose == ButtonPurpose.Primary ? accent : secondaryBorder);
                }
            }
            parent.gameObject.SetActive(any);
        }

        void SelectTabFromInput(Button source, int index)
        {
            if (!IsOpen || !isActiveAndEnabled || !source || !source.isActiveAndEnabled || !source.IsInteractable()) return;
            DisplayTab(index, true);
        }

        void DisplayTab(int index, bool resetScroll)
        {
            selectedTab = index;
            characterPanel.gameObject.SetActive(index == 0);
            settingsPanel.gameObject.SetActive(index == 1);
            growthPanel.gameObject.SetActive(index == 2);
            ColorTab(characterTab, index == 0);
            ColorTab(settingsTab, index == 1);
            ColorTab(growthTab, index == 2);
            if (!resetScroll) return;
            layout.scroll.StopMovement();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout.body);
            layout.scroll.verticalNormalizedPosition = 1;
        }

        void ColorTab(Button button, bool selected)
        {
            var colors = button.colors;
            colors.normalColor = selected ? buttonSelected : buttonNormal;
            colors.highlightedColor = colors.selectedColor = buttonSelected;
            colors.pressedColor = buttonPressed;
            colors.disabledColor = buttonDisabled;
            button.colors = colors;
            var label = button.GetComponentInChildren<Text>(true);
            if (label) label.color = selected ? accent : unselectedTabText;
        }

        protected override void UnbindScreen()
        {
            DetachListeners();
            if (seedInput) seedInput.SetTextWithoutNotify("");
            ClearText(error); ClearText(lastResult);
            ClearText(characterName); ClearText(characterDetails); ClearText(growthDetails);
            selectedTab = 0;
            if (characterPanel) characterPanel.gameObject.SetActive(false);
            if (settingsPanel) settingsPanel.gameObject.SetActive(false);
            if (growthPanel) growthPanel.gameObject.SetActive(false);
        }

        void DetachListeners()
        {
            if (seedInput && seedListener != null) seedInput.onValueChanged.RemoveListener(seedListener);
            if (characterTab && characterListener != null) characterTab.onClick.RemoveListener(characterListener);
            if (settingsTab && settingsListener != null) settingsTab.onClick.RemoveListener(settingsListener);
            if (growthTab && growthListener != null) growthTab.onClick.RemoveListener(growthListener);
            seedListener = null; characterListener = null; settingsListener = null; growthListener = null;
        }

        void OnDestroy() => DetachListeners();
    }
}
