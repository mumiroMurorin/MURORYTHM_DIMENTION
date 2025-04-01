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

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Update()
        {
            UpdateObjectUnderCursor();
            //SetEditorMode();
        }

        /// <summary>
        /// エディットモードの更新
        /// </summary>
        private void SetEditorMode()
        {
            EditMode raycastEditMode = GetEditModeUnderCursor();

            if (raycastEditMode == EditMode.None) { return; }

            chartEditorDataSetter.SetEditMode(raycastEditMode);
        }

        /// <summary>
        /// カーソルに乗っかっているコライダーのエディットモードを返す
        /// </summary>
        /// <returns></returns>
        private EditMode GetEditModeUnderCursor()
        {
            GameObject hitObject = GetObjectUnderCursor();

            // 同じく
            if(!hitObject.TryGetComponent(out IInteractableCollider interactable)) { return EditMode.None; }

            return interactable.GetEditMode();
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
                chartEditorDataSetter.SetScalableObject(null);
                chartEditorDataSetter.SetDestroyableObject(null);

                return;
            }

            // ノーツ配置場所の更新
            if(obj.TryGetComponent(out IDeployableCollider deployable))
            {
                chartEditorDataSetter.SetDeployableCollider(deployable);
            }
            else
            {
                chartEditorDataSetter.SetDeployableCollider(null);
            }

            // インタラクトされているノーツの更新 (移動)
            if (obj.TryGetComponent(out IMovableCollider movable))
            {
                chartEditorDataSetter.SetMovableObject(movable.Note);
            }
            else
            {
                chartEditorDataSetter.SetMovableObject(null);
            }

            // インタラクトされているノーツの更新 (拡大縮小)
            if (obj.TryGetComponent(out IScalableCollider scalable))
            {
                chartEditorDataSetter.SetScalableObject(scalable.Note);
            }
            else
            {
                chartEditorDataSetter.SetScalableObject(null);
            }

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

        /// <summary>
        /// カーソルに乗っかっているオブジェクトを返す
        /// </summary>
        /// <returns></returns>
        public GameObject GetObjectUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit)) { return null; }
            return hit.collider.gameObject;
        }
    }

    public interface ICursorInteracter
    {
        GameObject GetObjectUnderCursor();
    }

}
