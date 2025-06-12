using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class CursorInteracter : MonoBehaviour, ICursorInteracter
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] float rayDistance = 35f;

        IChartEditorDataSetter chartEditorDataSetter;
        IChartEditorDataGetter chartEditorDataGetter;

        int raycastLayer;
        bool isCursorIgnoremode;
        GameObject currentHitObject;

        List<EditMode> cursorIgnoreModes = new List<EditMode> 
        {
            EditMode.EditingConfig,
            EditMode.Destroy,
            EditMode.Connecting,
            EditMode.Explanation,
            EditMode.ChangeType,
            EditMode.SpaceEdit,
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
                    foreach (var ignoreMode in cursorIgnoreModes)
                    {
                        isCursorIgnoremode = false;
                        if (mode == ignoreMode) 
                        {
                            isCursorIgnoremode = true;
                            return;
                        }
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
            // オートモード中じゃなければ無効
            if (!chartEditorDataGetter.AutoEditMode.Value) { return; }

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

            if(currentHitObject != null && obj == currentHitObject) { あああ }

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
       
        //    // インタラクトされているRhythmConfigurableColliderの更新
        //    // あんまりよくないけどクリック処理もここでやっちゃう
        //    // 小節線
        //    if (obj.TryGetComponent(out IRhythmConfigurableBarCollider configurableBar))
        //    {
        //        if (Input.GetMouseButtonDown(0)) 
        //        {
        //            chartEditorDataSetter.SetEditMode(EditMode.EditingConfig);
        //            chartEditorDataSetter.SetRhythmConfigurableBar(configurableBar);
        //        }
        //    }

        //    // 分線
        //    if (obj.TryGetComponent(out IRhythmConfigurableSubDivisionCollider configurableSub))
        //    {
        //        if (Input.GetMouseButtonDown(0)) 
        //        {
        //            chartEditorDataSetter.SetEditMode(EditMode.EditingConfig);
        //            chartEditorDataSetter.SetRhythmConfigurableSubDivision(configurableSub);
        //        }
        //    }
        //}


        /// <summary>
        /// カーソルに乗っかっているオブジェクトを返す
        /// </summary>
        /// <returns></returns>
        public GameObject GetObjectUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
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
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit, rayDistance, raycastLayer)) { return Vector3.zero; }
            return hit.point;
        }
    }

    public interface ICursorInteracter
    {
        GameObject GetObjectUnderCursor();

        Vector3 GetWorldPositionUnderCursor();
    }

}
