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
            // 頂点の道具: 回す / 拡大・縮小する
            Rotate,
            Scale,
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
        // 右ボタンを押したときの、メニューを出す節と頂点 (無ければ null)
        NoteData_SpaceHold rightPressNote;
        VertexData rightPressVertex;
        // 節の線のダブルクリック (前にクリックした節・時刻・位置)
        NoteData_SpaceHold lastRingClickNote;
        float lastRingClickTime = -10f;
        Vector2 lastRingClickPosition;

        // 頂点・節を動かす
        NoteData_SpaceHold dragNote;
        int dragVertexIndex;
        // 選んでいる頂点をまとめて動かすときの、動かす頂点 (dragVertices の中の番号。空なら dragVertexIndex の1つだけ)
        readonly List<int> dragGroup = new List<int>();
        // 回す・拡大縮小するときの、中心と、掴んだ頂点の元の位置 (単位円の中の値)
        Vector2 transformCenter;
        Vector2 transformGrabPoint;
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
                // 回している・拡大縮小している間は、その絵のまま。動かしている間は「動かす」の絵
                if (dragKind == DragKind.Rotate) { SetCursor(EditMode.VerticesRotating); }
                else if (dragKind == DragKind.Scale) { SetCursor(EditMode.VerticesScaling); }
                else if (dragKind == DragKind.Vertex || dragKind == DragKind.Center || dragKind == DragKind.AxisX ||
                         dragKind == DragKind.AxisY || dragKind == DragKind.AxisZ || dragKind == DragKind.Control) { SetCursor(EditMode.VertexMoving); }
                else { SetCursor(EditMode.None); }

                UpdateDrag(local, mouse);
                if (!Input.GetMouseButton(dragButton)) { FinishDrag(); }
                lastMouse = mouse;
                return;
            }

            bool isOnViewport = isInside && !EditorMenuBar.IsMenuOpenOrJustClosed && view.IsPointerOnViewport(mouse);

            BuildHandles();
            hasHovered = isOnViewport && TryFindHandle(local, out hovered);
            UpdateCursor(isOnViewport, local);

            if (isOnViewport)
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f) { rig.Zoom(wheel); }

                // ◆ の位置の窓を出しているとき: 3Dの画面のクリックは、窓を閉じるだけ (打っている途中の値は、反映してから閉じる)
                if (IsControlPanelShown && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
                {
                    overlay.CommitTyping();
                    CloseControlPanel();
                    waitForMouseRelease = true;
                    lastMouse = mouse;
                    return;
                }

                if (Input.GetMouseButtonDown(0)) { BeginLeft(local, mouse); }
                else if (Input.GetMouseButtonDown(1)) { BeginRight(local, mouse); }
                else if (Input.GetMouseButtonDown(2)) { BeginDrag(DragKind.Pan, 2, mouse); }
            }

            lastMouse = mouse;
        }

        /// <summary>
        /// カーソルの絵を、今の道具とカーソルの下にあるものに合わせる (頂点レイヤーと同じ決まり)
        /// ・編集: 頂点・中心の上は「動かす」、頂点を置ける所は「置く」
        /// ・回転 / 拡大縮小: 頂点の上は「回す」/「拡大縮小」
        /// ・道具なし: 頂点・中心・矢印・◆の上は「動かす」
        /// それ以外は、標準の絵
        /// </summary>
        private void UpdateCursor(bool isOnViewport, Vector2 local)
        {
            EditMode cursor = EditMode.None;
            if (isOnViewport && !ModifierKey.Alt)
            {
                bool isOnVertex = hasHovered && hovered.Kind == HandleKind.Vertex;
                bool isOnHandle = hasHovered && hovered.Kind != HandleKind.None;

                switch (vertexTool)
                {
                    case PreviewVertexTool.Rotate:
                        cursor = isOnVertex ? EditMode.VerticesRotating : EditMode.None;
                        break;
                    case PreviewVertexTool.Scale:
                        cursor = isOnVertex ? EditMode.VerticesScaling : EditMode.None;
                        break;
                    case PreviewVertexTool.Edit:
                        if (isOnHandle) { cursor = EditMode.VertexMoving; }
                        else if (!ModifierKey.Ctrl && CanPlaceVertexAt(local)) { cursor = EditMode.VertexEdit; }
                        break;
                    default:
                        if (isOnHandle) { cursor = EditMode.VertexMoving; }
                        break;
                }
            }

            SetCursor(cursor);
        }

        EditMode shownCursor = EditMode.None;
        CursorTextureChanger cursorTextureChanger;
        bool isCursorChangerSearched;

        private void SetCursor(EditMode cursor)
        {
            if (cursor == shownCursor) { return; }
            shownCursor = cursor;

            if (!isCursorChangerSearched)
            {
                isCursorChangerSearched = true;
                cursorTextureChanger = Object.FindObjectOfType<CursorTextureChanger>(true);
            }
            if (cursorTextureChanger != null) { cursorTextureChanger.SetPreviewCursor(cursor); }
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
                        // 頂点の道具「回転」「拡大縮小」: 頂点をドラッグして、回す / 拡大・縮小する
                        if ((vertexTool == PreviewVertexTool.Rotate || vertexTool == PreviewVertexTool.Scale) && IsSingleSectionSelected && hovered.Section == selectedIndex)
                        {
                            StartTransformDrag(hovered.Section, hovered.Vertex, vertexTool == PreviewVertexTool.Rotate, local, mouse);
                            return;
                        }
                        StartVertexDrag(hovered.Section, hovered.Vertex, DragKind.Vertex, local, mouse);
                        return;
                    case HandleKind.Center:
                        // Ctrl+クリック: その節を、選択に加える・外す
                        if (ModifierKey.Ctrl)
                        {
                            ToggleSectionSelection(hovered.Section);
                            waitForMouseRelease = true;
                            return;
                        }
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
                    {
                        // ダブルクリック: 位置を数値で決める窓を出す
                        bool isDoubleClick = hovered.Span == lastControlClickSpan &&
                                             Time.unscaledTime - lastControlClickTime <= DOUBLE_CLICK_SECONDS &&
                                             Vector2.Distance(mouse, lastControlClickPosition) <= DOUBLE_CLICK_PIXELS;
                        if (isDoubleClick)
                        {
                            lastControlClickSpan = -1;
                            OpenControlPanel(hovered.Span);
                            waitForMouseRelease = true;
                            return;
                        }

                        lastControlClickSpan = hovered.Span;
                        lastControlClickTime = Time.unscaledTime;
                        lastControlClickPosition = mouse;
                        StartControlDrag(hovered.Span, local, mouse);
                        return;
                    }
                }
            }

            bool isRingPicked = TryPickRing(local, out int ringSection);

            // 頂点の道具「編集」: 選んでいる節の面の上を押すと、そこに頂点を置く (ほかの節の線を押したときは、その節を選ぶ)
            if (vertexTool == PreviewVertexTool.Edit && IsSingleSectionSelected && !ModifierKey.Ctrl && (!isRingPicked || ringSection == selectedIndex))
            {
                if (TryPlaceVertex(local, mouse)) { return; }
            }

            // 節の形の線: その節を選ぶ (Ctrl を押していれば、選択に加える・外す)
            // ダブルクリック: その節を、頂点レイヤーで編集する (録音 20261117_032001)
            if (isRingPicked)
            {
                bool isDoubleClick = !ModifierKey.Ctrl && lastRingClickNote != null && ReferenceEquals(sections[ringSection].Note, lastRingClickNote) &&
                                     Time.unscaledTime - lastRingClickTime <= DOUBLE_CLICK_SECONDS &&
                                     Vector2.Distance(mouse, lastRingClickPosition) <= DOUBLE_CLICK_PIXELS;
                if (isDoubleClick)
                {
                    lastRingClickNote = null;
                    waitForMouseRelease = true;
                    OpenSectionInVerticesLayer(sections[ringSection].Note);
                    return;
                }

                lastRingClickNote = sections[ringSection].Note;
                lastRingClickTime = Time.unscaledTime;
                lastRingClickPosition = mouse;

                if (ModifierKey.Ctrl) { ToggleSectionSelection(ringSection); }
                else { Select(ringSection); }

                waitForMouseRelease = true;
                return;
            }

            BeginDrag(DragKind.EmptyPress, 0, mouse);
        }

        private void BeginRight(Vector2 local, Vector2 mouse)
        {
            hasRightPressHandle = hasHovered && (hovered.Kind == HandleKind.Vertex || hovered.Kind == HandleKind.Control || hovered.Kind == HandleKind.Center);
            rightPressHandle = hovered;
            // つまみの上でなければ: 節の線の上ならその節、節を1つ選んでいれば、その節のメニュー (録音 20261117_023636)
            // (番号ではなく、節そのものを覚える: 離すまでの間に、元に戻すなどで番号がずれることがある)
            rightPressNote = hasRightPressHandle ? GetHandleNote(hovered) : null;
            rightPressVertex = hasRightPressHandle && hovered.Kind == HandleKind.Vertex && rightPressNote != null &&
                               hovered.Vertex >= 0 && hovered.Vertex < rightPressNote.SpaceVertices.Vertices.Count
                ? rightPressNote.SpaceVertices.Vertices[hovered.Vertex] : null;
            if (!hasRightPressHandle)
            {
                if (TryPickRing(local, out int ringSection)) { rightPressNote = sections[ringSection].Note; }
                else if (IsSingleSectionSelected) { rightPressNote = selectedNote; }
            }
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
                // (つまみは、少し動かすまでは動かさない。クリックしただけで、手のぶれのぶん動いてしまわないように)
                case DragKind.Vertex:
                case DragKind.Center:
                case DragKind.AxisX:
                case DragKind.AxisY:
                    if (hasDragMoved) { UpdateVertexDrag(local); }
                    break;
                case DragKind.AxisZ:
                    if (hasDragMoved) { UpdateTimeDrag(local); }
                    break;
                case DragKind.Control:
                    if (hasDragMoved) { UpdateControlDrag(local); }
                    break;
                case DragKind.Rotate:
                case DragKind.Scale:
                    if (hasDragMoved) { UpdateTransformDrag(local); }
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
                        if (!hasDragMoved && hasRightPressHandle)
                        {
                            // 押したときと同じ節・頂点のときだけ (押してから離すまでの間に、変わっていたら出さない)
                            bool isSame = rightPressNote != null && ReferenceEquals(GetHandleNote(rightPressHandle), rightPressNote) &&
                                          (rightPressHandle.Kind != HandleKind.Vertex ||
                                           (rightPressHandle.Vertex >= 0 && rightPressHandle.Vertex < rightPressNote.SpaceVertices.Vertices.Count &&
                                            ReferenceEquals(rightPressNote.SpaceVertices.Vertices[rightPressHandle.Vertex], rightPressVertex)));
                            if (isSame) { ShowHandleMenu(rightPressHandle); }
                        }
                        else if (!hasDragMoved && rightPressNote != null)
                        {
                            int index = chain.IndexOf(rightPressNote);
                            if (index >= 0) { ShowSectionMenu(index); }
                        }
                        break;
                    case DragKind.Vertex:
                    case DragKind.Center:
                    case DragKind.AxisX:
                    case DragKind.AxisY:
                    case DragKind.Rotate:
                    case DragKind.Scale:
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
                dragGroup.Clear();
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
            // 置いたばかりの頂点は、置いた操作として登録しておく (履歴に無い頂点が残らないように)
            RecordPlacedVertex();

            dragKind = DragKind.None;
            dragGroup.Clear();
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
            // ロックした節: 選ぶだけで、動かさない
            if (IsLockedSection(sectionIndex))
            {
                if (kind != DragKind.Center) { ShowLockedStatus(); }
                waitForMouseRelease = true;
                return;
            }

            SpaceHoldSection section = sections[sectionIndex];
            dragNote = section.Note;
            dragVertexIndex = vertexIndex;

            dragVertices.Clear();
            dragStartPositions.Clear();
            dragGroup.Clear();
            foreach (var vertex in section.Note.SpaceVertices.Vertices)
            {
                dragVertices.Add(vertex);
                dragStartPositions.Add(vertex.Position.Value);
            }
            if (vertexIndex >= dragVertices.Count) { return; }

            // 選んでいる頂点 (2つ以上) のどれかを掴んだときは、選んでいる頂点をまとめて動かす
            if (kind == DragKind.Vertex && vertexIndex >= 0 && selectedVertices.Count >= 2 && IsVertexSelected(dragVertices[vertexIndex]))
            {
                for (int i = 0; i < dragVertices.Count; i++)
                {
                    if (IsVertexSelected(dragVertices[i])) { dragGroup.Add(i); }
                }
            }

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
                if (dragGroup.Count > 0)
                {
                    foreach (int i in dragGroup) { dragVertices[i].SetPosition(dragStartPositions[i] + deltaUnit); }
                }
                else if (dragVertexIndex >= 0 && dragVertexIndex < dragVertices.Count)
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
            // 頂点の道具「編集」で置いた頂点: 置いた操作として1回ぶん登録する (動かした分は、置いた位置に含める)
            if (placedVertex != null)
            {
                RecordPlacedVertex();
                ShowStatus("頂点を置きました");
                return;
            }

            // 頂点を、動かさずにクリックしたとき: その頂点を選ぶ
            if (kind == DragKind.Vertex && !hasDragMoved)
            {
                ClickVertex(chain.IndexOf(dragNote), dragVertexIndex);
                return;
            }

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
            switch (kind)
            {
                case DragKind.Vertex: ShowStatus(dragGroup.Count > 0 ? $"頂点を {dragGroup.Count} 個動かしました" : "頂点を動かしました"); break;
                case DragKind.Rotate: ShowStatus("頂点を回しました"); break;
                case DragKind.Scale: ShowStatus("頂点を拡大・縮小しました"); break;
                default: ShowStatus("節を動かしました"); break;
            }
        }

        /// <summary>
        /// 頂点の道具「回転」「拡大縮小」: 頂点を掴んで、選んでいる頂点 (2つ以上選んでいなければ、すべての頂点) を、その中心のまわりに回す / 拡大・縮小する
        /// (頂点レイヤーの「回転」「拡大縮小」と同じ計算。動かすのは、節の面の中)
        /// </summary>
        private void StartTransformDrag(int sectionIndex, int vertexIndex, bool isRotate, Vector2 local, Vector2 mouse)
        {
            if (sectionIndex < 0 || sectionIndex >= sections.Count) { return; }
            if (IsLockedSection(sectionIndex))
            {
                ShowLockedStatus();
                waitForMouseRelease = true;
                return;
            }

            SpaceHoldSection section = sections[sectionIndex];
            var vertices = section.Note.SpaceVertices.Vertices;
            if (vertexIndex < 0 || vertexIndex >= vertices.Count) { return; }

            dragNote = section.Note;
            dragVertexIndex = vertexIndex;
            dragVertices.Clear();
            dragStartPositions.Clear();
            dragGroup.Clear();

            bool usesSelection = selectedVertices.Count >= 2;
            Vector2 sum = Vector2.zero;
            foreach (var vertex in vertices)
            {
                if (usesSelection && !IsVertexSelected(vertex)) { continue; }
                dragVertices.Add(vertex);
                dragStartPositions.Add(vertex.Position.Value);
                sum += vertex.Position.Value;
            }
            if (dragVertices.Count == 0) { return; }

            transformCenter = sum / dragVertices.Count;
            transformGrabPoint = vertices[vertexIndex].Position.Value;

            // 節の面の中で動かす (面をほとんど真横から見ているときは、画面に平行な面の中で)
            Vector3 grab = ToWorld(section.Vertices[vertexIndex], section.Depth);
            Ray ray = view.GetRay(local);
            dragPlaneZ = grab.z;
            dragUseViewPlane = Mathf.Abs(ray.direction.z) < GRAZING_DIRECTION_Z;
            dragPlanePoint = grab;
            dragPlaneNormal = rig.Forward;
            if (!TryHitDragPlane(ray, out dragStartHit)) { dragStartHit = grab; }

            BeginDrag(isRotate ? DragKind.Rotate : DragKind.Scale, 0, mouse);
        }

        private void UpdateTransformDrag(Vector2 local)
        {
            if (dragNote == null || dragVertices.Count == 0) { return; }
            if (!TryHitDragPlane(view.GetRay(local), out Vector3 hit)) { return; }

            Vector2 point = transformGrabPoint + (ToUnit(hit) - ToUnit(dragStartHit));
            if (dragKind == DragKind.Rotate)
            {
                float angle = Vector2Extensions.AngleBetweenVectors(transformGrabPoint, point, transformCenter);
                for (int i = 0; i < dragVertices.Count; i++) { dragVertices[i].SetPosition(dragStartPositions[i].RotatePoint(transformCenter, angle)); }
            }
            else
            {
                float baseDistance = (transformGrabPoint - transformCenter).magnitude;
                // 中心のすぐそばを掴んだときは、割合を決められないので、動かさない
                if (baseDistance < 0.0001f) { return; }

                float magnitude = (point - transformCenter).magnitude / baseDistance;
                for (int i = 0; i < dragVertices.Count; i++) { dragVertices[i].SetPosition(dragStartPositions[i].ScalePoint(transformCenter, magnitude)); }
            }
        }

        /// <summary>
        /// 頂点の道具「編集」: 選んでいる節の面の、押した位置に頂点を置いて、そのまま掴む
        /// </summary>
        /// <returns>置いたとき true</returns>
        private bool TryPlaceVertex(Vector2 local, Vector2 mouse)
        {
            if (!TryGetSelected(out SpaceHoldSection section)) { return false; }
            // ロックした節には、頂点を置かない
            if (NoteLock.IsLocked(section.Note)) { return false; }

            Ray ray = view.GetRay(local);
            Vector3 center = ToWorld(section.Center, section.Depth);
            dragPlaneZ = center.z;
            dragUseViewPlane = Mathf.Abs(ray.direction.z) < GRAZING_DIRECTION_Z;
            dragPlanePoint = center;
            dragPlaneNormal = rig.Forward;
            if (!TryHitDragPlane(ray, out Vector3 hit)) { return false; }

            // ノーツの面の外や、ほかの頂点の上には置かない (面の外を押したときは、何も無い所を押したのと同じにする)
            SpaceVertices target = section.Note.SpaceVertices;
            if (!PreviewVertexTools.CanPlaceAt(target, ToUnit(hit))) { return false; }

            // 頂点を置くと、頂点の選択は外れる (頂点レイヤーと同じ)
            ClearVertexSelection();

            SpaceVertices vertices = section.Note.SpaceVertices;
            VertexData added = PreviewVertexTools.AddVertexWithoutRecord(vertices, ToUnit(hit));
            placedVertices = vertices;
            placedVertex = added;

            // 頂点が増えたので、節を作り直してから掴む
            RefreshChain();
            if (selectedIndex < 0 || added.VertexIndex < 0)
            {
                RecordPlacedVertex();
                return true;
            }

            StartVertexDrag(selectedIndex, added.VertexIndex, DragKind.Vertex, local, mouse);
            if (dragKind != DragKind.Vertex) { RecordPlacedVertex(); }
            return true;
        }

        /// <summary>
        /// 頂点の道具「編集」で、その位置を押したら頂点を置くか (カーソルの絵を決めるのに使う。TryPlaceVertex と同じ決め方)
        /// </summary>
        private bool CanPlaceVertexAt(Vector2 local)
        {
            if (!TryGetSelected(out SpaceHoldSection section)) { return false; }
            if (NoteLock.IsLocked(section.Note)) { return false; }

            Ray ray = view.GetRay(local);
            Vector3 center = ToWorld(section.Center, section.Depth);
            bool useViewPlane = Mathf.Abs(ray.direction.z) < GRAZING_DIRECTION_Z;
            var plane = useViewPlane ? new Plane(rig.Forward, center) : new Plane(Vector3.forward, new Vector3(0f, 0f, center.z));
            if (!plane.Raycast(ray, out float enter)) { return false; }

            return PreviewVertexTools.CanPlaceAt(section.Note.SpaceVertices, ToUnit(ray.GetPoint(enter)));
        }

        /// <summary>
        /// 置いた頂点を、置いた操作として履歴に登録する (置いていなければ、何もしない)
        /// </summary>
        private void RecordPlacedVertex()
        {
            if (placedVertex == null) { return; }

            PreviewVertexTools.RecordAddedVertex(placedVertices, placedVertex);
            placedVertices = null;
            placedVertex = null;
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
            if (IsLockedSection(sectionIndex))
            {
                ShowLockedStatus();
                waitForMouseRelease = true;
                return;
            }

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
