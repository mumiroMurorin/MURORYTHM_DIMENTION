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
        IChartEditorDataGetter dataGetter;
        INotesDataSetter notesSetter;
        IDeployableNoteData deployingNoteData;
        bool isDeployedTentative;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataSetter notesSetter)
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
                .Where(editMode => editMode != EditMode.SpaceDeploy)
                .Subscribe(editMode => DestroyNote())
                .AddTo(this.gameObject);

            // ノーツの仮配置
            dataGetter.InteractableColliders.ObserveAdd()
                .Subscribe(collider =>
                {
                    if (collider.Value is IFreedomDeployableCollider matched) { UpdateNotePosition(matched); }
                }).AddTo(this.gameObject);

            // 配置ノーツの種類の変更
            dataGetter.DeploymentNoteType
                .Subscribe(noteType =>
                {
                    if (dataGetter.CurrentEditMode.Value != EditMode.SpaceDeploy) { return; }
                    DestroyNote();
                })
                .AddTo(this.gameObject);
        }

        void Update()
        {
            var collider = dataGetter.GetInteractableCollider<IFreedomDeployableCollider>();

            if (collider != null) { UpdateNotePosition(collider); }
            if (Input.GetMouseButtonDown(0)) { DeployNote(); }
        }

        /// <summary>
        /// 配置中のノーツの位置を更新する
        /// </summary>
        private void UpdateNotePosition(IFreedomDeployableCollider deployable)
        {
            // 配置モードでない際は返す
            if (dataGetter.CurrentEditMode.Value != EditMode.SpaceDeploy) { return; }
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
            if (dataGetter.CurrentEditMode.Value != EditMode.SpaceDeploy) { return; }

            var collider = dataGetter.GetInteractableCollider<IFreedomDeployableCollider>();
            if (collider == null) { return; }

            if (!isDeployedTentative) { return; }
            // 謎のヌルリファによりこの条件も追加
            if(deployingNoteData == null) { return; }

            // データ上の追加
            AddressInChart address = collider.Address;
            deployingNoteData.SetAddress(new AddressWithinRange(address, 1));
            notesSetter.AddDataToNoteObject(deployingNoteData, deployingNote.Note);

            // オブジェクトの設置
            deployingNote.OnDeploy();
            InstantiateNote();
        }

        /// <summary>
        /// ノートの生成
        /// </summary>
        private void InstantiateNote()
        {
            GameObject origin = GetNote(dataGetter.DeploymentNoteType.Value);
            if(origin == null) { return; }

            GameObject obj = Instantiate(origin);

            if (!obj.TryGetComponent(out IFreedomDeployableObject deployable))
            {
                Debug.LogWarning("ノーツにIFreedomDeployableObjectがくっついてねぇぞ！");
                return;
            }

            deployingNoteData = GetNoteData(dataGetter.DeploymentNoteType.Value);

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
            GameObject origin = GetNote(noteData.NoteType);
            if (origin == null) { return; }

            GameObject obj = Instantiate(origin);

            if (!obj.TryGetComponent(out IFreedomDeployableObject deployable))
            {
                Debug.LogWarning("ノーツにIFreedomDeployableObjectがくっついてねぇぞ！");
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
            Transform parent = dataGetter.ChartData.Value.GetPlacementLocation(new AddressInChart(noteData.Address));
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
}

