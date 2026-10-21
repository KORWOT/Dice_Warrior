using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FateDice;
using UnityEngine;

namespace DiceWarrior.SymbolLoop
{
    public sealed class SymbolLoopApp : MonoBehaviour
    {
        public UIManager ui;
        public SymbolLoopConfig config;
        public LoopSession Session { get; private set; }
        public bool IsPresenting => busy;
        Coroutine presentation;
        LoopStore store;
        Command pending;
        string notice="", menu="";
        bool busy, lobby=true, confirmAbandon, confirmRecovery, pendingUnavailable;
        int screenWidth, screenHeight;
        Rect safeArea;
        Func<uint> nextSeed;

        void Start()
        {
            if(Application.platform==RuntimePlatform.Android) Screen.orientation=ScreenOrientation.Portrait;
            Open(Path.Combine(Application.persistentDataPath,"symbol-loop-v1","player.json"));
        }
        // Integration tests provide an isolated path before Start; no scene-wide search or global override.
        public void Open(string path, Func<uint> seedSource=null)
        {
            if(store!=null) return;
            nextSeed=seedSource ?? (()=>BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(),0));
            store=new LoopStore(path);
            try { Session=new LoopSession(store,config.CreateRules()); pending=store.ReadPending(); }
            catch(Exception e) { notice=e.Message; pendingUnavailable=Session!=null; }
            Render();
        }
        void Update()
        {
            if(Screen.width==screenWidth && Screen.height==screenHeight && safeArea==Screen.safeArea) return;
            screenWidth=Screen.width; screenHeight=Screen.height; safeArea=Screen.safeArea; ui.Root.ApplySafeArea();
        }
        void Dispatch(Command command)
        {
            if(busy) return;
            busy=true; pending=command;
            var before=Session.Snapshot;
            try
            {
                store.WritePending(command);
                Session.Execute(command);
                store.ClearPending(command.id);
                pending=null; notice=""; menu=""; confirmAbandon=false;
                if(command.kind=="start") lobby=false;
            }
            catch(Exception e) { notice=e.Message; busy=false; Render(); return; }
            var after=Session.Snapshot;
            // Replayed receipts and restored saves bind directly; animation never issues a domain command.
            if(after.revision!=before.revision && before.run!=null &&
                (command.kind=="travel" || command.kind=="combat") && ui.ActiveScreen is SymbolLoopScreen view)
            {
                ui.SetInputLocked(true);
                presentation=StartCoroutine(Present(view.playback.Play(command.kind,before.run,after.run)));
                return;
            }
            busy=false; Render();
        }
        IEnumerator Present(IEnumerator sequence)
        {
            // Flatten nested routines so any visual failure also releases the input lock.
            var stack=new Stack<IEnumerator>(); stack.Push(sequence);
            while(stack.Count>0)
            {
                object current=null; bool moved;
                try { moved=stack.Peek().MoveNext(); if(moved) current=stack.Peek().Current; }
                catch(Exception error) { notice="연출을 마무리하고 저장 상태를 표시합니다: "+error.Message; break; }
                if(!moved) { stack.Pop(); continue; }
                if(current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
            presentation=null; busy=false; ui.SetInputLocked(false); Render();
        }
        void CancelPresentation(bool render)
        {
            if(presentation!=null) { StopCoroutine(presentation); presentation=null; }
            bool wasBusy=busy; busy=false;
            if(ui && wasBusy) ui.SetInputLocked(false);
            if(render && wasBusy && store!=null && ui && ui.ActiveScreen) Render();
        }
        void OnDisable() => CancelPresentation(true);
        void OnApplicationPause(bool paused) { if(paused) CancelPresentation(true); }
        void Reload()
        {
            try
            {
                if(Session==null) Session=new LoopSession(store,config.CreateRules()); else Session.Reload();
                pending=store.ReadPending(); pendingUnavailable=false; notice=""; menu=""; confirmRecovery=false;
            }
            catch(Exception e) { notice=e.Message; }
            Render();
        }
        void InspectRecovery()
        {
            try
            {
                var saved=store.InspectBackup();
                notice=$"백업: 성장 재화 {saved.profile.currency} / 성장 {saved.profile.growth}단계\n"+
                    (saved.run==null ? "진행 중인 여정 없음" : $"지역 {saved.run.region} · HP {saved.run.hp}")+
                    "\n복원하면 이 백업 이후의 진행을 되돌립니다.";
                confirmRecovery=true;
            }
            catch(Exception e) { notice=e.Message; }
            Render();
        }
        void Restore()
        {
            try
            {
                store.RestoreBackup(); Session=new LoopSession(store,config.CreateRules());
                store.DiscardPending();
                pending=null; pendingUnavailable=false; notice="백업을 복원했습니다."; confirmRecovery=false;
            }
            catch(Exception e) { notice=e.Message; }
            Render();
        }
        static string SymbolName(Symbol symbol) => new[]{"검","지팡이","불","방패"}[(int)symbol];
        static string TileName(Tile tile) => new[]{"출발","전투","이벤트","보물","상점","휴식"}[(int)tile];
        static string IntentName(Enemy enemy) => enemy.intent==Intent.Defend ? $"방어 {enemy.attack}" :
            enemy.intent==Intent.Heavy ? $"강공격 {enemy.attack+enemy.attack/2}" : $"공격 {enemy.attack}";
        public void Render()
        {
            CancelPresentation(false);
            var data=new LoopScreenData {title="DICE WARRIOR", subtitle="기호를 모으고, 순환을 넘어라",detail="",history="",stats="",center=""};
            var labels=new List<string>(); var actions=new List<Action>();
            data.onClose=()=>CancelPresentation(false);
            void Add(string text, Action action) { labels.Add(text); actions.Add(()=>{ if(!busy) action(); }); }
            void Cmd(string text,string kind,string arg="",uint seed=0)
            {
                var command=Session.Prepare(kind,arg,seed); Add(text,()=>Dispatch(command));
            }
            if(Session==null)
            {
                data.detail="저장을 열 수 없습니다.\n"+notice;
                Add("다시 불러오기",Reload);
                if(confirmRecovery) { Add("이 백업으로 복원",Restore); Add("취소",()=>{confirmRecovery=false;notice="";Render();}); }
                else Add("백업 내용 확인",InspectRecovery);
            }
            else if(pending!=null || pendingUnavailable)
            {
                data.detail=notice; data.history="저장에 실패한 입력을 같은 요청으로 다시 시도할 수 있습니다.";
                if(pending!=null) Add("같은 입력 재시도",()=>Dispatch(pending));
                Add("저장 다시 불러오기",Reload);
                Add("이 입력 취소",()=>{
                    try { store.DiscardPending(); pending=null; pendingUnavailable=false; Reload(); }
                    catch(Exception e) { notice=e.Message; Render(); }
                });
                if(confirmRecovery) Add("이 백업으로 복원",Restore); else Add("백업 내용 확인",InspectRecovery);
            }
            else
            {
                var doc=Session.Snapshot;
                var run=doc.run;
                if(lobby || run==null)
                {
                    data.title="DICE WARRIOR";
                    data.stats=$"성장 재화 {doc.profile.currency}   ·   영구 성장 {doc.profile.growth}단계";
                    bool active=run!=null && !(run.phase==Phase.Result && run.settled);
                    data.center="새로운 순환";
                    data.detail="숫자 주사위로 12칸을 이동하세요.\n전투는 전체 기호 조합으로 자동 발동합니다.\n세 지역을 완주하고 최종 수호자에게 도전하세요.";
                    data.history=$"시작 주사위: {config.rules.dice.FirstOrDefault(d=>d.id==doc.profile.startingDie)?.name ?? "다시 선택하세요"}\n"+
                        $"시작 무기: {config.rules.gear.FirstOrDefault(g=>g.id==doc.profile.startingWeapon)?.name ?? "다시 선택하세요"}\n"+
                        "전투 주사위 1개 → 2개 → 3개";
                    if(active) Add("여정 이어하기",()=>{lobby=false;Render();});
                    else
                    {
                        uint seed=nextSeed(); if(seed==0) seed=1;
                        Cmd("새 여정 시작","start",seed:seed);
                        if(doc.profile.currency>=20*(doc.profile.growth+1)) Cmd($"영구 HP +5 · {20*(doc.profile.growth+1)} 재화","grow");
                        foreach(var die in config.rules.dice)
                            Cmd((doc.profile.startingDie==die.id?"✓ ":"")+die.name+" 주사위","loadout","die:"+die.id);
                        foreach(var gear in config.rules.gear.Where(g=>g.slot==0))
                            Cmd((doc.profile.startingWeapon==gear.id?"✓ ":"")+gear.name,"loadout","weapon:"+gear.id);
                    }
                }
                else
                {
                    data.title=$"REGION {run.region:00} / 03";
                    data.subtitle=run.phase==Phase.Combat ? "기호 조합 전투" : run.phase==Phase.Result ? "여정의 기록" : "순환 캠페인";
                    data.stats=$"HP {run.hp} / {run.maxHp}   위력 {run.power}   수호 {run.guard}\n"+
                        $"Lv.{run.level}   XP {run.xp}/{run.level*8}   골드 {run.gold}   완주 {run.completedRegions}/3";
                    data.run=run;
                    data.showBoard=run.phase!=Phase.Combat && run.phase!=Phase.Result;
                    data.position=run.position;
                    data.center=$"REGION 0{run.region}\n{run.position:00} / 12";
                    for(int i=0;i<12;i++) data.tiles[i]=$"{i:00}\n{TileName(run.board[i])}";
                    for(int i=0;i<run.dice.Count;i++)
                    {
                        var die=run.rules.dice.Single(d=>d.id==run.dice[i].definitionId);
                        data.dice[i]=(run.lastTurn!=null ? SymbolName(run.lastTurn.faces[i]) : die.name)+"\n주사위 "+(i+1);
                    }
                    data.detail=run.message;
                    if(run.lastTurn!=null)
                    {
                        var turn=run.lastTurn;
                        data.history=string.Join("\n",turn.skills.Select(s=>s.status=="applied" ?
                            $"{s.name} · 피해 {s.damage} / 보호 {s.block}" : $"{s.name} · 적 처치로 생략"));
                        string intent=turn.intent==Intent.Attack?"공격":turn.intent==Intent.Heavy?"강공격":"방어";
                        data.history+="\n"+(turn.retaliation ? $"적 행동: {intent} · 받은 피해 {turn.retaliationDamage}" : "적 처치 · 반격 없음");
                    }
                    else data.history=string.Join("\n",run.equipment.Where(id=>id!=null).Select(id=>run.rules.gear.Single(g=>g.id==id).name));
                    if(confirmAbandon)
                    {
                        data.detail="현재 여정을 종료하고 완료한 지역만 정산할까요?";
                        Cmd("여정 종료 확정","abandon"); Add("계속 모험하기",()=>{confirmAbandon=false;Render();});
                    }
                    else if(menu=="dice")
                    {
                        data.detail="교체할 주사위 종류를 선택하세요.";
                        foreach(var die in run.rules.dice)
                        { string id=die.id; Add(die.name+" · "+string.Join("/",die.faces.Select(SymbolName)),()=>{menu="slot:"+id;Render();}); }
                        Add("뒤로",()=>{menu="";Render();});
                    }
                    else if(menu.StartsWith("slot:"))
                    {
                        data.detail="교체할 슬롯을 선택하세요.";
                        for(int i=0;i<run.dice.Count;i++) Cmd($"주사위 {i+1} 교체","choose",$"die:{i}:{menu.Substring(5)}");
                        Add("뒤로",()=>{menu="dice";Render();});
                    }
                    else switch(run.phase)
                    {
                        case Phase.Map:
                            Cmd("이동 주사위 굴리기","travel");
                            Add("로비로",()=>{lobby=true;Render();});
                            Add("여정 종료",()=>{confirmAbandon=true;Render();}); break;
                        case Phase.Combat:
                            data.center=run.enemy.name;
                            data.detail=$"{run.enemy.name}\nHP {run.enemy.hp} / {run.enemy.maxHp}   보호막 {run.enemy.shield}\n"+
                                $"다음 행동: {IntentName(run.enemy)} · 전체 조합 또는 기본기";
                            Cmd("기호 주사위 굴리기","combat");
                            Add("여정 종료",()=>{confirmAbandon=true;Render();}); break;
                        case Phase.Event:
                            data.detail="오래된 제단\n조사하거나 체력을 바쳐 보물을 얻을 수 있습니다.";
                            Cmd("조사 · XP +4","choose","study");
                            if(run.hp>8) Cmd("헌납 · HP -8 / 골드 +25","choose","risk");
                            Cmd("지나가기","choose","leave"); break;
                        case Phase.Rest:
                            data.detail="야영지\n회복과 훈련 중 하나를 선택하세요.";
                            Cmd("휴식 · HP +22","choose","heal"); Cmd("훈련 · XP +8","choose","train"); break;
                        case Phase.Treasure:
                        case Phase.Shop:
                            bool shop=run.phase==Phase.Shop;
                            data.detail=shop ? $"길 위의 상점 · 보유 골드 {run.gold}\n장비/주사위 20골드 · 회복 12골드" : "발견한 보물\n장비 또는 주사위 교체 중 하나를 선택하세요.";
                            if(shop && run.gold>=12) Cmd("회복 +25 · 12골드","choose","heal");
                            if(!shop || run.gold>=20)
                            {
                                foreach(var gear in run.rules.gear) Cmd(gear.name,"choose","gear:"+gear.id);
                                Add("주사위 교체",()=>{menu="dice";Render();});
                            }
                            Cmd("떠나기","choose","leave"); break;
                        case Phase.LapReward:
                            data.detail=$"지역 {run.region} 완주 보상\n골드 +{run.rules.lapGold} / XP +{run.rules.lapXp} / HP +{run.rules.lapHeal}\n"+
                                (run.region<3 ? "전투 주사위 1개가 추가됩니다." : "장비를 선택하고 최종 보스에게 도전하세요.");
                            if(run.region<3) Cmd("확정 보상 수령","lap");
                            else foreach(var gear in run.rules.gear) Cmd(gear.name,"lap",gear.id);
                            break;
                        case Phase.Result:
                            data.center=run.won ? "VICTORY\n순환의 수호자 격파" : "여정 종료";
                            data.detail=(run.won?"최종 수호자 격파!":"여정 종료")+$"\n완주 지역 {run.completedRegions}/3\n"+
                                (run.settled?$"성장 재화 {run.payout} 정산 완료":$"성장 재화 {run.completedRegions*run.rules.regionCurrency+(run.won?run.rules.winCurrency:0)}");
                            if(!run.settled) Cmd("성장 재화 받기","settle");
                            else Add("로비로",()=>{lobby=true;Render();}); break;
                    }
                }
                if(!string.IsNullOrEmpty(notice)) data.history=notice;
            }
            data.labels=labels.ToArray(); data.actions=actions.ToArray();
            ui.Show<SymbolLoopScreen>(data);
        }
        void OnDestroy() { if(ui) ui.CloseAll(); }
    }
}
