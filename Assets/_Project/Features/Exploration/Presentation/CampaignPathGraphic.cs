using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    // A single authored-style dashed connector. Endpoints and tint come from public map data.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampaignPathGraphic : MaskableGraphic
    {
        [Min(1)] public float dashLength = 7;
        [Min(1)] public float gapLength = 7;

        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            float dash = Mathf.Max(1, dashLength), stride = dash + Mathf.Max(1, gapLength);
            for (float x = rect.xMin; x < rect.xMax; x += stride)
            {
                int start = mesh.currentVertCount;
                float end = Mathf.Min(x + dash, rect.xMax);
                mesh.AddVert(new Vector3(x, rect.yMin), color, Vector2.zero);
                mesh.AddVert(new Vector3(x, rect.yMax), color, Vector2.zero);
                mesh.AddVert(new Vector3(end, rect.yMax), color, Vector2.zero);
                mesh.AddVert(new Vector3(end, rect.yMin), color, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
