using System;
using System.Linq;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionV5Content
    {
        public const string Version = "shared-world-preview-5";
        public const string BlankFaceId = "face.blank";
        public const string ClassTemplate = "$class";
        public static bool IsEnabled(CompetitionDefinition d) => d != null && d.rulesVersion == Version;
        public static void Validate(CompetitionDefinition d)
        {
            if (!IsEnabled(d)) { if (d.v5 != null) throw new ArgumentException("Legacy content contains v5 fields"); return; }
            var v = d.v5;
            if (v == null || d.maxCombatTurns != 20 || v.baseRerollsPerTurn != 1 || v.maxHoldChangesPerTurn != 12 ||
                v.aiPolicyVersion != "v5-keep-nonblank-1" || v.faces == null || v.faces.Length != 8 ||
                v.faces.Any(f => f == null || !Text(f.id, 100) || !Text(f.name, 100) || !Text(f.iconId, 100) ||
                    f.kind == V5FaceKind.Invalid || !Enum.IsDefined(typeof(V5FaceKind), f.kind)) ||
                v.faces.Select(f => f.id).Distinct(StringComparer.Ordinal).Count() != v.faces.Length)
                throw new ArgumentException("Invalid v5 face contract");
            foreach (var face in v.faces)
            {
                if (face.kind == V5FaceKind.Class ? !d.adventurers.Any(a => a.id == face.adventurerId) : face.adventurerId != null)
                    throw new ArgumentException("Invalid v5 face owner");
                if ((face.id == BlankFaceId) != (face.kind == V5FaceKind.Blank)) throw new ArgumentException("Invalid blank identity");
            }
            if (v.faces.Count(f => f.kind == V5FaceKind.Blank) != 1 || v.faces.Count(f => f.kind == V5FaceKind.Common) != 4 ||
                !new[] { "sword", "staff", "fire", "shield" }.All(id => v.faces.Any(f => f.id == "face.common." + id && f.kind == V5FaceKind.Common)) ||
                d.adventurers.Any(a => v.faces.Count(f => f.kind == V5FaceKind.Class && f.adventurerId == a.id) != 1))
                throw new ArgumentException("Missing common or class face");
            if (v.diceLayouts == null || v.diceLayouts.Length != d.combat.dice.Length ||
                v.diceLayouts.Any(x => x == null || !d.combat.dice.Any(die => die.id == x.dieId) || x.faceIds == null || x.faceIds.Length != 6) ||
                v.diceLayouts.Select(x => x.dieId).Distinct(StringComparer.Ordinal).Count() != v.diceLayouts.Length)
                throw new ArgumentException("Invalid v5 six-face layouts");
            foreach (var layout in v.diceLayouts)
            {
                if (layout.faceIds.Count(id => id == ClassTemplate) != 1 || layout.faceIds.Count(id => id == BlankFaceId) != 1 ||
                    layout.faceIds.Where(id => id != ClassTemplate && id != BlankFaceId).Any(id => !v.faces.Any(f => f.id == id && f.kind == V5FaceKind.Common)))
                    throw new ArgumentException("Each die needs four common, one class and one blank face");
                foreach (var adventurer in d.adventurers) Faces(d, adventurer.id, layout.dieId);
            }
            if (v.skills == null || v.skills.Length < 10 || v.skills.Length > 32 ||
                v.skills.Any(s => s == null || !Text(s.id, 100) || !Text(s.name, 100) || !Text(s.iconId, 100) || s.faceIds == null ||
                    s.faceIds.Length < 1 || s.faceIds.Length > 3 || s.priority < 0 || s.priority > 10000 || s.damage < 0 || s.damage > 1000 ||
                    s.block < 0 || s.block > 1000 || (s.damage == 0 && s.block == 0)) ||
                v.skills.Select(s => s.id).Distinct(StringComparer.Ordinal).Count() != v.skills.Length ||
                v.skills.Select(s => Signature(s.faceIds)).Distinct(StringComparer.Ordinal).Count() != v.skills.Length)
                throw new ArgumentException("Invalid v5 skill recipes");
            foreach (var skill in v.skills)
            {
                var faces = skill.faceIds.Select(id => Face(d, id)).ToArray();
                if (faces.Any(f => f.kind == V5FaceKind.Blank) ||
                    (skill.ownerAdventurerId != null && !d.adventurers.Any(a => a.id == skill.ownerAdventurerId)) ||
                    faces.Any(f => f.kind == V5FaceKind.Class && f.adventurerId != skill.ownerAdventurerId) ||
                    (faces.Any(f => f.kind == V5FaceKind.Class) != (skill.ownerAdventurerId != null)))
                    throw new ArgumentException("Invalid v5 skill owner");
                CompetitionV4Content.ValidateEffect(d, skill);
            }
            foreach (var face in v.faces.Where(f => f.kind != V5FaceKind.Blank))
                if (v.skills.Count(s => s.faceIds.Length == 1 && s.faceIds[0] == face.id) != 1)
                    throw new ArgumentException("Every nonblank face requires one single skill");
            foreach (var adventurer in d.adventurers)
                if (!v.skills.Any(s => s.ownerAdventurerId == adventurer.id && s.faceIds.Length == 2))
                    throw new ArgumentException("Missing class pair skill");
        }

        internal static string Signature(string[] faceIds) => string.Join("|", faceIds.OrderBy(id => id, StringComparer.Ordinal));
        static bool Text(string value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum;

        public static V5FaceDefinition Face(CompetitionDefinition d, string id)
        {
            if (!IsEnabled(d) || d.v5?.faces == null) throw new ArgumentException("Missing v5 faces");
            return d.v5.faces.SingleOrDefault(f => f != null && f.id == id) ?? throw new ArgumentException("Unknown v5 face: " + id);
        }

        public static V5FaceDefinition[] Faces(CompetitionDefinition d, string adventurerId, string dieDefinitionId)
        {
            if (!IsEnabled(d) || d.v5?.diceLayouts == null || !d.adventurers.Any(a => a.id == adventurerId))
                throw new ArgumentException("Unknown v5 adventurer");
            var layout = d.v5.diceLayouts.SingleOrDefault(x => x != null && x.dieId == dieDefinitionId);
            if (layout?.faceIds == null || layout.faceIds.Length != 6) throw new ArgumentException("Unknown v5 die layout");
            return layout.faceIds.Select(id => {
                var face = id == ClassTemplate ? d.v5.faces.Single(f => f.kind == V5FaceKind.Class && f.adventurerId == adventurerId) : Face(d, id);
                if (face.kind == V5FaceKind.Class && face.adventurerId != adventurerId) throw new ArgumentException("Foreign class face");
                return face;
            }).ToArray();
        }

        internal static void ValidateResults(CompetitionDefinition d, ParticipantState p, V5FaceResult[] results)
        {
            if (results == null || results.Length != p.dice.Count || results.Length < 1 || results.Length > 3)
                throw new ArgumentException("Invalid v5 result count");
            for (int i = 0; i < results.Length; ++i)
            {
                var face = results[i];
                if (face == null || face.slotIndex != i || face.dieInstanceId != p.dice[i].instanceId || face.faceIndex < 0 || face.faceIndex > 5 ||
                    face.faceId != Faces(d, p.adventurerId, p.dice[i].definitionId)[face.faceIndex].id)
                    throw new ArgumentException("Invalid v5 result identity or physical face");
            }
        }

        public static void ValidateParticipant(CompetitionState state, ParticipantState p)
        {
            if (!IsEnabled(state.definition)) { if (p.v5 != null) throw new ArgumentException("Legacy state contains v5 fields"); return; }
            if (p.v5 == null || state.matchId.Length > 100 || state.actionNumber > DiceWarrior.SymbolLoop.BackendProtocol.MaxCommands)
                throw new ArgumentException("Invalid v5 participant or match bounds");
            var selection = p.v5.selection;
            if ((p.phase == CompetitionPhase.Combat) != (selection != null)) throw new ArgumentException("Invalid v5 selection lifetime");
            if (selection != null)
            {
                if (p.encounterTurns >= state.definition.maxCombatTurns || selection.combatTurnSequence != p.encounterTurns + 1 ||
                    selection.holdChanges < 0 || selection.holdChanges > state.definition.v5.maxHoldChangesPerTurn)
                    throw new ArgumentException("Invalid v5 selection turn");
                if (selection.phase == V5SelectionPhase.AwaitRoll)
                {
                    if (selection.results == null || selection.results.Length != 0 || selection.rollSequence != 0 || selection.holdChanges != 0 || selection.rerollsLeft != 1)
                        throw new ArgumentException("Invalid v5 unrolled state");
                }
                else if (selection.phase == V5SelectionPhase.Selection)
                {
                    if (selection.rollSequence < 1 || selection.rollSequence > 2 || selection.rerollsLeft != 2 - selection.rollSequence)
                        throw new ArgumentException("Invalid v5 reroll state");
                    ValidateResults(state.definition, p, selection.results);
                    if (selection.results.Count(r => r.held) > selection.holdChanges) throw new ArgumentException("Invalid v5 hold history");
                }
                else throw new ArgumentException("Unknown v5 selection phase");
            }
            var log = p.v5.lastTurn;
            if ((log == null) != (p.lastTurn == null)) throw new ArgumentException("Missing v5 action log");
            if (log != null)
            {
                ValidateResults(state.definition, p, log.results);
                var planned = CompetitionV5Skills.Plan(state.definition, p, log.results).Select(s => s.id).ToArray();
                if (log.plannedSkillIds == null || !planned.SequenceEqual(log.plannedSkillIds) || log.noOp != (planned.Length == 0) ||
                    !planned.SequenceEqual(p.lastTurn.skills.Select(s => s.skillId)) || p.lastTurn.faces == null || p.lastTurn.faces.Length != 0)
                    throw new ArgumentException("Invalid v5 planned action log");
            }
        }
    }
}
