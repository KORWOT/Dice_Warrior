#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiceWarrior.SymbolLoop;
using FateDice;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DiceWarrior.SharedWorld.Tests
{
    [Category("V5UiRedesign12"), Category("V5UiImprovement11")]
    public sealed class CompetitionDiceRedesignTests
    {
        GameObject root, appObject, eventObject, cameraObject;
        UIManager ui;
        CompetitionApp app;
        Camera camera;
        RenderTexture target;
        CompetitionDefinition definition;
        string path;
        CompetitionScreen Screen => (CompetitionScreen)ui.ActiveScreen;
        V5DiceAreaView Area => Screen.v5Area;
        V5ShellView Shell => Screen.v5Shell;
        ParticipantState Human => app.Session.Snapshot.state.participants[0];

        [UnitySetUp] public IEnumerator Setup()
        {
            string directory = Path.GetFullPath(Path.Combine("artifacts/v5-ui-redesign-12/tests", Guid.NewGuid().ToString("N").Substring(0, 8)));
            Directory.CreateDirectory(directory); path = Path.Combine(directory, "match.json");
            definition = CompetitionContent.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Features/SharedWorld/Content/shared-world-preview-5.json").text);
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab"));
            ui = root.GetComponentInChildren<UIManager>(true);
            ui.prefabs = new BaseUI[] { AssetDatabase.LoadAssetAtPath<CompetitionScreen>("Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab") };
            cameraObject = new GameObject("Isolated dice redesign camera"); camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            target = new RenderTexture(720, 1280, 24); camera.targetTexture = target;
            ui.Root.canvas.renderMode = RenderMode.ScreenSpaceCamera; ui.Root.canvas.worldCamera = camera; ui.Root.canvas.planeDistance = 1;
            ui.Root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; ui.Root.canvas.scaleFactor = 1;
            eventObject = new GameObject("Isolated dice redesign events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            appObject = new GameObject("Isolated dice redesign app"); appObject.SetActive(false);
            app = appObject.AddComponent<CompetitionApp>(); app.ui = ui;
            app.Open(path, definition, new Profile { growth = 20 }, () => 33); appObject.SetActive(true);
            yield return null; Canvas.ForceUpdateCanvases();
            yield return Click("new"); yield return Wait();
            app.SetReducedMotion(false);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (ui) ui.CloseAll(); if (appObject) Object.Destroy(appObject); if (root) Object.Destroy(root);
            if (eventObject) Object.Destroy(eventObject); if (cameraObject) Object.Destroy(cameraObject);
            if (target) { target.Release(); Object.Destroy(target); }
            yield return null;
        }

        [UnityTest] public IEnumerator RollAndPartialRerollRevealTogetherWhileHeldPhysicalSlotStaysStill()
        {
            yield return Combat(3);
            yield return Tap(Area.roll);
            Assert.That(app.IsPresenting, Is.True); Assert.That(Area.DiceRolling, Is.True);
            AssertRollingHidden(new[] { 0, 1, 2 });
            yield return Wait(); AssertRevealed();
            yield return Tap(Area.dice[1]); yield return Wait();
            var held = Human.v5.selection.results[1]; string exactHeld = LoopJson.Encode(held);
            var card = (RectTransform)Area.dice[1].transform; var cube = Area.cubes[1];
            Vector3 cardPosition = card.position, cubePosition = cube.transform.position;
            Quaternion rotation = cube.transform.rotation; Vector3 scale = cube.transform.localScale;
            Assert.That(card.parent, Is.EqualTo(Area.rollZone), "Holding must keep the original tray parent.");
            Assert.That(card.anchoredPosition, Is.EqualTo(V5DiceAreaView.SlotPosition(1, 3)));
            yield return Tap(Area.reroll);
            Assert.That(Area.DiceRolling, Is.True); int samples = 0; float deadline = Time.realtimeSinceStartup + 5;
            while (Area.DiceRolling && !Area.cubes[0].RevealResult)
            {
                AssertRollingHidden(new[] { 0, 2 });
                Assert.That(LoopJson.Encode(Human.v5.selection.results[1]), Is.EqualTo(exactHeld));
                Assert.That(cube.Held && cube.RevealResult, Is.True);
                Assert.That(cube.ResultFaceIndex, Is.EqualTo(held.faceIndex)); Assert.That(cube.FrontFaceIndex, Is.EqualTo(held.faceIndex));
                Assert.That(cube.RollProgress, Is.EqualTo(1));
                Assert.That(Vector3.Distance(card.position, cardPosition), Is.LessThan(.01f));
                Assert.That(Vector3.Distance(cube.transform.position, cubePosition), Is.LessThan(.01f));
                Assert.That(Quaternion.Angle(cube.transform.rotation, rotation), Is.LessThan(.01f));
                Assert.That(cube.transform.localScale, Is.EqualTo(scale));
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); ++samples; yield return null;
            }
            Assert.That(samples, Is.GreaterThan(1)); AssertRevealed(); yield return Wait(); AssertRevealed();
            Assert.That(Human.v5.selection.rerollsLeft, Is.Zero);
        }

        [UnityTest] public IEnumerator FailedRerollWriteKeepsCommittedFacesAndRetriesOnlyAfterStorageRecovers()
        {
            yield return Combat(3); yield return Tap(Area.roll); yield return Wait();
            string saved = LoopJson.Encode(app.Session.Snapshot), disk = File.ReadAllText(path);
            int commands = app.Session.Snapshot.commands.Count;
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                yield return Tap(Area.reroll);
            Assert.That(app.IsPresenting || Area.DiceRolling, Is.False);
            Assert.That(LoopJson.Encode(app.Session.Snapshot), Is.EqualTo(saved)); Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
            Assert.That(Area.notice.text, Is.Not.Empty); Assert.That(Area.reroll.button.interactable, Is.True); AssertRevealed();
            yield return Tap(Area.reroll); yield return Wait(); AssertRevealed();
            Assert.That(app.Session.Snapshot.commands.Count, Is.EqualTo(commands + 1));
            Assert.That(Human.v5.selection.rerollsLeft, Is.Zero);
        }

        [UnityTest] public IEnumerator ClosingDuringRollAndRerollResumesSavedResultWithoutRepeatingInput()
        {
            yield return Combat(3);
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1) { yield return Tap(Area.dice[1]); yield return Wait(); }
                var button = pass == 0 ? Area.roll : Area.reroll;
                yield return Tap(button); Assert.That(app.IsPresenting, Is.True);
                string saved = LoopJson.Encode(app.Session.Snapshot), disk = File.ReadAllText(path);
                button.button.onClick.Invoke();
                Assert.That(LoopJson.Encode(app.Session.Snapshot), Is.EqualTo(saved), "A second callback during presentation cannot dispatch another command.");
                app.Close(); yield return null;
                Assert.That(app.InLobby, Is.True); Assert.That(app.IsPresenting, Is.False);
                yield return Click("resume"); yield return null;
                Assert.That(app.InLobby, Is.False); Assert.That(Area.DiceRolling, Is.False);
                Assert.That(LoopJson.Encode(app.Session.Snapshot), Is.EqualTo(saved)); Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
                AssertRevealed();
            }
            Assert.That(Human.v5.selection.results[1].held, Is.True); Assert.That(Human.v5.selection.rerollsLeft, Is.Zero);
        }

        [UnityTest] public IEnumerator ReducedMotionUsesIdenticalCommandsRandomStreamsAndCombatOutcome()
        {
            yield return Combat(3); var initial = app.Session.Snapshot;
            yield return CombatCycle(); var normal = app.Session.Snapshot;
            // Explicit isolated rewind fixture, not a product undo or replay command.
            Edit(candidate => { candidate.state = LoopJson.Copy(initial.state); candidate.commands = LoopJson.Copy(initial.commands); candidate.receipts = LoopJson.Copy(initial.receipts); });
            string beforeSetting = LoopJson.Encode(app.Session.Snapshot);
            app.SetReducedMotion(true); yield return null;
            Assert.That(app.ReducedMotion && Screen.playback.ReducedMotion, Is.True);
            Assert.That(LoopJson.Encode(app.Session.Snapshot), Is.EqualTo(beforeSetting));
            yield return CombatCycle(); var reduced = app.Session.Snapshot;
            Assert.That(LoopJson.Encode(reduced.state), Is.EqualTo(LoopJson.Encode(normal.state)));
            Assert.That(reduced.commands.Select(command => command.kind + ":" + command.arg),
                Is.EqualTo(normal.commands.Select(command => command.kind + ":" + command.arg)));
            Assert.That(reduced.state.actionNumber, Is.EqualTo(normal.state.actionNumber));
            Assert.That(Area.IsAnimating || Area.DiceRolling, Is.False);
        }

        [UnityTest] public IEnumerator TravelAcrossStartShowsSixButMovesOnlyTwoCellsAndStopsForReward()
        {
            uint seed = 1;
            for (; seed < 10000; seed++) { uint probe = seed; if (CompetitionRandom.Next(ref probe, 6) == 5) break; }
            Assert.That(seed, Is.LessThan(10000));
            // Explicit boundary fixture: actual travel still draws, saves and presents its own result.
            Edit(candidate => { var p = candidate.state.participants[0]; p.position = 10; p.travelRng = seed; });
            Assert.That(Screen.playback.VisiblePosition, Is.EqualTo(10));
            Assert.That(Shell.boardPrimary.BoundId, Is.EqualTo("travel:"));
            yield return Tap(Shell.boardPrimary);
            Assert.That(Human.lastTravel, Is.EqualTo(6)); Assert.That(Human.position, Is.Zero);
            var visited = new List<int> { 10 }; bool distanceExplained = false; float deadline = Time.realtimeSinceStartup + 8;
            while (app.IsPresenting)
            {
                int position = Screen.playback.VisiblePosition;
                if (position != visited.Last()) visited.Add(position);
                string text = Shell.notice.text;
                if (text.Contains("6") && text.Contains("2") && text.Contains("칸")) distanceExplained = true;
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null;
            }
            if (Screen.playback.VisiblePosition != visited.Last()) visited.Add(Screen.playback.VisiblePosition);
            Assert.That(visited, Is.EqualTo(new[] { 10, 11, 0 }));
            Assert.That(distanceExplained, Is.True, "The rolled six and actual two-cell route must both be explained.");
            Assert.That(Screen.playback.human.numericDie.Result, Is.EqualTo(6));
            Assert.That(Human.phase, Is.EqualTo(CompetitionPhase.LapReward)); Assert.That(Human.completedLaps, Is.Zero);
            Assert.That(app.Session.Snapshot.commands.Count(command => command.kind == "travel"), Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator GrantedSecondAndThirdDiceAppearInNextCombatButFinalGearAddsNoDie()
        {
            for (int count = 1; count <= 2; count++)
            {
                LapFixture(); var oldIds = Human.dice.Select(die => die.instanceId).ToArray();
                yield return Click("lap:" + (count == 1 ? "balanced" : "arcane"));
                Assert.That(Shell.rewardGrowth.activeSelf, Is.True);
                Assert.That(Shell.heading.text, Does.Contain("수령 완료"));
                Assert.That(Shell.summary.text, Does.Not.Contain("아직"));
                yield return Wait();
                Assert.That(Human.dice.Count, Is.EqualTo(count + 1));
                Assert.That(Human.dice.Take(count).Select(die => die.instanceId), Is.EqualTo(oldIds));
                Assert.That(Human.claimed.Count, Is.EqualTo(count));
                Assert.That(Shell.notice.text, Does.Contain("주사위").And.Contain((count + 1).ToString()));
                yield return Combat(count + 1, true);
                Assert.That(Area.cubes.Count(cube => cube.gameObject.activeInHierarchy), Is.EqualTo(count + 1));
                yield return Tap(Area.roll); yield return Wait(); AssertRevealed();
            }
            LapFixture(); string dice = LoopJson.Encode(Human.dice);
            yield return Click("lap:blade"); yield return Wait();
            Assert.That(Human.completedLaps, Is.EqualTo(3)); Assert.That(Human.enemy.boss, Is.True);
            Assert.That(LoopJson.Encode(Human.dice), Is.EqualTo(dice)); Assert.That(Human.dice.Count, Is.EqualTo(3));
            Assert.That(Human.claimed.Count, Is.EqualTo(3));
            Assert.That(Area.cubes.Count(cube => cube.gameObject.activeInHierarchy), Is.EqualTo(3));
        }

        IEnumerator CombatCycle()
        {
            yield return Tap(Area.roll); AssertReducedPose(); yield return Wait(); AssertRevealed();
            yield return Tap(Area.dice[0]); yield return Wait();
            yield return Tap(Area.reroll); AssertReducedPose(); yield return Wait(); AssertRevealed();
            yield return Tap(Area.execute); yield return Wait();
        }
        void AssertReducedPose()
        {
            if (!app.ReducedMotion) return;
            Assert.That(Area.ReducedMotion, Is.True);
            foreach (var cube in Area.cubes.Where(value => value.gameObject.activeInHierarchy))
            {
                Assert.That(cube.RollProgress, Is.EqualTo(1), "Reduced motion skips the cube tumble.");
                Assert.That(cube.RevealResult, Is.True);
            }
        }
        void AssertRollingHidden(int[] slots)
        {
            foreach (int slot in slots)
            {
                Assert.That(Area.cubes[slot].RevealResult, Is.False, "Moving physical slot " + slot);
                Assert.That(Area.dice[slot].label.text, Does.Contain("굴리는 중"));
                Assert.That(Area.dice[slot].label.text, Does.Not.Contain("기술 "));
                Assert.That(Area.dice[slot].button.interactable, Is.False);
            }
            Assert.That(Area.preview.text, Does.Contain("멈추면"));
            Assert.That(Area.preview.text, Does.Not.Contain("발동 예상"));
            Assert.That(Area.execute.button.interactable || Area.reroll.button.interactable, Is.False);
        }
        void AssertRevealed()
        {
            var state = app.Session.Snapshot.state; var p = state.participants[0];
            foreach (var result in p.v5.selection.results)
            {
                var cube = Area.cubes[result.slotIndex];
                Assert.That(cube.RevealResult, Is.True); Assert.That(cube.ResultFaceIndex, Is.EqualTo(result.faceIndex));
                Assert.That(cube.FrontFaceIndex, Is.EqualTo(result.faceIndex)); Assert.That(cube.RollProgress, Is.EqualTo(1));
                Assert.That(Area.dice[result.slotIndex].label.text, Does.Contain(CompetitionV5Content.Face(state.definition, result.faceId).name));
                Assert.That(((RectTransform)Area.dice[result.slotIndex].transform).anchoredPosition,
                    Is.EqualTo(V5DiceAreaView.SlotPosition(result.slotIndex, p.dice.Count)));
            }
            Assert.That(Area.preview.text, Is.EqualTo(V5DiceAreaView.PreviewText(CompetitionV5Skills.Preview(state.definition, p))));
        }
        IEnumerator Combat(int count, bool preserveDice = false)
        {
            // A clearly scoped valid combat fixture; no claim of natural board progression.
            Edit(candidate =>
            {
                var state = candidate.state; var p = state.participants[0];
                if (!preserveDice)
                {
                    p.completedLaps = count - 1;
                    p.claimed = Enumerable.Range(1, count - 1).Select(lap => state.matchId + ":" + p.id + ":lap:" + lap).ToList();
                    p.dice = Enumerable.Range(0, count).Select(slot => new DieSlot { instanceId = "ui12-die-" + slot, definitionId = "balanced" }).ToList();
                }
                ClearCombat(p); p.phase = CompetitionPhase.Encounter;
                string enemy = state.definition.v4.enemies.First(value => value.encounterId != "final").encounterId;
                p.offer = new EncounterOffer { id = "ui12-offer-" + candidate.revision, placeId = state.definition.world.places[p.position].id,
                    stage = p.completedLaps, visit = 1, choices = new[] {
                        new EncounterChoice { id = enemy, label = "격리 트레이 전투", kind = EncounterKind.Combat, available = true, enemyHp = 1000, enemyAttack = 1 },
                        new EncounterChoice { id = "harbor-work", label = "격리 대안", kind = EncounterKind.Study, available = true } } };
            });
            string choice = Human.offer.choices[0].id; yield return Click("choose:" + choice); yield return Wait();
            Assert.That(Human.dice.Count, Is.EqualTo(count));
        }
        static void ClearCombat(ParticipantState p)
        {
            p.lastTurn = null; p.v4.lastTurn = null; p.v5.lastTurn = null; p.v5.selection = null;
            p.encounterTurns = 0; p.turn = 0; p.enemy = null; p.v4.enemyId = null; p.offer = null;
        }
        void LapFixture() => Edit(candidate => { var p = candidate.state.participants[0]; ClearCombat(p); p.position = 0; p.phase = CompetitionPhase.LapReward; });
        void Edit(Action<CompetitionDocument> change)
        {
            var original = app.Session.Snapshot; var candidate = LoopJson.Copy(original); change(candidate);
            candidate.revision = checked(original.revision + 1); CompetitionStore.ValidateDocument(candidate);
            new CompetitionStore(path).Commit(candidate, original.revision, original.generation); app.Session.Reload(); app.Render(); Canvas.ForceUpdateCanvases();
        }
        IEnumerator Click(string id)
        {
            int index = Array.FindIndex(Shell.Data.choices, action => action.id == id);
            if (index >= 0)
            {
                int page = index / Shell.choices.Length;
                while (Shell.ChoicePage < page) yield return Tap(Shell.next);
                while (Shell.ChoicePage > page) yield return Tap(Shell.previous);
            }
            var button = Screen.GetComponentsInChildren<CommonButtonView>().Single(value => value.gameObject.activeInHierarchy && value.BoundId == id);
            yield return Tap(button);
        }
        IEnumerator Tap(CommonButtonView button)
        {
            Canvas.ForceUpdateCanvases();
            if (Shell.gameObject.activeInHierarchy && button.transform.IsChildOf(Shell.scrollContent))
            {
                var rect = (RectTransform)button.transform; float localY = Shell.scrollContent.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
                float range = Shell.scrollContent.rect.height - Shell.viewport.rect.height;
                Shell.scroll.StopMovement(); Shell.scroll.verticalNormalizedPosition = range <= 0 ? 1 : 1 - Mathf.Clamp01((-localY - Shell.viewport.rect.height * .5f) / range);
            }
            yield return null; Canvas.ForceUpdateCanvases(); Assert.That(button.button.interactable, Is.True, button.BoundId);
            var targetRect = (RectTransform)button.transform;
            var pointer = new PointerEventData(eventObject.GetComponent<EventSystem>()) { position = RectTransformUtility.WorldToScreenPoint(camera, targetRect.TransformPoint(targetRect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); eventObject.GetComponent<EventSystem>().RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, button.BoundId);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(button.button), "First raycast: " + string.Join(" / ", hits.Select(hit => hit.gameObject.name)));
            ExecuteEvents.Execute(button.button.gameObject, pointer, ExecuteEvents.pointerClickHandler); Canvas.ForceUpdateCanvases();
        }
        IEnumerator Wait()
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while (app.IsPresenting) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
            Canvas.ForceUpdateCanvases();
        }
    }
}
#endif
