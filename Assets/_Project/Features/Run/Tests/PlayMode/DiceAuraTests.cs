using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    public sealed class DiceAuraTests
    {
        const string PopupPath = "Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab";
        GameObject holder;

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (holder) Object.Destroy(holder);
            holder = null;
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        // Literal expected indices are independent of the production hand evaluator.
        static IEnumerable<TestCaseData> ParticipationExamples()
        {
            yield return Example("PairUsesLowestCandidateAndFirstTwoIndices", HandKind.Pair,
                new[] { 6, 2, 6, 2, 2, 1 }, new[] { -1, 0, -1, 0, -1, -1 });
            yield return Example("TwoPairsUsesFourDiceEvenWhenBothCandidatesAreTriples", HandKind.TwoPairs,
                new[] { 5, 5, 2, 2, 2, 5 }, new[] { 1, 1, 0, 0, -1, -1 });
            yield return Example("TripleUsesTheLowestOfTwoTripleCandidates", HandKind.Triple,
                new[] { 4, 4, 4, 1, 1, 1 }, new[] { -1, -1, -1, 0, 0, 0 });
            yield return Example("TripleUsesOnlyItsFirstThreeIndices", HandKind.Triple,
                new[] { 3, 3, 3, 3, 3, 6 }, new[] { 0, 0, 0, -1, -1, -1 });
            yield return Example("ThreePairsSeparatesThreeGroupsInAscendingFaceOrder", HandKind.ThreePairs,
                new[] { 6, 2, 4, 6, 4, 2 }, new[] { 2, 0, 1, 2, 1, 0 });
            yield return Example("FullHouseUsesTripleGroupZeroAndPairGroupOne", HandKind.FullHouse,
                new[] { 1, 2, 2, 1, 2, 6 }, new[] { 1, 0, 0, 1, 0, -1 });
            yield return Example("FullHouseChoosesLowestTripleAndLeavesOneExtraDie", HandKind.FullHouse,
                new[] { 4, 1, 4, 1, 4, 1 }, new[] { 1, 0, 1, 0, -1, 0 });
            yield return Example("FourKindUsesOnlyItsFirstFourIndices", HandKind.FourKind,
                new[] { 3, 3, 3, 3, 3, 6 }, new[] { 0, 0, 0, 0, -1, -1 });
            yield return Example("StraightPrefersOneThroughFiveWhenBothRunsExist", HandKind.Straight,
                new[] { 6, 1, 5, 2, 4, 3 }, new[] { -1, 0, 0, 0, 0, 0 });
            yield return Example("StraightUsesFirstDuplicateAndTwoThroughSixWhenNeeded", HandKind.Straight,
                new[] { 3, 6, 4, 2, 5, 3 }, new[] { 0, 0, 0, 0, 0, -1 });
            yield return Example("StraightUsesFirstIndexOfDuplicateOne", HandKind.Straight,
                new[] { 5, 2, 1, 4, 3, 1 }, new[] { 0, 0, 0, 0, 0, -1 });
            yield return Example("FullStraightUsesAllSixWithoutReordering", HandKind.FullStraight,
                new[] { 6, 1, 5, 2, 4, 3 }, new[] { 0, 0, 0, 0, 0, 0 });
            yield return Example("FiveKindUsesOnlyItsFirstFiveIndices", HandKind.FiveKind,
                new[] { 2, 2, 2, 2, 2, 2 }, new[] { 0, 0, 0, 0, 0, -1 });
            yield return Example("SixKindUsesAllSix", HandKind.SixKind,
                new[] { 4, 4, 4, 4, 4, 4 }, new[] { 0, 0, 0, 0, 0, 0 });
        }

        static TestCaseData Example(string name, HandKind hand, int[] values, int[] expected) =>
            new TestCaseData(hand, values, expected).SetName(name);

        [TestCaseSource(nameof(ParticipationExamples))]
        public void GroupsHighlightOnlyTheMinimumContributors(HandKind hand, int[] values, int[] expected)
        {
            var before = (int[])values.Clone();
            var actual = Groups(hand, values);
            CollectionAssert.AreEqual(expected, actual);
            CollectionAssert.AreEqual(before, values, "Highlighting must preserve the displayed dice order and values.");
        }

        static IEnumerable<TestCaseData> UnsupportedExamples()
        {
            foreach (var hand in new[] { HandKind.Pair, HandKind.TwoPairs, HandKind.Triple, HandKind.ThreePairs,
                         HandKind.FullHouse, HandKind.FourKind, HandKind.FiveKind, HandKind.SixKind })
                yield return new TestCaseData(hand, new[] { 1, 2, 3, 4, 5, 6 }).SetName("MissingShapeLeavesAllDiceUnlit_" + hand);
            yield return new TestCaseData(HandKind.ThreePairs, new[] { 1, 1, 2, 2, 2, 2 }).SetName("ThreePairsCannotSplitFourOfAKindIntoTwoPairs");
            yield return new TestCaseData(HandKind.FullHouse, new[] { 3, 3, 3, 3, 3, 3 }).SetName("FullHouseRequiresTwoDifferentFaceValues");
            yield return new TestCaseData(HandKind.Straight, new[] { 1, 2, 3, 4, 4, 6 }).SetName("MissingFiveCannotProduceAStraightAura");
            yield return new TestCaseData(HandKind.FullStraight, new[] { 1, 2, 3, 4, 5, 5 }).SetName("FullStraightRequiresAllSixDistinctFaces");
            yield return new TestCaseData((HandKind)999, new[] { 1, 2, 3, 4, 5, 6 }).SetName("UnknownHandLeavesAllDiceUnlit");
        }

        [TestCaseSource(nameof(UnsupportedExamples))]
        public void UnsupportedHandAndValuesDoNotInventContributors(HandKind hand, int[] values)
        {
            CollectionAssert.AreEqual(new[] { -1, -1, -1, -1, -1, -1 }, Groups(hand, values));
        }

        [Test] public void InvalidDiceInputIsRejectedBeforeAnyPartialResult()
        {
            foreach (var values in new[] { null, new int[5], new int[7], new[] { 1, 2, 3, 4, 5, 0 }, new[] { 1, 2, 3, 4, 5, 7 } })
                Assert.That(() => Groups(HandKind.Pair, values), Throws.InstanceOf<ArgumentException>());
        }

        [Test] public void HighlightingPreservesInputAndUnityRandomAndReturnsAnOwnedArray()
        {
            var values = new[] { 6, 2, 4, 6, 4, 2 };
            var before = (int[])values.Clone();
            var random = UnityEngine.Random.state;
            for (int i = 0; i < 10; i++)
            {
                var groups = Groups(HandKind.ThreePairs, values);
                CollectionAssert.AreEqual(new[] { 2, 0, 1, 2, 1, 0 }, groups);
                Assert.That(groups, Is.Not.SameAs(values));
                groups[0] = -100;
            }
            CollectionAssert.AreEqual(before, values);
            Assert.That(UnityEngine.Random.state, Is.EqualTo(random), "Cosmetic grouping consumed global Unity Random.");
        }

        [Test] public void AuthoredPopupConnectsSixIndependentNonblockingAurasAndOneCrest()
        {
            var popup = Prefab();
            var auras = Auras(popup);
            var crest = (Graphic)Read(popup.resultFeedback, "crest");
            Assert.That(auras.Distinct().Count(), Is.EqualTo(6));
            CollectionAssert.DoesNotContain(auras, crest);
            foreach (var graphic in auras.Concat(new[] { crest }))
            {
                Assert.That(graphic, Is.Not.Null);
                Assert.That(graphic.GetType().Name, Is.EqualTo("DiceAuraGraphic"));
                Assert.That(graphic.transform.IsChildOf(popup.transform), Is.True);
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null);
                Assert.That(graphic.raycastTarget, Is.False, "An aura must not intercept a die or button pointer.");
            }
        }

        [Test] public void AuraPaletteMigrationPreservesUserAlphaAndCustomRgb()
        {
            var authoring = AppDomain.CurrentDomain.GetAssemblies().Select(assembly =>
                assembly.GetType("FateDice.Editor.DicePresentationAuthoring")).FirstOrDefault(type => type != null);
            Assert.That(authoring, Is.Not.Null);
            var migrate = authoring.GetMethod("AuraDefaultColor", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(migrate, Is.Not.Null);
            var previous = new Color(.5f, .6f, .7f, 1);
            var alphaOnly = new Color(.5f, .6f, .7f, .3f);
            var customRgb = new Color(.2f, .6f, .7f, 1);
            Assert.That(migrate.Invoke(null, new object[] { previous, previous, Color.blue }), Is.EqualTo(Color.blue));
            Assert.That(migrate.Invoke(null, new object[] { alphaOnly, previous, Color.blue }), Is.EqualTo(alphaOnly));
            Assert.That(migrate.Invoke(null, new object[] { customRgb, previous, Color.blue }), Is.EqualTo(customRgb));
        }

        [UnityTest] public IEnumerator RealFeedbackShowsOnlyContributorsSettlesAndClearsOnRebindAndReset()
        {
            holder = new GameObject("Dice aura fixture", typeof(RectTransform), typeof(Canvas));
            holder.SetActive(false);
            holder.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var popup = Object.Instantiate(Prefab(), holder.transform, false);
            popup.gameObject.SetActive(true);
            holder.SetActive(true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var feedback = popup.resultFeedback;
            var auras = Auras(popup);
            var crest = (Graphic)Read(feedback, "crest");
            var triples = Data(HandKind.ThreePairs, new[] { 6, 2, 4, 6, 4, 2 }, .5f, 0);
            for (int i = 0; i < 6; i++) popup.dice[i].Render(triples.values[i]);
            feedback.Show(triples);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(feedback.IsPlaying, Is.False, "A zero hold must show static auras without a motion wait.");
            foreach (var aura in auras)
            {
                Assert.That(Read(aura, "Visible"), Is.EqualTo(true));
                Assert.That((float)Read(aura, "Strength"), Is.GreaterThan(0));
                Assert.That((float)Read(aura, "Phase"), Is.EqualTo(1).Within(.001f));
                AssertMesh(aura, true);
            }
            // For [6,2,4,6,4,2], the middle-value pair is group 1 and uses the accent.
            Assert.That(Read(auras[0], "Primary"), Is.EqualTo(Read(auras[1], "Primary")));
            Assert.That(Read(auras[2], "Primary"), Is.EqualTo(Read(auras[4], "Primary")));
            Assert.That(Read(auras[2], "Primary"), Is.Not.EqualTo(Read(auras[0], "Primary")));
            Assert.That(Read(crest, "Visible"), Is.EqualTo(true));
            AssertMesh(crest, true);

            var pairs = Data(HandKind.TwoPairs, new[] { 1, 2, 1, 3, 2, 5 }, .35f, 1);
            for (int i = 0; i < 6; i++) popup.dice[i].Render(pairs.values[i]);
            feedback.Show(pairs);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var expected = new[] { true, true, true, false, true, false };
            for (int i = 0; i < 6; i++)
            {
                Assert.That(Read(auras[i], "Visible"), Is.EqualTo(expected[i]), "A prior hand left an aura on an unrelated die.");
                AssertMesh(auras[i], expected[i]);
            }
            float settleDeadline = Time.realtimeSinceStartup + 3;
            while (feedback.Phase != DiceResultFeedback.ResultPhase.Reading)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(settleDeadline));
                yield return null;
            }
            Canvas.ForceUpdateCanvases();
            Assert.That(feedback.IsPlaying, Is.False, "Aura motion did not settle after the short entrance.");
            foreach (var aura in auras.Where((aura, index) => expected[index]))
                Assert.That((float)Read(aura, "Phase"), Is.EqualTo(1).Within(.001f));
            feedback.ResetFeedback();
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var aura in auras.Concat(new[] { crest }))
            {
                Assert.That(Read(aura, "Visible"), Is.EqualTo(false));
                AssertMesh(aura, false);
            }
            // The renderer's public visual boundary must retain supplied data and clear it.
            Invoke(auras[0], "SetVisual", Color.cyan, Color.magenta, .7f, .6f);
            Assert.That(Read(auras[0], "Primary"), Is.EqualTo(Color.cyan));
            Assert.That(Read(auras[0], "Secondary"), Is.EqualTo(Color.magenta));
            Assert.That((float)Read(auras[0], "Strength"), Is.EqualTo(.7f));
            Assert.That((float)Read(auras[0], "Phase"), Is.EqualTo(.6f));
            Invoke(auras[0], "Clear");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(Read(auras[0], "Visible"), Is.EqualTo(false));
            AssertMesh(auras[0], false);
            foreach (var graphic in auras.Concat(new[] { crest }))
            {
                graphic.color = new Color(1, 1, 1, 0);
                Invoke(graphic, "SetVisual", Color.red, Color.yellow, 1f, .5f);
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var graphic in auras.Concat(new[] { crest }))
            {
                var mesh = graphic.canvasRenderer.GetMesh();
                Assert.That(mesh, Is.Not.Null);
                Assert.That(mesh.colors32.All(tint => tint.a == 0), Is.True, "White spark centers ignored Graphic opacity.");
                var bounds = graphic.rectTransform.rect;
                foreach (var vertex in mesh.vertices)
                    Assert.That(vertex.x >= bounds.xMin - .1f && vertex.x <= bounds.xMax + .1f &&
                        vertex.y >= bounds.yMin - .1f && vertex.y <= bounds.yMax + .1f, Is.True,
                        graphic.name + " light geometry escaped its culling rect: " + vertex);
            }
        }

        static DiceRollUIData Data(HandKind hand, int[] values, float strength, float hold) => new DiceRollUIData
        {
            comboName = "오라 검증", hand = hand, values = values, comboStrength = strength,
            holdSeconds = hold, rolling = false, duration = 0
        };

        static DiceRollUI Prefab()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<DiceRollUI>(PopupPath);
            Assert.That(prefab, Is.Not.Null);
            return prefab;
