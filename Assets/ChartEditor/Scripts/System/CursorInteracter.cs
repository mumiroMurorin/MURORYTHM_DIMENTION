using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class CursorInteracter : MonoBehaviour, ICursorInteracter
    {
        [SerializeField] Camera groundViewCamera;
        [SerializeField] Camera verticesViewCamera;
        [SerializeField] float rayDistance = 35f;

        IChartEditorDataSetter chartEditorDataSetter;
        IChartEditorDataGetter chartEditorDataGetter;

        int raycastLayer;
        bool isCursorIgnoremode;
        GameObject currentHitObject;
        Camera currentViewCamera;

        // カーソルの下にあるものでモードを切り替えないモード (ツールで選んだモードと、ダイアログなどを開いている間)
        EditMode[] cursorIgnoreModes = new EditMode[]
        {
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Connect,
            EditMode.DisConnect,
            EditMode.Connecting,
            EditMode.Explanation,
            EditMode.ChangeType,
            EditMode.VertexPlace,
            EditMode.VertexEdit,
            EditMode.VertexMoving,
            EditMode.VerticesRotate,
            EditMode.VerticesRotating,
            EditMode.VerticesScale,
            EditMode.VerticesScaling,
            EditMode.Preview,
            EditMode.SpaceHoldView,
        };

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataSetter = chartEditorDataSetter;
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Awake()
        {
            // アウトラインを描くカメラを登録する (ノーツが作られるより前に済ませる)
            EditorViewCameras.GroundView = groundViewCamera;
            EditorViewCameras.VerticesView = verticesViewCamera;
        }

        private void OnDestroy()
        {
            if (EditorViewCameras.GroundView == groundViewCamera) { EditorViewCameras.GroundView = null; }
            if (EditorViewCameras.VerticesView == verticesViewCamera) { EditorViewCameras.VerticesView = null; }
        }

        private void Start()
        {
            raycastLayer = 1 << LayerMask.NameToLayer("CursorInteractable");
            Bind();
        }

        private void Bind()
        {
            // カーソル無視モードの更新
            chartEditorDataGetter?.CurrentEditMode
                .Subscribe(mode => {
                    isCursorIgnoremode = false;

                    // 一つずつモードを取り出してどれかに該当したら無視モードにする
                    if (mode.IsInEditModeList(cursorIgnoreModes)) { isCursorIgnoremode = true; }
                })
                .AddTo(this.gameObject);

            // Rayを発射するカメラの更新
            chartEditorDataGetter?.EditNoteType
                .Subscribe(type => {
                    switch (type)
                    {
                        case EditNoteType.Ground:
                            currentViewCamera = groundViewCamera;
                            break;
                        case EditNoteType.Space:
                            currentViewCamera = groundViewCamera;
                            break;
                        case EditNoteType.Vertices:
                            currentViewCamera = verticesViewCamera;
                            break;
                    }
                })
                .AddTo(this.gameObject);
        }

        private void Update()
        {
            // 譜面の読み込み中は、カーソル下のオブジェクトを「無し」にして編集操作が始まらないようにする
            if (chartEditorDataGetter.IsLoadingChart.Value)
            {
                if (currentHitObject != null)
                {
                    currentHitObject = null;
                    chartEditorDataSetter.SetInteractableColliders(null);
                }

                chartEditorDataSetter.SetEditMode(EditMode.None);
                return;
            }

            // 画面上のメニューを開いている間は、カーソル下のオブジェクトを「無し」にして、メニューの裏の譜面が操作されないようにする
            // (メニューの外をクリックして閉じたときに、そのクリックでノーツが動いたり繋がったりしないように。
            //  エディットモードは変えない。メニューを閉じれば、そのまま続きの操作ができる)
            if (EditorMenuBar.IsBlockingChartInput)
            {
                if (currentHitObject != null)
                {
                    currentHitObject = null;
                    chartEditorDataSetter.SetInteractableColliders(null);
                }
                return;
            }

            // ホールドを曲げる制御点の印にカーソルが乗っている間と、制御点を動かしている間も、
            // カーソル下のオブジェクトを「無し」にする (印の下にあるノーツやレーンが、クリックで操作されないように)
            if (HoldCurveEditor.IsCapturingPointer)
            {
                if (currentHitObject != null)
                {
                    currentHitObject = null;
                    chartEditorDataSetter.SetInteractableColliders(null);
                }

                // カーソルの下のもので決まるモード (配置、移動など) のときは、制御点のモードにする
                // (ツールで選んだモード (選択など) と、制御点を動かしている最中のモードは、そのまま)
                bool isMouseBusy = Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0);
                if (!isCursorIgnoremode && !isMouseBusy && !HoldCurveEditor.IsDragging)
                {
                    chartEditorDataSetter.SetEditMode(EditMode.CurveHandle);
                }
                return;
            }

            UpdateObjectUnderCursor();
            SetEditorMode();
        }

        /// <summary>
        /// エディットモードの更新
        /// そのコライダーの一番上にあるInteractableColliderのEditModeが反映される
        /// </summary>
        private void SetEditorMode()
        {
            EditMode raycastEditMode = GetEditModeUnderCursor();

            // ホールドを1本だけ編集している間は、ノーツを置けない (レーンの上にカーソルを置いても、配置のモードにしない)
            if (HoldIsolation.IsActive && raycastEditMode == EditMode.Deploy) { raycastEditMode = EditMode.None; }

            // 無視リストに入ってるモード中であれば無効
            if (isCursorIgnoremode) { return; }

            // カーソル下にUIがある場合は無視
            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            // -カーソル下に何もないときは無効-
            // if (raycastEditMode == EditMode.None) { return; }

            // 長押し中は無効
            if (Input.GetMouseButton(0)) { return; }
            if (Input.GetMouseButtonUp(0)) { return; }
            if (Input.GetMouseButtonDown(0)) { return; }

            chartEditorDataSetter.SetEditMode(raycastEditMode);
        }

        /// <summary>
        /// カーソルに乗っかっているコライダーのエディットモードを返す
        /// </summary>
        /// <returns></returns>
        private EditMode GetEditModeUnderCursor()
        {
            GameObject hitObject = GetObjectUnderCursor();

            if(hitObject == null) { return EditMode.None; }
            // ロックしたノーツの上: 選ぶことだけできる (移動・幅を変える などのモードにしない)
            if (IsLockedNoteObject(hitObject)) { return EditMode.None; }
            // (ダイナミックノーツを選んでいるときも、ノーツの上はいつものモード (移動・幅を変える)。ドラッグすればノーツを動かせ、
            //  動かさずに離せば、そのノーツにダイナミックノーツを載せる (NoteDeployerBase): 録音 20261118_221348)
            if(!hitObject.TryGetComponent(out IInteractableCollider interactable)) { return EditMode.None; }

            return interactable.EditMode;
        }

        /// <summary>
        /// カーソル下のオブジェクトについて更新
        /// </summary>
        private void UpdateObjectUnderCursor()
        {
            GameObject obj = GetObjectUnderCursor();
            bool isLocked = IsLockedNoteObject(obj);

            // ヒットオブジェクトの更新
            // 前フレームと変わってなかったら更新しない (そのノーツをロックした・外したときは、更新する)
            if(currentHitObject != null && obj == currentHitObject && isLocked == isCurrentHitLocked) { return; }
            currentHitObject = obj;
            isCurrentHitLocked = isLocked;

            // カーソル下に何もなかったら返す
            if(obj == null)
            {
                chartEditorDataSetter.SetInteractableColliders(null);
                return;
            }

            // すべてのコンポーネントを取得
            IInteractableCollider[] allComponents = obj.GetComponents<IInteractableCollider>();
            // ロックしたノーツは、選ぶための部品だけを渡す (移動・幅・接続・種類の道具からは、見えないようにする)
            if (isLocked) { allComponents = SelectableOnly(allComponents); }
            chartEditorDataSetter.SetInteractableColliders(allComponents);
        }

        // カーソルの下のノーツを、ロックしているか (前のフレームのもの)
        bool isCurrentHitLocked;

        private static IInteractableCollider[] SelectableOnly(IInteractableCollider[] components)
        {
            var selectable = new List<IInteractableCollider>();
            foreach (var component in components)
            {
                if (component is ISelectableNoteCollider) { selectable.Add(component); }
            }
            return selectable.ToArray();
        }

        /// <summary>
        /// カーソルに乗っかっているオブジェクトを返す
        /// </summary>
        /// <returns></returns>
        public GameObject GetObjectUnderCursor()
        {
            if(currentViewCamera == null) { return null; }

            Ray ray = currentViewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // ホールドを1本だけ編集している間は、ほかのノーツを通り抜けて、その奥にあるものを返す
            // (ロックしたノーツは通り抜けない。選ぶことだけできる: IsLockedNoteObject)
            if (HoldIsolation.IsActive)
            {
                return HoldIsolation.TryRaycast(ray, rayDistance, raycastLayer, out hit) ? hit.collider.gameObject : null;
            }

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit, rayDistance, raycastLayer)) { return null; }
            return hit.collider.gameObject;
        }

        /// <summary>
        /// そのオブジェクトが、ダイナミックノーツを載せる先になるノーツの一部か (DynamicNoteAttachments.IsPlacementTarget)
        /// </summary>
        private static bool IsDynamicPlacementTarget(GameObject obj)
        {
            if (obj == null) { return false; }

            var noteObject = obj.GetComponentInParent<NoteObject>();
            return noteObject != null && DynamicNoteAttachments.IsPlacementTarget(noteObject.NoteData);
        }

        /// <summary>
        /// そのオブジェクトが、ロックしたノーツの一部か
        /// </summary>
        private static bool IsLockedNoteObject(GameObject obj)
        {
            if (obj == null || !NoteLock.HasAny) { return false; }

            var noteObject = obj.GetComponentInParent<NoteObject>();
            return noteObject != null && NoteLock.IsLocked(noteObject.NoteData);
        }

        /// <summary>
        /// カーソルの下にある、いちばん手前のノーツ (右クリックのメニュー用。ロックしたノーツも見つける)
        /// ホールドを1本だけ編集している間は、そのホールドのノーツだけを見つける
        /// </summary>
        public IDeployableNoteData FindNoteUnderCursor()
        {
            if (currentViewCamera == null) { return null; }

            Ray ray = currentViewCamera.ScreenPointToRay(Input.mousePosition);
            int count = Physics.RaycastNonAlloc(ray, noteHitBuffer, rayDistance, raycastLayer);
            IDeployableNoteData nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (noteHitBuffer[i].distance >= nearestDistance) { continue; }

                var noteObject = noteHitBuffer[i].collider != null ? noteHitBuffer[i].collider.GetComponentInParent<NoteObject>() : null;
                IDeployableNoteData note = noteObject != null ? noteObject.NoteData : null;
                if (note == null || HoldIsolation.IsBlockedByIsolation(note)) { continue; }

                nearest = note;
                nearestDistance = noteHitBuffer[i].distance;
            }
            return nearest;
        }

        readonly RaycastHit[] noteHitBuffer = new RaycastHit[32];

        /// <summary>
        /// Rayがヒットした位置(ワールド座標)を返す
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWorldPositionUnderCursor()
        {
            if (currentViewCamera == null) { return Vector3.one * -9999; }

            Ray ray = currentViewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // ホールドを1本だけ編集している間は、ほかのノーツを通り抜ける (GetObjectUnderCursor と同じものを指すように)
            if (HoldIsolation.IsActive)
            {
                return HoldIsolation.TryRaycast(ray, rayDistance, raycastLayer, out hit) ? hit.point : Vector3.one * -9999;
            }

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit, rayDistance, raycastLayer)) { return Vector3.one * -9999; }
            return hit.point;
        }
    }

    public interface ICursorInteracter
    {
        GameObject GetObjectUnderCursor();

        Vector3 GetWorldPositionUnderCursor();
    }

}
