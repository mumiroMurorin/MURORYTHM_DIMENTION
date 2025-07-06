using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using ChartEditor;

namespace ChartEditor
{
    public class NoteDeployer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        // SubclassSelectorを自作クラスの中にいれると上手く動作しないので苦肉の策
        [Tooltip("ノートデータ(抽象クラス)")]
        [SerializeReference, SubclassSelector] IDeployableNoteData[] noteDataList;
        [SerializeField] NoteTypeToNoteObjectList noteList;
        [SerializeField] NoteObjectsController noteObjectsController;

        IDeployableObject deployingNote;
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
                .Where(editMode => editMode != EditMode.Deploy)
                .Subscribe(editMode => DestroyNote())
                .AddTo(this.gameObject);

            // ノーツの仮配置
            chartEditorDataGetter.InteractableColliders.ObserveAdd()
                .Subscribe(collider =>
                {
                    if (collider.Value is IDeployableCollider matched) { UpdateNotePosition(matched); }
                }).AddTo(this.gameObject);

            // 配置ノーツの種類の変更
            chartEditorDataGetter.DeploymentNoteType
                .Subscribe(noteType =>
                {
                    if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Deploy) { return; }
                    DestroyNote();
                })
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
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Deploy) { return; }
            if (deployable == null) { return; }

            // 配置ノーツが無かったら新規インスタンス化
            if (deployingNote == null) { InstantiateNote(); }
            // それでもなかったら返す
            if (deployingNote == null) { return; }

            deployingNote.OnMove(deployable.deployParent);
            isDeployedTentative = true;
        }

        /// <summary>
        /// ノーツの配置
        /// </summary>
        private void DeployNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Deploy) { return; }
            var collider = chartEditorDataGetter.GetInteractableCollider<IDeployableCollider>();

            if (collider == null) { return; }
            if (!isDeployedTentative) { return; }
            // 謎のヌルリファによりこの条件も追加
            if (deployingNoteData == null) { return; }

            // データ上の追加
            AddressInChart address = collider.Address;
            deployingNoteData.SetAddress(new AddressWithinRange(address, 1));
            chartEditorDataGetter.ChartData.Value.AddNote(deployingNoteData);
            
            // オブジェクトの設置
            deployingNote.OnDeploy();

            noteObjectsController.AddNote(deployingNoteData, deployingNote.Note);

            InstantiateNote();
        }

        /// <summary>
        /// ノートの生成
        /// </summary>
        private void InstantiateNote()
        {
            GameObject origin = noteList.GetNote(chartEditorDataGetter.DeploymentNoteType.Value);
            if (origin == null) { return; }

            GameObject obj = Instantiate(origin);

            if (!obj.TryGetComponent(out IDeployableObject deployable))
            {
                Debug.LogWarning("ノーツにIDeployableObjectがくっついてねぇぞ！");
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
            GameObject origin = noteList.GetNote(noteData.NoteType);
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
            Transform parent = chartEditorDataGetter.ChartData.Value.GetPlacementLocation(new AddressInChart(noteData.Address));
            deployable.OnMove(parent);
            deployable.OnDeploy();

            noteObjectsController.AddNote(noteData, deployable.Note);
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

        private Transform GetNoteParentTransform(AddressWithinRange address)
        {
            return chartEditorDataGetter.ChartData.Value.GetPlacementLocation(new AddressInChart(address));
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

    [System.Serializable]
    public class NoteTypeToNoteObjectList
    {
        [SerializeField] NoteTypeToNoteObject[] notePrefabs;

        /// <summary>
        /// 引数に対応するノーツを返す
        /// </summary>
        /// <param name="noteType"></param>
        /// <returns></returns>
        public GameObject GetNote(DeploymentNoteType noteType)
        {
            foreach (var note in notePrefabs)
            {
                if (noteType == note.DeploymentNoteType) { return note.NoteObject; }
            }

            //Debug.LogWarning($"対応するノーツが存在しませんでした: {noteType}");
            return null;
        }
    }

    [System.Serializable]
    public class NoteTypeToNoteObject
    {
        [SerializeField] DeploymentNoteType noteType;
        [SerializeField] GameObject noteObject;

        public DeploymentNoteType DeploymentNoteType { get { return noteType; } }

        public GameObject NoteObject { get { return noteObject; } }
    }
}

