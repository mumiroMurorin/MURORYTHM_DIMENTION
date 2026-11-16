using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold の3D編集の、描くもの (帯の面・線・つまみ・文字)
    /// </summary>
    public partial class SpaceHold3DEditor
    {
        static readonly Color selectedRingColor = new Color(1f, 0.9f, 0.3f, 1f);
        static readonly Color trajectoryColor = new Color(1f, 0.55f, 0.15f, 0.95f);
        static readonly Color seamColor = new Color(1f, 0.35f, 0.8f, 0.6f);
        static readonly Color laneColor = new Color(0.6f, 0.62f, 0.68f, 0.22f);
        static readonly Color barColor = new Color(0.65f, 0.67f, 0.75f, 0.35f);
        static readonly Color contextRingColor = new Color(0.72f, 0.72f, 0.75f, 0.30f);
        static readonly Color curveColor = new Color(1f, 0.85f, 0.3f, 0.85f);
        static readonly Color curveFaintColor = new Color(1f, 0.85f, 0.3f, 0.25f);
        static readonly Color axisXColor = new Color(1f, 0.32f, 0.3f, 1f);
        static readonly Color axisYColor = new Color(0.4f, 1f, 0.45f, 1f);
        static readonly Color axisZColor = new Color(0.35f, 0.62f, 1f, 1f);
        static readonly Color vertexStartColor = new Color(0.3f, 1f, 0.65f, 1f);
        static readonly Color vertexEndColor = new Color(1f, 0.45f, 0.85f, 1f);

        // 描くときに使う節 (制御点を動かしている間は、区間の中継点を、仮の中継点に入れ替えたもの)
        readonly List<SpaceHoldSection> drawnSections = new List<SpaceHoldSection>();
        // 0番の頂点の通り道 (帯を作るときに一緒に求める)
        readonly List<Vector3> seamPoints = new List<Vector3>();
        int tunnelSignature;

        // ほかのスペースのノーツ (灰色)
        readonly List<Vector3> contextVertices = new List<Vector3>();
        readonly List<int> contextTriangles = new List<int>();
        readonly List<Vector2[]> contextRingShapes = new List<Vector2[]>();
        readonly List<float> contextRingDepths = new List<float>();
        float nextContextRefreshTime;

        readonly List<Vector3> meshVertices = new List<Vector3>();
        readonly List<int> meshTriangles = new List<int>();
        readonly List<Vector3> pointBuffer = new List<Vector3>();
        readonly List<NoteData_SpaceHold> chainBuffer = new List<NoteData_SpaceHold>();
        readonly List<SpaceHoldSection> sectionBuffer = new List<SpaceHoldSection>();

        private void Draw()
        {
            // マウスの操作で変わった形を、同じフレームのうちに映す
            RefreshChain();
            if (sections.Count == 0) { return; }

            view.UpdateRenderTarget();
            rig.Apply(view.Camera);
            view.BeginFrame();

            BuildDrawnSections();
            UpdateTunnelMesh();
            if (Time.unscaledTime >= nextContextRefreshTime) { RefreshContext(); }

            float width = GetLineWidth();
            DrawGuides(width);
            DrawContext(width);
            DrawSections(width);
            DrawCurves(width);
            DrawGizmo(width);

            BuildHandles();
            DrawHandles();
            DrawTexts();
            gizmo?.Refresh(rig.Rotation);

            view.EndFrame();
        }

        /// <summary>
        /// 線の太さ (見ている距離に合わせて、画面の上でだいたい同じ太さに見えるように)
        /// </summary>
        private float GetLineWidth() => Mathf.Max(0.01f, rig.Distance * 0.0018f);

        private void BuildDrawnSections()
        {
            drawnSections.Clear();

            bool isPreviewing = dragKind == DragKind.Control && dragSpanIndex >= 0 && dragSpanIndex < spans.Count;
            if (!isPreviewing)
            {
                drawnSections.AddRange(sections);
                return;
            }

            Span span = spans[dragSpanIndex];
            for (int i = 0; i <= span.StartIndex; i++) { drawnSections.Add(sections[i]); }
            foreach (var baked in previewBaked)
            {
                drawnSections.Add(new SpaceHoldSection { Note = null, Vertices = baked.Vertices, Line = baked.Line, Time = baked.Time, Depth = baked.Depth });
            }
            for (int i = span.EndIndex; i < sections.Count; i++) { drawnSections.Add(sections[i]); }
        }

        #region 帯の面

        private void UpdateTunnelMesh()
        {
            int signature = ComputeSignature(drawnSections);
            if (signature == tunnelSignature && meshVertices.Count > 0) { return; }
            tunnelSignature = signature;

            meshVertices.Clear();
            meshTriangles.Clear();
            seamPoints.Clear();
            AppendTube(drawnSections, meshVertices, meshTriangles, seamPoints);
            view.SetTunnelMesh(meshVertices, meshTriangles);
        }

        private int ComputeSignature(List<SpaceHoldSection> list)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + list.Count;
                hash = hash * 31 + depthScaleStep;
                hash = hash * 31 + depthOrigin.GetHashCode();
                foreach (var section in list)
                {
                    hash = hash * 31 + section.Depth.GetHashCode();
                    foreach (var vertex in section.Vertices) { hash = hash * 31 + vertex.GetHashCode(); }
                }
                return hash;
            }
        }

        /// <summary>
        /// 節をつないだ帯の面を作る (ゲームと同じく、頂点の数をそろえて、隣の節とまっすぐつなぐ)
        /// </summary>
        private void AppendTube(IReadOnlyList<SpaceHoldSection> list, List<Vector3> vertices, List<int> triangles, List<Vector3> seam)
        {
            if (list.Count == 0) { return; }
            foreach (var section in list)
            {
                if (section.Vertices == null || section.Vertices.Length == 0) { return; }
            }

            int count = SpaceHoldSections.GetNormalizedVertexCount(list);
            int baseIndex = vertices.Count;
            foreach (var section in list)
            {
                Vector2[] resampled = SpaceHoldSections.Resample(section.Vertices, count);
                if (resampled.Length != count)
                {
                    // 頂点の数がそろわない (ありえないが、念のため): この帯は作らない
                    vertices.RemoveRange(baseIndex, vertices.Count - baseIndex);
                    return;
                }

                for (int j = 0; j < count; j++) { vertices.Add(ToLocal(resampled[j], section.Depth)); }
                seam?.Add(ToLocal(resampled[0], section.Depth));
            }

            for (int i = 0; i < list.Count - 1; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    int a = baseIndex + i * count + j;
                    int b = baseIndex + i * count + (j + 1) % count;
                    int c = baseIndex + (i + 1) * count + (j + 1) % count;
                    int d = baseIndex + (i + 1) * count + j;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(c); triangles.Add(d); triangles.Add(a);
                }
            }
        }

        #endregion

        #region ほかのスペースのノーツ (灰色)

        /// <summary>
        /// 開いている SpaceHold の近く (時間の範囲) にある、ほかのスペースのノーツを集め直す
        /// </summary>
        private void RefreshContext()
        {
            nextContextRefreshTime = Time.unscaledTime + CONTEXT_REFRESH_SECONDS;

            contextVertices.Clear();
            contextTriangles.Clear();
            contextRingShapes.Clear();
            contextRingDepths.Clear();

            if (timeline == null || sections.Count == 0)
            {
                view.SetContextMesh(contextVertices, contextTriangles);
                return;
            }

            GetDepthRange(out float minDepth, out float maxDepth);
            const float MARGIN = 1.5f;
            minDepth -= MARGIN;
            maxDepth += MARGIN;

            var visitedChains = new HashSet<int>();
            var allNotes = notesGetter.DataToNoteObject;
            for (int i = 0; i < allNotes.Count; i++)
            {
                var data = allNotes[i].Data;
                if (data == null) { continue; }

                if (data is NoteData_SpaceHold spaceHold)
                {
                    if (chain.Contains(spaceHold)) { continue; }

                    int chainIndex = spaceHold.ChainIndex.Value;
                    if (chainIndex >= 0 && !visitedChains.Add(chainIndex)) { continue; }

                    SpaceHoldSections.GetChain(notesGetter, spaceHold, chainBuffer);
                    if (!SpaceHoldSections.TryBuildSections(timeline, chainBuffer, sectionBuffer)) { continue; }
                    if (sectionBuffer.Count == 0) { continue; }

                    float first = Mathf.Min(sectionBuffer[0].Depth, sectionBuffer[sectionBuffer.Count - 1].Depth);
                    float last = Mathf.Max(sectionBuffer[0].Depth, sectionBuffer[sectionBuffer.Count - 1].Depth);
                    if (last < minDepth || first > maxDepth) { continue; }

                    if (sectionBuffer.Count >= 2) { AppendTube(sectionBuffer, contextVertices, contextTriangles, null); }
                    foreach (var section in sectionBuffer)
                    {
                        if (section.Note.NoteType == DeploymentNoteType.SpaceHoldMeshRelay) { continue; }
                        contextRingShapes.Add(section.Vertices);
                        contextRingDepths.Add(section.Depth);
                    }
                }
                else if (data is IVerticesControlableNoteData verticesData)
                {
                    // スペースブレイクなど: 形だけ
                    if (!timeline.TryGet(data.Address, out _, out float depth)) { continue; }
                    if (depth < minDepth || depth > maxDepth) { continue; }

                    contextRingShapes.Add(SpaceHoldSections.GetVertices(verticesData.SpaceVertices));
                    contextRingDepths.Add(depth);
                }
            }

            chainBuffer.Clear();
            sectionBuffer.Clear();
            view.SetContextMesh(contextVertices, contextTriangles);
        }

        private void DrawContext(float width)
        {
            for (int i = 0; i < contextRingShapes.Count; i++)
            {
                DrawShape(contextRingShapes[i], contextRingDepths[i], contextRingColor, width * 0.9f);
            }
        }

        private void GetDepthRange(out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            foreach (var section in sections)
            {
                min = Mathf.Min(min, section.Depth);
                max = Mathf.Max(max, section.Depth);
            }
            if (min > max) { min = max = 0f; }
        }

        #endregion

        #region 線

        /// <summary>
        /// ノーツの面 (下半分の半円の筒) のレーンの線と、小節線の位置の弧
        /// </summary>
        private void DrawGuides(float width)
        {
            GetDepthRange(out float minDepth, out float maxDepth);
            float start = minDepth - 0.5f;
            float end = maxDepth + 0.5f;

            // レーンの線 (16レーンの境目。真左 (-180度) から真右 (0度) まで)
            const float LANE_ANGLE = 11.25f;
            for (int lane = 0; lane <= 16; lane++)
            {
                float angle = (-180f + lane * LANE_ANGLE) * Mathf.Deg2Rad;
                Vector2 point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                pointBuffer.Clear();
                pointBuffer.Add(ToLocal(point, start));
                pointBuffer.Add(ToLocal(point, end));
                view.DrawLine(pointBuffer, laneColor, width * 0.7f, false);
            }

            // 小節線の位置の弧
            if (timeline == null) { return; }
            for (int line = 0; line < timeline.LineCount; line++)
            {
                if (timeline.GetSub(line) != 0) { continue; }

                float depth = timeline.GetDepth(line);
                if (depth < start || depth > end) { continue; }

                pointBuffer.Clear();
                const int SEGMENTS = 32;
                for (int i = 0; i <= SEGMENTS; i++)
                {
                    float angle = Mathf.Lerp(-180f, 0f, i / (float)SEGMENTS) * Mathf.Deg2Rad;
                    pointBuffer.Add(ToLocal(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), depth));
                }
                view.DrawLine(pointBuffer, barColor, width, false);
            }
        }

        /// <summary>
        /// 節の形、軌跡 (節の中心を通る線)、0番の頂点の通り道
        /// </summary>
        private void DrawSections(float width)
        {
            int hoveredSection = hasHovered ? hovered.Section : -1;

            for (int i = 0; i < drawnSections.Count; i++)
            {
                SpaceHoldSection section = drawnSections[i];
                bool isVirtual = section.Note == null;
                bool isSelected = !isVirtual && ReferenceEquals(section.Note, selectedNote);
                bool isMeshRelay = isVirtual || section.Note.NoteType == DeploymentNoteType.SpaceHoldMeshRelay;

                Color color = isSelected ? selectedRingColor : SpaceHoldFilmStrip.GetTypeColor(isVirtual ? DeploymentNoteType.SpaceHoldMeshRelay : section.Note.NoteType);
                if (isMeshRelay && !isSelected) { color.a = 0.55f; }
                if (!isVirtual && !isSelected && hoveredSection >= 0 && hoveredSection < sections.Count && ReferenceEquals(sections[hoveredSection].Note, section.Note))
                {
                    color = Color.Lerp(color, Color.white, 0.4f);
                }

                float ringWidth = isSelected ? width * 2.2f : isMeshRelay ? width * 0.8f : width * 1.3f;
                DrawShape(section.Vertices, section.Depth, color, ringWidth);
            }

            // 軌跡
            pointBuffer.Clear();
            foreach (var section in drawnSections) { pointBuffer.Add(ToLocal(section.Center, section.Depth)); }
            view.DrawLine(pointBuffer, trajectoryColor, width * 1.6f, false);

            // 0番の頂点の通り道
            view.DrawLine(seamPoints, seamColor, width * 0.9f, false);
        }

        private void DrawShape(Vector2[] vertices, float depth, Color color, float width)
        {
            if (vertices == null || vertices.Length < 2) { return; }

            pointBuffer.Clear();
            foreach (var vertex in vertices) { pointBuffer.Add(ToLocal(vertex, depth)); }
            view.DrawLine(pointBuffer, color, width, true);
        }

        /// <summary>
        /// ベジェ曲線 (区間ごと)。動かしている区間・カーソルを乗せている区間は、はっきり描き、制御点から両端の節への線も描く
        /// </summary>
        private void DrawCurves(float width)
        {
            for (int s = 0; s < spans.Count; s++)
            {
                Span span = spans[s];
                Vector3 control = s == dragSpanIndex ? dragControl : span.Control;
                Vector3 p0 = SpaceHoldCurveMath.GetPoint(sections[span.StartIndex]);
                Vector3 p1 = SpaceHoldCurveMath.GetPoint(sections[span.EndIndex]);

                bool isActive = s == dragSpanIndex || (hasHovered && hovered.Kind == HandleKind.Control && hovered.Span == s);
                bool isCurved = (control - SpaceHoldCurveMath.GetStraightControl(sections[span.StartIndex], sections[span.EndIndex])).sqrMagnitude > 1e-8f;
                if (!isActive && !isCurved) { continue; }

                pointBuffer.Clear();
                const int SEGMENTS = 24;
                for (int i = 0; i <= SEGMENTS; i++)
                {
                    Vector3 point = SpaceHoldCurveMath.Evaluate(p0, control, p1, i / (float)SEGMENTS);
                    pointBuffer.Add(ToLocal(new Vector2(point.x, point.y), point.z));
                }
                view.DrawLine(pointBuffer, isActive ? curveColor : curveFaintColor, width * (isActive ? 1.2f : 0.8f), false);

                if (!isActive) { continue; }

                Vector3 controlLocal = ToLocal(new Vector2(control.x, control.y), control.z);
                pointBuffer.Clear();
                pointBuffer.Add(ToLocal(new Vector2(p0.x, p0.y), p0.z));
                pointBuffer.Add(controlLocal);
                pointBuffer.Add(ToLocal(new Vector2(p1.x, p1.y), p1.z));
                view.DrawLine(pointBuffer, curveFaintColor, width * 0.7f, false);
            }
        }

        /// <summary>
        /// 選んでいる節の矢印 (赤: 横、緑: 縦、青: 時間)
        /// </summary>
        private void DrawGizmo(float width)
        {
            if (!TryGetSelected(out SpaceHoldSection section)) { return; }

            Vector3 originWorld = ToWorld(section.Center, section.Depth);
            float length = GetAxisLength(originWorld);
            Vector3 origin = ToLocal(section.Center, section.Depth);

            DrawAxis(origin, Vector3.right * length, axisXColor, width);
            DrawAxis(origin, Vector3.up * length, axisYColor, width);
            DrawAxis(origin, Vector3.forward * length, axisZColor, width);
        }

        private void DrawAxis(Vector3 origin, Vector3 axis, Color color, float width)
        {
            pointBuffer.Clear();
            pointBuffer.Add(origin);
            pointBuffer.Add(origin + axis);
            view.DrawLine(pointBuffer, color, width * 2.2f, false);
        }

        #endregion

        #region つまみ・文字

        private void DrawHandles()
        {
            foreach (var handle in handleList)
            {
                bool isHovered = hasHovered && hovered.Kind == handle.Kind && hovered.Section == handle.Section && hovered.Vertex == handle.Vertex && hovered.Span == handle.Span;
                float grow = isHovered ? 1.35f : 1f;

                switch (handle.Kind)
                {
                    case HandleKind.Control:
                        {
                            bool isDragged = handle.Span == dragSpanIndex;
                            Color color = isDragged || isHovered ? new Color(1f, 0.92f, 0.45f, 1f) : new Color(1f, 0.85f, 0.3f, 0.75f);
                            view.DrawHandle(handle.Local, 13f * grow, color, false, 45f);
                            break;
                        }
                    case HandleKind.AxisX:
                        view.DrawHandle(handle.Local, 12f * grow, axisXColor, false);
                        break;
                    case HandleKind.AxisY:
                        view.DrawHandle(handle.Local, 12f * grow, axisYColor, false);
                        break;
                    case HandleKind.AxisZ:
                        view.DrawHandle(handle.Local, 12f * grow, axisZColor, false);
                        break;
                    case HandleKind.Vertex:
                        {
                            int count = sections[handle.Section].Vertices.Length;
                            Color color = Color.Lerp(vertexStartColor, vertexEndColor, count <= 1 ? 0f : handle.Vertex / (float)(count - 1));
                            view.DrawHandle(handle.Local, 11f * grow, isHovered ? Color.white : color, true);
                            view.DrawLabel(handle.Local + new Vector2(7f, 4f), handle.Vertex.ToString(), 12f, new Color(1f, 1f, 1f, 0.85f));
                            break;
                        }
                    case HandleKind.Center:
                        {
                            SpaceHoldSection section = sections[handle.Section];
                            bool isSelected = handle.Section == selectedIndex;
                            Color color = isSelected ? Color.white : SpaceHoldFilmStrip.GetTypeColor(section.Note.NoteType);
                            if (section.Note.NoteType == DeploymentNoteType.SpaceHoldMeshRelay && !isSelected && !isHovered) { color.a = 0.6f; }
                            view.DrawHandle(handle.Local, (isSelected ? 13f : 9f) * grow, color, true);
                            break;
                        }
                }
            }

            // 選んでいる節の名前
            if (TryGetSelected(out SpaceHoldSection selected) && view.TryProject(ToWorld(selected.Center, selected.Depth), out Vector2 local))
            {
                view.DrawLabel(local + new Vector2(10f, -22f),
                    $"{SpaceHoldFilmStrip.GetTypeShortName(selected.Note.NoteType)} {SpaceHoldFilmStrip.FormatLine(timeline, selected.Line)}",
                    13f, selectedRingColor);
            }
        }

        private void DrawTexts()
        {
            if (TryGetSelected(out SpaceHoldSection selected))
            {
                view.InfoLabel.text =
                    $"選んでいる節: {SpaceHoldFilmStrip.GetTypeShortName(selected.Note.NoteType)}  {SpaceHoldFilmStrip.FormatLine(timeline, selected.Line)}" +
                    $"   ({selectedIndex + 1} / {sections.Count})   頂点 {selected.Vertices.Length} 個";
            }
            else
            {
                view.InfoLabel.text = $"節を選んでいません ({sections.Count} 個の節)。節の中心の点か、形の線を押すと選べます";
            }

            if (dragKind == DragKind.Control)
            {
                view.StatusLabel.text = $"曲線: メッシュ中継点 {previewBaked.Count} 個 (離すと置き直します。手で置いたメッシュ中継点も置き直されます)";
            }
            else
            {
                view.StatusLabel.text = Time.unscaledTime < statusUntil ? statusText : string.Empty;
            }
        }

        #endregion
    }
}
