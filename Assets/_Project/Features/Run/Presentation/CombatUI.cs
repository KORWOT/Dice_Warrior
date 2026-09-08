using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class CombatUI : RunScreenView<CombatUIData>
    {
        public RectTransform artworkRoot, rollChoices, actionChoices, arena;
        public Image artwork, enemyHealthFill, playerHealthFill;
        public Text artworkFallback, turn, enemyName, enemyHealth, enemyShield, intentValue;
        public Text playerName, playerShield, playerAttributes, fatePower, rerolls;
        public CommonButtonView menuButton, diePrefab;
        public ScrollRect actionScroll;
        public GridLayoutGroup actionGrid;
        public CombatStageGraphic stageGraphic;
        public int layoutVersion;
        public Text actionFeedback, damageFeedback;
        public Image hitFlash;
        [Range(.1f, 2)] public float actionFeedbackSeconds = .38f;
        [Range(0, 24)] public float shakePixels = 9;
        [Range(0, 2)] public float feedbackHoldSeconds = .25f;
        public int feedbackVersion;
        public bool IsFeedbackPlaying { get; private set; }
        Vector2 restingArena;
        Vector3 restingDamageScale;
        Vector2 restingDamagePosition;
        Color restingDamageColor, restingActionColor;
        int actionCount;

        static readonly Color Red = new Color(.77f, .27f, .23f);
        static readonly Color Blue = new Color(.25f, .56f, .76f);
        static ButtonAppearance Palette(bool defensive) => new ButtonAppearance
        {
            purpose = ButtonPurpose.Primary,
            normal = defensive ? new Color(.032f, .068f, .091f) : new Color(.10f, .033f, .03f),
            selected = defensive ? new Color(.10f, .22f, .29f) : new Color(.29f, .085f, .06f),
            pressed = defensive ? new Color(.15f, .32f, .41f) : new Color(.40f, .13f, .09f),
            disabled = new Color(.035f, .041f, .05f)
        };
        static ButtonAppearance DicePalette() => new ButtonAppearance
        {
            purpose = ButtonPurpose.Die, normal = new Color(.09f, .22f, .32f),
            selected = new Color(.20f, .40f, .53f), pressed = new Color(.25f, .49f, .62f),
            disabled = new Color(.07f, .17f, .25f)
        };

        protected override void BindHUD(CombatUIData data)
        {
            ResetFeedback();
            if (!menuButton || !diePrefab || !enemyHealthFill || !playerHealthFill || !arena)
                throw new InvalidOperationException("CombatUI requires its dedicated header, health bars, arena and dice prefab.");
            SetText(layout.header, data.battleLabel); SetText(layout.stats, data.playerHealth);
            SetText(layout.situation, data.intentLabel); SetText(layout.fate, data.handLabel);
            SetText(layout.notice, data.hud.notice); ClearText(layout.gear);
            SetText(turn, data.turnLabel); SetText(enemyName, data.enemyName);
            SetText(enemyHealth, data.enemyHealth); SetText(enemyShield, data.enemyShield);
            SetText(intentValue, data.intentValue); SetText(playerName, data.playerName);
            SetText(playerShield, data.playerShield); SetText(playerAttributes, data.playerAttributes);
            SetText(fatePower, data.fatePowerLabel); SetText(rerolls, data.rerollLabel);
            SetHealth(enemyHealthFill, data.enemyHealth01); SetHealth(playerHealthFill, data.playerHealth01);
            var menu = data.hud.menu;
            menuButton.gameObject.SetActive(menu != null);
            if (menu != null)
            {
                menuButton.Bind(menu.key, menu.text, null, "", Palette(true), menu.interactable, false, _ => menu.clicked?.Invoke());
                Widgets.Buttons[menu.key] = menuButton.button;
            }
            Widgets.ShowDice(data.hud.dice, data.hud.dieClicked, diePrefab, DicePalette());
        }

        protected override void BindScreen(CombatUIData data)
        {
            if (!rollChoices || !actionChoices || !actionScroll || !actionGrid || !stageGraphic)
                throw new InvalidOperationException("CombatUI requires its authored action tray and stage graphic.");
            SetArtwork(artworkRoot, artwork, artworkFallback, data.revealed);
            stageGraphic.showFigures = !artwork.sprite;
            stageGraphic.SetVerticesDirty();
            artworkFallback.gameObject.SetActive(false);
            var roll = data.roll;
            rollChoices.gameObject.SetActive(roll != null);
            if (roll != null) Widgets.Choice(rollChoices, roll, appearance: Palette(true));
            actionCount = data.actions?.Length ?? 0;
            actionScroll.gameObject.SetActive(actionCount > 0);
            actionChoices.gameObject.SetActive(actionCount > 0);
            if (data.actions != null)
            {
                foreach (var offer in data.actions)
                {
                    var defensive = offer.tags != null && offer.tags.Contains("defense");
                    var button = Widgets.ActionCard(actionChoices, offer, Palette(defensive));
                    var view = button.GetComponent<ActionCardView>();
                    view.frame.SetBorder(data.context.visuals.ResolveGrade(offer.grade).border, defensive ? Blue : Red);
                }
            }
            ResizeCards();
            actionScroll.horizontalNormalizedPosition = 0;
        }

        void LateUpdate() => ResizeCards();
        void ResizeCards()
        {
            if (!actionGrid || !actionScroll || !actionScroll.viewport || actionCount == 0) return;
            int visible = Math.Min(3, actionCount);
            float width = (actionScroll.viewport.rect.width - actionGrid.spacing.x * (visible - 1) - actionGrid.padding.horizontal) / visible;
            var size = actionGrid.cellSize;
            if (width > 0 && Mathf.Abs(size.x - width) > .1f)
                actionGrid.cellSize = new Vector2(width, size.y);
        }
        static void SetHealth(Image fill, float value)
        {
            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        public IEnumerator PlayFeedback(CombatFeedbackData data)
        {
            if (!actionFeedback || !damageFeedback || !hitFlash || !arena)
                throw new InvalidOperationException("Combat feedback requires its authored texts, flash and arena.");
            ResetFeedback();
            restingArena = arena.anchoredPosition;
            restingDamageScale = damageFeedback.rectTransform.localScale;
            restingDamagePosition = damageFeedback.rectTransform.anchoredPosition;
            restingDamageColor = damageFeedback.color; restingActionColor = actionFeedback.color;
            IsFeedbackPlaying = true;
            try
            {
                actionFeedback.color = Data.context.presentation.gradeColors[(int)data.grade];
                SetText(actionFeedback, data.actionLabel + " · " + KoreanText.Grade(data.grade));
                string dealt = data.attack > 0 ? "적 체력 -" + data.enemyHpLost + "\n공격 " + data.attack + " · 막힘 " + data.blocked : "방어 태세";
                if (data.blockGained > 0) dealt += "\n수호 +" + data.blockGained;
                SetText(damageFeedback, dealt);
                SetText(enemyHealth, data.enemyHpAfter + " / " + data.enemyMaxHp);
                SetHealth(enemyHealthFill, (float)data.enemyHpAfter / data.enemyMaxHp);
                ClearText(enemyShield);
                if (data.blockGained > 0) SetText(playerShield, "수호 +" + data.blockGained);
                yield return Impact(data.attack > 0 ? Red : Blue, data.attack > 0, data.enemyHpLost, data.enemyMaxHp);
                if (data.retaliates)
                {
                    actionFeedback.color = restingActionColor;
                    SetText(actionFeedback, "적 행동 · " + data.enemyActionLabel);
                    SetText(damageFeedback, data.enemyDefends ? "적 수호 +" + data.enemyBlockGained :
                        "내 체력 -" + data.playerHpLost + "\n받은 공격 " + data.incoming + " · 수호 흡수 " + data.absorbed);
                    SetText(enemyShield, data.enemyDefends ? "수호 " + data.enemyBlockGained : null);
                    SetText(layout.stats, "체력 " + data.playerHpAfter + "/" + data.playerMaxHp);
                    SetHealth(playerHealthFill, (float)data.playerHpAfter / data.playerMaxHp);
                    SetText(playerShield, "수호 0");
                    yield return Impact(data.enemyDefends ? Blue : Red, !data.enemyDefends, data.playerHpLost, data.playerMaxHp);
                }
                yield return new WaitForSecondsRealtime(Mathf.Clamp(feedbackHoldSeconds, 0, 2));
            }
            finally { ResetFeedback(); }
        }
        IEnumerator Impact(Color color, bool shake, int hpLost, int maxHp)
        {
            damageFeedback.color = Color.Lerp(color, Color.white, .48f);
            float began = Time.unscaledTime;
            float seconds = Mathf.Clamp(actionFeedbackSeconds, .1f, 2);
            float strength = Mathf.Lerp(.45f, 1, Mathf.Clamp01((float)hpLost / Math.Max(1, maxHp) * 3));
            while (IsFeedbackPlaying && Time.unscaledTime - began < seconds)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - began) / seconds);
                float falloff = 1 - t;
                arena.anchoredPosition = restingArena + (shake ? new Vector2(Mathf.Sin(t * 51), Mathf.Cos(t * 43)) * Mathf.Clamp(shakePixels, 0, 24) * strength * falloff : Vector2.zero);
                hitFlash.color = new Color(color.r, color.g, color.b, .17f * falloff);
                damageFeedback.rectTransform.localScale = restingDamageScale * (1 + .13f * falloff);
                damageFeedback.rectTransform.anchoredPosition = restingDamagePosition + Vector2.up * 12 * t;
                yield return null;
            }
            arena.anchoredPosition = restingArena;
            hitFlash.color = Color.clear;
            damageFeedback.rectTransform.localScale = restingDamageScale;
            damageFeedback.rectTransform.anchoredPosition = restingDamagePosition;
        }
        public void ResetFeedback()
        {
            if (IsFeedbackPlaying)
            {
                if (arena) arena.anchoredPosition = restingArena;
                if (damageFeedback)
                {
                    damageFeedback.rectTransform.localScale = restingDamageScale;
                    damageFeedback.rectTransform.anchoredPosition = restingDamagePosition;
                    damageFeedback.color = restingDamageColor;
                }
                if (actionFeedback) actionFeedback.color = restingActionColor;
            }
            IsFeedbackPlaying = false;
            if (hitFlash) hitFlash.color = Color.clear;
            ClearText(actionFeedback); ClearText(damageFeedback);
        }
        void OnDisable() => ResetFeedback();
        protected override void UnbindScreen()
        {
            ResetFeedback();
            if (menuButton) menuButton.Unbind();
            if (artworkRoot && artwork && artworkFallback) SetArtwork(artworkRoot, artwork, artworkFallback, null);
            foreach (var field in new[] { turn, enemyName, enemyHealth, enemyShield, intentValue, playerName, playerShield, playerAttributes, fatePower, rerolls })
                ClearText(field);
            actionCount = 0;
        }
    }
}
