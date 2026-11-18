using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;
using static UndoRedo.Vertices.VerticesMoveRecord;

namespace ChartEditor
{
    /// <summary>
    /// 頂点のレイヤーで、中心点 (掴むと多角形全体が動く点) を右クリックすると、中心の位置を数値で決める窓を出す
    ///
    /// 窓の見た目は、ホールドの曲線の制御点の窓と同じ (シーンの HoldCurveControlPanelView を複製して、見出しと欄の名前を替える)。
    /// ・X: 横、Y: 縦 (頂点の座標と同じ。円の中: -1〜1)
    /// ・出す値は、中心点の位置 (形の重心。3D編集の「節の中心」と同じ: VertexObjectsController.CalcCenter)
    /// ・数値を入れると、頂点すべてを同じだけ動かす (中心点のドラッグと同じ。形が円からはみ出すときは、はみ出さないところで止める)。元に戻せる
    /// 窓は中心点の右に出て、中心点を追いかける。窓の外を押す・Esc・× で閉じる。
    /// 編集する節が替わったとき、レイヤーを替えたとき、3D編集を開いたときも閉じる。
    /// </summary>
    public class VertexCenterPanel : ITickable, IPostLateTickable, IDisposable
    {
        static VertexCenterPanel instance;

        readonly IChartEditorDataGetter dataGetter;
        readonly INotesDataGetter notesGetter;

        // 窓 (はじめて出すときに、ホールドの曲線の窓を複製して作る)
        HoldCurveControlPanelView panel;
        bool isPanelCreated;

        // 窓で位置を決めている節と、その中心点
        IVerticesControlableNoteData panelTarget;
        VertexObject panelHandle;
        // 窓を出す場所 (画面のピクセル。中心点が映らないときは、前の場所のまま)
        Vector2 lastPanelPosition;

        // 窓を、中心点からどれだけ右に出すか (画面のピクセル。中心点に重ならないように)
        const float PANEL_OFFSET_X = 28f;

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

        void ITickable.Tick()
        {
            HandlePanelInput();

            // 中心点の右クリック: 中心の位置を数値で決める窓
            if (Input.GetMouseButtonDown(1) && CanOpen())
            {
                VertexObject handle = FindCenterHandleUnderCursor();
                if (handle != null) { Open(handle); }
            }
        }

        void IPostLateTickable.PostLateTick()
        {
            // 窓の数値と場所を、そのフレームの動き (頂点の移動、元に戻す など) がすべて終わった後の中心点に合わせる
            Refresh();
        }

        /// <summary>
        /// 窓を出している間: 窓の外が押されたら閉じる (打っている途中の値は、反映してから)。Esc でも閉じる
        /// </summary>
        private void HandlePanelInput()
        {
            if (panel == null || !panel.IsShown) { return; }

            bool isPressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
            if (isPressed && !panel.IsPointerOver(Input.mousePosition))
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
        /// カーソルの下にある中心点 (無ければ null)
        /// </summary>
        private VertexObject FindCenterHandleUnderCursor()
        {
            var collider = dataGetter.GetInteractableCollider<IPointMovableCollider>();
            VertexObject vertex = collider?.Vertex?.Vertex;
            return vertex != null && vertex.IsCenterHandle ? vertex : null;
        }

        private void Open(VertexObject handle)
        {
            var target = notesGetter.EditingVertices.Value;
            if (target == null || handle == null) { return; }
            if (IsLocked(target))
            {
                Debug.Log("【ロック】ロックしたノーツの形は、動かせません (外すには、右クリックのメニューの「ロックを外す」)");
                return;
            }

            if (!TryCreatePanel()) { return; }

            panelTarget = target;
            panelHandle = handle;
            lastPanelPosition = (Vector2)Input.mousePosition + new Vector2(PANEL_OFFSET_X, 0f);
            panel.Show(GetPanelPosition());
            Refresh();
        }

        private void Close()
        {
            if (panel != null) { panel.Hide(); }
            panelTarget = null;
            panelHandle = null;
        }

        /// <summary>
        /// 窓を作る (ホールドの曲線の窓を複製して、見出しと欄の名前を替える。作れなかったときは false)
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

            panel.Configure("中心の位置", "X", "Y", "X: 横、Y: 縦 (円の中: -1〜1)", "0.###");
            panel.OnValueEntered += OnValueEntered;
            panel.OnCloseClicked += Close;
            return true;
        }

        /// <summary>
        /// 窓の数値と場所を、今の中心点に合わせる (節が替わった、レイヤーを替えた などのときは閉じる)
        /// </summary>
        private void Refresh()
        {
            if (panel == null || !panel.IsShown) { return; }

            if (!IsTargetAlive())
            {
                Close();
                return;
            }

            Vector2 center = VertexObjectsController.CalcCenter(panelTarget.SpaceVertices);
            panel.SetValues(center.x, center.y);
            panel.Move(GetPanelPosition());
        }

        /// <summary>
        /// 窓で位置を決めている節を、まだ頂点のレイヤーで編集しているか
        /// </summary>
        private bool IsTargetAlive()
        {
            if (panelTarget == null || panelHandle == null) { return false; }
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return false; }
            if (SpaceHold3DEditor.IsOpen) { return false; }
            if (!ReferenceEquals(notesGetter.EditingVertices.Value, panelTarget)) { return false; }
            return !IsLocked(panelTarget);
        }

        /// <summary>
        /// 窓を出す場所 (中心点の右。画面のピクセル)
        /// </summary>
        private Vector2 GetPanelPosition()
        {
            Camera camera = EditorViewCameras.VerticesView;
            if (camera == null || panelHandle == null) { return lastPanelPosition; }

            Vector3 screen = camera.WorldToScreenPoint(panelHandle.transform.position);
            // (中心点がカメラの後ろにあるときは、前の場所のまま)
            if (screen.z > 0f) { lastPanelPosition = new Vector2(screen.x + PANEL_OFFSET_X, screen.y); }
            return lastPanelPosition;
        }

        /// <summary>
        /// 窓に数値が入力されたとき: 中心がその位置になるように、頂点すべてを同じだけ動かす (元に戻すのは1回ぶん)
        /// </summary>
        private void OnValueEntered(int axis, float value)
        {
            if (!IsTargetAlive()) { return; }
            // 頂点を掴んで動かしている最中は、受け付けない
            if (!dataGetter.CurrentEditMode.Value.IsInEditModeList(acceptedModes)) { return; }

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

            Refresh();
        }

        private static bool IsLocked(IVerticesControlableNoteData target)
        {
            return target is IDeployableNoteData note && NoteLock.IsLocked(note);
        }
    }
}
