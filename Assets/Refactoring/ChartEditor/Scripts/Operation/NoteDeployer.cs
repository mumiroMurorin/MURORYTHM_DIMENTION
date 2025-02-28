using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class NoteDeployer : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform noteParent;
        [SerializeField] GameObject noteObj;

        GameObject deployingNote;
        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Start()
        {
            InstantiateNote(); // 仮
            Bind();
        }

        private void Bind()
        {

        }

        void Update()
        {
            UpdateNotePosition();
            DeployNote();
        }

        /// <summary>
        /// 配置中のノーツの位置を更新する
        /// </summary>
        private void UpdateNotePosition()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.deploy) { return; }

            Transform interactedTransform = GetTransformUnderCursor();
            if (interactedTransform == null) { return; }
            if (deployingNote.transform.position == interactedTransform.position)  { return; }

            deployingNote.transform.position = interactedTransform.position;
            deployingNote.transform.SetParent(interactedTransform);
        }

        /// <summary>
        /// カーソルに乗っかているコライダーのTransformを返す
        /// </summary>
        /// <returns></returns>
        private Transform GetTransformUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit)) { return null; }

            // インタラクトオブジェクト出なければnullを返す
            GameObject hitObject = hit.collider.gameObject;
            if (!hitObject.TryGetComponent(out IInteractableCollider interactable)) { return null; }

            return hitObject.transform;
        }

        /// <summary>
        /// ノーツの配置
        /// </summary>
        private void DeployNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.deploy) { return; }
            if (!Input.GetMouseButtonDown(0)) { return; }

            if (deployingNote.TryGetComponent(out IDeployableObject deployable))
            {
                deployable.OnDeploy();
            }

            InstantiateNote();
        }

        /// <summary>
        /// ノートの生成
        /// </summary>
        private void InstantiateNote()
        {
            deployingNote = Instantiate(noteObj);

            if (deployingNote.TryGetComponent(out IDeployableObject deployable))
            {
                deployable.OnInstantiate();
            }
        }
    }
}

