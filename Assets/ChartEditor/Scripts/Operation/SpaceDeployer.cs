using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using ChartEditor;

namespace ChartEditor
{
    public class SpaceDeployer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        // SubclassSelectorを自作クラスの中にいれると上手く動作しないので苦肉の策
        [Tooltip("ノートデータ(抽象クラス)")]
        [SerializeReference, SubclassSelector] IDeployableNoteData[] noteDataList;
        [SerializeField] NoteTypeToNoteObject[] notes;

        IFreedomDeployableObject deployingNote;
        IChartEditorDataGetter chartEditorDataGetter;
        IDeployableNoteData deployingNoteData;
        bool isDeployedTentative;

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
            // ノーツの削除
            chartEditorDataGetter.CurrentEditMode
                .Where(editMode => editMode != EditMode.SpaceDeploy)
                .Subscribe(editMode => DestroyNote())
                .AddTo(this.gameObject);

            // ノーツの仮配置
            chartEditorDataGetter.FreedomDeployableCollider
                .Subscribe(UpdateNotePosition)
                .AddTo(this.gameObject);

            // 配置ノーツの種類の変更
            chartEditorDataGetter.DeploymentNoteType
                .Subscribe(noteType =>
                {
                    if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.SpaceDeploy) { return; }
                    DestroyNote();
                })
                .AddTo(this.gameObject);
        }

        void Update()
        {
            var collider = chartEditorDataGetter.FreedomDeployableCollider.Value;
            if (collider != null) { UpdateNotePosition(collider); }
            if (Input.GetMouseButtonDown(0)) { DeployNote(); }
        }

        /// <summary>
        /// 配置中のノーツの位置を更新する
        /// </summary>
        private void UpdateNotePosition(IFreedomDeployableCollider deployable)
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.SpaceDeploy) { return; }
            if (deployable == null) { return; }

            // 配置ノーツが無かったら新規インスタンス化
            if (deployingNote == null) { InstantiateNote(); }
            // それでもなかったら返す
            if (deployingNote == null) { return; }

            Vector3 pos = new Vector3(cursorInteracter.Value.GetWorldPositionUnderCursor().x, deployable.deployParent.position.y, deployable.deployParent.position.z);
            deployingNote.OnMove(deployable.deployParent, pos);
            isDeployedTentative = true;
        }

        /// <summary>
        /// ノーツの配置
        /// </summary>
        private void DeployNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.SpaceDeploy) { return; }
            if (chartEditorDataGetter.FreedomDeployableCollider.Value == null) { return; }
            if (!isDeployedTentative) { return; }
            // 謎のヌルリファによりこの条件も追加
            if(deployingNoteData == null) { return; }

            // データ上の追加
            AddressInChart address = chartEditorDataGetter.FreedomDeployableCollider.Value.Address;
            chartEditorDataGetter.ChartData.Value.AddNote(deployingNoteData, address);
            
            // オブジェクトの設置
            deployingNote.OnDeploy();
            InstantiateNote();
        }

        /// <summary>
        /// ノートの生成
        /// </summary>
        private void InstantiateNote()
        {
            GameObject origin = GetNote(chartEditorDataGetter.DeploymentNoteType.Value);
            if(origin == null) { return; }

            GameObject obj = Instantiate(origin);

            if (!obj.TryGetComponent(out IFreedomDeployableObject deployable))
            {
                Debug.LogWarning("ノーツにIFreedomDeployableObjectがくっついてねぇぞ！");
                return;
            }

            deployingNoteData = GetNoteData(chartEditorDataGetter.DeploymentNoteType.Value);

            // チェインノーツのときデータセット
            if(deployingNoteData is IChainNoteData)
            {
                ((IChainNoteData)deployingNoteData).SetNoteObject(obj.GetComponent<IConnectableObject>());
            }

            deployable.OnInstantiate(deployingNoteData, GetNoteParentTransform);
            deployable.OnDestroyListner += () => OnDestroyDeployingNote();

            deployingNote = deployable;
            isDeployedTentative = false;
        }

        /// <summary>
        /// 外部データから配置
        /// </summary>
        /// <param name="groundNoteData"></param>
        public void DeployForNoteData(IDeployableNoteData noteData)
        {
            GameObject origin = GetNote(chartEditorDataGetter.DeploymentNoteType.Value);
            if (origin == null) { return; }

            GameObject obj = Instantiate(origin);

            if (!obj.TryGetComponent(out IDeployableObject deployable))
            {
                Debug.LogWarning("ノーツにIDeployableObjectがくっついてねぇぞ！");
                return;
            }

            // チェインノーツのときデータセット
            if (noteData is IChainNoteData)
            {
                ((IChainNoteData)noteData).SetNoteObject(obj.GetComponent<IConnectableObject>());
            }

            deployable.OnInstantiate(noteData, GetNoteParentTransform);
            deployable.OnDestroyListner += () => OnDestroyDeployingNote();

            // 配置
            Transform parent = chartEditorDataGetter.ChartData.Value.GetPlacementLocation(noteData.Address);
            deployable.OnMove(parent);
            deployable.OnDeploy();
        }

        /// <summary>
        /// ノートの削除
        /// </summary>
        private void DestroyNote()
        {
            if (deployingNote == null) { return; }
            deployingNote.OnDisable();
        }

        private void OnDestroyDeployingNote()
        {
            deployingNote = null;
            deployingNoteData = null;
        }

        /// <summary>
        /// 引数に対応するノーツを返す
        /// </summary>
        /// <param name="noteType"></param>
        /// <returns></returns>
        private GameObject GetNote(DeploymentNoteType noteType)
        {
            foreach(var note in notes)
            {
                if(noteType == note.DeploymentNoteType) { return note.NoteObject; }
            }

            //Debug.LogWarning($"対応するノーツが存在しませんでした: {noteType}");
            return null;
        }

        private Transform GetNoteParentTransform(AddressInChart address)
        {
            return chartEditorDataGetter.ChartData.Value.GetPlacementLocation(address);
        }

        private IDeployableNoteData GetNoteData(DeploymentNoteType noteType)
        {
            foreach(var data in noteDataList)
            {
                if(data.NoteType == noteType) 
                {
                    return data.Copy();
                }
            }

            Debug.LogWarning($"【System】{noteType}に対応する抽象クラスがありません");
            return null;
        }

    }
}

