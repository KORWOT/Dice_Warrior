using System;
using System.Collections.Generic;

namespace DiceWarrior.SymbolLoop
{
    public enum Symbol { Sword, Staff, Fire, Shield }
    public enum Tile { Start, Combat, Event, Treasure, Shop, Rest }
    public enum Phase { Map, Combat, Event, Treasure, Shop, Rest, LapReward, Result }
    public enum Intent { Attack, Heavy, Defend }

    [Serializable] public sealed class Skill
    {
        public string id, name, tag;
        public Symbol[] symbols;
        public int damage, block;
    }
    [Serializable] public sealed class Die
    {
        public string id, name;
        public Symbol[] faces;
    }
    [Serializable] public sealed class Gear
    {
        public string id, name, tag;
        public int slot, damage, block;
    }
    [Serializable] public sealed class Rules
    {
        public int hp = 60, power = 5, guard = 4;
        public int enemyHp = 16, enemyHpPerRegion = 8, enemyAttack = 6, bossHp = 68, bossAttack = 13;
        public int combatGold = 12, combatXp = 5, lapGold = 15, lapXp = 5, lapHeal = 12;
        public int regionCurrency = 10, winCurrency = 20;
        public Die[] dice = {
            new Die { id="balanced", name="균형", faces=new[]{Symbol.Sword,Symbol.Sword,Symbol.Staff,Symbol.Fire,Symbol.Shield,Symbol.Shield}},
            new Die { id="arcane", name="비전", faces=new[]{Symbol.Staff,Symbol.Staff,Symbol.Fire,Symbol.Fire,Symbol.Sword,Symbol.Shield}},
            new Die { id="steel", name="강철", faces=new[]{Symbol.Sword,Symbol.Sword,Symbol.Sword,Symbol.Shield,Symbol.Shield,Symbol.Fire}}
        };
        public Skill[] basics = {
            new Skill {id="slash",name="베기",tag="steel",symbols=new[]{Symbol.Sword},damage=2},
            new Skill {id="bolt",name="마력탄",tag="arcane",symbols=new[]{Symbol.Staff},damage=1},
            new Skill {id="flame",name="불꽃",tag="fire",symbols=new[]{Symbol.Fire},damage=3},
            new Skill {id="guard",name="방어",tag="guard",symbols=new[]{Symbol.Shield},block=3}
        };
        public Skill[] combos = {
            new Skill {id="arc-slash",name="검기",tag="steel",symbols=new[]{Symbol.Sword,Symbol.Staff},damage=11},
            new Skill {id="fire-bolt",name="화염탄",tag="fire",symbols=new[]{Symbol.Staff,Symbol.Fire},damage=13},
            new Skill {id="guard-slash",name="수호 베기",tag="guard",symbols=new[]{Symbol.Sword,Symbol.Shield},damage=6,block=5},
            new Skill {id="flame-arc",name="화염 검기",tag="fire",symbols=new[]{Symbol.Sword,Symbol.Staff,Symbol.Fire},damage=23},
            new Skill {id="citadel",name="성채 베기",tag="guard",symbols=new[]{Symbol.Sword,Symbol.Shield,Symbol.Shield},damage=9,block=14}
        };
        public Gear[] gear = {
            new Gear {id="blade",name="강철검 · 강철 피해 +2",slot=0,tag="steel",damage=2},
            new Gear {id="wand",name="불꽃 지팡이 · 불 피해 +3",slot=0,tag="fire",damage=3},
            new Gear {id="mail",name="수호 갑옷 · 방어 보호막 +3",slot=1,tag="guard",block=3},
            new Gear {id="charm",name="비전 장신구 · 비전 피해 +3",slot=2,tag="arcane",damage=3},
            new Gear {id="aegis",name="성채 갑옷 · 방어 보호막 +6",slot=1,tag="guard",block=6}
        };
    }
    [Serializable] public sealed class DieSlot { public string instanceId, definitionId; }
    [Serializable] public sealed class SkillLog
    {
        public string skillId, name, status;
        public int damage, block, enemyHpBefore, enemyHpAfter, enemyShieldBefore, enemyShieldAfter;
    }
    [Serializable] public sealed class TurnLog
    {
        public int number, playerHpBefore, playerHpAfter, enemyHpBefore, enemyHpAfter, retaliationDamage, playerBlock;
        public Intent intent;
        public bool retaliation;
        public Symbol[] faces = Array.Empty<Symbol>();
        public List<SkillLog> skills = new List<SkillLog>();
    }
    [Serializable] public sealed class Enemy
    {
        public string name;
        public int hp, maxHp, attack, shield;
        public bool boss;
        public Intent intent;
    }
    [Serializable] public sealed class Run
    {
        public string id;
        public Rules rules;
        public uint seed, travelRng, combatRng;
        public int region = 1, completedRegions, position, lastTravel, hp, maxHp, power, guard, gold, xp, level = 1, turn;
        public bool won, settled;
        public int payout;
        public Phase phase;
        public Tile[] board;
        public List<DieSlot> dice = new List<DieSlot>();
        public string[] equipment = new string[3];
        public List<string> claimed = new List<string>();
        public Enemy enemy;
        public TurnLog lastTurn;
        public string message;
    }
    [Serializable] public sealed class Profile
    {
        public int currency, growth;
        public string startingDie = "balanced", startingWeapon = "blade";
    }
    [Serializable] public sealed class Receipt { public string id, fingerprint; public long revision; }
    [Serializable] public sealed class PlayerDocument
    {
        public int schema = 1, generation;
        public long revision;
        public Profile profile = new Profile();
        public Run run;
        public List<Receipt> receipts = new List<Receipt>();
    }
    public sealed class Command
    {
        public string id, kind, arg, runId;
        public long revision;
        public int generation;
        public uint seed;
    }
}
