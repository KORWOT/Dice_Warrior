using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    public sealed class FateChoicePopupTests
    {
        const string PopupPath = "Assets/_Project/Features/Fate/Prefabs/FateChoiceUI.prefab";
        UIRoot root;
        UIManager manager;
        GameObject ownedEvents;
        RunUIContext context;
        int confirmed, closed, rerolled, forbiddenOfferCallbacks;
        string confirmedId;
        int rerolledIndex;

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (root) Object.Destroy(root.gameObject);
            if (ownedEvents) Object.Destroy(ownedEvents);
            root = null; manager = null; ownedEvents = null;
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator CardTapOnlySelectsAndExplicitRequestsDispatchOnce()
        {
            yield return Setup(1280);
            var data = Data(3);
            var popup = manager.ShowPopup<FateChoiceUI>(data);
            yield return Settled(popup);
            Assert.That(popup.SelectedId, Is.Null);
            Assert.That(popup.confirmButton.button.IsInteractable(), Is.False);
            Click(popup.Cards[1].frame.button);
            Assert.That(popup.SelectedId, Is.EqualTo("offer-1"));
            Assert.That(popup.Cards.Select(card => card.IsSelected), Is.EqualTo(new[] { false, true, false }));
            Assert.That(confirmed + forbiddenOfferCallbacks, Is.Zero, "Tapping a card cannot choose its actual event.");
            Click(popup.confirmButton.button);
            popup.confirmButton.button.onClick.Invoke();
            Assert.That(confirmed, Is.EqualTo(1)); Assert.That(confirmedId, Is.EqualTo("offer-1"));
            Assert.That(forbiddenOfferCallbacks, Is.Zero);
            manager.ShowPopup<FateChoiceUI>(Data(3, "next-"));
            yield return Settled(popup);
            Click(popup.closeButton.button); popup.closeButton.button.onClick.Invoke();
            Assert.That(closed, Is.EqualTo(1)); Assert.That(confirmed, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator RebindAndRerollClearSelectionAndRejectOldCardInputs()
        {
            yield return Setup(1280);
            var popup = manager.ShowPopup<FateChoiceUI>(Data(3));
            yield return Settled(popup);
            var oldCard = popup.Cards[0];
            Click(oldCard.frame.button);
            int version = popup.BindingVersion;
            var replacement = Data(3, "reroll-");
            manager.ShowPopup<FateChoiceUI>(replacement);
            Assert.That(popup.BindingVersion, Is.GreaterThan(version));
            Assert.That(popup.SelectedId, Is.Null);
            oldCard.frame.button.onClick.Invoke();
            Assert.That(popup.SelectedId, Is.Null);
            yield return Settled(popup);
            Click(popup.Cards[2].frame.button);
            Click(popup.rerollButtons[2].button);
            popup.rerollButtons[2].button.onClick.Invoke();
            Assert.That(popup.SelectedId, Is.Null);
            Assert.That(popup.Cards.All(card => !card.IsSelected), Is.True);
            Assert.That(rerolled, Is.EqualTo(1)); Assert.That(rerolledIndex, Is.EqualTo(2));
            Assert.That(confirmed + forbiddenOfferCallbacks, Is.Zero);
            CollectionAssert.AreEqual(new[] { "reroll-0", "reroll-1", "reroll-2" }, replacement.offers.Select(offer => offer.id));
        }

        [UnityTest] public IEnumerator SelectionWaitsForFeelThenExitAndOldPlaybackCannotFadeNewBinding()
        {
            yield return Setup(1280);
            var popup = manager.ShowPopup<FateChoiceUI>(Data(3));
            yield return Settled(popup);
            var stale = popup.PlaySelection("offer-0");
            Assert.That(stale.MoveNext(), Is.True);
            manager.ShowPopup<FateChoiceUI>(Data(3, "new-"));
            var position = popup.panel.localPosition;
            Assert.That(stale.MoveNext(), Is.False);
            (stale as IDisposable)?.Dispose();
            Assert.That(popup.group.alpha, Is.EqualTo(1));
            Assert.That(popup.panel.localPosition, Is.EqualTo(position));
            yield return Settled(popup);
            var selected = popup.Cards[0];
            var feedback = selected.frame.GetComponent<SelectionFeedback>();
            Assert.That(feedback, Is.Not.Null);
            feedback.playbackSpeed = .5f;
            popup.exitSeconds = .1f;
            var player = typeof(SelectionFeedback).GetField("player").GetValue(feedback);
            Assert.That(player, Is.Not.Null);
            var isPlaying = player.GetType().GetProperty("IsPlaying");
            var scale = selected.transform.localScale;
            float began = Time.unscaledTime;
            bool sawPlaying = false, sawExit = false;
            var playback = popup.PlaySelection("new-0");
            try
            {
                while (playback.MoveNext())
                {
                    Assert.That(Time.unscaledTime - began, Is.LessThan(10));
                    sawPlaying |= (bool)isPlaying.GetValue(player);
                    if (popup.group.alpha < 1)
                    {
                        sawExit = true;
                        Assert.That((bool)isPlaying.GetValue(player), Is.False, "Popup exit began before its actual FEEL player completed.");
                    }
                    yield return playback.Current;
                }
            }
            finally { (playback as IDisposable)?.Dispose(); }
            Assert.That(sawPlaying, Is.True, "The real authored FEEL player was never played.");
            Assert.That(sawExit, Is.True, "The nonzero authored exit was skipped.");
            Assert.That(popup.group.alpha, Is.Zero);
            Assert.That(selected.transform.localScale, Is.EqualTo(scale));
            popup.ResetPresentation();
            Assert.That(popup.group.alpha, Is.EqualTo(1));
            Assert.That(popup.panel.localPosition, Is.EqualTo(position));
            Assert.That(confirmed + forbiddenOfferCallbacks, Is.Zero, "Presentation must not execute the game request.");
        }

        [UnityTest] public IEnumerator Portrait1280KeepsThreePublicCardsAndRerollDiceReadable() => Portrait(1280);
        [UnityTest] public IEnumerator Portrait1600KeepsThreePublicCardsAndRerollDiceReadable() => Portrait(1600);
        IEnumerator Portrait(int height)
        {
            yield return Setup(height);
            var popup = manager.ShowPopup<FateChoiceUI>(Data(3));
            yield return Settled(popup);
            var viewport = ScreenRect(popup.cardScroll.viewport);
            var rectangles = popup.Cards.Select(card => ScreenRect((RectTransform)card.transform)).ToArray();
            for (int i = 0; i < 3; i++)
            {
                Assert.That(Contains(viewport, rectangles[i]) && Contains(Screen.safeArea, rectangles[i]), Is.True);
                Assert.That(rectangles[i].center.y, Is.EqualTo(rectangles[0].center.y).Within(.5f));
                Assert.That(rectangles[i].height, Is.GreaterThan(rectangles[i].width));
                if (i > 0) Assert.That(rectangles[i].xMin, Is.GreaterThan(rectangles[i - 1].xMax));
                AssertRaycast(popup.Cards[i].frame.button);
            }
            float firstY = ScreenRect((RectTransform)popup.rerollButtons[0].transform).center.y;
            for (int i = 0; i < 6; i++)
            {
                var rect = ScreenRect((RectTransform)popup.rerollButtons[i].transform);
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(48));
                Assert.That(rect.center.y, Is.EqualTo(firstY).Within(.5f));
                Assert.That(Contains(Screen.safeArea, rect), Is.True);
                Assert.That(popup.rerollFaces[i].Value, Is.EqualTo(i + 1));
                AssertRaycast(popup.rerollButtons[i].button);
            }
            foreach (var text in popup.GetComponentsInChildren<Text>().Where(text => !string.IsNullOrWhiteSpace(text.text)))
            {
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 2), text.name + " clips its public text.");
                Assert.That(Contains(Screen.safeArea, ScreenRect(text.rectTransform)), Is.True, text.name + " leaves the safe area.");
            }
            Assert.That(popup.Cards[0].description.text, Does.Not.Contain("offer-"));
            Assert.That(popup.Cards[0].frame.label.text, Is.EqualTo("전투  /  일반"));
        }

        [UnityTest] public IEnumerator FiveOffersRemainReachableThroughHorizontalScrolling()
        {
            yield return Setup(1280);
            var popup = manager.ShowPopup<FateChoiceUI>(Data(5));
            yield return Settled(popup);
            Assert.That(popup.Cards.Count, Is.EqualTo(5));
            CollectionAssert.AreEqual(new[] { "offer-0", "offer-1", "offer-2", "offer-3", "offer-4" }, popup.Cards.Select(card => card.OfferedId));
            Assert.That(popup.cardContent.rect.width, Is.GreaterThan(popup.cardScroll.viewport.rect.width));
            popup.cardScroll.horizontalNormalizedPosition = 1;
            yield return Settled(popup);
            Click(popup.Cards[4].frame.button);
            Assert.That(popup.SelectedId, Is.EqualTo("offer-4"));
            Click(popup.confirmButton.button);
            Assert.That(confirmedId, Is.EqualTo("offer-4")); Assert.That(confirmed, Is.EqualTo(1));
        }

        IEnumerator Setup(int height)
        {
            confirmed = closed = rerolled = forbiddenOfferCallbacks = 0;
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, (uint)height, "Fate choice popup");
            var source = UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>("Assets/_Project/Features/Run/Prefabs/GameApplication.prefab").controller;
            root = Object.Instantiate(source.uiRootPrefab);
            manager = root.GetComponent<UIManager>();
            context = new RunUIContext { manager = manager, prefabs = source.uiPrefabs, visuals = source.visuals, presentation = source.config.Snapshot().presentation };
