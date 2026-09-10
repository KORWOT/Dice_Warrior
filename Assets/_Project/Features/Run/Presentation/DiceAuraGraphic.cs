using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    // Canvas-native light geometry. No particles, textures, random stream or material instances.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DiceAuraGraphic : MaskableGraphic
    {
        public enum AuraShape { Halo, Crest }
        public AuraShape shape;
        [Range(0, 6)] public int variation;
        public bool Visible { get; private set; }
        public Color Primary { get; private set; }
        public Color Secondary { get; private set; }
        public float Strength { get; private set; }
        public float Phase { get; private set; }
        public DiceEffectComplexity Complexity { get; private set; }
        public float Opacity { get; private set; }

        public void SetVisual(Color primary, Color secondary, float strength, float phase)
            => SetVisual(primary, secondary, strength, phase, DiceEffectComplexity.Grand, 1);

        public void SetVisual(Color primary, Color secondary, float strength, float phase,
            DiceEffectComplexity complexity = DiceEffectComplexity.Grand, float opacity = 1f)
        {
            Visible = true;
            Primary = primary; Secondary = secondary;
            Strength = Mathf.Clamp01(strength); Phase = Mathf.Clamp01(phase);
            Complexity = complexity; Opacity = Mathf.Clamp01(opacity);
            SetVerticesDirty();
        }

        public void Clear()
        {
            Visible = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!Visible) return;
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            if (shape == AuraShape.Halo) Halo(mesh, rect.center, radius);
            else if (Complexity != DiceEffectComplexity.Simple)
                Crest(mesh, rect.center, Mathf.Min(radius, rect.width / 2.9f));
            // The authored sizes have a margin; this also protects unusually small/narrow custom rects.
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.position = new Vector3(Mathf.Clamp(vertex.position.x, rect.xMin, rect.xMax),
                    Mathf.Clamp(vertex.position.y, rect.yMin, rect.yMax), vertex.position.z);
                mesh.SetUIVertex(vertex, i);
            }
        }

        Color Tint(Color tint, float opacity = 1)
        {
            return new Color(tint.r * color.r, tint.g * color.g, tint.b * color.b,
                tint.a * color.a * opacity * Opacity);
        }

        float Motion => Mathf.Max(0, Mathf.Sin(Phase * Mathf.PI));

        void Halo(VertexHelper mesh, Vector2 center, float radius)
        {
            if (Complexity == DiceEffectComplexity.Simple)
            {
                SimpleHalo(mesh, center, radius);
                return;
            }
            if (Complexity == DiceEffectComplexity.Runic)
            {
                RunicHalo(mesh, center, radius);
                return;
            }
            float unit = radius / 90;
            float arrival = Motion;
            float sweep = Phase * (Complexity == DiceEffectComplexity.Legendary ? 260 : 130);
            float r = radius * (.54f + .02f * arrival);
            Color primary = Tint(Primary), secondary = Tint(Secondary);
            GlowArc(mesh, center, Vector2.one * r, 0, 360, (13 + 5 * Strength) * unit, primary, 64);
            GlowArc(mesh, center, Vector2.one * (r + 6 * unit), variation * 43 + sweep, 255, 3 * unit, secondary, 48);
            GlowArc(mesh, center, Vector2.one * (r - 5 * unit), 170 + variation * 31 - sweep * .7f, 115, 2 * unit, Tint(Primary, .7f), 32);
            for (int i = 0; i < 4; i++)
            {
                float angle = 90 * i + (i % 2 == 0 ? 0 : variation * 5) + arrival * 12;
                Star(mesh, center + Polar(angle) * (r + 4 * unit), (3.5f + Strength * 2 + arrival * 2) * unit,
                    i % 2 == 0 ? primary : secondary);
            }
            int count = 7 + Mathf.RoundToInt(Strength * 7);
            for (int i = 0; i < count; i++)
            {
                float angle = i * 137.508f + variation * 27 + sweep * .3f;
                float scatter = .72f + .075f * Mathf.Sin(i * 7.17f + variation);
                var point = center + Polar(angle) * (radius * scatter);
                Star(mesh, point, (1.3f + ((i + variation) % 3) * .55f + arrival) * unit,
                    Tint(i % 3 == 0 ? Secondary : Primary, .6f));
            }
            if (Complexity == DiceEffectComplexity.Legendary)
                LegendaryWaves(mesh, center, radius, false);
        }

        void SimpleHalo(VertexHelper mesh, Vector2 center, float radius)
        {
            float r = radius * (.54f + .008f * Motion);
            GlowArc(mesh, center, Vector2.one * r, 0, 360, radius * .055f, Tint(Primary, .82f), 48);
            for (int i = 0; i < 2; i++)
                Star(mesh, center + Polar(55 + i * 180 + variation * 7) * r,
                    radius * (.013f + .003f * Motion), Tint(Secondary, .8f));
        }

        void RunicHalo(VertexHelper mesh, Vector2 center, float radius)
        {
            float r = radius * (.54f + .012f * Motion);
            float sweep = Phase * 65 + variation * 31;
            GlowArc(mesh, center, Vector2.one * r, sweep, 310, radius * .075f, Tint(Primary), 48);
            GlowArc(mesh, center, Vector2.one * (r + radius * .07f), 180 - sweep, 150,
                radius * .022f, Tint(Secondary, .85f), 32);
            for (int i = 0; i < 4; i++)
            {
                float lit = Mathf.Clamp01((Phase * 1.4f - i * .2f) * 4);
                var direction = Polar(i * 90 + variation * 9);
                Line(mesh, center + direction * (r + radius * .035f),
                    center + direction * (r + radius * .11f), radius * .012f, Tint(Secondary, lit));
            }
        }

        void Crest(VertexHelper mesh, Vector2 center, float radius)
        {
            if (Complexity == DiceEffectComplexity.Runic)
            {
                RunicCrest(mesh, center, radius);
                return;
            }
            float unit = radius / 214;
            float arrival = Motion;
            float pulse = arrival * Mathf.Sin(Phase * Mathf.PI * 6);
            float sweep = Phase * (Complexity == DiceEffectComplexity.Legendary ? 190 : 90);
            float r = radius * (.72f + .03f * arrival);
            var primary = Tint(Primary, .52f + pulse * .1f);
            var secondary = Tint(Secondary, .56f + pulse * .12f);
            GlowArc(mesh, center, Vector2.one * r, 0, 360, 3 * unit, primary, 96);
            GlowArc(mesh, center, Vector2.one * (r + 11 * unit), 12 + sweep, 140, 2 * unit, secondary, 44);
            GlowArc(mesh, center, Vector2.one * (r + 11 * unit), 192 + sweep, 140, 2 * unit, secondary, 44);
            GlowArc(mesh, center, Vector2.one * (r - 13 * unit), 70 - sweep * .75f, 220, 1.7f * unit, Tint(Primary, .24f), 64);
            for (int i = 0; i < 32; i++)
            {
                float angle = i * 11.25f + sweep * .15f;
                var p = Polar(angle);
                var a = center + p * (r + 3 * unit);
                var b = center + p * (r + (i % 4 == 0 ? 9 : 6) * unit);
                Line(mesh, a, b, (i % 4 == 0 ? 1.1f : .65f) * unit, secondary);
            }
            // Two intersecting trails leave the center clear for the result text and die faces.
            var orbit = new Vector2(radius * 1.27f, radius * (.35f + .025f * pulse));
            GlowArc(mesh, center + Vector2.down * (37 * unit), orbit, 5 + sweep * .5f, 165,
                (3 + Strength * 3) * unit, secondary, 60);
            GlowArc(mesh, center + Vector2.up * (10 * unit), orbit, 185 - sweep * .5f, 165,
                (3 + Strength * 3) * unit, primary, 60);
            Star(mesh, center + Vector2.up * (r + 13 * unit), (10 + Strength * 5) * unit, Tint(Secondary, .95f));
            Star(mesh, center + Vector2.down * (r + 13 * unit), (5 + Strength * 2) * unit, Tint(Primary, .9f));
            int count = 18 + Mathf.RoundToInt(Strength * 22);
            for (int i = 0; i < count; i++)
            {
                float angle = i * 137.508f + sweep * .2f;
                var direction = Polar(angle);
                float distance = radius * (.8f + .14f * Mathf.Sin(i * 9.31f));
                var point = center + new Vector2(direction.x * 1.26f, direction.y) * distance;
                Color tint = Tint(i % 3 == 0 ? Secondary : Primary, .55f + .25f * arrival);
                Star(mesh, point, (1.7f + (i % 4) * .65f + Strength * 1.1f) * unit, tint);
                if (Strength >= .6f && i % 3 == 0)
                    Line(mesh, point, point + direction * ((10 + 24 * Strength + arrival * 12) * unit),
                        .9f * unit, Tint(Secondary, .4f));
            }
            if (Complexity == DiceEffectComplexity.Legendary)
            {
                LegendaryWaves(mesh, center, radius, true);
                Crown(mesh, center + Vector2.up * (radius * .68f), radius);
            }
        }

        void RunicCrest(VertexHelper mesh, Vector2 center, float radius)
        {
            float r = radius * (.64f + .015f * Motion);
            float sweep = Phase * 38;
            GlowArc(mesh, center, Vector2.one * r, sweep, 290, radius * .008f, Tint(Primary, .35f), 64);
            for (int i = 0; i < 8; i++)
            {
                float lit = Mathf.Clamp01((Phase * 1.4f - i * .13f) * 5);
                var direction = Polar(i * 45 + sweep);
                Line(mesh, center + direction * (r + radius * .018f),
                    center + direction * (r + radius * .052f), radius * .005f, Tint(Secondary, lit * .7f));
            }
            Star(mesh, center + Vector2.up * (r + radius * .045f), radius * .014f, Tint(Secondary, .65f));
        }

        void LegendaryWaves(VertexHelper mesh, Vector2 center, float radius, bool isCrest)
        {
            float pulse = Motion;
            for (int i = 0; i < 3; i++)
            {
                float wave = Mathf.Repeat(Phase * 2 + i / 3f, 1);
                float alpha = Mathf.Sin(wave * Mathf.PI) * pulse;
                float r = radius * ((isCrest ? .57f : .52f) + .28f * wave);
                GlowArc(mesh, center, Vector2.one * r, 0, 360, radius * (.012f + .012f * Strength),
                    Tint(i % 2 == 0 ? Primary : Secondary, alpha * .65f), isCrest ? 64 : 40);
            }
            int count = isCrest ? 28 : 12;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 360f / count + variation * 13 + Phase * 45;
                var direction = Polar(angle);
                float extension = .045f + .09f * pulse * (.5f + .5f * Mathf.Sin(i * 2.3f + Phase * 30));
                float start = isCrest ? .77f : .68f;
                var stretch = isCrest ? new Vector2(1.16f, 1) : Vector2.one;
                var a = center + Vector2.Scale(direction, stretch) * (radius * start);
                var b = center + Vector2.Scale(direction, stretch) * (radius * (start + extension));
                Line(mesh, a, b, radius * (isCrest ? .005f : .013f),
                    Tint(i % 3 == 0 ? Secondary : Primary, .5f + pulse * .35f));
            }
        }

        void Crown(VertexHelper mesh, Vector2 center, float radius)
        {
            var gold = Tint(Secondary, .72f + .18f * Motion);
            Vector2 previous = center + new Vector2(-.22f, 0) * radius;
            for (int i = 0; i < 5; i++)
            {
                float x = -.18f + i * .09f;
                float height = i == 2 ? .14f : (i % 2 == 0 ? .1f : .07f);
                var peak = center + new Vector2(x, height * (.8f + .2f * Motion)) * radius;
                var valley = center + new Vector2(x + .045f, 0) * radius;
                Line(mesh, previous, peak, radius * .008f, gold);
                Line(mesh, peak, valley, radius * .008f, gold);
                Star(mesh, peak, radius * .008f, gold);
                previous = valley;
            }
            GlowArc(mesh, center + Vector2.up * (radius * .02f),
                new Vector2(radius * .23f, radius * .05f), 190, 160, radius * .009f, gold, 24);
        }

        static Vector2 Polar(float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        static void GlowArc(VertexHelper mesh, Vector2 center, Vector2 radius, float start, float span,
            float width, Color tint, int segments)
        {
            Color clear = new Color(tint.r, tint.g, tint.b, 0);
            Color soft = tint; soft.a *= .7f;
            Color hot = Color.Lerp(tint, Color.white, .38f); hot.a = tint.a;
            for (int i = 0; i < segments; i++)
            {
                Vector2 a = Polar(start + span * i / segments), b = Polar(start + span * (i + 1) / segments);
                Vector2 pa = center + Vector2.Scale(a, radius), pb = center + Vector2.Scale(b, radius);
                Quad(mesh, pa - a * width, pb - b * width, pb, pa, clear, clear, soft, soft);
                Quad(mesh, pa, pb, pb + b * width, pa + a * width, soft, soft, clear, clear);
                Quad(mesh, pa - a * .7f, pb - b * .7f, pb + b * .7f, pa + a * .7f, hot, hot, hot, hot);
            }
        }

        static void Star(VertexHelper mesh, Vector2 point, float size, Color tint)
        {
            Color clear = new Color(tint.r, tint.g, tint.b, 0);
            Color hot = Color.Lerp(tint, Color.white, .8f); hot.a = tint.a;
            Diamond(mesh, point, size * 2.7f, size * 3.6f, tint * new Color(1, 1, 1, .22f), clear);
            Diamond(mesh, point, size * .38f, size * 1.8f, hot, tint);
            Diamond(mesh, point, size * 1.1f, size * .27f, hot, tint);
        }

        static void Diamond(VertexHelper mesh, Vector2 center, float x, float y, Color middle, Color edge)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center, middle, Vector2.zero);
            mesh.AddVert(center + Vector2.up * y, edge, Vector2.zero);
            mesh.AddVert(center + Vector2.right * x, edge, Vector2.zero);
            mesh.AddVert(center + Vector2.down * y, edge, Vector2.zero);
            mesh.AddVert(center + Vector2.left * x, edge, Vector2.zero);
            for (int i = 0; i < 4; i++) mesh.AddTriangle(first, first + i + 1, first + (i + 1) % 4 + 1);
        }

        static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var d = (b - a).normalized;
            var offset = new Vector2(-d.y, d.x) * width;
            var clear = new Color(tint.r, tint.g, tint.b, 0);
            Quad(mesh, a - offset, b - offset * .1f, b + offset * .1f, a + offset, tint, clear, clear, tint);
        }

        static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            Color ca, Color cb, Color cc, Color cd)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(a, ca, Vector2.zero); mesh.AddVert(b, cb, Vector2.zero);
            mesh.AddVert(c, cc, Vector2.zero); mesh.AddVert(d, cd, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
