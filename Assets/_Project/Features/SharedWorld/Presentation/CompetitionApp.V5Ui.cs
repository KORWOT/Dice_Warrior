using System;
using System.Collections.Generic;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    // Presentation only. Every mutation still goes through the original session/account boundary.
    public sealed partial class CompetitionApp
    {
        string v5DetailsReturn="",v5LastChange="";
        V5DiceNetData suspendedV5Net,managementV5Net;
        bool UseV5Ui=>CompetitionV5Content.IsEnabled(!lobby && Session?.Snapshot.state!=null?Session.Snapshot.state.definition:definition);
        static bool IsDetails(string value)=>value=="stats" || value=="skills" || value=="elements" || value=="tags" || value=="effects";
        void OpenV5Details(string section="stats",V5DiceNetData fromNet=null)
        {v5DetailsReturn=menu;suspendedV5Net=fromNet;diceDetails=null;menu=section;detailPage=0;Render();}
        void V5Menu(string value){menu=value;diceDetails=null;if(value=="adventure")managementV5Net=null;Render();}
        void OpenV5Management(V5DiceNetData fromNet)
        {managementV5Net=fromNet;diceDetails=null;menu="adventure";Render();}
        public bool HandleBack()
        {
            if(!active || busy || !UseV5Ui)return false;
            if(diceDetails!=null){diceDetails=null;Render();return true;}
            if(confirmNewGame)confirmNewGame=false;
            else if(confirmAbandon)confirmAbandon=false;
            else if(confirmRestore)confirmRestore=false;
            else if(confirmAccountRestore)confirmAccountRestore=false;
            else if(IsDetails(menu)) {menu=v5DetailsReturn;v5DetailsReturn="";diceDetails=suspendedV5Net;suspendedV5Net=null;detailPage=0;}
            else if(menu=="adventure" && managementV5Net!=null){menu="";diceDetails=managementV5Net;managementV5Net=null;}
            else if(menu=="discard")menu="account";
            else if(menu!="")menu="";
            else if(!lobby){menu="adventure";managementV5Net=null;}
            else return false;
            Render();return true;
        }
        static string V5Phase(CompetitionPhase phase)=>phase==CompetitionPhase.Map?"이동 대기":phase==CompetitionPhase.Encounter?"사건 선택":phase==CompetitionPhase.Combat?"전투":phase==CompetitionPhase.LapReward?"완주 보상 선택":"내 모험 종료";
        static string V5EndReason(CompetitionState state)=>state.endReason=="all-complete"?"모든 모험가가 완료 또는 탈락했습니다.":state.endReason=="round-limit"?$"{state.definition.maxRounds}라운드 도달로 경기가 종료되었습니다.":"경기가 종료되었습니다.";
        static string V5ChangeSummary(CompetitionState before,CompetitionState after,string kind)
        {
            if(before==null || after==null || kind.StartsWith("combat_",StringComparison.Ordinal))return "";
            var a=before.participants[0];var b=after.participants[0];var changes=new List<string>();
            if(kind=="travel")return $"이동 주사위 {b.lastTravel} · 실제 {Math.Min(b.lastTravel,before.definition.world.places.Length-a.position)}칸 → {after.definition.world.places[b.position].name}"+(b.phase==CompetitionPhase.LapReward?" · 출발점에서 완주 보상":"");
            if(kind=="lap" && b.dice.Count>a.dice.Count)changes.Add($"전투 주사위 {a.dice.Count} → {b.dice.Count}개 · "+after.definition.combat.dice.Single(x=>x.id==b.dice.Last().definitionId).name+" 합류");
            if(a.hp!=b.hp)changes.Add($"HP {a.hp} → {b.hp}");if(a.gold!=b.gold)changes.Add($"골드 {a.gold} → {b.gold}");
            if(a.xp!=b.xp || a.level!=b.level)changes.Add($"레벨 {a.level} → {b.level} · XP {a.xp} → {b.xp}");
            return (kind=="lap"?"완주 보상 수령":kind=="choose"?"사건 해결":kind=="abandon"?"내 모험 포기":"AI 라운드 완료")+(changes.Count>0?" · "+string.Join(" / ",changes):"");
        }
        string V5Notice()
        {
            string text=notice;
            if(lobby && adventurerError!="")text=adventurerError;
            if(busy)return menu=="account" || Account?.Busy==true?"계정 요청 처리 중 · 서버 확인을 기다리고 있습니다.":"확정 상태를 처리하고 있습니다. 잠시 기다려 주세요.";
            if(text.IndexOf("Sharing violation",StringComparison.OrdinalIgnoreCase)>=0 || text.IndexOf("access to the path",StringComparison.OrdinalIgnoreCase)>=0)
                return "저장 파일을 사용할 수 없습니다. 마지막 확정 상태를 유지합니다. 파일 사용 상태를 확인한 뒤 다시 시도해 주세요.";
            return text;
        }
        void RenderV5(CompetitionState state)
        {
            var shell=new V5ShellData {mode="lobby",title="DICE WARRIOR",summary="공동 모험 · 인간 1명 + AI 3명",body="",notice=V5Notice()};
            var data=new CompetitionScreenData {title=shell.title,subtitle="",status="",detail="",history="",state=lobby?null:state,onClose=CancelPresentation,shell=shell,v5NetData=diceDetails,reducedMotion=ReducedMotion};
            var choices=new List<V5ShellAction>();var tools=new List<V5ShellAction>();var tabs=new List<V5ShellAction>();
            V5ShellAction Item(string id,string label,string detail,Action action,bool available=true,bool selected=false)=>new V5ShellAction {id=id,label=label,detail=detail,enabled=available&&!busy,selected=selected,invoke=()=>{if(!busy)action();}};
            void Add(string id,string label,string detail,Action action,bool available=true,bool selected=false)=>choices.Add(Item(id,label,detail,action,available,selected));
            void Tool(string id,string label,Action action,bool available=true)=>tools.Add(Item(id,label,"",action,available));
            void Command(string label,string detail,string kind,string arg="",bool available=true)=>Add(kind+":"+arg,label,detail,()=>Dispatch(kind,arg),available);
            void Back(){shell.back=busy?null:(Action)(()=>HandleBack());}
            if(confirmNewGame)
            {
                shell.mode="confirm";shell.title="새 모험을 시작할까요?";shell.summary="현재 모험 진행을 새 경기로 교체합니다.";
                shell.body="영구 성장과 재화는 유지됩니다.\n새 경기 저장이 성공해야 교체하며 이전 진행 원문은 별도 사본으로 보존합니다.\n취소하면 기존 모험을 계속할 수 있습니다.";
                Add("confirm-new","새 모험 시작 확정","현재 모험을 교체합니다.",()=>{confirmNewGame=false;StartMatch(true);});Back();
            }
            else if(confirmAccountRestore || confirmRestore)
            {
                shell.mode="confirm";shell.title=confirmRestore?"공동 경기 백업 복원":restorePreferences?"출전 설정 백업 복원":"계정 백업 복원";
                shell.summary="확인만으로 현재 저장을 바꾸지 않습니다.";shell.body=notice+"\n\n확정하면 해당 로컬 상태를 되돌리고 현재 원문을 보존합니다. 서버 미결 요청은 취소하지 않습니다.";
                if(confirmRestore)Add("restore-match","경기 백업 복원 확정","공동 모험 진행만 복원합니다.",Restore);
                else Add("restore-account",restorePreferences?"출전 설정 복원 확정":"계정 백업 복원 확정","복원 후 오류·미결이 없으면 서버 프로필을 다시 확인합니다.",()=>{confirmAccountRestore=false;AccountAction(ct=>Account.RestoreBackupAsync(restorePreferences,ct));});Back();
            }
            else if(menu=="account" && Account!=null)
            {
                shell.mode="account";shell.title="계정 · 영구 성장";var current=profile??new Profile();long price=20L*(current.growth+1L);
                shell.summary=Account.HasProfile?$"보유 재화 {current.currency} · 성장 {current.growth}단계 · 영구 HP +{current.growth*5}":"서버 프로필 미확인";
                string reason=Account.HasLegacyRun?"이전 1인 모험 기록 보존으로 구매 보류. 현재 공동 모험 UI에는 해당 기록의 종료/정산 기능이 없습니다.":!Account.Ready?AccountMessage():current.currency<price?$"재화 부족 · {price-current.currency} 더 필요합니다.":"구매할 수 있습니다.";
                shell.body=(Account.HasProfile?$"[구매 후 예상 · 승인 전에는 적용되지 않음]\n재화 {current.currency} → {current.currency-price}\n성장 {current.growth} → {current.growth+1}단계 · 영구 HP +5\n\n":"")+
                    "새 모험 시작 시 인간과 AI 3명에게 같은 성장 보너스를 적용합니다. 진행 중 경기는 유지됩니다.\n\n"+reason+"\n\n[저장 상태]\n"+(Account.StorageError!=null?"계정 파일 읽기 오류":Account.Pending!=null?"이전 요청 결과 미확정":Account.Ready?"서버 확인 완료 · 로컬 계정 저장 확인":"계정 확인 필요");
                if(current.currency<price)shell.body=shell.body.Replace($"재화 {current.currency} → {current.currency-price}",$"재화 {current.currency} · 구매에 {price-current.currency} 부족");
                Add("grow",$"영구 HP +5 구매 · {price} 재화","서버 승인 후 확정됩니다.",()=>AccountAction(ct=>Account.GrowAsync(ct),true),Account.Ready&&!Account.HasLegacyRun&&current.currency>=price);
                Add("refresh","프로필 다시 확인","서버의 확정 재화·성장을 조회합니다.",()=>AccountAction(ct=>Account.RefreshAsync(ct)));
                Add("account-reload","계정 저장 다시 읽기","원문을 자동 초기화하지 않습니다.",()=>AccountAction(async ct=>{Account.Reload();await Account.RefreshAsync(ct);}));
                if(Account.Pending!=null)
                {
                    if(LoopSession.IsBackend(Account.Pending.kind))Add("retry","같은 요청 다시 확인","기존 요청 ID로 결과를 확인합니다.",()=>AccountAction(ct=>Account.RetryPendingAsync(ct)));
                    else if(Account.Pending.backend==null)Add("discard","이전 입력 보존 취소","서버 요청을 취소하는 기능이 아닙니다.",()=>V5Menu("discard"));
                }
                Add("account-backup","계정 백업 확인","로컬 계정 기록 · 서버 권한은 별도 확인",()=>InspectAccountBackup(false));
                Add("loadout-backup","출전 설정 백업 확인","다음 모험의 주사위·무기 선택",()=>InspectAccountBackup(true));
                Tool("match-backup","경기 백업 확인",InspectBackup,store!=null);Back();
            }
            else if(menu=="discard")
            {
                shell.mode="confirm";shell.title="이전 로컬 입력을 보존하고 취소할까요?";shell.body="미결 입력 원문을 별도 보존 사본으로 옮깁니다. 서버 요청은 취소하지 않습니다.";
                Add("discard-confirm","입력 보존 취소 확정","원문 보존 후 계정 상태를 다시 확인합니다.",()=>{menu="account";AccountAction(ct=>Account.DiscardLocalPendingAsync(ct));});Back();
            }
            else if(menu=="settings")
            {
                shell.mode="settings";shell.title="설정 · 저장 복구";shell.summary="모험 진행과 별도로 확인하는 관리 기능";
                shell.body="보기 설정은 경기 결과를 바꾸지 않습니다.\n백업은 내용을 확인한 뒤 복원 여부를 선택합니다.";
                Add("motion","동작 줄이기: "+(ReducedMotion?"켜짐":"꺼짐"),"주사위 회전·이동 연출을 간소화합니다.",()=>SetReducedMotion(!ReducedMotion));
                Add("backup","경기 백업 확인","현재 진행은 복원을 확정하기 전까지 유지합니다.",InspectBackup,store!=null);
                Back();
            }
            else if(Session==null)
            {
                shell.mode="error";shell.title="저장 또는 연결 확인이 필요합니다";shell.body="기존 기록은 보존했습니다. 원인을 확인한 뒤 다시 읽거나 백업을 확인하세요.\n"+competitionError;
                if(store!=null){Add("reload","경기 저장 다시 읽기","현재 저장을 다시 읽습니다.",Reload);Add("backup","경기 백업 확인","확정 전에는 현재 저장을 바꾸지 않습니다.",InspectBackup);}
                else Add("config","HTTP 설정 다시 불러오기","접속 설정을 확인한 뒤 다시 시도합니다.",OpenDefault);
                if(Account!=null)Tool("account","계정 관리",()=>V5Menu("account"));
            }
            else if(menu=="adventurers")
            {
                shell.mode="adventurers";shell.title="다음 모험가 선택";shell.summary="선택 즉시 저장 · 다음 새 모험에 적용";shell.body="진행 중인 모험가와 경기 기록은 유지됩니다.";
                var rosterData=new CompetitionScreenData();CompetitionElementView.Roster(rosterData,definition,profile,adventurerSelection?.adventurerId);
                for(int i=0;i<definition.adventurers.Length;i++){var hero=definition.adventurers[i];Add(hero.id,hero.name+" 선택",rosterData.adventurerCards[i],()=>SelectAdventurer(hero.id),adventurerError=="",hero.id==adventurerSelection?.adventurerId);}
                if(adventurerError!="")Tool("hero-reload","모험가 선택 다시 읽기",()=>{ReadAdventurer();notice="";Render();});Back();
            }
            else if(menu=="loadout")
            {
                shell.mode="loadout";shell.title="다음 모험의 출전 구성";shell.summary="선택 즉시 저장 · 이 화면에서 연속 변경 가능";
                string hero=adventurerSelection?.adventurerId??"wanderer";
                shell.body="[시작 주사위]\n"+CompetitionV5UiText.Die(definition,hero,profile.startingDie)+"\n\n[시작 무기]\n"+CompetitionV5UiText.Gear(definition,null,profile.startingWeapon)+"\n\n돌아가기는 저장 취소가 아닙니다. 진행 중인 경기에는 소급 적용하지 않습니다.";
                foreach(var die in definition.combat.dice){var id=die.id;Add("die:"+id,"시작 주사위 · "+die.name,CompetitionV5UiText.Die(definition,hero,id),()=>SelectLoadout("die",id),true,id==profile.startingDie);}
                foreach(var gear in definition.combat.gear.Where(g=>g.slot==0)){var id=gear.id;Add("weapon:"+id,"시작 무기 · "+gear.name,CompetitionV5UiText.Gear(definition,null,id),()=>SelectLoadout("weapon",id),true,id==profile.startingWeapon);}
                Tool("dice-details","선택 주사위 6면",()=>ShowDiceDetails());Back();
            }
            else if(IsDetails(menu))
            {
                var shown=lobby?null:state?.participants[0];var d=shown==null?definition:state.definition;var view=new CompetitionScreenData();
                int pages=CompetitionElementView.Details(view,d,profile,shown,adventurerSelection?.adventurerId??"wanderer",menu,detailPage);detailPage=Math.Max(0,Math.Min(detailPage,pages-1));
                shell.mode="details";shell.title=view.growthTitle.Replace("V5 ","");shell.summary=(shown==null?"다음 모험 기준":"현재 경기 기준")+" · "+view.growthSummary;
                shell.body=(menu=="stats"?string.Join("\n",view.statValues.Select((v,i)=>v+" · "+view.statSources[i].Replace("\n"," ")))+"\n\n":"")+view.growthBody;
                shell.body=CompetitionV5UiText.Friendly(d,shell.body).Replace("기존 계정","영구 성장 보너스").Replace("(이번 경기)",shown==null?"(다음 모험)":"(현재 경기)");
                var names=new[]{"능력치","주사위·스킬","속성·상성","태그 조회","효과 출처"};var ids=new[]{"stats","skills","elements","tags","effects"};
                for(int i=0;i<ids.Length;i++){string id=ids[i];tabs.Add(Item(id,names[i],"",()=>{menu=id;detailPage=0;Render();},true,menu==id));}
                if(pages>1){Tool("previous","이전 페이지",()=>{detailPage--;Render();},detailPage>0);Tool("next","다음 페이지",()=>{detailPage++;Render();},detailPage<pages-1);}Back();
            }
            else if(lobby || state==null)
            {
                shell.title="공동 모험 · 로비";var selection=profile??new Profile();string hero=adventurerSelection?.adventurerId??"wanderer";
                string heroName=definition.adventurers.FirstOrDefault(a=>a.id==hero)?.name??"모험가 재선택 필요";
                string die=definition.combat.dice.FirstOrDefault(d=>d.id==selection.startingDie)?.name??"주사위 재선택 필요";
                string weapon=definition.combat.gear.FirstOrDefault(g=>g.id==selection.startingWeapon)?.name??"무기 재선택 필요";
                shell.summary=Account!=null&&!Account.HasProfile?"서버 프로필 미확인":$"재화 {selection.currency} · 영구 성장 {selection.growth}단계"+(Account!=null&&!Account.Ready?" · 마지막 확인값":"");
                shell.body=$"다음 새 모험 · {heroName}\n{die} 주사위 / {weapon}\n\n"+(state==null?"12장소 · 개인 3바퀴 → 최종 시련":$"저장된 경기 · {state.participants[0].name}\n{(state.finished?"경기 완료":V5Phase(state.participants[0].phase))} · 라운드 {Math.Min(state.worldRound,state.definition.maxRounds)}/{state.definition.maxRounds} · 완주 {state.participants[0].completedLaps}/{state.definition.lapsToFinish}")+
                    "\n공동 경기의 영구 재화 지급은 0입니다.";
                var newGame=Item("new","새 모험 시작",state==null?"현재 출전 구성으로 시작합니다.":"기존 경기를 교체하기 전에 확인합니다.",()=>{if(state==null)StartMatch();else{confirmNewGame=true;notice="";Render();}},(Account==null||Account.Ready)&&adventurerError=="");
                if(state!=null&&!state.finished)
                {shell.primary=Item("resume","경기 이어하기","저장된 "+V5Phase(state.participants[0].phase)+" 단계로 복귀합니다.",Resume);choices.Add(newGame);}
                else shell.primary=newGame;
                shell.compactChoices=true;
                if(state?.finished==true)Add("results","지난 경기 결과","완료된 경기의 순위·종료 이유를 봅니다.",Resume);
                Add("heroes","모험가 선택","다음 새 모험의 주인공",()=>V5Menu("adventurers"));
                Add("loadout","출전 선택","시작 주사위·무기",()=>V5Menu("loadout"),Account==null||Account.LoadoutError==null);
                Add("stats","출전 능력치","다음 모험 기준의 능력치·기술",()=>OpenV5Details(),adventurerError==""&&(Account==null||Account.HasProfile));
                if(Account!=null)Add("account","계정 관리","영구 성장·재화·저장 복구",()=>V5Menu("account"));
                Tool("net","주사위\n6면",()=>ShowDiceDetails());Tool("settings","설정·복구",()=>V5Menu("settings"));
                if(string.IsNullOrEmpty(shell.notice))shell.notice=AccountMessage();
            }
            else
            {
                var p=state.participants[0];var d=state.definition;
                shell.summary=$"세계 라운드 {Math.Min(state.worldRound,d.maxRounds)}/{d.maxRounds} · 내 완주 {p.completedLaps}/{d.lapsToFinish}";
                if(confirmAbandon&&!state.finished)
                {
                    shell.mode="confirm";shell.title="내 모험을 포기할까요?";shell.body="내 참가자만 탈락 처리됩니다. 다른 AI의 경기는 계속되며 이후 AI 라운드를 진행할 수 있습니다.";
                    Command("내 모험 포기 확정","포기는 진행 보존 후 로비 복귀와 다른 행동입니다.","abandon");Back();
                }
                else if(menu=="adventure")
                {
                    shell.mode="menu";shell.title="모험 메뉴 · 진행 관리";shell.body=p.name+" · "+V5Phase(p.phase)+"\n조회나 닫기는 현재 모험을 바꾸지 않습니다.";
                    if(state.lastRound.Count>0)shell.body+="\n\n[직전 AI 행동 결과]\n"+string.Join("\n",state.lastRound.Select(x=>x.name+": "+x.message));
                    Add("leave","로비로 · 진행 유지","현재 위치·사건·전투를 보존합니다.",Close);
                    if(!state.finished&&!p.finished&&!p.eliminated)Add("abandon","내 모험 포기","확인 후 내 참가자만 탈락합니다.",()=>{confirmAbandon=true;Render();});
                    Add("details","능력치 상세","현재 경기의 확정 능력치",()=>OpenV5Details());Tool("backup","경기 백업 확인",InspectBackup);Back();
                    Tool("motion","동작 줄이기: "+(ReducedMotion?"켜짐":"꺼짐"),()=>SetReducedMotion(!ReducedMotion));
                }
                else if(state.finished)
                {
                    shell.mode="results";shell.title="경기 결과 · 나의 "+p.rank+"위";shell.summary=V5EndReason(state);shell.body=CompetitionV5UiText.Results(state)+"\n\n영구 재화 지급 0 · 현재 공동 경기 순위 보상은 없습니다.";
                    shell.primary=Item("lobby","로비로","현재 경기 결과를 보존합니다.",Close);Tool("details","능력치 상세",()=>OpenV5Details());
                }
                else if(p.phase==CompetitionPhase.Combat)
                {
                    data.shell=null;data.v5Combat=true;data.v5Command=Dispatch;data.v5DiceInfo=()=>ShowDiceDetails();data.v5CombatInfo=()=>ShowDiceDetails(information:true);data.v5Notice=V5Notice();
                }
                else if(p.phase==CompetitionPhase.Encounter)
                {
                    shell.mode="encounter";shell.title=d.world.places[p.position].name+" · 사건 선택";
                    shell.body=$"주사위 눈 {p.lastTravel} → {d.world.places[p.position].name} · HP {p.hp}/{p.maxHp} · 골드 {p.gold}\n개인 사건입니다. 선택하면 비용과 효과가 확정됩니다.";
                    foreach(var choice in p.offer.choices)Command(choice.label,CompetitionV5UiText.Encounter(d,p,choice),"choose",choice.id,choice.available);
                    Tool("details","능력치 상세",()=>OpenV5Details());Tool("menu","모험 메뉴",()=>V5Menu("adventure"));
                }
                else if(p.phase==CompetitionPhase.LapReward)
                {
                    shell.mode="reward";shell.title=$"{p.completedLaps+1}바퀴 완주 보상";shell.summary="선택 즉시 수령 확정 · 아직 보상을 지급하지 않았습니다.";
                    shell.body=$"현재 확정 완주 {p.completedLaps}/{d.lapsToFinish} · HP {p.hp}/{p.maxHp}\n보유 주사위 {p.dice.Count}개: "+string.Join(" / ",p.dice.Select(x=>d.combat.dice.Single(y=>y.id==x.definitionId).name))+"\n\n"+(p.completedLaps<d.lapsToFinish-1?$"전투 주사위 {p.dice.Count} → {p.dice.Count+1}개\n후보 버튼을 누르면 수령하고 AI 행동 후 보드로 돌아갑니다.":"장비를 선택하면 수령 후 최종 시련에 진입합니다.");
                    if(p.completedLaps<d.lapsToFinish-1)
                        foreach(var die in d.combat.dice)Command(die.name+" 주사위 · 수령 확정",CompetitionV5UiText.Lap(state,die.id),"lap",die.id);
                    else foreach(var gear in d.combat.gear)Command(gear.name+" · 수령 확정",CompetitionV5UiText.Lap(state,gear.id),"lap",gear.id);
                    Tool("net","주사위 상세",()=>ShowDiceDetails(p.completedLaps<d.lapsToFinish-1));Tool("details","능력치 상세",()=>OpenV5Details());Tool("menu","모험 메뉴",()=>V5Menu("adventure"));
                }
                else
                {
                    shell.mode=p.phase==CompetitionPhase.Complete?"spectator":"board";shell.showBoard=true;
                    shell.title=p.phase==CompetitionPhase.Complete?"내 모험 종료 · 남은 경기 진행":"공동 모험 · 내 차례";
                    shell.summary+=$"\nHP {p.hp}/{p.maxHp} · 골드 {p.gold} · LV {p.level} · XP {p.xp}";
                    if(p.phase==CompetitionPhase.Complete)
                        Command("다음 AI 라운드",$"내 결과: {(p.eliminated?"탈락":"완료")} · "+p.message+$"\n남은 모험가 {state.participants.Count(x=>!x.finished&&!x.eliminated)}명","advance");
                    else Command("이동 주사위 굴리기","현재 "+d.world.places[p.position].name+$" · 다음 목표 {p.completedLaps+1}바퀴 완주","travel");
                    Tool("details","능력치 상세",()=>OpenV5Details());Tool("menu","모험 메뉴",()=>V5Menu("adventure"));
                    if(string.IsNullOrEmpty(shell.notice))shell.notice=v5LastChange;
                }
            }
            shell.choices=choices.ToArray();shell.tools=tools.ToArray();shell.tabs=tabs.ToArray();
            data.v5NetData=diceDetails;ui.Show<CompetitionScreen>(data);
        }
    }
}
