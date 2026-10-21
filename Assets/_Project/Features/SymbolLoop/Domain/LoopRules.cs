using System;
using System.Linq;

namespace DiceWarrior.SymbolLoop
{
    public static class LoopRules
    {
        public static uint Next(ref uint state)
        {
            if (state == 0) state = 0x9e3779b9;
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return state;
        }
        public static Tile[] Board(uint seed, int region)
        {
            uint rng = seed ^ unchecked((uint)region * 0x85ebca6b);
            var board = new[] { Tile.Start, Tile.Combat, Tile.Event, Tile.Treasure, Tile.Shop, Tile.Rest,
                Tile.Combat, Tile.Combat, Tile.Event, Tile.Treasure, Tile.Rest, Tile.Combat };
            for (int i = 11; i > 1; --i)
            {
                int j = 1 + (int)(Next(ref rng) % (uint)i);
                (board[i], board[j]) = (board[j], board[i]);
            }
            return board;
        }
        public static Run Start(Rules rules, Profile profile, uint seed, string id)
        {
            ValidateRules(rules);
            if (seed == 0 || string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Run identity");
            if (!rules.dice.Any(d => d.id == profile.startingDie) ||
                !rules.gear.Any(g => g.id == profile.startingWeapon && g.slot == 0)) throw new ArgumentException("Loadout");
            var run = new Run {id=id, rules=rules, seed=seed, travelRng=seed, combatRng=seed ^ 0xa511e9b3,
                hp=rules.hp + profile.growth * 5, maxHp=rules.hp + profile.growth * 5,
                power=rules.power, guard=rules.guard, board=Board(seed, 1), message="첫 번째 지역에 도착했습니다."};
            run.dice.Add(new DieSlot { instanceId=id+":starter", definitionId=profile.startingDie });
            run.equipment[0] = profile.startingWeapon;
            return run;
        }
        public static void Travel(Run run, int value)
        {
            Require(run, Phase.Map);
            if (value < 1 || value > 6) throw new ArgumentOutOfRangeException(nameof(value));
            run.lastTravel = value;
            run.lastTurn = null;
            if (run.position + value >= 12)
            {
                run.position = 0; run.phase = Phase.LapReward;
                run.message = "지역 완주! 확정 보상을 수령하세요.";
                return;
            }
            run.position += value;
            switch (run.board[run.position])
            {
                case Tile.Combat: BeginCombat(run, false); break;
                case Tile.Event: run.phase = Phase.Event; break;
                case Tile.Treasure: run.phase = Phase.Treasure; break;
                case Tile.Shop: run.phase = Phase.Shop; break;
                case Tile.Rest: run.phase = Phase.Rest; break;
                default: throw new InvalidOperationException("Landing tile");
            }
            run.message = $"이동 {value} · {run.position}번 칸에 도착했습니다.";
        }
        public static void BeginCombat(Run run, bool boss)
        {
            run.phase = Phase.Combat;
            int hp = boss ? run.rules.bossHp : run.rules.enemyHp + (run.region - 1) * run.rules.enemyHpPerRegion;
            run.enemy = new Enemy {name=boss ? "순환의 수호자" : $"지역 {run.region} 수호병", boss=boss,
                hp=hp, maxHp=hp, attack=boss ? run.rules.bossAttack : run.rules.enemyAttack + run.region - 1,
                intent=(Intent)(Next(ref run.combatRng) % 3)};
        }
        public static void RollCombat(Run run)
        {
            Require(run, Phase.Combat);
            var faces = run.dice.Select(slot => run.rules.dice.Single(d => d.id == slot.definitionId))
                .Select(d => d.faces[Next(ref run.combatRng) % 6]).ToArray();
            SymbolCombat.Resolve(run, faces);
            if (run.hp <= 0)
            {
                run.phase=Phase.Result; run.message="모험이 끝났습니다.";
            }
            else if (run.enemy.hp <= 0)
            {
                run.gold += run.rules.combatGold; AddXp(run, run.rules.combatXp);
                run.won = run.enemy.boss;
                run.phase = run.won ? Phase.Result : Phase.Map;
                run.message = run.won ? "순환의 수호자를 쓰러뜨렸습니다!" : $"전투 승리 · 골드 +{run.rules.combatGold} / XP +{run.rules.combatXp}";
            }
            else run.enemy.intent = (Intent)(Next(ref run.combatRng) % 3);
        }
        public static void ClaimLap(Run run, string gearId)
        {
            Require(run, Phase.LapReward);
            string id=run.id+":"+run.region+":lap";
            if (run.claimed.Contains(id)) throw new InvalidOperationException("Already claimed");
            if (run.region == 3) Equip(run, gearId);
            run.claimed.Add(id);
            run.completedRegions = run.region;
            run.gold += run.rules.lapGold; AddXp(run, run.rules.lapXp);
            run.hp = Math.Min(run.maxHp, run.hp + run.rules.lapHeal);
            if (run.region < 3)
            {
                run.dice.Add(new DieSlot {instanceId=id, definitionId=run.rules.dice[run.region].id});
                run.region++; run.board=Board(run.seed, run.region); run.position=0; run.phase=Phase.Map;
                run.message=$"지역 {run.region} · 전투 주사위가 {run.dice.Count}개가 되었습니다.";
            }
            else { BeginCombat(run, true); run.message="최종 완주 보상을 받았습니다. 보스에게 도전하세요."; }
        }
        public static void Choose(Run run, string arg)
        {
            if (arg == "leave" && (run.phase == Phase.Event || run.phase == Phase.Treasure || run.phase == Phase.Shop))
            { run.phase=Phase.Map; run.message="다음 여정을 준비합니다."; return; }
            switch (run.phase)
            {
                case Phase.Rest:
                    if (arg == "heal") { run.hp=Math.Min(run.maxHp, run.hp+22); run.message="휴식으로 HP를 회복했습니다."; }
                    else if (arg == "train") { AddXp(run, 8); run.message="훈련으로 XP 8을 얻었습니다."; }
                    else throw new ArgumentException("Rest choice");
                    run.phase=Phase.Map; return;
                case Phase.Event:
                    if (arg == "risk" && run.hp > 8) { run.hp-=8; run.gold+=25; run.message="HP 8을 소모하고 골드 25를 얻었습니다."; }
                    else if (arg == "study") { AddXp(run, 4); run.message="유적을 조사해 XP 4를 얻었습니다."; }
                    else throw new ArgumentException("Event choice");
                    run.phase=Phase.Map; return;
                case Phase.Treasure:
                case Phase.Shop:
                    bool shop = run.phase == Phase.Shop;
                    int cost = arg == "heal" ? 12 : 20;
                    if (shop && run.gold < cost) throw new InvalidOperationException("골드가 부족합니다.");
                    if (arg == "heal" && shop) run.hp=Math.Min(run.maxHp, run.hp+25);
                    else if (arg.StartsWith("gear:")) Equip(run, arg.Substring(5));
                    else if (arg.StartsWith("die:"))
                    {
                        var parts=arg.Split(':');
                        if (parts.Length != 3 || !int.TryParse(parts[1], out int slot) || slot < 0 || slot >= run.dice.Count ||
                            !run.rules.dice.Any(d => d.id == parts[2])) throw new ArgumentException("Die choice");
                        run.dice[slot]=new DieSlot {instanceId=run.id+":replacement:"+run.turn+":"+run.position+":"+slot,
                            definitionId=parts[2]};
                    }
                    else throw new ArgumentException("Treasure choice");
                    if (shop) run.gold-=cost;
                    run.message=shop ? "구매를 완료했습니다." : "보물을 획득했습니다.";
                    if (!shop) run.phase=Phase.Map;
                    return;
                default: throw new InvalidOperationException("No choice here");
            }
        }
        static void Equip(Run run, string id)
        {
            var gear=run.rules.gear.SingleOrDefault(g => g.id == id);
            if (gear == null) throw new ArgumentException("Unknown equipment");
            run.equipment[gear.slot]=gear.id;
        }
        public static void AddXp(Run run, int amount)
        {
            run.xp+=amount;
            while (run.xp >= run.level*8)
            {
                run.xp-=run.level*8; run.level++; run.maxHp+=6; run.hp=Math.Min(run.maxHp,run.hp+6); run.power++; run.guard++;
            }
        }
        static void Require(Run run, Phase phase)
        {
            if (run == null || run.phase != phase || run.hp <= 0) throw new InvalidOperationException("Wrong phase");
        }
        public static void ValidateRules(Rules rules)
        {
            if (rules == null || rules.hp <= 0 || rules.power < 0 || rules.guard < 0 ||
                rules.enemyHp <= 0 || rules.enemyHpPerRegion < 0 || rules.enemyAttack <= 0 || rules.bossHp <= 0 || rules.bossAttack <= 0 ||
                rules.combatGold < 0 || rules.combatXp < 0 || rules.lapGold < 0 || rules.lapXp < 0 || rules.lapHeal < 0 ||
                rules.regionCurrency < 0 || rules.winCurrency < 0) throw new ArgumentException("Invalid balance");
            if (rules.dice == null || rules.dice.Length < 3 || rules.dice.Select(d=>d.id).Distinct().Count()!=rules.dice.Length ||
                rules.dice.Any(d=>string.IsNullOrEmpty(d.id) || d.faces==null || d.faces.Length!=6 || d.faces.Any(s=>(int)s<0 || (int)s>3)))
                throw new ArgumentException("Invalid dice");
            if (rules.basics == null || rules.basics.Length!=4 || rules.combos==null ||
                rules.basics.Any(s=>s.symbols==null || s.symbols.Length!=1) ||
                rules.basics.Select(s=>SignatureOf(s)).Distinct().Count()!=4 ||
                rules.combos.Any(s=>s.symbols==null || s.symbols.Length<2 || s.symbols.Length>3) ||
                rules.combos.Select(SignatureOf).Distinct().Count()!=rules.combos.Length)
                throw new ArgumentException("Invalid combo table");
            var skills=rules.basics.Concat(rules.combos).ToArray();
            if (skills.Select(s=>s.id).Distinct().Count()!=skills.Length ||
                skills.Any(s=>string.IsNullOrWhiteSpace(s.id)||s.damage<0||s.block<0||(s.damage==0&&s.block==0)))
                throw new ArgumentException("Invalid skill");
            if (rules.gear==null || rules.gear.Length==0 || rules.gear.Select(g=>g.id).Distinct().Count()!=rules.gear.Length ||
                rules.gear.Any(g=>string.IsNullOrWhiteSpace(g.id)||g.slot<0||g.slot>2||g.damage<0||g.block<0))
                throw new ArgumentException("Invalid equipment");
        }
        public static bool ValidSubmission(SubmissionReceipt receipt)
        {
            if(receipt==null || string.IsNullOrWhiteSpace(receipt.receiptId) || string.IsNullOrWhiteSpace(receipt.requestId)) return false;
            if(receipt.status==BackendStatus.PendingVerification) return receipt.appliedAmount==0;
            return receipt.status==BackendStatus.Approved && receipt.verificationVersion==BackendProtocol.VerificationVersion &&
                receipt.appliedAmount>=0 && receipt.completedRegions>=0 && receipt.completedRegions<=3 &&
                (!receipt.won || (receipt.completedRegions==3 && !receipt.abandoned));
        }
        static string SignatureOf(Skill skill) => SymbolCombat.Signature(skill.symbols);
        public static void Validate(PlayerDocument doc)
        {
            if (doc==null || doc.schema!=2 || doc.revision<0 || doc.generation<0 || doc.profile==null ||
                doc.profile.currency<0 || doc.profile.growth<0 || doc.receipts==null ||
                doc.receipts.Any(r=>r==null || string.IsNullOrWhiteSpace(r.id)||string.IsNullOrWhiteSpace(r.fingerprint)||r.revision>doc.revision) ||
                doc.receipts.Select(r=>r.id).Distinct().Count()!=doc.receipts.Count) throw new ArgumentException("Invalid profile");
            if(doc.commands==null || doc.commands.Count>BackendProtocol.MaxCommands ||
                doc.commands.Where((c,i)=>c==null || c.sequence!=i+1 || string.IsNullOrWhiteSpace(c.clientCommandId)).Any())
                throw new ArgumentException("Invalid command checkpoint");
            if(doc.authority!=null && (doc.authority.revision<0 || doc.authority.values==null ||
                doc.authority.values.currency<0 || doc.authority.values.growth<0)) throw new ArgumentException("Invalid authority cache");
            if(doc.ticket!=null && (doc.run==null || doc.ticket.serverRunId!=doc.run.id ||
                doc.ticket.rulesVersion!=BackendProtocol.RulesVersion)) throw new ArgumentException("Invalid run ticket");
            var r=doc.run;
            if(r==null) return;
            ValidateRules(r.rules);
            if (string.IsNullOrWhiteSpace(r.id) || r.seed==0 || r.region<1 || r.region>3 || r.position<0 || r.position>11 ||
                r.completedRegions<0 || r.completedRegions>3 || r.region!=Math.Min(r.completedRegions+1,3) ||
                r.hp<0 || r.maxHp<=0 || r.hp>r.maxHp || r.level<1 ||
                r.power<0 || r.guard<0 || r.gold<0 || r.xp<0 || !Enum.IsDefined(typeof(Phase),r.phase) ||
                r.board==null || r.board.Length!=12 || r.board[0]!=Tile.Start || r.board.Skip(1).Any(t=>(int)t<1 || (int)t>5) ||
                Enumerable.Range(1,5).Any(t=>!r.board.Contains((Tile)t)) ||
                r.dice==null || r.dice.Count!=Math.Min(r.completedRegions+1,3) ||
                r.dice.Any(d=>d==null || string.IsNullOrWhiteSpace(d.instanceId) || !r.rules.dice.Any(x=>x.id==d.definitionId)) ||
                r.dice.Select(d=>d.instanceId).Distinct().Count()!=r.dice.Count ||
                r.equipment==null || r.equipment.Length!=3 ||
                r.equipment.Where((id,i)=>id!=null && !r.rules.gear.Any(g=>g.id==id && g.slot==i)).Any() ||
                r.claimed==null || r.claimed.Count!=r.completedRegions ||
                !r.claimed.SequenceEqual(Enumerable.Range(1,r.completedRegions).Select(i=>r.id+":"+i+":lap")))
                throw new ArgumentException("Invalid run");
            if ((r.phase==Phase.Combat && (r.enemy==null||r.enemy.hp<=0||r.hp<=0)) ||
                (r.enemy!=null && (r.enemy.hp<0||r.enemy.hp>r.enemy.maxHp||r.enemy.shield<0||r.enemy.attack<=0||!Enum.IsDefined(typeof(Intent),r.enemy.intent))) ||
                (r.settled && (r.phase!=Phase.Result || doc.submission==null || doc.submission.serverRunId!=r.id ||
                    r.payout!=doc.submission.appliedAmount || !ValidSubmission(doc.submission))) ||
                (r.won && (r.phase!=Phase.Result||r.completedRegions!=3||r.hp<=0)))
                throw new ArgumentException("Invalid combat/result");
            if(r.lastTurn!=null && (r.lastTurn.faces==null || r.lastTurn.faces.Length!=r.dice.Count ||
                r.lastTurn.faces.Any(s=>(int)s<0 || (int)s>3) || r.lastTurn.skills==null || r.lastTurn.skills.Count<1 ||
                r.lastTurn.skills.Any(s=>s==null || (s.status!="applied" && s.status!="enemy-defeated") || s.damage<0 || s.block<0)))
                throw new ArgumentException("Invalid resolved log");
        }
    }
}
