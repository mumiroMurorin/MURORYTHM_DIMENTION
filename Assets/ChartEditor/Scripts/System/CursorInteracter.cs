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

        IChartEditorDataSetter chartEditorDataSetter;
        IChartEditorDataGetter chartEditorDataGetter;

        int raycastLayer;

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataSetter = chartEditorDataSetter;
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Start()
        {
            raycastLayer = 1 << LayerMask.NameToLayer("CursorInteractable");
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

            // カーソル下に何もないときは無効
            if (raycastEditMode == EditMode.None) { return; }
            // コンフィグエディット中は無効
            if (chartEditorDataGetter.CurrentEditMode.Value == EditMode.EditingConfig) { return; }
            // 削除モード中は無効
            if (chartEditorDataGetter.CurrentEditMode.Value == EditMode.Destroy) { return; }
            // ノーツ接続中は無効
            if (chartEditorDataGetter.CurrentEditMode.Value == EditMode.Connecting) { return; }
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
            
            if(obj == null) 
            {
                chartEditorDataSetter.SetDeployableCollider(null);
                chartEditorDataSetter.SetMovableObject(null);
                chartEditorDataSetter.SetScalableObject(null, chartEditorDataGetter.IsRightAnchored);
                chartEditorDataSetter.SetDestroyableObject(null);
                chartEditorDataSetter.SetConnectableObject(null);

                return;
            }

            UpdateDeployableObject(obj);
            UpdateMovableObject(obj);
            UpdateScalableObject(obj);
            UpdateConnectableObject(obj);
            UpdateDestroyableObject(obj);
            UpdateRhythmConfigurableCollider(obj);
        }


        #region UpdateObjectFunc

        private void UpdateDeployableObject(GameObject obj)
        {
            // ノーツ配置場所の更新
            if (obj.TryGetComponent(out IDeployableCollider deployable))
            {
                chartEditorDataSetter.SetDeployableCollider(deployable);
            }
            else
            {
                chartEditorDataSetter.SetDeployableCollider(null);
            }
        }

        private void UpdateMovableObject(GameObject obj)
        {
            // インタラクトされているノーツの更新 (移動)
            if (obj.TryGetComponent(out IMovableCollider movable))
            {
                chartEditorDataSetter.SetMovableObject(movable.Note);
            }
            else
            {
                chartEditorDataSetter.SetMovableObject(null);
            }
        }

        private void UpdateScalableObject(GameObject obj)
        {
            // インタラクトされているノーツの更新 (拡大縮小)
            if (obj.TryGetComponent(out IScalableCollider scalable))
            {
                chartEditorDataSetter.SetScalableObject(scalable.Note, !scalable.IsRightEdge);
            }
            else
            {
                chartEditorDataSetter.SetScalableObject(null, chartEditorDataGetter.IsRightAnchored);
            }
        }

        private void UpdateConnectableObject(GameObject obj)
        {
            // インタラクトされているノーツの更新 (拡大縮小)
            if (obj.TryGetComponent(out IConnectableCollider connectable))
            {
                chartEditorDataSetter.SetConnectableObject(connectable.Note);
            }
            else
            {
                chartEditorDataSetter.SetConnectableObject(null);
            }
        }

        private void UpdateDestroyableObject(GameObject obj)
        {
            // インタラクトされているノーツの更新 (削除)
            if (obj.TryGetComponent(out IDestroyableCollider destroyable))
            {
                chartEditorDataSetter.SetDestroyableObject(destroyable.Note);
            }
            else
            {
                chartEditorDataSetter.SetDestroyableObject(null);
            }
        }

        private void UpdateRhythmConfigurableCollider(GameObject obj)
        {
            if (chartEditorDataGetter.CurrentEditMode.Value == EditMode.EditingConfig) { return; }

            // インタラクトされているRhythmConfigurableColliderの更新
            // あんまりよくないけどクリック処理もここでやっちゃう
            // 小節線
            if (obj.TryGetComponent(out IRhythmConfigurableBarCollider configurableBar))
            {
                if (Input.GetMouseButtonDown(0)) 
                {
                    chartEditorDataSetter.SetEditMode(EditMode.EditingConfig);
                    chartEditorDataSetter.SetRhythmConfigurableBar(configurableBar);
                }
            }

            // 分線
            if (obj.TryGetComponent(out IRhythmConfigurableSubDivisionCollider configurableSub))
            {
                if (Input.GetMouseButtonDown(0)) 
                {
                    chartEditorDataSetter.SetEditMode(EditMode.EditingConfig);
                    chartEditorDataSetter.SetRhythmConfigurableSubDivision(configurableSub);
                }
            }
        }

        #endregion


        /// <summary>
        /// カーソルに乗っかっているオブジェクトを返す
        /// </summary>
        /// <returns></returns>
        public GameObject GetObjectUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit, 20f, raycastLayer)) { return null; }
            return hit.collider.gameObject;
        }
    }

    public interface ICursorInteracter
    {
        GameObject GetObjectUnderCursor();
    }

}