#else
            throw new InvalidOperationException("Production prefab tests require the Editor.");
#endif
        }

        static Graphic[] Auras(DiceRollUI popup)
        {
            Assert.That(popup.resultFeedback, Is.Not.Null);
            var auras = Read(popup.resultFeedback, "dieAuras") as Array;
            Assert.That(auras, Is.Not.Null, "Feedback requires six authored dieAuras.");
            Assert.That(auras.Length, Is.EqualTo(6));
            return auras.Cast<object>().Select(value => value as Graphic).ToArray();
        }

        static object Read(object target, string name)
        {
            Assert.That(target, Is.Not.Null, "Missing owner for " + name);
            var type = target.GetType();
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(target);
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, type.Name + " is missing public " + name);
            return property.GetValue(target);
        }

        static void Invoke(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, args.Select(value => value.GetType()).ToArray());
            Assert.That(method, Is.Not.Null, target.GetType().Name + " is missing " + name);
            method.Invoke(target, args);
        }

        static void AssertMesh(Graphic graphic, bool visible)
        {
            var mesh = graphic.canvasRenderer.GetMesh();
            if (visible) Assert.That(mesh && mesh.vertexCount > 0, Is.True, graphic.name + " has no visible aura mesh.");
            else Assert.That(mesh == null || mesh.vertexCount == 0, Is.True, graphic.name + " retained a stale aura mesh.");
        }

        static int[] Groups(HandKind hand, int[] values)
        {
            var type = typeof(DiceRollUI).Assembly.GetType("FateDice.DiceComboHighlights");
            Assert.That(type, Is.Not.Null, "Missing presentation helper DiceComboHighlights.");
            var method = type.GetMethod("Groups", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(HandKind), typeof(int[]) }, null);
            Assert.That(method, Is.Not.Null, "DiceComboHighlights requires public static Groups(HandKind, int[]).");
            try { return (int[])method.Invoke(null, new object[] { hand, values }); }
            catch (TargetInvocationException error)
            {
                ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw();
                throw;
            }
        }
    }
}
