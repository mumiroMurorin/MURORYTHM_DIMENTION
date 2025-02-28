using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class NoteDeployer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
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
            Bind();
        }

        private void Bind()
        {
            chartEditorDataGetter.CurrentEditMode
                .Subscribe(editMode => ActiveNote(editMode == EditMode.deploy))
                .AddTo(this.gameObject);
        }

        void Update()
        {
            UpdateNotePosition();
            if (Input.GetMouseButtonDown(0)) { DeployNote(); }
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
            // インタラクトオブジェクト出なければnullを返す
            GameObject hitObject = cursorInteracter.Value.GetObjectUnderCursor();
            if (hitObject == null) { return null; }
            if (!hitObject.TryGetComponent(out IDeployableCollider deployable)) { return null; }

            return hitObject.transform;
        }

        /// <summary>
        /// ノーツの配置
        /// </summary>
        private void DeployNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.deploy) { return; }
            if (GetTransformUnderCursor() == null) { return; }

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

        /// <summary>
        /// ノートの(非)アクティブ化
        /// </summary>
        /// <param name="isActive"></param>
        private void ActiveNote(bool isActive)
        {
            if(deployingNote == null) { InstantiateNote(); }
            deployingNote?.SetActive(isActive);
        }
    }
}

