using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FateDice
{
    public partial class RunUIController
    {
        public IMetaProgressionService Meta { get; private set; }
        bool metaBusy;
        int metaViewVersion;
        StartRunRequest pendingStart;
        GrowthRequest pendingGrowth;

        async void RunMeta<T>(Func<Task<T>> action, Action<T> completed)
        {
            if (Busy) return;
            metaBusy = true; error = null; int version = metaViewVersion; SyncInputLock();
            try
            {
                var result = await action();
                // A server response can outlive the view. The service owns persistence; an old
                // continuation must not navigate or replace a newly bound scene/controller.
                if (this && isActiveAndEnabled && version == metaViewVersion)
                { metaBusy = false; completed(result); }
            }
            catch (Exception e)
            {
                if (this)
                {
                    error = e is MetaCommandException ? e.Message : PlayerError(e);
                    if (e is MetaCommandException) { pendingStart = null; pendingGrowth = null; }
                }
            }
            finally
            {
                if (this)
                {
                    metaBusy = false;
                    if (isActiveAndEnabled && version == metaViewVersion)
                    {
                        try
                        {
                            // A failed response can follow a successful disk/server commit.
                            // Reconcile the saved run so continue/abandon never depend on an old preview.
                            var commandError = error;
                            ReadSavedPreview();
                            if (!invalidSave) error = commandError;
                            Render();
                        }
                        catch (Exception e) { error = PlayerError(e); if (Widgets != null) Widgets.Notice.text = error; }
                    }
                    SyncInputLock();
                }
            }
        }
        void StartMetaJourney()
        {
            if (pendingStart == null) pendingStart = new StartRunRequest { requestId = Guid.NewGuid().ToString("N"),
                expectedRevision = Meta.Profile.revision, abandonActiveRun = savedPreview != null && savedPreview.phase != RunPhase.Result };
            var request = pendingStart;
            RunMeta(() => Meta.StartRunAsync(request), state =>
            {
                pendingStart = null; pendingGrowth = null;
                Session = new RunSession(state, Store); savedPreview = state.DeepCopy(); selectedSeed = state.initialSeed;
                if (navigation != null) navigation.RequestInGame(); else menu = false;
            });
        }
        void ChangeMetaLoadout(PlayerProfileData profile, Action<RunLoadout> change)
        {
            if (Busy) return;
            var l = profile.loadout.Copy(); change(l);
            var request = new LoadoutRequest { requestId = Guid.NewGuid().ToString("N"), expectedRevision = profile.revision, loadout = l };
            RunMeta(() => Meta.SaveLoadoutAsync(request), _ => { pendingStart = null; });
        }
        void GrowMetaCharacter(PlayerProfileData profile)
        {
            if (pendingGrowth == null) pendingGrowth = new GrowthRequest { requestId = Guid.NewGuid().ToString("N"),
                expectedRevision = profile.revision, characterId = profile.loadout.characterId };
            var request = pendingGrowth;
            RunMeta(() => Meta.GrowAsync(request), _ => { pendingGrowth = null; pendingStart = null; });
        }
        string MetaResultText(string runId)
        {
            if (Meta == null) return "";
            var paid = Meta.Settlement(runId);
            return paid == null ? "\n\n성장 재화 정산 대기\n아래 버튼에서 보상을 확정합니다. 골드와 보유품은 여정에 남습니다." :
                "\n\n성장 재화 +" + paid.currency + " · 정산 완료" + (paid.outcome == "legacy" ? "\n구형 여정은 영구 보상 대상이 아닙니다." : "");
        }
        void ReturnFromMetaResult()
        {
            if (Meta == null) { ShowMenu(); return; }
            if (Busy || Session == null) return;
            string runId = Session.ReadSnapshot().runId;
            var request = new SettlementRequest { requestId = "settle-" + runId, expectedRevision = Meta.Profile.revision, runId = runId };
            RunMeta(() => Meta.SettleAsync(request), _ =>
            {
                // Settlement already wrote the checkpoint and receipt atomically; no second run save.
                pendingStart = null;
                if (navigation != null) navigation.RequestLobby(); else EnterLobby();
            });
        }
        void RenderMetaMenu()
        {
            var rules = PreviewConfig;
            PlayerProfileData p;
            try { p = Meta.Profile; }
            catch (Exception e)
            {
                invalidSave = true; error = PlayerError(e);
                Show<MenuUI>(new MenuUIData { context = Context(rules.presentation), metaEnabled = true,
                    characterName = "프로필 복구 필요", characterDetails = "원본을 보존했습니다. 저장 파일을 확인해 주세요.",
                    growthDetails = "프로필을 읽을 수 없어 출전과 성장을 중단했습니다.", error = error,
                    hud = new RunHUDData { header = "탐험 준비", notice = error } }); return;
            }
            var l = p.loadout; var policy = Meta.Policy;
            int rank = p.characters.Single(x => x.characterId == l.characterId).rank;
            bool unsettled = Meta.NeedsSettlement;
            var gear = new List<UIChoiceData>();
            for (int i = 0; i < 3; i++)
            {
                int slot = i; string current = l.equipment[i];
                var owned = new[] { "" }.Concat(p.equipment.Where(x => rules.growth.equipment.Any(e => e.id == x.definitionId && (int)e.slot == slot)).Select(x => x.instanceId)).ToArray();
                var item = p.equipment.FirstOrDefault(x => x.instanceId == current);
                string label = item == null ? "장착 없음" : KoreanText.Content(rules.Equipment(item.definitionId).label);
                gear.Add(Choice("meta-gear-" + slot, new[] { "무기", "방어구", "장신구" }[slot] + " · " + label + (owned.Length > 1 ? "  ›" : "\n보유 장비 없음"),
                    () => ChangeMetaLoadout(p, next => next.equipment[slot] = owned[(Array.IndexOf(owned, current ?? "") + 1) % owned.Length]),
                    owned.Length > 1, ButtonPurpose.Navigation));
            }
            for (int i = 0; i < 6; i++)
            {
                int slot = i;
                var candidates = p.dice.Where(x => x.instanceId == l.dice[slot] || !l.dice.Contains(x.instanceId)).ToArray();
                var current = p.dice.Single(x => x.instanceId == l.dice[slot]);
                gear.Add(Choice("meta-die-" + slot, "주사위 " + (slot + 1) + " · " + KoreanText.Content(rules.Die(current.definitionId).label) +
                    (candidates.Length > 1 ? "  ›" : " · 장착 중"), () => ChangeMetaLoadout(p,
                        next => next.dice[slot] = candidates[(Array.FindIndex(candidates, x => x.instanceId == current.instanceId) + 1) % candidates.Length].instanceId),
                    candidates.Length > 1, ButtonPurpose.Navigation));
            }
            Show<MenuUI>(new MenuUIData
            {
                context = Context(rules.presentation), metaEnabled = true,
                hud = new RunHUDData { header = "탐험 준비", notice = error ?? (unsettled ? "완료된 여정의 정산을 먼저 확인해 주세요." : "성장 재화 " + p.growthCurrency + " · 출전 설정 자동 저장") },
                characterName = KoreanText.Content(rules.growth.characterName) + " · 성장 " + rank + "단계",
                characterDetails = "다음 여정의 기본 능력치 (장비 효과 별도)\n\n체력 " + checked(rules.growth.startingMaxHp + rank * policy.hpPerRank) +
                    "   위력 " + checked(rules.growth.startingPower + rank * policy.powerPerRank) + "   방어력 " + checked(rules.growth.startingGuard + rank * policy.guardPerRank) +
                    "\n\n보유 장비 " + p.equipment.Length + "개 · 주사위 " + p.dice.Length + "개\n출전 장비와 주사위는 세팅에서 선택합니다.",
                characters = p.characters.Select(c => Choice("meta-character-" + c.characterId, "선택됨 · " + KoreanText.Content(rules.growth.characterName),
                    () => ChangeMetaLoadout(p, next => next.characterId = c.characterId), false, ButtonPurpose.Navigation, true)).ToArray(),
                growthDetails = "영구 성장 재화  " + p.growthCurrency + "\n\n모험가 성장  " + rank + " / " + policy.maxRank +
                    "\n다음 단계 비용  " + (rank >= policy.maxRank ? "최대 단계" : policy.Cost(rank).ToString()) +
                    "\n단계당 체력 +" + policy.hpPerRank + " · 위력 +" + policy.powerPerRank + " · 방어력 +" + policy.guardPerRank +
                    "\n\n영구 성장은 다음 새 여정에 적용됩니다.\n진행 중 여정의 레벨·골드·획득 장비·주사위는 해당 여정에만 적용됩니다.",
                growthChoices = new[] { Choice("meta-grow", rank >= policy.maxRank ? "최대 성장 단계" : "모험가 성장 · " + policy.Cost(rank),
                    () => GrowMetaCharacter(p), rank < policy.maxRank && p.growthCurrency >= policy.Cost(rank)) },
                loadoutChoices = gear.ToArray(),
                trials = p.wildcards.Select(id => Choice("trial-" + id, KoreanText.Content(rules.Action(id).label),
                    () => ChangeMetaLoadout(p, next => next.wildcardId = id), true, ButtonPurpose.Trial, l.wildcardId == id)).ToArray(),
                caps = Enum.GetValues(typeof(Grade)).Cast<Grade>().Select(grade => Choice("cap-" + grade, ShortGrade(grade),
                    () => ChangeMetaLoadout(p, next => next.cap = grade), true, ButtonPurpose.Grade, l.cap == grade)).ToArray(),
                start = Choice("new", unsettled ? "이전 여정 정산 필요" : savedPreview != null && savedPreview.phase != RunPhase.Result ? "현재 여정 포기 후 새 여정" : "새 여정", StartNewJourney, !invalidSave && !unsettled),
                resume = savedPreview != null ? Choice("continue", unsettled ? "완료 여정 정산하기" : savedPreview.phase == RunPhase.Result ? "지난 결과 확인" : "저장된 여정 이어하기", ContinueJourney, !invalidSave) : null,
                error = error
            });
        }
    }
}
