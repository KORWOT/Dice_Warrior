using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    // Quiet, anonymous scenery for an empty artwork slot; it carries no combat information.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Fate Dice/UI/Combat Stage Graphic")]
    public sealed class CombatStageGraphic : Graphic
    {
        public bool showFigures = true;
        bool drawnFigures;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;
        }
#endif
        void LateUpdate()
        {
            if (drawnFigures != showFigures) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            drawnFigures = showFigures;
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            Quad(mesh, rect, 0, 0, 1, 1, new Color(.018f, .022f, .028f), new Color(.038f, .043f, .054f));
            Polygon(mesh, rect, new Color(.053f, .061f, .074f),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(.87f, .39f), new Vector2(.12f, .39f));
            Polygon(mesh, rect, new Color(.064f, .073f, .089f),
                new Vector2(.16f, .06f), new Vector2(.79f, .07f), new Vector2(.71f, .29f), new Vector2(.33f, .31f));
            Polygon(mesh, rect, new Color(.025f, .031f, .042f),
                new Vector2(0, .17f), new Vector2(.50f, .22f), new Vector2(.70f, .37f), new Vector2(0, .34f));
            Disc(mesh, rect, new Vector2(.66f, .40f), new Vector2(.25f, .045f), new Color(.012f, .014f, .019f));
            Disc(mesh, rect, new Vector2(.28f, .12f), new Vector2(.22f, .035f), new Color(.012f, .016f, .023f));
            if (!showFigures) return;

            // Distant angular mass, with no face, species, weapon or status symbols.
            Polygon(mesh, rect, new Color(.16f, .078f, .076f),
                new Vector2(.49f, .44f), new Vector2(.84f, .43f), new Vector2(.80f, .70f),
                new Vector2(.70f, .86f), new Vector2(.59f, .83f), new Vector2(.52f, .66f));
            Polygon(mesh, rect, new Color(.034f, .036f, .047f),
                new Vector2(.50f, .43f), new Vector2(.85f, .42f), new Vector2(.79f, .69f),
                new Vector2(.69f, .85f), new Vector2(.60f, .82f), new Vector2(.53f, .65f));
            Polygon(mesh, rect, new Color(.060f, .062f, .076f),
                new Vector2(.53f, .47f), new Vector2(.65f, .48f), new Vector2(.65f, .75f), new Vector2(.56f, .66f));
            Polygon(mesh, rect, new Color(.045f, .046f, .057f),
                new Vector2(.68f, .45f), new Vector2(.83f, .43f), new Vector2(.75f, .67f), new Vector2(.69f, .73f));

            // A nearer back-facing draped silhouette, kept clear of the lower-left HUD.
            Polygon(mesh, rect, new Color(.085f, .14f, .20f),
                new Vector2(.22f, .17f), new Vector2(.48f, .16f), new Vector2(.43f, .38f),
                new Vector2(.35f, .49f), new Vector2(.27f, .44f));
            Polygon(mesh, rect, new Color(.021f, .029f, .043f),
                new Vector2(.21f, .16f), new Vector2(.47f, .15f), new Vector2(.42f, .37f),
                new Vector2(.35f, .48f), new Vector2(.28f, .43f));
            Polygon(mesh, rect, new Color(.040f, .057f, .079f),
                new Vector2(.23f, .17f), new Vector2(.32f, .20f), new Vector2(.35f, .44f), new Vector2(.28f, .40f));
            Polygon(mesh, rect, new Color(.052f, .064f, .084f),
                new Vector2(.30f, .42f), new Vector2(.38f, .41f), new Vector2(.40f, .50f),
                new Vector2(.36f, .56f), new Vector2(.30f, .53f), new Vector2(.28f, .47f));
        }

        void Quad(VertexHelper mesh, Rect rect, float left, float bottom, float right, float top, Color low, Color high)
        {
            int start = mesh.currentVertCount;
            Vertex(mesh, rect, new Vector2(left, bottom), low);
            Vertex(mesh, rect, new Vector2(left, top), high);
            Vertex(mesh, rect, new Vector2(right, top), high);
            Vertex(mesh, rect, new Vector2(right, bottom), low);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        void Polygon(VertexHelper mesh, Rect rect, Color tint, params Vector2[] points)
        {
            int start = mesh.currentVertCount;
            foreach (Vector2 point in points) Vertex(mesh, rect, point, tint);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }

        void Disc(VertexHelper mesh, Rect rect, Vector2 center, Vector2 radius, Color tint)
        {
            int start = mesh.currentVertCount;
            Vertex(mesh, rect, center, tint);
            const int sides = 20;
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                Vertex(mesh, rect, center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y), tint);
            }
            for (int i = 0; i < sides; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % sides);
        }

        void Vertex(VertexHelper mesh, Rect rect, Vector2 point, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height);
            vertex.color = tint * color;
            vertex.uv0 = Vector2.zero;
            mesh.AddVert(vertex);
        }
    }
}