#else
            throw new InvalidOperationException("Actual prefab tests require the Editor.");
#endif
            if (!EventSystem.current) ownedEvents = new GameObject("Fate popup events", typeof(EventSystem));
            yield return null;
            Assert.That(Screen.width, Is.EqualTo(720)); Assert.That(Screen.height, Is.EqualTo(height));
        }
        FateChoiceUIData Data(int count, string prefix = "offer-") => new FateChoiceUIData
        {
            context = context,
            hud = new RunHUDData { fate = "운명력 4", dice = new[] { 1, 2, 3, 4, 5, 6 }, dieClicked = index => { rerolled++; rerolledIndex = index; } },
            offers = Enumerable.Range(0, count).Select(i => new FateOfferUIData { id = prefix + i, type = (NodeType)i, grade = (Grade)i, clicked = _ => forbiddenOfferCallbacks++ }).ToArray(),
            confirm = id => { confirmed++; confirmedId = id; }, close = () => closed++
        };
        static IEnumerator Settled(FateChoiceUI popup)
        {
            yield return null; Canvas.ForceUpdateCanvases(); popup.RefreshLayout();
            yield return null; Canvas.ForceUpdateCanvases();
        }
        void Click(Button button)
        {
            var hit = AssertRaycast(button);
            var pointer = Pointer(button);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerClickHandler);
        }
        GameObject AssertRaycast(Button button)
        {
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True);
            var hits = new List<RaycastResult>(); root.GetComponent<GraphicRaycaster>().Raycast(Pointer(button), hits);
            var hit = hits.Select(item => ExecuteEvents.GetEventHandler<IPointerClickHandler>(item.gameObject)).FirstOrDefault(item => item != null);
            Assert.That(hit, Is.SameAs(button.gameObject), "A visible popup control is not reachable by its actual pointer.");
            return hit;
        }
        static PointerEventData Pointer(Button button) => new PointerEventData(EventSystem.current)
        { button = PointerEventData.InputButton.Left, position = ScreenRect((RectTransform)button.transform).center };
        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var points = corners.Select(point => RectTransformUtility.WorldToScreenPoint(null, point)).ToArray();
            return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y), points.Max(point => point.x), points.Max(point => point.y));
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 2 && inner.xMax <= outer.xMax + 2 && inner.yMin >= outer.yMin - 2 && inner.yMax <= outer.yMax + 2;

        [Test] public void AuthoredFatePopupRequiresExplicitConfirmAndClose()
        {
            var type = typeof(FateCardView).Assembly.GetType("FateDice.FateChoiceUI");
            Assert.That(type, Is.Not.Null, "Missing authored fate-choice popup type FateChoiceUI.");
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);
            Assert.That(prefab, Is.Not.Null, "Missing authored FateChoiceUI prefab.");
            var view = prefab.GetComponent(type);
            Assert.That(view, Is.Not.Null);
            foreach (var name in new[] { "confirmButton", "closeButton" })
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
                Assert.That(field, Is.Not.Null, "FateChoiceUI requires public " + name);
                Assert.That(field.GetValue(view), Is.InstanceOf<CommonButtonView>());
            }
            var data = type.BaseType.GetGenericArguments()[0];
            Assert.That(data.Name, Is.EqualTo("FateChoiceUIData"));
            Assert.That(typeof(RunUIData).IsAssignableFrom(data), Is.True);
            Assert.That(data.GetField("offers").FieldType, Is.EqualTo(typeof(FateOfferUIData[])));
            Assert.That(data.GetField("confirm").FieldType, Is.EqualTo(typeof(Action<string>)));
            Assert.That(data.GetField("close").FieldType, Is.EqualTo(typeof(Action)));
#else
            throw new InvalidOperationException("Authored popup tests require the Editor.");
#endif
        }
    }
}
