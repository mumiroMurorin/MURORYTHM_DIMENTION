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

        IDeployableObject deployingNote;
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
            // ノーツの出現
            chartEditorDataGetter.CurrentEditMode
                .Where(editMode => editMode == EditMode.deploy)
                .Subscribe(editMode => InstantiateNote())
                .AddTo(this.gameObject);

            // ノーツの削除
            chartEditorDataGetter.CurrentEditMode
                .Where(editMode => editMode != EditMode.deploy)
                .Subscribe(editMode => DestroyNote())
                .AddTo(this.gameObject);

            // ノーツの仮配置
            chartEditorDataGetter.DeployableCollider
                .Subscribe(collider => UpdateNotePosition(collider))
                .AddTo(this.gameObject);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) { DeployNote(); }
        }

        /// <summary>
        /// 配置中のノーツの位置を更新する
        /// </summary>
        private void UpdateNotePosition(IDeployableCollider deployable)
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.deploy) { return; }
            if (deployable == null) { return; }

            deployingNote.OnMove(deployable.transform);
        }

        /// <summary>
        /// ノーツの配置
        /// </summary>
        private void DeployNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.deploy) { return; }
            if (chartEditorDataGetter.DeployableCollider.Value == null) { return; }

            deployingNote.OnDeploy();
            InstantiateNote();
        }

        /// <summary>
        /// ノートの生成
        /// </summary>
        private void InstantiateNote()
        {
            GameObject obj = Instantiate(noteObj);
            if(!obj.TryGetComponent(out IDeployableObject deployable))
            {
                Debug.LogWarning("ノーツにIDeployableObjectがくっついてねぇぞ！");
                return;
            }

            deployable.OnInstantiate();

            deployingNote = deployable;
        }

        /// <summary>
        /// ノートの削除
        /// </summary>
        private void DestroyNote()
        {
            if (deployingNote == null) { return; }
            deployingNote.OnDisable();
            deployingNote = null;
        }
    }
}

