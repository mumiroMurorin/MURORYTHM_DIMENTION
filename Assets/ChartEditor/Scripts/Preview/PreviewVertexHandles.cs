using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// プレビューの上に、選択中のスペースのノーツの「頂点」と「中心点」のつまみを表示し、ドラッグで動かせるようにする
    ///
    /// ・頂点のつまみを動かすと、その頂点だけが動く (形が変わる)
    /// ・中心点のつまみを動かすと、すべての頂点が一緒に動く (形はそのままで位置が変わる)
    ///   複数のノーツを選択しているときは、選択中のノーツがすべて一緒に動く
    ///
    /// ・全画面プレビューの頂点の道具 (PreviewVertexTools) で「回転」「拡大縮小」を選んでいるときは、頂点のつまみを動かすと、
    ///   そのノーツの頂点すべてが、頂点の中心のまわりに回る / 中心から拡大・縮小する
    ///
    /// ・全画面プレビューで、頂点を持つノーツを1つだけ選んでいる間は、そのノーツの頂点を選べる (選んだ頂点のつまみは、橙色)
    ///   選択中の頂点を掴むと、選択中の頂点がまとめて動く。回転・拡大縮小も、2つ以上選んでいれば、選択中の頂点だけに行う
    ///   また、つまみの横に、頂点の番号を出す (頂点スライドで、どの頂点が何番かを見るため)
    ///
    /// つまみはプレビューの世界ではなく、画面に重ねた板として描く。
    /// (奥のノーツでも同じ大きさで見え、ノーツの面に埋もれず、プレビューのカメラの設定にも左右されない)
    /// 押された・動かされたの判断は PreviewNotePicker が行い、ここは「つまみの表示」と「頂点の計算」を受け持つ。
    /// </summary>
    public class PreviewVertexHandles
    {
        /// <summary>
        /// ノーツの、プレビュー上の距離(譜面上の距離)を求める処理
        /// </summary>
        public delegate bool NoteDistanceGetter(IDeployableNoteData note, out float distance);

        // つまみを出すノーツの数の上限 (ホールドをまるごと選んだときに、画面がつまみで埋まらないように)
        const int MAX_NOTE_COUNT = 16;
        // ほかの画面部品よりも手前に出す (読み込み中の覆いよりは奥)
        const int SORTING_ORDER = 20000;
        // つまみの大きさと、押したとみなす距離 (画面の高さ 1080 のときのピクセル数)
        const float HANDLE_SIZE = 13f;
        const float HANDLE_SIZE_MINI = 8f;
        const float PICK_RADIUS = 15f;
        const float PICK_RADIUS_MINI = 9f;
        const float REFERENCE_SCREEN_HEIGHT = 1080f;
        // 中心点は、頂点よりこの距離(ピクセル)だけ選ばれにくくする (重なったときは頂点を優先)
        const float CENTER_PICK_PENALTY = 3f;

        static readonly Color vertexColor = new Color(1f, 1f, 1f, 0.95f);
        static readonly Color centerColor = new Color(1f, 0.82f, 0.1f, 0.95f);
        static readonly Color draggingColor = new Color(0.2f, 0.9f, 1f, 1f);
        static readonly Color selectedColor = new Color(1f, 0.55f, 0.1f, 1f);
        static readonly Color outlineColor = new Color(0f, 0f, 0f, 0.9f);

        // 頂点の番号の札 (画面の高さ 1080 のときのピクセル数)
        const float INDEX_LABEL_HEIGHT = 15f;
        const float INDEX_LABEL_WIDTH = 15f;
        const float INDEX_LABEL_WIDTH_WIDE = 22f;
        const float INDEX_FONT_SIZE = 11f;
        static readonly Color indexLabelColor = new Color(0f, 0f, 0f, 0.65f);
        // 番号の文字 (毎フレーム文字列を作らないように、先に作っておく)
        static readonly string[] indexTexts = CreateIndexTexts(64);

        private static string[] CreateIndexTexts(int count)
        {
            var texts = new string[count];
            for (int i = 0; i < count; i++) { texts[i] = i.ToString(); }
            return texts;
        }

        readonly INotesDataGetter notesGetter;
        readonly INoteSpawnDataOptionGetter optionHolder;

        // 今出ているつまみ
        readonly List<Handle> handles = new List<Handle>();
        // つまみを描く板 (足りなくなったら増やし、余ったら隠す)
        GameObject canvasObject;
        readonly List<Image> handleImages = new List<Image>();
        // 頂点の番号の札 (足りなくなったら増やし、余ったら隠す)
        readonly List<IndexLabel> indexLabels = new List<IndexLabel>();

        // --- 選択中の頂点 ---
        // 頂点を選べるノーツの、頂点の並び (全画面プレビューで、頂点を持つノーツを1つだけ選んでいる間だけ。無いときは null)
        SpaceVertices selectionOwner;
        readonly List<VertexData> selectedVertices = new List<VertexData>();
        readonly List<VertexData> rectVertices = new List<VertexData>();

        // 最後に掴んだつまみ (頂点。中心点のつまみなら null) と、その頂点の並び
        VertexData grabbedVertex;
        SpaceVertices grabbedVertices;

        private class IndexLabel
        {
            public RectTransform Root;
            public TextMeshProUGUI Text;
            public int ShownIndex = -1;
        }

        // --- 掴んでいるつまみ ---
        bool isGrabbing;
        bool isGrabbingCenter;
        float grabDistance;
        // 掴んだ位置 (頂点と同じ、単位円の中の座標)
        Vector2 grabPoint;
        // 掴んだつまみで、何をするか
        GrabKind grabKind;
        // 回す・拡大縮小するときの中心 (掴んだときの、そのノーツの頂点の平均の位置)
        Vector2 grabCenter;

        private enum GrabKind
        {
            // 掴んだ頂点 (中心点なら、すべての頂点) を、カーソルの動きのぶんだけ動かす
            Move,
            // すべての頂点を、中心のまわりに回す
            Rotate,
            // すべての頂点を、中心から拡大・縮小する
            Scale,
        }
        // 動かす頂点と、掴んだときの座標
        readonly List<DragTarget> dragTargets = new List<DragTarget>();

        private struct Handle
        {
            public IDeployableNoteData Note;
            public SpaceVertices Vertices;
            // 頂点。中心点のつまみでは null
            public VertexData Vertex;
            // ノーツのある断面の位置 (譜面上の距離)
            public float Distance;
            public Vector2 ScreenPosition;
        }

        private struct DragTarget
        {
            public VertexData Vertex;
            public Vector2 OriginPosition;
        }

        public PreviewVertexHandles(INotesDataGetter notesGetter, INoteSpawnDataOptionGetter optionHolder)
        {
            this.notesGetter = notesGetter;
            this.optionHolder = optionHolder;
        }

        /// <summary>
        /// つまみを掴んでいる最中か
        /// </summary>
        public bool IsGrabbing => isGrabbing;

        #region 頂点の選択

        /// <summary>選択中の頂点の数</summary>
        public int SelectedCount => selectedVertices.Count;

        /// <summary>最後に掴んだつまみの頂点 (中心点のつまみなら null)</summary>
        public VertexData GrabbedVertex => grabbedVertex;

        /// <summary>最後に掴んだつまみの、頂点の並び</summary>
        public SpaceVertices GrabbedVertices => grabbedVertices;

        /// <summary>
        /// 頂点を選べるノーツを決める (その頂点の並び。選べるノーツが無いときは null)
        /// 別のノーツに替わったら、選択は空になる。同じノーツのままなら、消された頂点だけを選択から外す
        /// </summary>
        public void SetSelectionOwner(SpaceVertices owner)
        {
            // Delete キーの処理 (NoteDestroyer) から、選択中の頂点が見えるようにする
            PreviewVertexTools.ShareSelection(owner, selectedVertices);

            if (!ReferenceEquals(owner, selectionOwner))
            {
                selectionOwner = owner;
                selectedVertices.Clear();
                return;
            }

            for (int i = selectedVertices.Count - 1; i >= 0; i--)
            {
                if (!Contains(owner, selectedVertices[i])) { selectedVertices.RemoveAt(i); }
            }
        }

        public bool IsSelected(VertexData vertex)
        {
            foreach (var selected in selectedVertices)
            {
                if (ReferenceEquals(selected, vertex)) { return true; }
            }
            return false;
        }

        public void ClearSelection()
        {
            selectedVertices.Clear();
        }

        /// <summary>
        /// その頂点だけを選ぶ (選べるノーツの頂点でなければ、何もしない)
        /// </summary>
        public void SelectSingle(SpaceVertices owner, VertexData vertex)
        {
            if (!CanSelect(owner, vertex)) { return; }

            selectedVertices.Clear();
            selectedVertices.Add(vertex);
        }

        /// <summary>
        /// その頂点を、選択に加える。もう選ばれていれば、選択から外す
        /// </summary>
        public void ToggleSelection(SpaceVertices owner, VertexData vertex)
        {
            if (!CanSelect(owner, vertex)) { return; }

            for (int i = 0; i < selectedVertices.Count; i++)
            {
                if (ReferenceEquals(selectedVertices[i], vertex))
                {
                    selectedVertices.RemoveAt(i);
                    return;
                }
            }
            selectedVertices.Add(vertex);
        }

        /// <summary>
        /// 画面上の枠の中にある、選べるノーツの頂点を選ぶ
        /// </summary>
        /// <param name="isToggle">true のとき、今の選択に加える (もう選ばれているものは外す)。false のとき、枠の中のものだけを選ぶ</param>
        /// <returns>枠の中に頂点があったとき true (1つも無ければ、選択は変えずに false)</returns>
        public bool SelectInRect(Rect screenRect, bool isToggle)
        {
            if (selectionOwner == null) { return false; }

            rectVertices.Clear();
            foreach (var handle in handles)
            {
                if (handle.Vertex == null || !ReferenceEquals(handle.Vertices, selectionOwner)) { continue; }
                if (screenRect.Contains(handle.ScreenPosition)) { rectVertices.Add(handle.Vertex); }
            }
            if (rectVertices.Count == 0) { return false; }

            if (!isToggle) { selectedVertices.Clear(); }
            foreach (var vertex in rectVertices)
            {
                if (isToggle) { ToggleSelection(selectionOwner, vertex); }
                else { selectedVertices.Add(vertex); }
            }

            rectVertices.Clear();
            return true;
        }

        private bool CanSelect(SpaceVertices owner, VertexData vertex)
        {
            return owner != null && ReferenceEquals(owner, selectionOwner) && Contains(owner, vertex);
        }

        /// <summary>
        /// その頂点が、今もその並びの中にあるか
        /// </summary>
        private static bool Contains(SpaceVertices vertices, VertexData vertex)
        {
            if (vertices == null || vertex == null) { return false; }

            int index = vertex.VertexIndex;
            return index >= 0 && index < vertices.Vertices.Count && ReferenceEquals(vertices.Vertices[index], vertex);
        }

        #endregion

        #region 表示

        /// <summary>
        /// つまみの位置を、今の頂点の位置に合わせる (毎フレーム呼ぶ)
        /// </summary>
        /// <param name="view">つまみを出すプレビュー。null のときは隠す</param>
        /// <param name="noteParent">プレビューのノーツの親 (ノーツと一緒に動く地面)</param>
        /// <param name="getDistance">ノーツの、プレビュー上の距離を求める処理 (つまみを出さないノーツでは false を返す)</param>
        /// <param name="includesEditingNote">頂点レイヤーで編集中のノーツにも、つまみを出すか</param>
        public void Refresh(PreviewView view, Transform noteParent, NoteDistanceGetter getDistance, bool includesEditingNote)
        {
            handles.Clear();

            if (view != null && view.IsActive && noteParent != null && getDistance != null)
            {
                CollectHandles(view, noteParent, getDistance, includesEditingNote);
            }

            // 頂点を選べるノーツが、画面に映っていないとき (つまみが出ていないとき) は、選択を外す
            // (見えていない頂点を、Delete で消したり、まとめて動かしたりしないように)
            if (selectedVertices.Count > 0 && !HasVertexHandleOf(selectionOwner)) { selectedVertices.Clear(); }

            Draw(view);
        }

        /// <summary>
        /// そのノーツの頂点のつまみが、1つでも出ているか
        /// </summary>
        private bool HasVertexHandleOf(SpaceVertices vertices)
        {
            if (vertices == null) { return false; }

            foreach (var handle in handles)
            {
                if (handle.Vertex != null && ReferenceEquals(handle.Vertices, vertices)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// つまみをすべて隠す
        /// </summary>
        public void Hide()
        {
            handles.Clear();
            Draw(null);
        }

        /// <summary>
        /// 選択中のスペースのノーツ(と、頂点を編集中のノーツ)について、画面に映っている頂点と中心点を集める
        /// </summary>
        private void CollectHandles(PreviewView view, Transform noteParent, NoteDistanceGetter getDistance, bool includesEditingNote)
        {
            float curveRadius = optionHolder.NoteCurveRadius.Value;
            int noteCount = 0;

            foreach (var note in notesGetter.SelectingNotes)
            {
                if (noteCount >= MAX_NOTE_COUNT) { break; }
                // ロックしたノーツの形は、つまみを出さない (動かせないように)
                if (NoteLock.IsLocked(note)) { continue; }
                if (AddNoteHandles(note, view, noteParent, curveRadius, getDistance)) { noteCount++; }
            }

            // 頂点レイヤーで編集中のノーツ (選択とは別に覚えられている)
            // 頂点レイヤーを出た後も「編集中のノーツ」は覚えられたままなので、頂点レイヤーを開いている間だけ対象にする
            if (includesEditingNote &&
                notesGetter.EditingVertices.Value is IDeployableNoteData editingNote &&
                noteCount < MAX_NOTE_COUNT &&
                !IsSelecting(editingNote))
            {
                AddNoteHandles(editingNote, view, noteParent, curveRadius, getDistance);
            }
        }

        private bool IsSelecting(IDeployableNoteData note)
        {
            foreach (var selecting in notesGetter.SelectingNotes)
            {
                if (ReferenceEquals(selecting, note)) { return true; }
            }

            return false;
        }

        /// <summary>
        /// 1つのノーツの頂点と中心点のつまみを加える
        /// </summary>
        /// <returns>頂点を持つノーツだったとき true</returns>
        private bool AddNoteHandles(IDeployableNoteData note, PreviewView view, Transform noteParent, float curveRadius, NoteDistanceGetter getDistance)
        {
            if (note is not IVerticesControlableNoteData verticesNote) { return false; }

            var vertices = verticesNote.SpaceVertices;
            if (vertices == null || vertices.Vertices.Count == 0) { return false; }
            if (!getDistance(note, out float distance)) { return false; }

            Vector2 sum = Vector2.zero;
            foreach (var vertex in vertices.Vertices)
            {
                Vector2 position = vertex.Position.Value;
                sum += position;
                AddHandle(note, vertices, vertex, position, distance, view, noteParent, curveRadius);
            }

            // 中心点 (頂点の平均の位置)
            AddHandle(note, vertices, null, sum / vertices.Vertices.Count, distance, view, noteParent, curveRadius);
            return true;
        }

        private void AddHandle(
            IDeployableNoteData note,
            SpaceVertices vertices,
            VertexData vertex,
            Vector2 position,
            float distance,
            PreviewView view,
            Transform noteParent,
            float curveRadius)
        {
            // 頂点の座標(単位円の中) → 断面の中での座標 → プレビュー世界の位置 → 画面上の位置
            Vector3 localPoint = PreviewTrackSurface.GetPoint(
                distance,
                position.x * PreviewTrackSurface.VERTEX_SCALE,
                position.y * PreviewTrackSurface.VERTEX_SCALE,
                curveRadius);

            // 画面に映っていない頂点には、つまみを出さない
            if (!view.TryGetScreenPoint(noteParent.TransformPoint(localPoint), out Vector2 screenPosition)) { return; }

            handles.Add(new Handle
            {
                Note = note,
                Vertices = vertices,
                Vertex = vertex,
                Distance = distance,
                ScreenPosition = screenPosition,
            });
        }

        /// <summary>
        /// 集めたつまみを画面に描く
        /// </summary>
        private void Draw(PreviewView view)
        {
            if (handles.Count == 0)
            {
                if (canvasObject != null && canvasObject.activeSelf) { canvasObject.SetActive(false); }
                return;
            }

            EnsureCanvas();
            if (!canvasObject.activeSelf) { canvasObject.SetActive(true); }

            float scale = Screen.height / REFERENCE_SCREEN_HEIGHT;
            float size = (view != null && !view.IsFullScreen ? HANDLE_SIZE_MINI : HANDLE_SIZE) * scale;

            for (int i = 0; i < handles.Count; i++)
            {
                if (i >= handleImages.Count) { handleImages.Add(CreateHandleImage()); }

                var handle = handles[i];
                var image = handleImages[i];
                bool isCenter = handle.Vertex == null;

                var rectTransform = image.rectTransform;
                rectTransform.position = new Vector3(handle.ScreenPosition.x, handle.ScreenPosition.y, 0f);
                // 中心点は少し大きく、45度回したひし形にして見分けやすくする
                rectTransform.sizeDelta = Vector2.one * (isCenter ? size * 1.15f : size);
                rectTransform.localEulerAngles = new Vector3(0f, 0f, isCenter ? 45f : 0f);

                image.color = IsDragging(handle)
                    ? draggingColor
                    : (isCenter ? centerColor : (IsSelected(handle.Vertex) ? selectedColor : vertexColor));
                if (!image.gameObject.activeSelf) { image.gameObject.SetActive(true); }
            }

            for (int i = handles.Count; i < handleImages.Count; i++)
            {
                if (handleImages[i].gameObject.activeSelf) { handleImages[i].gameObject.SetActive(false); }
            }

            DrawIndexLabels(view, size, scale);
        }

        /// <summary>
        /// 頂点のつまみの横に、頂点の番号を出す
        /// 全画面プレビューで、つまみが1つのノーツのものだけのとき (頂点を編集しているとき) に出す
        /// (ホールドをまるごと選んだときなどは、番号だらけになるので出さない。ミニプレビューは小さいので出さない)
        /// </summary>
        private void DrawIndexLabels(PreviewView view, float handleSize, float scale)
        {
            int count = 0;

            if (view != null && view.IsFullScreen && IsSingleNote())
            {
                foreach (var handle in handles)
                {
                    if (handle.Vertex == null) { continue; }

                    int index = handle.Vertex.VertexIndex;
                    if (index < 0) { continue; }

                    if (count >= indexLabels.Count) { indexLabels.Add(CreateIndexLabel()); }
                    var label = indexLabels[count++];

                    // つまみの右上
                    bool isWide = index >= 10;
                    label.Root.position = new Vector3(
                        handle.ScreenPosition.x + handleSize * 0.5f + 2f * scale,
                        handle.ScreenPosition.y + handleSize * 0.5f + 2f * scale,
                        0f);
                    label.Root.sizeDelta = new Vector2((isWide ? INDEX_LABEL_WIDTH_WIDE : INDEX_LABEL_WIDTH) * scale, INDEX_LABEL_HEIGHT * scale);
                    label.Text.fontSize = INDEX_FONT_SIZE * scale;

                    if (label.ShownIndex != index)
                    {
                        label.ShownIndex = index;
                        label.Text.text = index < indexTexts.Length ? indexTexts[index] : index.ToString();
                    }
                    if (!label.Root.gameObject.activeSelf) { label.Root.gameObject.SetActive(true); }
                }
            }

            for (int i = count; i < indexLabels.Count; i++)
            {
                if (indexLabels[i].Root.gameObject.activeSelf) { indexLabels[i].Root.gameObject.SetActive(false); }
            }
        }

        /// <summary>
        /// 出ているつまみが、すべて同じノーツのものか
        /// </summary>
        private bool IsSingleNote()
        {
            if (handles.Count == 0) { return false; }

            SpaceVertices first = handles[0].Vertices;
            foreach (var handle in handles)
            {
                if (!ReferenceEquals(handle.Vertices, first)) { return false; }
            }
            return true;
        }

        private IndexLabel CreateIndexLabel()
        {
            var labelObject = new GameObject("Index", typeof(RectTransform));
            labelObject.transform.SetParent(canvasObject.transform, false);

            var root = (RectTransform)labelObject.transform;
            // 左下の角を、つまみの右上に合わせる
            root.pivot = Vector2.zero;

            // 明るい背景でも暗い背景でも読めるように、暗い札の上に白い文字で書く
            var background = labelObject.AddComponent<Image>();
            background.color = indexLabelColor;
            background.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(labelObject.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.color = Color.white;

            return new IndexLabel { Root = root, Text = text };
        }

        /// <summary>
        /// 今ドラッグで動かしているつまみか
        /// </summary>
        private bool IsDragging(Handle handle)
        {
            if (!isGrabbing || dragTargets.Count == 0) { return false; }

            if (handle.Vertex == null)
            {
                // 中心点: 中心点を掴んで、このノーツを動かしているとき
                // (複数のノーツをまとめて動かしているときは、動いているノーツすべての中心点が当てはまる)
                return isGrabbingCenter && handle.Vertices.Vertices.Count > 0 && IsDragTarget(handle.Vertices.Vertices[0]);
            }

            return IsDragTarget(handle.Vertex);
        }

        /// <summary>
        /// 今ドラッグで動かしている頂点か
        /// </summary>
        private bool IsDragTarget(VertexData vertex)
        {
            foreach (var target in dragTargets)
            {
                if (ReferenceEquals(target.Vertex, vertex)) { return true; }
            }

            return false;
        }

        /// <summary>
        /// つまみを描くための、画面に重ねる土台を作る (最初の1回だけ)
        /// </summary>
        private void EnsureCanvas()
        {
            if (canvasObject != null) { return; }

            canvasObject = new GameObject("PreviewVertexHandles");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;
            // クリックを受け取る部品(GraphicRaycaster)は付けない。つまみは、下にあるボタンなどへのクリックを遮らない
        }

        private Image CreateHandleImage()
        {
            var handleObject = new GameObject("Handle", typeof(RectTransform));
            handleObject.transform.SetParent(canvasObject.transform, false);

            var image = handleObject.AddComponent<Image>();
            image.raycastTarget = false;

            // 明るい背景でも暗い背景でも見えるように、黒いふちを付ける
            var outline = handleObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return image;
        }

        /// <summary>
        /// 後片付け
        /// </summary>
        public void Dispose()
        {
            if (canvasObject != null) { Object.Destroy(canvasObject); }

            canvasObject = null;
            handleImages.Clear();
            indexLabels.Clear();
            handles.Clear();
            selectedVertices.Clear();
            selectionOwner = null;
            PreviewVertexTools.ShareSelection(null, null);
        }

        #endregion

        #region 掴む・動かす・離す

        /// <summary>
        /// 画面上の位置につまみがあれば、それを掴む
        /// </summary>
        /// <param name="ray">その位置を通る、プレビュー世界の光線</param>
        /// <returns>掴めたとき true</returns>
        public bool TryGrab(PreviewView view, Vector2 screenPosition, Ray ray, Transform noteParent)
        {
            isGrabbing = false;
            grabKind = GrabKind.Move;
            dragTargets.Clear();

            if (noteParent == null) { return false; }

            // 押した位置にいちばん近いつまみ
            int nearestIndex = FindNearestHandleIndex(view, screenPosition);
            if (nearestIndex < 0) { return false; }

            var handle = handles[nearestIndex];
            grabbedVertex = handle.Vertex;
            grabbedVertices = handle.Vertices;

            // 押した位置が、ノーツのある断面のどこに当たるか (動かした量を測る基準にする)
            if (!TryGetVertexPoint(ray, noteParent, handle.Distance, out grabPoint)) { return false; }

            isGrabbingCenter = handle.Vertex == null;
            grabDistance = handle.Distance;

            if (isGrabbingCenter)
            {
                // 中心点: そのノーツのすべての頂点を一緒に動かす
                AddDragTargets(handle.Vertices);

                // 複数のノーツを選択しているときは、選択中のノーツをすべて一緒に動かす
                // (つまみが出ていないノーツも含める。画面の外にあるものや、つまみを出す数の上限を超えた分)
                if (IsSelecting(handle.Note))
                {
                    foreach (var note in notesGetter.SelectingNotes)
                    {
                        if (ReferenceEquals(note, handle.Note)) { continue; }
                        if (NoteLock.IsLocked(note)) { continue; }
                        if (note is not IVerticesControlableNoteData verticesNote) { continue; }

                        AddDragTargets(verticesNote.SpaceVertices);
                    }
                }
            }
            else if (selectedVertices.Count >= 2 && ReferenceEquals(handle.Vertices, selectionOwner) && IsSelected(handle.Vertex))
            {
                // 選択中の頂点を掴んだ: 選択中の頂点を、まとめて動かす
                foreach (var selected in selectedVertices)
                {
                    dragTargets.Add(new DragTarget { Vertex = selected, OriginPosition = selected.Position.Value });
                }
            }
            else
            {
                dragTargets.Add(new DragTarget { Vertex = handle.Vertex, OriginPosition = handle.Vertex.Position.Value });
            }

            isGrabbing = dragTargets.Count > 0;
            return isGrabbing;
        }

        /// <summary>
        /// 画面上の位置に、掴めるつまみがあるか (掴みはしない。カーソルの形を変えるための確認用)
        /// </summary>
        public bool IsOverHandle(PreviewView view, Vector2 screenPosition)
        {
            return FindNearestHandleIndex(view, screenPosition) >= 0;
        }

        /// <summary>
        /// 画面上の位置にある、頂点のつまみを探す (中心点のつまみは含めない)
        /// </summary>
        /// <param name="vertices">その頂点を持つノーツの、頂点の並び</param>
        public bool TryFindVertex(PreviewView view, Vector2 screenPosition, out SpaceVertices vertices, out VertexData vertex)
        {
            vertices = null;
            vertex = null;

            int index = FindNearestHandleIndex(view, screenPosition, false);
            if (index < 0) { return false; }

            vertices = handles[index].Vertices;
            vertex = handles[index].Vertex;
            return true;
        }

        /// <summary>
        /// 画面上の位置に頂点のつまみがあれば、それを掴んで、そのノーツの頂点すべてを回す / 拡大・縮小する
        /// (頂点の中心が基準。掴んだ頂点が、カーソルの方へ向くように回る / カーソルまでの距離の割合で大きさが変わる)
        /// </summary>
        /// <param name="target">動かしてよいノーツの頂点の並び (ほかのノーツのつまみは掴まない)</param>
        /// <param name="isRotate">true = 回す、false = 拡大・縮小する</param>
        /// <returns>掴めたとき true</returns>
        public bool TryGrabForTransform(PreviewView view, Vector2 screenPosition, Ray ray, Transform noteParent, SpaceVertices target, bool isRotate)
        {
            isGrabbing = false;
            grabKind = GrabKind.Move;
            dragTargets.Clear();

            if (noteParent == null || target == null) { return false; }

            int index = FindNearestHandleIndex(view, screenPosition, false);
            if (index < 0) { return false; }

            var handle = handles[index];
            if (!ReferenceEquals(handle.Vertices, target) || handle.Vertices.Vertices.Count == 0) { return false; }
            if (!TryGetVertexPoint(ray, noteParent, handle.Distance, out grabPoint)) { return false; }

            grabbedVertex = handle.Vertex;
            grabbedVertices = handle.Vertices;

            // 頂点を2つ以上選んでいるときは、選択中の頂点だけを回す・拡大縮小する (頂点レイヤーと同じ)。そうでなければ、すべての頂点
            if (selectedVertices.Count >= 2 && ReferenceEquals(target, selectionOwner))
            {
                foreach (var selected in selectedVertices)
                {
                    dragTargets.Add(new DragTarget { Vertex = selected, OriginPosition = selected.Position.Value });
                }
            }
            else
            {
                AddDragTargets(handle.Vertices);
            }
            if (dragTargets.Count == 0) { return false; }

            Vector2 sum = Vector2.zero;
            foreach (var dragTarget in dragTargets) { sum += dragTarget.OriginPosition; }
            grabCenter = sum / dragTargets.Count;

            isGrabbingCenter = false;
            grabDistance = handle.Distance;
            grabKind = isRotate ? GrabKind.Rotate : GrabKind.Scale;
            isGrabbing = true;
            return true;
        }

        /// <summary>
        /// 画面上の位置から、掴める距離にあるつまみのうち、いちばん近いものの番号を返す。無ければ -1
        /// </summary>
        /// <param name="includesCenter">中心点のつまみも対象にするか</param>
        private int FindNearestHandleIndex(PreviewView view, Vector2 screenPosition, bool includesCenter = true)
        {
            if (view == null || handles.Count == 0) { return -1; }

            float scale = Screen.height / REFERENCE_SCREEN_HEIGHT;
            float pickRadius = (view.IsFullScreen ? PICK_RADIUS : PICK_RADIUS_MINI) * scale;

            int nearestIndex = -1;
            float nearestScore = float.MaxValue;
            for (int i = 0; i < handles.Count; i++)
            {
                if (!includesCenter && handles[i].Vertex == null) { continue; }

                float distance = Vector2.Distance(handles[i].ScreenPosition, screenPosition);
                if (distance > pickRadius) { continue; }

                float score = handles[i].Vertex == null ? distance + CENTER_PICK_PENALTY * scale : distance;
                if (score < nearestScore)
                {
                    nearestScore = score;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        /// <summary>
        /// ノーツの頂点すべてを、動かす対象に加える
        /// </summary>
        private void AddDragTargets(SpaceVertices vertices)
        {
            if (vertices == null) { return; }

            foreach (var vertex in vertices.Vertices)
            {
                dragTargets.Add(new DragTarget { Vertex = vertex, OriginPosition = vertex.Position.Value });
            }
        }

        /// <summary>
        /// 掴んだつまみを、光線の指す位置へ動かす
        /// </summary>
        /// <returns>頂点が実際に動いたとき true</returns>
        public bool Drag(Ray ray, Transform noteParent)
        {
            if (!isGrabbing || noteParent == null) { return false; }
            if (!TryGetVertexPoint(ray, noteParent, grabDistance, out Vector2 point)) { return false; }

            // 回す・拡大縮小する (頂点レイヤーの「回転」「拡大縮小」と同じ計算)
            if (grabKind != GrabKind.Move)
            {
                float angle = 0f;
                float magnitude = 1f;
                if (grabKind == GrabKind.Rotate)
                {
                    angle = Vector2Extensions.AngleBetweenVectors(grabPoint, point, grabCenter);
                }
                else
                {
                    float baseDistance = (grabPoint - grabCenter).magnitude;
                    // 中心のすぐそばを掴んだときは、割合を決められないので、動かさない
                    if (baseDistance < 0.0001f) { return false; }

                    magnitude = (point - grabCenter).magnitude / baseDistance;
                }

                bool isTransformed = false;
                foreach (var target in dragTargets)
                {
                    Vector2 before = target.Vertex.Position.Value;
                    Vector2 next = grabKind == GrabKind.Rotate
                        ? target.OriginPosition.RotatePoint(grabCenter, angle)
                        : target.OriginPosition.ScalePoint(grabCenter, magnitude);
                    target.Vertex.SetPosition(next);
                    if (target.Vertex.Position.Value != before) { isTransformed = true; }
                }

                return isTransformed;
            }

            // 掴んだ位置からの移動量だけ動かす (掴んだ瞬間に、つまみがカーソルの位置へ飛ばないように)
            Vector2 translation = point - grabPoint;

            // 中心点で全体を動かすときは、形が崩れないよう、どの頂点も範囲の外に出ない位置で止める
            // (複数のノーツをまとめて動かしているときは、どれか1つでも縁に着いたら、全体がそこで止まる)
            // (選択中の頂点をまとめて動かすときも同じ)
            if (isGrabbingCenter || dragTargets.Count > 1) { translation = LimitTranslation(translation); }

            bool isMoved = false;
            foreach (var target in dragTargets)
            {
                Vector2 before = target.Vertex.Position.Value;
                target.Vertex.SetPosition(target.OriginPosition + translation);
                if (target.Vertex.Position.Value != before) { isMoved = true; }
            }

            return isMoved;
        }

        /// <summary>
        /// 掴んだつまみを離す。頂点が動いていればUndo履歴に登録する
        /// </summary>
        public void Release()
        {
            if (!isGrabbing) { return; }
            isGrabbing = false;

            var previous = new List<VertexDataToPos>();
            var current = new List<VertexDataToPos>();
            bool isChanged = false;

            foreach (var target in dragTargets)
            {
                previous.Add(new VertexDataToPos(target.Vertex, target.OriginPosition));
                current.Add(new VertexDataToPos(target.Vertex, target.Vertex.Position.Value));

                if (target.Vertex.Position.Value != target.OriginPosition) { isChanged = true; }
            }

            dragTargets.Clear();

            // 履歴に登録されると、その通知でプレビューも更新される
            if (isChanged) { UndoRedo.Vertices.VerticesMoveRecord.RecordVertcesMoving(previous, current); }
        }

        /// <summary>
        /// 掴んだつまみを離す。元に戻す履歴には登録しない (呼び出し側が、別の形で登録するとき用)
        /// </summary>
        public void ReleaseWithoutRecord()
        {
            isGrabbing = false;
            dragTargets.Clear();
        }

        /// <summary>
        /// 光線が、指定した位置の断面を通る点を、頂点と同じ座標(単位円の中)で求める
        /// </summary>
        public bool TryGetVertexPoint(Ray ray, Transform noteParent, float distance, out Vector2 point)
        {
            point = default;
            if (!PreviewTrackSurface.TryGetSectionPoint(ray, noteParent, optionHolder.NoteCurveRadius.Value, distance, out float x, out float y)) { return false; }

            point = new Vector2(x, y) / PreviewTrackSurface.VERTEX_SCALE;
            return true;
        }

        /// <summary>
        /// すべての頂点が配置できる範囲(単位円)に収まるよう、移動量を縮める
        /// (頂点は範囲の外に出ると個別に縁へ押し戻されるので、そのまま動かすと形が崩れる)
        /// </summary>
        private Vector2 LimitTranslation(Vector2 translation)
        {
            if (IsAllInsideAfter(translation)) { return translation; }

            // 収まる最大の割合を二分探索で求める
            float low = 0f;
            float high = 1f;
            for (int i = 0; i < 16; i++)
            {
                float middle = (low + high) * 0.5f;
                if (IsAllInsideAfter(translation * middle)) { low = middle; }
                else { high = middle; }
            }

            return translation * low;
        }

        private bool IsAllInsideAfter(Vector2 translation)
        {
            foreach (var target in dragTargets)
            {
                // 縁の上にある頂点は、計算の誤差でわずかに範囲の外にいることがある
                // 「今より外へは出さない」という条件にして、縁に接した多角形でも内側へは動かせるようにする
                float limit = Mathf.Max(1f, target.OriginPosition.sqrMagnitude) + 0.0001f;
                if ((target.OriginPosition + translation).sqrMagnitude > limit) { return false; }
            }

            return true;
        }

        #endregion
    }
}
