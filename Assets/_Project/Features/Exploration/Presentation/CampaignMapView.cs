using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    // A presentation of the supplied public graph. It never creates paths or reads run history/RNG.
    public sealed class CampaignMapView : MonoBehaviour
    {
        public RectTransform edgeLayer, nodeLayer, playerMarker;
        public Text currentLocation;
        [Min(140)] public float rowSpacing = 174;
        public Color availableColor = new Color(.34f, .83f, .78f);
        public Color futureColor = new Color(.29f, .34f, .39f);
        public Color arrivedColor = new Color(.96f, .78f, .38f);
        public Color completedColor = new Color(.28f, .42f, .39f);
        public IReadOnlyDictionary<string, ExplorationNodeView> Nodes => views;

        readonly Dictionary<string, ExplorationNodeView> views = new Dictionary<string, ExplorationNodeView>();
        readonly Dictionary<string, NodeState> graph = new Dictionary<string, NodeState>();
        readonly Dictionary<string, int> depths = new Dictionary<string, int>();
        readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        readonly List<List<string>> rows = new List<List<string>>();
        readonly List<Edge> edges = new List<Edge>();
        readonly HashSet<string> activeIds = new HashSet<string>();
        readonly HashSet<string> completedIds = new HashSet<string>();
        string[] roots = Array.Empty<string>();
        string[] completedOrder = Array.Empty<string>();
        string selected;
        int generation;
        bool arranging, moving;
        bool focusPending;
        float lastWidth;

        sealed class Edge
        {
            public string from, to;
            public Image image;
        }

        public void Bind(IReadOnlyList<NodeState> active, IReadOnlyList<string> available, string selectedId,
            ExplorationNodeView prefab, FateDiceVisualCatalog visuals, Action<string> choose, IReadOnlyList<NodeState> completed = null)
        {
            if (!edgeLayer || !nodeLayer || !playerMarker || !currentLocation || !prefab || !visuals)
                throw new InvalidOperationException("CampaignMapView requires its authored layers, marker, label and node original.");
            if (active == null || available == null) throw new ArgumentNullException("Public graph snapshots");
            var nextRoots = available.Count > 0 ? available.ToArray() :
                (string.IsNullOrEmpty(selectedId) ? Array.Empty<string>() : new[] { selectedId });
            var completedNodes = completed ?? Array.Empty<NodeState>();
            if (completedNodes.Any(node => node == null || string.IsNullOrEmpty(node.id)))
                throw new ArgumentException("Completed map nodes require nonempty IDs.");
            var nextCompleted = completedNodes.Select(node => node.id).Distinct().ToArray();
            bool rebuild = views.Count == 0 || (available.Count > 0 && !roots.SequenceEqual(nextRoots)) ||
                !completedOrder.SequenceEqual(nextCompleted);
            bool refocus = rebuild || selected != selectedId;
            if (rebuild)
            {
                Clear(); roots = nextRoots; completedOrder = nextCompleted;
                foreach (var node in active)
                {
                    if (node == null || string.IsNullOrEmpty(node.id) || graph.ContainsKey(node.id))
                        throw new ArgumentException("Public map nodes require unique nonempty IDs.");
                    graph.Add(node.id, node);
                }
                var knownIds = new HashSet<string>(graph.Keys.Concat(nextCompleted));
                foreach (var node in completedNodes)
                {
                    if (graph.ContainsKey(node.id)) continue;
                    // Only verified completed nodes enter the display. Archived alternatives stay absent.
                    graph.Add(node.id, new NodeState { id = node.id, type = node.type,
                        childIds = node.childIds.Where(knownIds.Contains).Distinct().ToList() });
                    completedIds.Add(node.id);
                }
                BuildRows();
                foreach (var row in rows)
                    foreach (var id in row)
                    {
                        var view = Instantiate(prefab, nodeLayer, false);
                        view.name = "Node " + id;
                        views.Add(id, view);
                    }
                if (completedOrder.Length == 0)
                    foreach (var id in roots) AddEdge(null, id);
                foreach (var node in graph.Values)
                    foreach (var child in node.childIds.Distinct()) AddEdge(node.id, child);
            }
            selected = selectedId;
            activeIds.Clear();
            foreach (var node in active) activeIds.Add(node.id);
            var selectableIds = new HashSet<string>(available);
            foreach (var entry in views)
            {
                var view = entry.Value;
                if (!IsPresent(entry.Key))
                {
                    if (view.gameObject.activeSelf && !view.IsFading) view.FadeOut(visuals.nodeFadeSeconds);
                    continue;
                }
                var node = graph[entry.Key];
                bool done = completedIds.Contains(entry.Key);
                bool selectable = !done && selectableIds.Contains(entry.Key), chosen = entry.Key == selected;
                string label = (done ? "완료 · " : "") + KoreanText.Node(node.type);
                view.Bind(node.id, node.type, label, visuals.ResolveNode(node.type),
                    visuals.ResolveButton(ButtonPurpose.Navigation), selectable, chosen, choose);
                var tone = chosen ? arrivedColor : done ? completedColor : selectable ? availableColor : futureColor;
                view.frame.SetBorder(null, tone);
                view.frame.label.color = chosen ? arrivedColor : done ? new Color(.49f, .62f, .57f) :
                    selectable ? new Color(.96f, .96f, .92f) : new Color(.65f, .69f, .72f);
                var colors = view.frame.button.colors;
                colors.normalColor = chosen ? new Color(.20f, .17f, .10f) : new Color(.06f, .14f, .16f);
                colors.disabledColor = chosen ? new Color(.20f, .17f, .10f) : new Color(.055f, .066f, .079f);
                view.frame.button.colors = colors;
                view.frame.group.alpha = 1;
            }
            currentLocation.text = string.IsNullOrEmpty(selected) ?
                (completedOrder.Length == 0 ? "현재 위치  ·  연결된 길을 선택하세요" : "현재 위치  ·  " + KoreanText.Node(graph[completedOrder.Last()].type) + " 완료") :
                "도착  ·  " + KoreanText.Node(graph[selected].type);
            focusPending |= refocus;
            RefreshLayout();
            UpdateEdges();
        }

        // Topological layers allow a shared child to have one position and several incoming edges.
        void BuildRows()
        {
            var indegree = graph.Keys.ToDictionary(id => id, _ => 0);
            foreach (var node in graph.Values)
                foreach (var child in node.childIds.Distinct())
                {
                    if (!graph.ContainsKey(child)) throw new ArgumentException("Public map edge has a missing node: " + child);
                    indegree[child]++;
                }
            foreach (var id in graph.Keys) depths[id] = 0;
            var starts = completedOrder.Concat(roots).Distinct().Where(indegree.ContainsKey).Where(id => indegree[id] == 0).ToArray();
            var pending = new Queue<string>(starts);
            foreach (var id in graph.Keys)
                if (indegree[id] == 0 && !starts.Contains(id)) pending.Enqueue(id);
            var ordered = new List<string>();
            while (pending.Count > 0)
            {
                var id = pending.Dequeue(); ordered.Add(id);
                foreach (var child in graph[id].childIds.Distinct())
                {
                    depths[child] = Math.Max(depths[child], depths[id] + 1);
                    if (--indegree[child] == 0) pending.Enqueue(child);
                }
            }
            if (ordered.Count != graph.Count) throw new ArgumentException("Public map must not contain a cycle.");
            foreach (var id in ordered)
            {
                while (rows.Count <= depths[id]) rows.Add(new List<string>());
                rows[depths[id]].Add(id);
            }
            // Stable barycentres group descendants while preserving the supplied order for ties.
            for (int depth = 1; depth < rows.Count; depth++)
            {
                var previous = rows[depth - 1];
                rows[depth] = rows[depth].OrderBy(id =>
                {
                    var parents = previous.Where(parent => graph[parent].childIds.Contains(id)).ToArray();
                    return parents.Length == 0 ? double.MaxValue : parents.Average(parent => previous.IndexOf(parent));
                }).ToList();
            }
        }

        void AddEdge(string from, string to)
        {
            if (!views.ContainsKey(to) || (from != null && !views.ContainsKey(from))) return;
            var rect = new GameObject("Path " + (from ?? "current") + " -> " + to,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(edgeLayer, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            var image = rect.GetComponent<Image>(); image.raycastTarget = false;
            edges.Add(new Edge { from = from, to = to, image = image });
        }

        public void RefreshLayout()
        {
            if (arranging || !nodeLayer || rows.Count == 0) return;
            arranging = true;
            try
            {
                var rect = (RectTransform)transform;
                var scroll = GetComponentInParent<ScrollRect>();
                float viewportWidth = scroll && scroll.viewport ? scroll.viewport.rect.width : rect.rect.width;
                if (viewportWidth < 100) viewportWidth = 680;
                int columns = rows.Max(row => row.Count);
                bool wide = columns > 9;
                float width = wide ? Mathf.Max(viewportWidth, columns * 72 + 24) : viewportWidth;
                if (scroll && scroll.content)
                {
                    scroll.horizontal = wide;
                    var size = scroll.content.sizeDelta; size.x = width - viewportWidth; scroll.content.sizeDelta = size;
                    if (!wide) scroll.horizontalNormalizedPosition = .5f;
                }
                width -= 8;
                float height = Mathf.Max(470, (rows.Count - 1) * rowSpacing + 298);
                var element = GetComponent<LayoutElement>();
                if (element) element.minHeight = element.preferredHeight = height;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                for (int depth = 0; depth < rows.Count; depth++)
                {
                    var row = rows[depth];
                    for (int i = 0; i < row.Count; i++)
                    {
                        string id = row[i];
                        bool done = completedIds.Contains(id), current = roots.Contains(id) || id == selected;
                        var node = (RectTransform)views[id].transform;
                        node.anchorMin = node.anchorMax = new Vector2(.5f, 0); node.pivot = new Vector2(.5f, .5f);
                        node.anchoredPosition = new Vector2((i + .5f) * width / row.Count - width / 2, 186 + depth * rowSpacing);
                        node.sizeDelta = new Vector2(current ? Mathf.Min(116, width / row.Count - 16) :
                            done ? Mathf.Min(96, width / row.Count - 6) : Mathf.Min(64, width / row.Count - 6), current ? 106 : 80);
                        var frame = views[id].frame;
                        frame.label.fontSize = current ? 19 : done ? 14 : 16;
                        frame.iconFallback.fontSize = current ? 28 : 22;
                        positions[id] = node.anchoredPosition;
                    }
                }
                if (!moving) playerMarker.anchoredPosition = MarkerPosition(selected);
                var label = currentLocation.rectTransform;
                label.anchorMin = label.anchorMax = new Vector2(.5f, 0);
                label.anchoredPosition = completedOrder.Length == 0 ? new Vector2(0, 26) :
                    new Vector2(viewportWidth * .25f + 10, CurrentOrigin().y);
                label.sizeDelta = new Vector2(completedOrder.Length == 0 ? viewportWidth - 20 : viewportWidth * .45f, 40);
                if (wide && currentLocation.text.IndexOf("좌우", StringComparison.Ordinal) < 0)
                    currentLocation.text += "\n넓은 갈림길 · 좌우로 움직여 확인";
                lastWidth = viewportWidth;
                UpdateEdges();
                if (focusPending) FocusCurrent(scroll);
            }
            finally { arranging = false; }
        }

        Vector2 MarkerPosition(string id) => !string.IsNullOrEmpty(id) && positions.TryGetValue(id, out var point)
            ? point + new Vector2(0, -62) : CurrentOrigin();

        Vector2 CurrentOrigin() => completedOrder.Length > 0 && positions.TryGetValue(completedOrder.Last(), out var point)
            ? point + new Vector2(0, 66) : new Vector2(0, 74);

        bool IsPresent(string id) => activeIds.Contains(id) || completedIds.Contains(id);

        void FocusCurrent(ScrollRect scroll)
        {
            if (!scroll || !scroll.content || !scroll.viewport || !isActiveAndEnabled) return;
            var candidates = !string.IsNullOrEmpty(selected) && positions.ContainsKey(selected) ? new[] { selected } :
                roots.Where(id => activeIds.Contains(id) && positions.ContainsKey(id)).ToArray();
            if (candidates.Length == 0) return;
            float y = candidates.Average(id => positions[id].y);
            var point = scroll.content.InverseTransformPoint(nodeLayer.TransformPoint(new Vector3(0, y, 0)));
            float hidden = scroll.content.rect.height - scroll.viewport.rect.height;
            float fromTop = scroll.content.rect.yMax - point.y - scroll.viewport.rect.height * .55f;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = hidden > 0 ? 1 - Mathf.Clamp01(fromTop / hidden) : 1;
        }

        public IEnumerator AnimateNodeArrival(string id, float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!playerMarker || !positions.ContainsKey(id)) yield break;
            int token = ++generation; moving = true;
            var start = playerMarker.anchoredPosition; var target = MarkerPosition(id);
            selected = id;
            currentLocation.text = KoreanText.Node(graph[id].type) + "로 이동 중";
            float elapsed = 0;
            try
            {
                while (elapsed < seconds)
                {
                    if (!this || !isActiveAndEnabled || token != generation) yield break;
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / seconds));
                    playerMarker.anchoredPosition = Vector2.Lerp(start, target, t);
                    UpdateEdges();
                    yield return null;
                }
                if (token == generation && playerMarker)
                {
                    playerMarker.anchoredPosition = target;
                    currentLocation.text = "도착  ·  " + KoreanText.Node(graph[id].type);
                }
            }
            finally { if (token == generation) moving = false; }
        }

        void UpdateEdges()
        {
            foreach (var edge in edges)
            {
                if (!edge.image || !positions.TryGetValue(edge.to, out var to)) continue;
                var from = edge.from == null ? new Vector2(0, 74) : positions[edge.from];
                var delta = to - from;
                var direction = delta.normalized;
                float startInset = edge.from == null ? 20 : ((RectTransform)views[edge.from].transform).rect.height / 2 + 2;
                float endInset = ((RectTransform)views[edge.to].transform).rect.height / 2 + 2;
                var start = from + direction * startInset; var end = to - direction * endInset;
                var rect = edge.image.rectTransform;
                rect.anchoredPosition = (start + end) / 2;
                rect.sizeDelta = new Vector2(Vector2.Distance(start, end), edge.from == null ? 3 : 2);
                rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                bool chosen = edge.to == selected || edge.from == selected;
                bool completedEdge = edge.from != null && completedIds.Contains(edge.from) && completedIds.Contains(edge.to);
                bool nextPath = edge.from == null || edge.from != null && completedIds.Contains(edge.from) && roots.Contains(edge.to);
                Color color = chosen ? arrivedColor : completedEdge ? completedColor : nextPath ? availableColor : futureColor;
                float alpha = views[edge.to].frame.group.alpha;
                if (edge.from != null) alpha = Mathf.Min(alpha, views[edge.from].frame.group.alpha);
                if (!IsPresent(edge.to) || edge.from != null && !IsPresent(edge.from))
                    color.a = alpha * .7f;
                else color.a = chosen || edge.from == null ? .85f : .6f;
                edge.image.color = color;
            }
        }

        void LateUpdate()
        {
            if (rows.Count == 0) return;
            var scroll = GetComponentInParent<ScrollRect>();
            float width = scroll && scroll.viewport ? scroll.viewport.rect.width : ((RectTransform)transform).rect.width;
            if (Mathf.Abs(width - lastWidth) > .5f) RefreshLayout();
            if (focusPending)
            {
                Canvas.ForceUpdateCanvases();
                FocusCurrent(scroll);
                focusPending = false;
            }
            UpdateEdges();
        }
        void OnRectTransformDimensionsChange() { if (!arranging && rows.Count > 0) RefreshLayout(); }
        void OnDisable() { generation++; moving = false; }
        public void Clear()
        {
            generation++; moving = false;
            foreach (var view in views.Values)
            {
                if (!view) continue;
                view.Unbind(); view.gameObject.SetActive(false); DestroyOwned(view.gameObject);
            }
            foreach (var edge in edges)
                if (edge.image) { edge.image.gameObject.SetActive(false); DestroyOwned(edge.image.gameObject); }
            views.Clear(); graph.Clear(); depths.Clear(); positions.Clear(); rows.Clear(); edges.Clear(); activeIds.Clear(); completedIds.Clear();
            roots = completedOrder = Array.Empty<string>(); selected = null; focusPending = false;
            if (currentLocation) currentLocation.text = "";
        }
        static void DestroyOwned(GameObject item)
        {
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }
    }
}
