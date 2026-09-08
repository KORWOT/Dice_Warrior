using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FateDice
{
    [Serializable]
    public sealed class VisualArtwork
    {
        [Tooltip("Optional compact image. Missing images use the fallback glyph and tint.")] public Sprite icon;
        [Tooltip("Optional larger card/event picture. Displayed with preserved aspect ratio.")] public Sprite artwork;
        [Tooltip("Presentation tint only; never a gameplay modifier.")] public Color tint = Color.white;
        [Tooltip("Readable fallback when no image is assigned.")] public string fallbackGlyph = "?";
    }
    [Serializable] public sealed class ActionVisualEntry { public string contentId; public VisualArtwork visual = new VisualArtwork(); }
    [Serializable] public sealed class EventVisualEntry { public string contentId; public VisualArtwork visual = new VisualArtwork(); }
    [Serializable] public sealed class NodeVisualEntry { public NodeType type; public VisualArtwork visual = new VisualArtwork(); }
    [Serializable] public sealed class GradeVisualEntry { public Grade grade; public Sprite border; public Sprite badge; }

    [CreateAssetMenu(menuName="Fate Dice/Visual Catalog", fileName="DefaultFateDiceVisuals")]
    public sealed class FateDiceVisualCatalog : ScriptableObject
    {
        [Tooltip("Optional appearance overrides keyed by the original action's stable ID.")] public ActionVisualEntry[] actions = Array.Empty<ActionVisualEntry>();
        [Tooltip("Optional event images. Resolve only after the actual event is revealed.")] public EventVisualEntry[] events = Array.Empty<EventVisualEntry>();
        [Tooltip("One public node appearance for each NodeType, including Boss.")] public NodeVisualEntry[] nodes = Array.Empty<NodeVisualEntry>();
        [Tooltip("Public pre-choice fate appearance by NodeType only; never an event ID.")] public NodeVisualEntry[] fates = Array.Empty<NodeVisualEntry>();
        [Tooltip("One row per offered Grade. Grade colors remain in GameConfigData.presentation.gradeColors.")] public GradeVisualEntry[] grades = Array.Empty<GradeVisualEntry>();
        [Tooltip("One style for each common button purpose.")] public ButtonAppearance[] buttons = Array.Empty<ButtonAppearance>();
        public VisualArtwork defaultAction = new VisualArtwork();
        public VisualArtwork defaultEvent = new VisualArtwork();
        [Tooltip("Cosmetic node fade duration in seconds; finite and nonnegative. Does not consume rules RNG.")]
        [Min(0)] public float nodeFadeSeconds = .18f;

        public void Validate(GameConfigData config)
        {
            var actionIds = ActionIds(config);
            var eventIds = EventIds(config);
            ValidateArtwork(defaultAction, nameof(defaultAction));
            ValidateArtwork(defaultEvent, nameof(defaultEvent));
            ValidateContentRows(actions, "actions", actionIds, row => row.contentId, row => row.visual);
            ValidateContentRows(events, "events", eventIds, row => row.contentId, row => row.visual);
            ValidateNodeRows(nodes, "nodes");
            ValidateNodeRows(fates, "fates");
            ValidateKeys(grades, "grades", row => row.grade);
            ValidateButtons();
            Require(Finite(nodeFadeSeconds) && nodeFadeSeconds >= 0, nameof(nodeFadeSeconds),
                "Fade seconds must be finite and nonnegative.");
        }

        public VisualArtwork ResolveAction(GameConfigData config, string contentId)
        {
            var ids = ActionIds(config);
            Require(!string.IsNullOrWhiteSpace(contentId) && ids.Contains(contentId), "actions.contentId",
                "Unknown action stable ID: " + (contentId ?? "<null>"));
            ValidateArtwork(defaultAction, nameof(defaultAction));
            ValidateContentRows(actions, "actions", ids, row => row.contentId, row => row.visual);
            var row = actions.FirstOrDefault(entry => entry.contentId == contentId);
            return row?.visual ?? defaultAction;
        }

        public VisualArtwork ResolveEvent(GameConfigData config, string contentId)
        {
            var ids = EventIds(config);
            Require(!string.IsNullOrWhiteSpace(contentId) && ids.Contains(contentId), "events.contentId",
                "Unknown event stable ID: " + (contentId ?? "<null>"));
            ValidateArtwork(defaultEvent, nameof(defaultEvent));
            ValidateContentRows(events, "events", ids, row => row.contentId, row => row.visual);
            var row = events.FirstOrDefault(entry => entry.contentId == contentId);
            return row?.visual ?? defaultEvent;
        }

        public VisualArtwork ResolveNode(NodeType type)
        {
            RequireEnum(type, "nodes.type");
            ValidateNodeRows(nodes, "nodes");
            return nodes.First(row => row.type == type).visual;
        }

        // This entry has no config or event ID: pre-choice artwork can use public type only.
        public VisualArtwork ResolveFate(NodeType type)
        {
            RequireEnum(type, "fates.type");
            ValidateNodeRows(fates, "fates");
            return fates.First(row => row.type == type).visual;
        }

        public GradeVisualEntry ResolveGrade(Grade grade)
        {
            RequireEnum(grade, "grades.grade");
            ValidateKeys(grades, "grades", row => row.grade);
            return grades.First(row => row.grade == grade);
        }

        public ButtonAppearance ResolveButton(ButtonPurpose purpose)
        {
            RequireEnum(purpose, "buttons.purpose");
            ValidateButtons();
            return buttons.First(row => row.purpose == purpose);
        }

        static HashSet<string> ActionIds(GameConfigData config) =>
            ContentIds(config?.combat?.actions, "config.combat.actions", row => row.id);

        static HashSet<string> EventIds(GameConfigData config) =>
            ContentIds(config?.world?.events, "config.world.events", row => row.id);

        static HashSet<string> ContentIds<T>(T[] rows, string path, Func<T,string> id) where T : class
        {
            Require(rows != null, path, "Content definitions are missing.");
            var result = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Length; i++)
            {
                string entry = path + "[" + i + "]";
                Require(rows[i] != null, entry, "Content definition is null.");
                string key = id(rows[i]);
                Require(!string.IsNullOrWhiteSpace(key), entry + ".id", "A stable ID is required.");
                Require(result.Add(key), entry + ".id", "Duplicate stable ID: " + key);
            }
            return result;
        }

        static void ValidateContentRows<T>(T[] rows, string path, HashSet<string> validIds,
            Func<T,string> id, Func<T,VisualArtwork> visual) where T : class
        {
            Require(rows != null, path, "Use an empty list for unregistered optional mappings.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Length; i++)
            {
                string entry = path + "[" + i + "]";
                Require(rows[i] != null, entry, "A mapping row is null.");
                string key = id(rows[i]);
                Require(!string.IsNullOrWhiteSpace(key) && validIds.Contains(key), entry + ".contentId",
                    "Unknown content stable ID: " + (key ?? "<null>"));
                Require(seen.Add(key), entry + ".contentId", "Duplicate content mapping: " + key);
                // The row may deliberately use the default appearance. A missing row is also legal.
                var art = visual(rows[i]);
                if (art != null) ValidateArtwork(art, entry + ".visual");
            }
        }

        static void ValidateNodeRows(NodeVisualEntry[] rows, string path)
        {
            ValidateKeys(rows, path, row => row.type);
            for (int i = 0; i < rows.Length; i++) ValidateArtwork(rows[i].visual, path + "[" + i + "].visual");
        }

        void ValidateButtons()
        {
            ValidateKeys(buttons, "buttons", row => row.purpose);
            for (int i = 0; i < buttons.Length; i++)
            {
                string path = "buttons[" + i + "]";
                Require(Finite(buttons[i].normal), path + ".normal", "Button color must be finite.");
                Require(Finite(buttons[i].selected), path + ".selected", "Button color must be finite.");
                Require(Finite(buttons[i].pressed), path + ".pressed", "Button color must be finite.");
                Require(Finite(buttons[i].disabled), path + ".disabled", "Button color must be finite.");
            }
        }

        static void ValidateKeys<T,TKey>(T[] rows, string path, Func<T,TKey> key) where T : class where TKey : struct
        {
            Require(rows != null, path, "Required mapping rows are missing.");
            var seen = new HashSet<TKey>();
            for (int i = 0; i < rows.Length; i++)
            {
                string entry = path + "[" + i + "]";
                Require(rows[i] != null, entry, "A mapping row is null.");
                TKey value = key(rows[i]);
                RequireEnum(value, entry);
                Require(seen.Add(value), entry, "Duplicate mapping key: " + value);
            }
            foreach (TKey value in Enum.GetValues(typeof(TKey)))
                Require(seen.Contains(value), path, "Required mapping key is missing: " + value);
        }

        static void ValidateArtwork(VisualArtwork visual, string path)
        {
            Require(visual != null, path, "A fallback appearance object is required.");
            Require(Finite(visual.tint), path + ".tint", "Appearance tint must be finite.");
            Require(visual.icon != null || visual.artwork != null || !string.IsNullOrWhiteSpace(visual.fallbackGlyph),
                path + ".fallbackGlyph", "An image-free appearance requires a readable fallback glyph.");
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Finite(Color color) => Finite(color.r) && Finite(color.g) && Finite(color.b) && Finite(color.a);

        static void RequireEnum<T>(T value, string path) where T : struct =>
            Require(Enum.IsDefined(typeof(T), value), path, "Invalid enum key: " + value);

        static void Require(bool condition, string path, string message)
        {
            if (!condition) throw new InvalidOperationException(nameof(FateDiceVisualCatalog) + "." + path + ": " + message);
        }
    }
}
