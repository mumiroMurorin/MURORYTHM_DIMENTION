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

        IChartEditorDataGetter dataGetter;
        INotesDataSetter notesSetter;

        IDeployableObject deployingNote;
        IDeployableNoteData deployingNoteData;
        bool isDeployedTentative;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter,INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.notesSetter = notesSetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // ノーツの削除
            dataGetter.CurrentEditMode
                .Where(editMode => editMode != EditMode.Deploy)
                .Subscribe(editMode => DestroyNote())
                .AddTo(this.gameObject);

            // ノーツの仮配置
            dataGetter.InteractableColliders.ObserveAdd()
                .Subscribe(collider =>
                {
                    if (collider.Value is IDeployableCollider matched) { UpdateNotePosition(matched); }
                }).AddTo(this.gameObject);

            // 配置ノーツの種類の変更
            dataGetter.DeploymentNoteType
                .Subscribe(noteType =>
                {
                    if (dataGetter.CurrentEditMode.Value != EditMode.Deploy) { return; }
                    DestroyNote();
                })
                .AddTo(this.gameObject);
        }

        void Update()
        {
            // 配置モードでない際は返す
            if (dataGetter.CurrentEditMode.Value != EditMode.Deploy) { return; }

            // 左クリックで配置
            if (Input.GetMouseButtonDown(0)) { DeployNoteOnClick(); }
        }

        /// <summary>
        /// 配置中のノーツの位置を更新する
        /// </summary>
        private void UpdateNotePosition(IDeployableCollider deployable)
        {
            // 配置モードでない際は返す
            if (dataGetter.CurrentEditMode.Value != EditMode.Deploy) { return; }
            if (deployable == null) { return; }

            // 配置ノーツが無かったら新規インスタンス化
            if (deployingNote == null) { SpawnNewNote(dataGetter.DeploymentNoteType.Value); }
            // それでもなかったら返す
            if (deployingNote == null) { return; }

            deployingNote.OnMove(deployable.deployParent);
            isDeployedTentative = true;
        }

        /// <summary>
        /// 左クリックによるノーツの配置
        /// </summary>
        private void DeployNoteOnClick()
        {
            var collider = dataGetter.GetInteractableCollider<IDeployableCollider>();

            if (collider == null) { return; }
            if (!isDeployedTentative) { return; }
            // 謎のヌルリファによりこの条件も追加
            if (deployingNoteData == null) { return; }

            // データ上の追加
            AddressInChart address = collider.Address;
            deployingNoteData.SetAddress(new AddressWithinRange(address, 1));
            notesSetter.AddDataToNoteObject(deployingNoteData, deployingNote.Note);

            // オブジェクトの設置
            deployingNote.OnDeploy();

            SpawnNewNote(dataGetter.DeploymentNoteType.Value);
        }

        /// <summary>
        /// ノートの生成
        /// </summary>
        private void SpawnNewNote(DeploymentNoteType noteType)
        {
            deployingNoteData = GetNoteData(noteType);
            deployingNote = InstantiateNoteObject(deployingNoteData);

            isDeployedTentative = false;
        }

        private IDeployableObject InstantiateNoteObject(IDeployableNoteData noteData)
        {
            GameObject origin = noteList.GetNote(noteData.NoteType);
            if (origin == null) { return null; }

            GameObject obj = Instantiate(origin);

            if (!obj.TryGetComponent(out IDeployableObject deployable))
            {
                Debug.LogWarning("ノーツにIDeployableObjectがくっついてねぇぞ！");
                return null;
            }

            // チェインノーツのときデータセット
            if (noteData is IChainNoteData)
            {
                ((IChainNoteData)noteData).SetNoteObject(obj.GetComponent<IConnectableObject>());
            }

            deployable.OnInstantiate(noteData, GetNoteParentTransform);
            deployable.OnDestroyListner += () => OnDestroyDeployingNote(noteData);

            return deployable;
        }

        /// <summary>
        /// 外部データから配置
        /// </summary>
        /// <param name="groundNoteData"></param>
        public void DeployForNoteData(IDeployableNoteData noteData)
        {
            var obj = InstantiateNoteObject(noteData);
            if (obj == null) { return; }

            // 配置
            Transform parent = dataGetter.ChartData.Value.GetPlacementLocation(new AddressInChart(noteData.Address));
            obj.OnMove(parent);
            obj.OnDeploy();

            notesSetter.AddDataToNoteObject(noteData, obj.Note);
        }


        /// <summary>
        /// ノートの削除
        /// </summary>
        private void DestroyNote()
        {
            if (deployingNote == null) { return; }
            deployingNote.OnDisable();
        }

        private void OnDestroyDeployingNote(IDeployableNoteData noteData)
        {
            if (noteData != deployingNoteData) { return; }

            deployingNote = null;
            deployingNoteData = null;
        }

        private Transform GetNoteParentTransform(IReadOnlyAddressWithinRange address)
        {
            return dataGetter.ChartData.Value.GetPlacementLocation(new AddressInChart(address));
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

