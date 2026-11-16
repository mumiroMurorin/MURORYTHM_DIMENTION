using System.Collections.Generic;
using UndoRedo.Notes;
using UndoRedo.Vertices;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold の3D編集の、マウスの操作 (つまみを選ぶ・動かす、カメラを動かす)
    /// </summary>
    public partial class SpaceHold3DEditor
    {
        enum DragKind
        {
            None,
            Orbit,
            Pan,
            // 何も無い所を押した (動かせば回す、動かさずに離せば選ぶのをやめる)
            EmptyPress,
            // 右ボタンを押した (動かせば回す、動かさずに離せば、つまみによって消す・戻す)
            RightPress,
            Vertex,
            Center,
            AxisX,
            AxisY,
            AxisZ,
            Control,
        }

        enum HandleKind
        {
            None,
            AxisX,
            AxisY,
            AxisZ,
            Control,
            Vertex,
            Center,
        }

        /// <summary>
        /// 画面上のつまみ1つぶん
        /// </summary>
        struct Handle
        {
            public HandleKind Kind;
            public int Section;
            public int Vertex;
            public int Span;
            public Vector2 Local;
            public float Radius;
        }

        // つまみを掴める範囲 (画面部品の単位。1920×1080 基準)
        const float HANDLE_PICK_RADIUS = 10f;
        // 節の形の線を押したとみなす距離
        const float RING_PICK_DISTANCE = 7f;
        // ドラッグを始めたとみなす、カーソルの動き (ピクセル)
        const float DRAG_START_PIXELS = 4f;
        // 節の面を、ほとんど真横から見ているとみなす傾き (このときは、画面に平行な面の中で動かす)
        const float GRAZING_DIRECTION_Z = 0.15f;

        readonly List<Handle> handleList = new List<Handle>();
        Handle hovered;
        bool hasHovered;

        DragKind dragKind;
        int dragButton;
        Vector2 dragStartMouse;
        Vector2 lastMouse;
        bool hasDragMoved;
        bool waitForMouseRelease;

        // 頂点・節を動かす
        NoteData_SpaceHold dragNote;
        int dragVertexIndex;
        readonly List<VertexData> dragVertices = new List<VertexData>();
        readonly List<Vector2> dragStartPositions = new List<Vector2>();
        bool dragUseViewPlane;
        float dragPlaneZ;
        Vector3 dragPlanePoint;
        Vector3 dragPlaneNormal;
        Vector3 dragStartHit;
        Vector3 dragAxisOrigin;
        Vector3 dragAxisDirection;
        float dragAxisStartT;

        // 時間の向きに動かす
        AddressWithinRange dragStartAddress;
        float dragStartDepth;
        int dragLineMin;
        int dragLineMax;

        // 制御点を動かす
        int dragSpanIndex = -1;
        Vector3 dragStartControl;
        Vector3 dragControl;
        Vector3 dragStartControlWorld;
        readonly List<SpaceHoldCurveMath.BakedSection> previewBaked = new List<SpaceHoldCurveMath.BakedSection>();

        // 右ボタンを押したときのつまみ
        Handle rightPressHandle;
        bool hasRightPressHandle;

        bool IsDragging => dragKind != DragKind.None;

        private void HandleMouse()
        {
            Vector2 mouse = Input.mousePosition;
            rig.Apply(view.Camera);

            // 開いたときのクリックを離すまでは、何もしない
            if (waitForMouseRelease)
            {
                hasHovered = false;
                if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2)) { waitForMouseRelease = false; }
                lastMouse = mouse;
                return;
            }

            bool isInside = view.TryGetViewportLocal(mouse, out Vector2 local);

            if (IsDragging)
            {
                UpdateDrag(local, mouse);
                if (!Input.GetMouseButton(dragButton)) { FinishDrag(); }
                lastMouse = mouse;
                return;
            }

            bool isOnViewport = isInside && !EditorMenuBar.IsMenuOpenOrJustClosed && view.IsPointerOnViewport(mouse);

            BuildHandles();
            hasHovered = isOnViewport && TryFindHandle(local, out hovered);

            if (isOnViewport)
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f) { rig.Zoom(wheel); }

                if (Input.GetMouseButtonDown(0)) { BeginLeft(local, mouse); }
                else if (Input.GetMouseButtonDown(1)) { BeginRight(mouse); }
                else if (Input.GetMouseButtonDown(2)) { BeginDrag(DragKind.Pan, 2, mouse); }
            }

            lastMouse = mouse;
        }

        private void BeginDrag(DragKind kind, int button, Vector2 mouse)
        {
            dragKind = kind;
            dragButton = button;
            dragStartMouse = mouse;
            lastMouse = mouse;
            hasDragMoved = false;
        }

        private void BeginLeft(Vector2 local, Vector2 mouse)
        {
            // Alt+左ドラッグ: 回す (Unity と同じ)
            if (ModifierKey.Alt)
            {
                BeginDrag(DragKind.Orbit, 0, mouse);
                return;
            }

            if (hasHovered)
            {
                switch (hovered.Kind)
                {
                    case HandleKind.Vertex:
                        StartVertexDrag(hovered.Section, hovered.Vertex, DragKind.Vertex, local, mouse);
                        return;
                    case HandleKind.Center:
                        StartVertexDrag(hovered.Section, -1, DragKind.Center, local, mouse);
                        return;
                    case HandleKind.AxisX:
                        StartVertexDrag(hovered.Section, -1, DragKind.AxisX, local, mouse);
                        return;
                    case HandleKind.AxisY:
                        StartVertexDrag(hovered.Section, -1, DragKind.AxisY, local, mouse);
                        return;
                    case HandleKind.AxisZ:
                        StartTimeDrag(hovered.Section, local, mouse);
                        return;
                    case HandleKind.Control:
                        StartControlDrag(hovered.Span, local, mouse);
                        return;
                }
            }

            // 節の形の線: その節を選ぶ (Ctrl を押していて、選んでいる節の線なら、そこに頂点を足す)
            if (TryPickRing(local, out int ringSection))
            {
                if (ModifierKey.Ctrl && ringSection == selectedIndex) { AddVertexAt(local); }
                else { Select(ringSection); }

                waitForMouseRelease = true;
                return;
            }

            BeginDrag(DragKind.EmptyPress, 0, mouse);
        }

        private void BeginRight(Vector2 mouse)
        {
            hasRightPressHandle = hasHovered && (hovered.Kind == HandleKind.Vertex || hovered.Kind == HandleKind.Control);
            rightPressHandle = hovered;
            BeginDrag(DragKind.RightPress, 1, mouse);
        }

        private void UpdateDrag(Vector2 local, Vector2 mouse)
        {
            Vector2 delta = mouse - lastMouse;
            if ((mouse - dragStartMouse).sqrMagnitude > DRAG_START_PIXELS * DRAG_START_PIXELS) { hasDragMoved = true; }

            switch (dragKind)
            {
                case DragKind.Orbit:
                    rig.Orbit(delta);
                    break;
                case DragKind.EmptyPress:
                case DragKind.RightPress:
                    if (hasDragMoved) { rig.Orbit(delta); }
                    break;
                case DragKind.Pan:
                    float scale = view.Canvas != null && view.Canvas.scaleFactor > 0f ? view.Canvas.scaleFactor : 1f;
                    rig.Pan(delta, view.ViewportSize.y * scale);
                    break;
                case DragKind.Vertex:
                case DragKind.Center:
                case DragKind.AxisX:
                case DragKind.AxisY:
                    UpdateVertexDrag(local);
                    break;
                case DragKind.AxisZ:
                    UpdateTimeDrag(local);
                    break;
                case DragKind.Control:
                    UpdateControlDrag(local);
                    break;
            }
        }

        /// <summary>
        /// ドラッグを終える (変えたものを、1回の操作として記録する)
        /// </summary>
        private void FinishDrag()
        {
            DragKind kind = dragKind;
            dragKind = DragKind.None;

            try
            {
                switch (kind)
                {
                    case DragKind.EmptyPress:
                        if (!hasDragMoved) { Select(-1); }
                        break;
                    case DragKind.RightPress:
                        if (!hasDragMoved && hasRightPressHandle) { OnRightClickHandle(rightPressHandle); }
                        break;
                    case DragKind.Vertex:
                    case DragKind.Center:
                    case DragKind.AxisX:
                    case DragKind.AxisY:
                        FinishVertexDrag(kind);
                        break;
                    case DragKind.AxisZ:
                        FinishTimeDrag();
                        break;
                    case DragKind.Control:
                        FinishControlDrag();
                        break;
                }
            }
            finally
            {
                dragNote = null;
                dragVertices.Clear();
                dragStartPositions.Clear();
                dragSpanIndex = -1;
                previewBaked.Clear();
                hasRightPressHandle = false;
            }
        }

        /// <summary>
        /// ドラッグを、何も記録せずにやめる (開き直したとき)
        /// </summary>
        private void CancelDrag()
        {
            dragKind = DragKind.None;
            dragNote = null;
            dragVertices.Clear();
            dragStartPositions.Clear();
            dragSpanIndex = -1;
            previewBaked.Clear();
            hasRightPressHandle = false;
        }

        #region 頂点・節を動かす

        private void StartVertexDrag(int sectionIndex, int vertexIndex, DragKind kind, Vector2 local, Vector2 mouse)
        {
            if (sectionIndex < 0 || sectionIndex >= sections.Count) { return; }

            if (sectionIndex != selectedIndex) { Select(sectionIndex); }

            SpaceHoldSection section = sections[sectionIndex];
            dragNote = section.Note;
            dragVertexIndex = vertexIndex;

            dragVertices.Clear();
            dragStartPositions.Clear();
            foreach (var vertex in section.Note.SpaceVertices.Vertices)
            {
                dragVertices.Add(vertex);
                dragStartPositions.Add(vertex.Position.Value);
            }
            if (vertexIndex >= dragVertices.Count) { return; }

            Vector3 grab = vertexIndex >= 0 ? ToWorld(section.Vertices[vertexIndex], section.Depth) : ToWorld(section.Center, section.Depth);
            Ray ray = view.GetRay(local);

            if (kind == DragKind.AxisX || kind == DragKind.AxisY)
            {
                dragAxisOrigin = grab;
                dragAxisDirection = kind == DragKind.AxisX ? Vector3.right : Vector3.up;
                if (!TryGetAxisParameter(ray, dragAxisOrigin, dragAxisDirection, out dragAxisStartT)) { dragAxisStartT = 0f; }
            }
            else
            {
                // 節の面の中で動かす。面をほとんど真横から見ているときは、画面に平行な面の中で動かして、奥行きは捨てる
                dragPlaneZ = grab.z;
                dragUseViewPlane = Mathf.Abs(ray.direction.z) < GRAZING_DIRECTION_Z;
                dragPlanePoint = grab;
                dragPlaneNormal = rig.Forward;
                if (!TryHitDragPlane(ray, out dragStartHit)) { dragStartHit = grab; }
            }

            BeginDrag(kind, 0, mouse);
        }

        private void UpdateVertexDrag(Vector2 local)
        {
            if (dragNote == null || dragVertices.Count == 0) { return; }

            Ray ray = view.GetRay(local);
            Vector2 deltaUnit;
            if (dragKind == DragKind.AxisX || dragKind == DragKind.AxisY)
            {
                if (!TryGetAxisParameter(ray, dragAxisOrigin, dragAxisDirection, out float t)) { return; }
                Vector3 move = dragAxisDirection * (t - dragAxisStartT);
                deltaUnit = new Vector2(move.x, move.y) / RADIUS;
            }
            else
            {
                if (!TryHitDragPlane(ray, out Vector3 hit)) { return; }
                deltaUnit = ToUnit(hit) - ToUnit(dragStartHit);
            }

            if (dragKind == DragKind.Vertex)
            {
                if (dragVertexIndex >= 0 && dragVertexIndex < dragVertices.Count)
                {
                    dragVertices[dragVertexIndex].SetPosition(dragStartPositions[dragVertexIndex] + deltaUnit);
                }
                return;
            }

            for (int i = 0; i < dragVertices.Count; i++)
            {
                dragVertices[i].SetPosition(dragStartPositions[i] + deltaUnit);
            }
        }

        private void FinishVertexDrag(DragKind kind)
        {
            var previous = new List<VertexDataToPos>();
            var current = new List<VertexDataToPos>();
            for (int i = 0; i < dragVertices.Count; i++)
            {
                Vector2 now = dragVertices[i].Position.Value;
                if (now == dragStartPositions[i]) { continue; }

                previous.Add(new VertexDataToPos(dragVertices[i], dragStartPositions[i]));
                current.Add(new VertexDataToPos(dragVertices[i], now));
            }

            if (current.Count == 0) { return; }

            VerticesMoveRecord.RecordVertcesMoving(previous, current);
            ShowStatus(kind == DragKind.Vertex ? "頂点を動かしました" : "節を動かしました");
        }

        private bool TryHitDragPlane(Ray ray, out Vector3 hit)
        {
            hit = default;

            var plane = dragUseViewPlane
                ? new Plane(dragPlaneNormal, dragPlanePoint)
                : new Plane(Vector3.forward, new Vector3(0f, 0f, dragPlaneZ));
            if (!plane.Raycast(ray, out float enter)) { return false; }

            hit = ray.GetPoint(enter);
            hit.z = dragPlaneZ;
            return true;
        }

        /// <summary>
        /// 光線に、いちばん近い軸の上の位置 (軸の始まりからの距離)
        /// </summary>
        private static bool TryGetAxisParameter(Ray ray, Vector3 origin, Vector3 direction, out float t)
        {
            t = 0f;
            Vector3 w0 = origin - ray.origin;
            float b = Vector3.Dot(direction, ray.direction);
            float d = Vector3.Dot(direction, w0);
            float e = Vector3.Dot(ray.direction, w0);
            float denominator = 1f - b * b;
            if (denominator < 1e-4f) { return false; }

            t = (b * e - d) / denominator;
            return true;
        }

        #endregion

        #region 時間の向きに動かす

        private void StartTimeDrag(int sectionIndex, Vector2 local, Vector2 mouse)
        {
            if (sectionIndex < 0 || sectionIndex >= sections.Count || timeline == null) { return; }

            if (sectionIndex != selectedIndex) { Select(sectionIndex); }

            SpaceHoldSection section = sections[sectionIndex];
            dragNote = section.Note;
            dragStartAddress = new AddressWithinRange(section.Note.Address);
            dragStartDepth = section.Depth;

            // 前後の節は追い越さない (始点・終点は、譜面の端まで)
            dragLineMin = sectionIndex > 0 ? Mathf.Min(section.Line, sections[sectionIndex - 1].Line + 1) : 0;
            dragLineMax = sectionIndex + 1 < sections.Count ? Mathf.Max(section.Line, sections[sectionIndex + 1].Line - 1) : timeline.LineCount - 1;

            dragAxisOrigin = ToWorld(section.Center, section.Depth);
            dragAxisDirection = Vector3.forward;
            if (!TryGetAxisParameter(view.GetRay(local), dragAxisOrigin, dragAxisDirection, out dragAxisStartT)) { dragAxisStartT = 0f; }

            BeginDrag(DragKind.AxisZ, 0, mouse);
        }

        private void UpdateTimeDrag(Vector2 local)
        {
            if (dragNote == null || timeline == null) { return; }
            if (!TryGetAxisParameter(view.GetRay(local), dragAxisOrigin, dragAxisDirection, out float t)) { return; }

            float depth = dragStartDepth + (t - dragAxisStartT) / (DEPTH_UNIT * DepthScale);
            int line = timeline.FindNearestLineByDepth(depth, dragLineMin, dragLineMax);
            if (line < 0 || line == timeline.GetLine(dragNote.Address)) { return; }

            dragNote.SetAddress(timeline.CreateSpaceAddress(line));
        }

        private void FinishTimeDrag()
        {
            if (dragNote == null || dragStartAddress == null) { return; }

            var previous = new List<NoteDataToAddress> { new NoteDataToAddress(dragNote, dragStartAddress) };
            var current = new List<NoteDataToAddress> { new NoteDataToAddress(dragNote, dragNote.Address) };
            if (NotesMoveRecord.RecordNotesMovingIfChanged(previous, current))
            {
                var nowTimeline = SpaceHoldTimeline.Get(dataGetter.ChartData.Value);
                ShowStatus($"節を {SpaceHoldFilmStrip.FormatLine(nowTimeline, nowTimeline?.GetLine(dragStartAddress) ?? -1)} から {SpaceHoldFilmStrip.FormatLine(nowTimeline, nowTimeline?.GetLine(dragNote.Address) ?? -1)} へ動かしました");
            }
        }

        #endregion

        #region 制御点を動かす

        private void StartControlDrag(int spanIndex, Vector2 local, Vector2 mouse)
        {
            if (spanIndex < 0 || spanIndex >= spans.Count) { return; }

            Span span = spans[spanIndex];
            dragSpanIndex = spanIndex;
            dragStartControl = span.Control;
            dragControl = span.Control;
            dragStartControlWorld = ToWorld(new Vector2(span.Control.x, span.Control.y), span.Control.z);

            // 制御点は、カメラに向いた面の中で動かす (どの向きから見ても、カーソルに付いてくるように)
            dragUseViewPlane = true;
            dragPlanePoint = dragStartControlWorld;
            dragPlaneNormal = rig.Forward;
            var plane = new Plane(dragPlaneNormal, dragPlanePoint);
            Ray ray = view.GetRay(local);
            dragStartHit = plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : dragStartControlWorld;

            RebuildPreview();
            BeginDrag(DragKind.Control, 0, mouse);
        }

        private void UpdateControlDrag(Vector2 local)
        {
            if (dragSpanIndex < 0 || dragSpanIndex >= spans.Count) { return; }

            var plane = new Plane(dragPlaneNormal, dragPlanePoint);
            Ray ray = view.GetRay(local);
            if (!plane.Raycast(ray, out float enter)) { return; }

            Vector3 world = dragStartControlWorld + (ray.GetPoint(enter) - dragStartHit);
            Vector2 unit = ToUnit(world);

            Span span = spans[dragSpanIndex];
            dragControl = SpaceHoldCurveMath.ClampControl(sections[span.StartIndex], sections[span.EndIndex], new Vector3(unit.x, unit.y, ToDepth(world)));
            RebuildPreview();
        }

        /// <summary>
        /// 制御点を動かしている間の、仮の中継点 (まだ譜面には入れない)
        /// </summary>
        private void RebuildPreview()
        {
            previewBaked.Clear();
            if (dragSpanIndex < 0 || dragSpanIndex >= spans.Count || timeline == null) { return; }

            Span span = spans[dragSpanIndex];
            SpaceHoldCurveMath.Bake(timeline, sections[span.StartIndex], sections[span.EndIndex], dragControl, SpaceHoldSections.GetNormalizedVertexCount(sections), previewBaked);
        }

        private void FinishControlDrag()
        {
            if (dragSpanIndex < 0 || dragSpanIndex >= spans.Count) { return; }
            if (!hasDragMoved) { return; }

            Span span = spans[dragSpanIndex];
            int count = CommitCurve(span.StartIndex, span.EndIndex, dragControl);
            if (count >= 0) { ShowStatus($"曲線のとおりに、メッシュ中継点を {count} 個置きました"); }
        }

        #endregion

        #region 右クリック・頂点を足す

        private void OnRightClickHandle(Handle handle)
        {
            switch (handle.Kind)
            {
                // 頂点を消す (形を作るのに必要な数より少なくはならない)
                case HandleKind.Vertex:
                    if (handle.Section < 0 || handle.Section >= sections.Count) { return; }
                    var vertices = sections[handle.Section].Note.SpaceVertices;
                    if (handle.Vertex < 0 || handle.Vertex >= vertices.Vertices.Count) { return; }
                    if (PreviewVertexTools.RemoveVertex(vertices, vertices.Vertices[handle.Vertex])) { ShowStatus("頂点を消しました"); }
                    else { ShowStatus("これ以上、頂点を消せません"); }
                    break;

                // まっすぐに戻す (間のメッシュ中継点を消す)
                case HandleKind.Control:
                    if (handle.Span < 0 || handle.Span >= spans.Count) { return; }
                    Span span = spans[handle.Span];
                    int count = CommitCurve(span.StartIndex, span.EndIndex, SpaceHoldCurveMath.GetStraightControl(sections[span.StartIndex], sections[span.EndIndex]));
                    if (count >= 0) { ShowStatus("2つの節の間を、まっすぐに戻しました"); }
                    break;
            }
        }

        /// <summary>
        /// 選んでいる節の、カーソルの位置に頂点を足す (いちばん近い辺に入る)
        /// </summary>
        private void AddVertexAt(Vector2 local)
        {
            if (!TryGetSelected(out SpaceHoldSection section)) { return; }

            Ray ray = view.GetRay(local);
            Vector3 center = ToWorld(section.Center, section.Depth);
            dragPlaneZ = center.z;
            dragUseViewPlane = Mathf.Abs(ray.direction.z) < GRAZING_DIRECTION_Z;
            dragPlanePoint = center;
            dragPlaneNormal = rig.Forward;
            if (!TryHitDragPlane(ray, out Vector3 hit)) { return; }

            PreviewVertexTools.AddVertex(section.Note.SpaceVertices, ToUnit(hit));
            ShowStatus("頂点を足しました");
        }

        #endregion

        #region つまみを探す

        /// <summary>
        /// 今の画面での、つまみの位置を集める
        /// </summary>
        private void BuildHandles()
        {
            handleList.Clear();
            if (view.Camera == null) { return; }

            // 制御点 (◆)
            for (int i = 0; i < spans.Count; i++)
            {
                Vector3 control = i == dragSpanIndex ? dragControl : spans[i].Control;
                AddHandle(HandleKind.Control, -1, -1, i, ToWorld(new Vector2(control.x, control.y), control.z));
            }

            // 選んでいる節の、矢印の先と頂点
            if (TryGetSelected(out SpaceHoldSection selected))
            {
                Vector3 origin = ToWorld(selected.Center, selected.Depth);
                float length = GetAxisLength(origin);
                AddHandle(HandleKind.AxisX, selectedIndex, -1, -1, origin + Vector3.right * length);
                AddHandle(HandleKind.AxisY, selectedIndex, -1, -1, origin + Vector3.up * length);
                AddHandle(HandleKind.AxisZ, selectedIndex, -1, -1, origin + Vector3.forward * length);

                for (int v = 0; v < selected.Vertices.Length; v++)
                {
                    AddHandle(HandleKind.Vertex, selectedIndex, v, -1, ToWorld(selected.Vertices[v], selected.Depth));
                }
            }

            // 節の中心
            for (int i = 0; i < sections.Count; i++)
            {
                AddHandle(HandleKind.Center, i, -1, -1, ToWorld(sections[i].Center, sections[i].Depth));
            }
        }

        private void AddHandle(HandleKind kind, int section, int vertex, int span, Vector3 world)
        {
            if (!view.TryProject(world, out Vector2 local)) { return; }

            handleList.Add(new Handle
            {
                Kind = kind,
                Section = section,
                Vertex = vertex,
                Span = span,
                Local = local,
                Radius = HANDLE_PICK_RADIUS,
            });
        }

        /// <summary>
        /// カーソルの下のつまみ (重なっているときは、矢印 → 制御点 → 頂点 → 中心 の順に優先する)
        /// </summary>
        private bool TryFindHandle(Vector2 local, out Handle found)
        {
            found = default;
            bool isFound = false;
            float bestDistance = float.MaxValue;
            int bestPriority = int.MaxValue;

            foreach (var handle in handleList)
            {
                float distance = (handle.Local - local).magnitude;
                if (distance > handle.Radius) { continue; }

                int priority = (int)handle.Kind;
                if (priority < bestPriority || (priority == bestPriority && distance < bestDistance))
                {
                    found = handle;
                    bestPriority = priority;
                    bestDistance = distance;
                    isFound = true;
                }
            }

            return isFound;
        }

        /// <summary>
        /// カーソルの近くにある、節の形の線
        /// </summary>
        private bool TryPickRing(Vector2 local, out int sectionIndex)
        {
            sectionIndex = -1;
            float best = RING_PICK_DISTANCE;

            for (int i = 0; i < sections.Count; i++)
            {
                Vector2[] vertices = sections[i].Vertices;
                if (vertices.Length < 2) { continue; }

                bool hasPrevious = view.TryProject(ToWorld(vertices[vertices.Length - 1], sections[i].Depth), out Vector2 previous);
                for (int v = 0; v < vertices.Length; v++)
                {
                    bool hasCurrent = view.TryProject(ToWorld(vertices[v], sections[i].Depth), out Vector2 current);
                    if (hasPrevious && hasCurrent)
                    {
                        float distance = DistanceToSegment(local, previous, current);
                        if (distance < best)
                        {
                            best = distance;
                            sectionIndex = i;
                        }
                    }

                    previous = current;
                    hasPrevious = hasCurrent;
                }
            }

            return sectionIndex >= 0;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared <= 1e-6f) { return (point - a).magnitude; }

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
            return (point - (a + ab * t)).magnitude;
        }

        /// <summary>
        /// 選んでいる節の矢印の長さ (画面の上で、だいたい同じ長さに見えるように)
        /// </summary>
        private float GetAxisLength(Vector3 origin)
        {
            float distance = rig.IsOrthographic || view.Camera == null
                ? rig.Distance
                : Vector3.Distance(view.Camera.transform.position, origin);
            return Mathf.Max(0.5f, distance * 0.12f);
        }

        #endregion
    }
}
