using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;
using static UndoRedo.Vertices.VerticesMoveRecord;

namespace ChartEditor
{
    /// <summary>
    /// 頂点・中心点の位置を、数値で決める窓
    ///
    /// ・頂点のレイヤーで、中心点 (掴むと多角形全体が動く点) や頂点を右クリックすると出る (録音 20261118_161919: 頂点も。
    ///   上の「頂点詳細設定」の欄の代わり)
    /// ・全画面プレビューで、頂点・中心点のつまみを右クリックしても出る (PreviewNotePicker が OpenForPreview を呼ぶ。録音 20261118_162441)
    /// 窓の見た目は、ホールドの曲線の制御点の窓と同じ (シーンの HoldCurveControlPanelView を複製して、見出しと欄の名前を替える)。
    /// ・X: 横、Y: 縦 (頂点の座標と同じ。円の中: -1〜1)。欄の名前を左右にドラッグしても動かせる (NumberScrubHandle)
    /// ・中心点: 出す値は、中心点の位置 (形の重心。3D編集の「節の中心」と同じ: VertexObjectsController.CalcCenter)。
    ///   数値を入れると、頂点すべてを同じだけ動かす (中心点のドラッグと同じ。形が円からはみ出すときは、はみ出さないところで止める)
    /// ・頂点: その頂点だけを動かす (円の外には出ない)
    /// どれも元に戻せる。窓はつまみの右に出て、つまみを追いかける。窓の外を押す・Esc・× で閉じる。
    /// 編集する節が替わったとき、レイヤーを替えたとき、3D編集を開いたとき、頂点が消えたときも閉じる。
    /// </summary>
    public class VertexCenterPanel : ITickable, IPostLateTickable, IDisposable
    {
        static VertexCenterPanel instance;

        readonly IChartEditorDataGetter dataGetter;
        readonly INotesDataGetter notesGetter;

        // 窓 (はじめて出すときに、ホールドの曲線の窓を複製して作る)
        HoldCurveControlPanelView panel;
        bool isPanelCreated;

        // 窓で位置を決めている節と、その頂点 (null = 中心点)
        IVerticesControlableNoteData panelTarget;
        VertexData panelVertex;
        // 頂点のレイヤーで、窓が追いかけるつまみ
        VertexObject panelHandle;
        // 全画面プレビューで開いたときの、窓がまだ使えるかの確かめ方・つまみの画面の位置・反映したあとの処理
        bool isPreviewContext;
        Func<bool> previewIsAlive;
        Func<Vector2?> previewScreenPosition;
        Action previewOnApplied;
        // 窓を出す場所 (画面のピクセル。つまみが映らないときは、前の場所のまま)
        Vector2 lastPanelPosition;
        // 窓を出したフレーム (そのフレームの右クリックは、窓を出したクリックなので、「窓の外を押した」として閉じない)
        int openedFrame = -1;

        // 窓を、つまみからどれだけ右に出すか (画面のピクセル。つまみに重ならないように)
        const float PANEL_OFFSET_X = 28f;
        // 欄の名前のドラッグで、1ピクセルあたりに動く量 (円の半径が 1)
        const float SCRUB_STEP = 0.005f;

        // 右クリックで窓を出してよいモード (頂点のツールの操作待ち。ドラッグの途中は出さない)
        static readonly EditMode[] acceptedModes =
        {
            EditMode.VertexEdit,
            EditMode.VertexPlace,
            EditMode.VerticesRotate,
            EditMode.VerticesScale,
        };

        public VertexCenterPanel(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            instance = this;
        }

        void IDisposable.Dispose()
        {
            if (instance == this) { instance = null; }
        }

        /// <summary>
        /// カーソルが窓の上にあるか (窓の上を押したときに、裏の頂点を掴まないように: VerticesDragOperationBase)
        /// </summary>
        public static bool IsPointerOverPanel =>
            instance != null && instance.panel != null && instance.panel.IsPointerOver(Input.mousePosition);

