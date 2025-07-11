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
        // SubclassSelectorを自作クラスの中にいれると上手く動作しないので苦肉の策
        [SerializeField] NoteTypeToNoteObjectList noteList;
        [Tooltip("ノートデータ(抽象クラス)")]
        [SerializeReference, SubclassSelector] IDeployableNoteData[] noteDataList;

        IChartEditorDataGetter dataGetter;

        IDeployableObject deployingNote;
        IDeployableNoteData deployingNoteData;
        bool isDeployedTentative;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
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
                    if (dataGetter.CurrentEditMode.Value != EditMode.Deploy) { return; }
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
            if (deployingNoteData == null) { return; }

            // データ上の追加
            var address = collider.Address;
            deployingNoteData.SetAddress(new AddressWithinRange(address, 1));
            dataGetter.ChartData.Value.AddNote(deployingNoteData);

            DestroyNote();
            SpawnNewNote(dataGetter.DeploymentNoteType.Value);
        }

        /// <summary>
        /// ノートの生成
        /// </summary>
        private void SpawnNewNote(DeploymentNoteType noteType)
        {
            deployingNoteData = GetNoteData(noteType);
            if (deployingNoteData == null) { return; }

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
            if (noteData is IChainNoteData chainData)
            {
                chainData.SetNoteObject(obj.GetComponent<IConnectableObject>());
            }

            deployable.OnInstantiate(noteData, GetNoteParentTransform);
            deployable.OnDestroyListner += () => OnDestroyDeployingNote(noteData);

            return deployable;
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
            foreach (var data in noteDataList)
            {
                if (data.NoteType == noteType)
                {
                    return data.Copy();
                }
            }

            Debug.LogWarning($"【System】{noteType}に対応する抽象クラスがありません");
            return null;
        }

    }
}

