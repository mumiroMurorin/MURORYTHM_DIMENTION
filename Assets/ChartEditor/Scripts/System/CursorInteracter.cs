using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        EditMode[] cursorIgnoreModes = new EditMode[] 
        {
            EditMode.NoteSelect,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Connect,
            EditMode.Connecting,
            EditMode.Explanation,
            EditMode.ChangeType,
        };

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataSetter = chartEditorDataSetter;
            this.chartEditorDataGetter = chartEditorDataGetter;
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

            // 無視リストに入ってるモード中であれば無効
            if (isCursorIgnoremode) { return; }
            // カーソル下に何もないときは無効
            if (raycastEditMode == EditMode.None) { return; }

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
            if(!hitObject.TryGetComponent(out IInteractableCollider interactable)) { return EditMode.None; }

            return interactable.EditMode;
        }

        /// <summary>
        /// カーソル下のオブジェクトについて更新
        /// </summary>
        private void UpdateObjectUnderCursor()
        {
            GameObject obj = GetObjectUnderCursor();

            // ヒットオブジェクトの更新
            // 前フレームと変わってなかったら更新しない
            if(currentHitObject != null && obj == currentHitObject) { return; }
            currentHitObject = obj;

            // カーソル下に何もなかったら返す
            if(obj == null)
            {
                chartEditorDataSetter.SetInteractableColliders(null);
                return;
            }

            // すべてのコンポーネントを取得
            IInteractableCollider[] allComponents = obj.GetComponents<IInteractableCollider>();
            chartEditorDataSetter.SetInteractableColliders(allComponents);
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

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit, rayDistance, raycastLayer)) { return null; }
            return hit.collider.gameObject;
        }

        /// <summary>
        /// Rayがヒットした位置(ワールド座標)を返す
        /// </summary>
        /// <returns></returns>
        public Vector3 GetWorldPositionUnderCursor()
        {
            if (currentViewCamera == null) { return Vector3.one * -9999; }

            Ray ray = currentViewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

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