        /// <summary>
        /// 全画面プレビューで、頂点・中心点の位置の窓を出す
        /// </summary>
        /// <param name="target">頂点を持つノーツ</param>
        /// <param name="vertex">頂点 (null = 中心点)</param>
        /// <param name="isAlive">窓をまだ出していてよいか (プレビューで、そのノーツを選んだままか)</param>
        /// <param name="getScreenPosition">つまみの、今の画面の位置 (映っていなければ null)</param>
        /// <param name="onApplied">数値を反映したあとに呼ぶ処理 (プレビューを描き直す)</param>
        public static void OpenForPreview(IVerticesControlableNoteData target, VertexData vertex, Func<bool> isAlive, Func<Vector2?> getScreenPosition, Action onApplied)
        {
            if (instance == null || target == null) { return; }
            instance.OpenInPreview(target, vertex, isAlive, getScreenPosition, onApplied);
        }

        void ITickable.Tick()
        {
            HandlePanelInput();

            // 中心点・頂点の右クリック: 位置を数値で決める窓
            if (Input.GetMouseButtonDown(1) && CanOpen())
            {
                VertexObject handle = FindHandleUnderCursor();
                if (handle != null) { Open(handle); }
            }
        }

        void IPostLateTickable.PostLateTick()
        {
            // 窓の数値と場所を、そのフレームの動き (頂点の移動、元に戻す など) がすべて終わった後のつまみに合わせる
            Refresh();
        }

        /// <summary>
        /// 窓を出している間: 窓の外が押されたら閉じる (打っている途中の値は、反映してから)。Esc でも閉じる
        /// </summary>
        private void HandlePanelInput()
        {
            if (panel == null || !panel.IsShown) { return; }

            bool isPressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
            if (isPressed && Time.frameCount != openedFrame && !panel.IsPointerOver(Input.mousePosition))
            {
                panel.CommitTyping();
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && !InputFieldFocus.IsTyping) { Close(); }
        }

        private bool CanOpen()
        {
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return false; }
            // メニューを開いている間と、SpaceHold の3D編集を開いている間は出さない
            if (EditorMenuBar.IsBlockingChartInput) { return false; }
            if (!dataGetter.CurrentEditMode.Value.IsInEditModeList(acceptedModes)) { return false; }

            // 左ボタンを押している間 (ドラッグの途中) と、画面部品の上は出さない
            if (Input.GetMouseButton(0)) { return false; }
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return false; }

            return notesGetter.EditingVertices.Value != null;
        }

        /// <summary>
        /// カーソルの下にある中心点・頂点 (無ければ null)
        /// </summary>
        private VertexObject FindHandleUnderCursor()
        {
            var collider = dataGetter.GetInteractableCollider<IPointMovableCollider>();
            return collider?.Vertex?.Vertex;
        }

        private void Open(VertexObject handle)
        {
            var target = notesGetter.EditingVertices.Value;
            if (target == null || handle == null) { return; }

            VertexData vertex = handle.IsCenterHandle ? null : handle.VertexData;
            if (!handle.IsCenterHandle && (vertex == null || IndexOfVertex(target, vertex) < 0)) { return; }
            if (!BeginOpen(target, vertex)) { return; }

            isPreviewContext = false;
            panelHandle = handle;
            lastPanelPosition = (Vector2)Input.mousePosition + new Vector2(PANEL_OFFSET_X, 0f);
            panel.Show(GetPanelPosition());
            Refresh();
        }

        private void OpenInPreview(IVerticesControlableNoteData target, VertexData vertex, Func<bool> isAlive, Func<Vector2?> getScreenPosition, Action onApplied)
        {
            if (vertex != null && IndexOfVertex(target, vertex) < 0) { return; }
            if (!BeginOpen(target, vertex)) { return; }

            isPreviewContext = true;
            panelHandle = null;
            previewIsAlive = isAlive;
            previewScreenPosition = getScreenPosition;
            previewOnApplied = onApplied;
            lastPanelPosition = (Vector2)Input.mousePosition + new Vector2(PANEL_OFFSET_X, 0f);
            panel.Show(GetPanelPosition());
            Refresh();
        }

        /// <summary>
        /// 窓を出す前の準備 (ロックの確かめ、窓を作る、見出しを替える)。出せないときは false
        /// </summary>
        private bool BeginOpen(IVerticesControlableNoteData target, VertexData vertex)
        {
            if (IsLocked(target))
            {
                Debug.Log("【ロック】ロックしたノーツの形は、動かせません (外すには、右クリックのメニューの「ロックを外す」)");
                return false;
            }
            if (!TryCreatePanel()) { return false; }

            // 前に出していた窓は、打っている途中の値を反映してから替える
            if (panel.IsShown) { panel.CommitTyping(); }

            panelTarget = target;
            panelVertex = vertex;
            openedFrame = Time.frameCount;

            string title = vertex == null ? "中心の位置" : $"頂点 {IndexOfVertex(target, vertex)} の位置";
            panel.Configure(title, "X", "Y", "X: 横、Y: 縦 (円の中: -1〜1)", "0.###");
            return true;
        }

        private void Close()
        {
            if (panel != null) { panel.Hide(); }
            panelTarget = null;
            panelVertex = null;
            panelHandle = null;
            isPreviewContext = false;
            previewIsAlive = null;
            previewScreenPosition = null;
            previewOnApplied = null;
        }

        /// <summary>
        /// 窓を作る (ホールドの曲線の窓を複製して、欄の名前のドラッグの量を替える。作れなかったときは false)
        /// </summary>
        private bool TryCreatePanel()
        {
            if (panel != null) { return true; }
            if (isPanelCreated) { return false; }
            isPanelCreated = true;

            var original = HoldCurveControlPanelView.FindOriginal();
            if (original == null)
            {
                Debug.LogWarning("【頂点】位置を数値で決める窓の元 (HoldCurveControlPanelView) がシーンにありません");
                return false;
            }

            panel = original.CreateCopy("VertexCenterControlCanvas");
            if (panel == null) { return false; }

            panel.SetScrubSettings(NumberScrubHandle.Settings.Float(SCRUB_STEP, -1f, 1f), NumberScrubHandle.Settings.Float(SCRUB_STEP, -1f, 1f));
            panel.OnValueEntered += OnValueEntered;
            panel.OnCloseClicked += Close;
            return true;
        }

        /// <summary>
        /// 窓の数値と場所を、今のつまみに合わせる (節が替わった、レイヤーを替えた などのときは閉じる)
        /// </summary>
        private void Refresh()
        {
            if (panel == null || !panel.IsShown) { return; }

            if (!IsTargetAlive())
            {
                Close();
                return;
            }

            Vector2 position = panelVertex != null ? panelVertex.Position.Value : VertexObjectsController.CalcCenter(panelTarget.SpaceVertices);
            panel.SetValues(position.x, position.y);
            panel.Move(GetPanelPosition());
        }

        /// <summary>
        /// 窓で位置を決めている節 (と頂点) を、まだ編集しているか
        /// </summary>
        private bool IsTargetAlive()
        {
            if (panelTarget == null) { return false; }
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (SpaceHold3DEditor.IsOpen) { return false; }
            if (IsLocked(panelTarget)) { return false; }
            // 頂点が消えたとき (Delete、元に戻す など)
            if (panelVertex != null && IndexOfVertex(panelTarget, panelVertex) < 0) { return false; }

            if (isPreviewContext)
            {
                return dataGetter.EditNoteType.Value == EditNoteType.Preview && previewIsAlive != null && previewIsAlive();
            }

            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return false; }
            return ReferenceEquals(notesGetter.EditingVertices.Value, panelTarget);
        }

        /// <summary>
        /// 窓を出す場所 (つまみの右。画面のピクセル)
        /// </summary>
        private Vector2 GetPanelPosition()
        {
            if (isPreviewContext)
            {
                Vector2? screen = previewScreenPosition?.Invoke();
                if (screen.HasValue) { lastPanelPosition = screen.Value + new Vector2(PANEL_OFFSET_X, 0f); }
                return lastPanelPosition;
            }

            Camera camera = EditorViewCameras.VerticesView;
            VertexObject handle = FindLiveHandle();
            if (camera == null || handle == null) { return lastPanelPosition; }

            Vector3 world = camera.WorldToScreenPoint(handle.transform.position);
            // (つまみがカメラの後ろにあるときは、前の場所のまま)
            if (world.z > 0f) { lastPanelPosition = new Vector2(world.x + PANEL_OFFSET_X, world.y); }
            return lastPanelPosition;
        }

        /// <summary>
        /// 窓が追いかける、頂点のレイヤーのつまみ (頂点のつまみが作り直されていたら、同じ頂点のものを探し直す)
        /// </summary>
        private VertexObject FindLiveHandle()
        {
            if (panelHandle != null) { return panelHandle; }
            if (panelVertex == null) { return null; }

            foreach (var pair in notesGetter.DataToVertexObject)
            {
                if (pair != null && ReferenceEquals(pair.Data, panelVertex) && pair.Object != null)
                {
                    panelHandle = pair.Object;
                    return panelHandle;
                }
            }
            return null;
        }

        /// <summary>
        /// 窓に数値が入力されたとき: 中心点なら、中心がその位置になるように頂点すべてを同じだけ動かす。頂点なら、その頂点を動かす (元に戻すのは1回ぶん)
        /// </summary>
        private void OnValueEntered(int axis, float value)
        {
            if (!IsTargetAlive()) { return; }
            // 頂点のレイヤーで、頂点を掴んで動かしている最中は、受け付けない
            if (!isPreviewContext && !dataGetter.CurrentEditMode.Value.IsInEditModeList(acceptedModes)) { return; }

            if (panelVertex != null) { MoveVertex(axis, value); }
            else { MoveCenter(axis, value); }

            previewOnApplied?.Invoke();
            Refresh();
        }

        private void MoveVertex(int axis, float value)
        {
            Vector2 before = panelVertex.Position.Value;
            Vector2 wanted = axis == 0 ? new Vector2(value, before.y) : new Vector2(before.x, value);
            // 円の外には出さない (頂点の位置は、円の中だけ)
            if (wanted.sqrMagnitude > 1f)
            {
                wanted = wanted.normalized;
                Debug.Log("【頂点】円の外には置けないので、円の縁に置きました");
            }
            if ((wanted - before).sqrMagnitude < 1e-12f) { return; }

            // (記録すると、その場で動く)
            RecordVertexMoving(panelVertex, before, wanted);
        }

        private void MoveCenter(int axis, float value)
        {
            var vertices = panelTarget.SpaceVertices.Vertices;
            if (vertices.Count == 0) { return; }

            Vector2 center = VertexObjectsController.CalcCenter(panelTarget.SpaceVertices);
            Vector2 wanted = axis == 0 ? new Vector2(value, center.y) : new Vector2(center.x, value);
            Vector2 translation = wanted - center;
            if (translation.sqrMagnitude < 1e-12f) { return; }

            // 形が円からはみ出さないところまでにする (中心点のドラッグと同じ)
            var origins = new List<Vector2>(vertices.Count);
            foreach (var vertex in vertices) { origins.Add(vertex.Position.Value); }
            Vector2 limited = VerticesDragOperationBase.LimitTranslationInsideCircle(origins, translation);
            if (limited.sqrMagnitude < 1e-12f)
            {
                Debug.Log("【頂点】これ以上その向きに動かすと、形が円からはみ出します");
                return;
            }

            var previous = new List<VertexDataToPos>(vertices.Count);
            var current = new List<VertexDataToPos>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
            {
                previous.Add(new VertexDataToPos(vertices[i], origins[i]));
                current.Add(new VertexDataToPos(vertices[i], origins[i] + limited));
            }

            // (記録すると、その場で動く)
            RecordVertcesMoving(previous, current);

            if ((limited - translation).sqrMagnitude > 1e-8f)
            {
                Debug.Log("【頂点】形が円からはみ出さないところまで動かしました");
            }
        }

        /// <summary>その頂点が、節の頂点の何番めか (無ければ -1)</summary>
        private static int IndexOfVertex(IVerticesControlableNoteData target, VertexData vertex)
        {
            var vertices = target?.SpaceVertices?.Vertices;
            if (vertices == null || vertex == null) { return -1; }
            for (int i = 0; i < vertices.Count; i++)
            {
                if (ReferenceEquals(vertices[i], vertex)) { return i; }
            }
            return -1;
        }

        private static bool IsLocked(IVerticesControlableNoteData target)
        {
            return target is IDeployableNoteData note && NoteLock.IsLocked(note);
        }
    }
}
