using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using UniRx;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// ノーツを配置する操作の共通処理 (グラウンド・スペース共通)
    /// カーソル下に仮ノーツを表示し、クリックで譜面に追加する
    /// 派生クラスは「どのコライダーに置けるか」と「ノーツの横幅」を実装する
    /// </summary>
    public abstract class NoteDeployerBase : MonoBehaviour
    {
        // SubclassSelectorを自作クラスの中にいれると上手く動作しないので苦肉の策
        [SerializeField] NoteTypeToNoteObjectList noteList;
        [Tooltip("ノートデータ(抽象クラス)")]
        [SerializeReference, SubclassSelector] IDeployableNoteData[] noteDataList;

        protected IChartEditorDataGetter dataGetter;

        IDeployableObject deployingNote;
        IDeployableNoteData deployingNoteData;
        bool isDeployedTentative;

        /// <summary>
        /// 配置を受け付けるエディットモード
        /// </summary>
        protected abstract EditMode DeployMode { get; }

        /// <summary>
        /// 配置するノーツの横幅(レーン数)
        /// </summary>
        protected abstract int DeployRangeCount { get; }

        /// <summary>
        /// カーソル下の「置ける場所」のコライダーを取得する
        /// </summary>
        protected abstract IInteractableCollider GetDeployColliderUnderCursor();

        /// <summary>
        /// コライダーが「置ける場所」であれば、設置先とアドレスを取り出す
        /// </summary>
        protected abstract bool TryGetDeployLocation(IInteractableCollider collider, out Transform parent, out AddressInChart address);

        /// <summary>
        /// 雛形から複製した仮ノーツのデータを調整する
        /// </summary>
        protected virtual void SetupNoteData(IDeployableNoteData noteData) { }

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
        }

        private void Start()
        {
            Bind();
        }

        protected virtual void Bind()
        {
            // ノーツの削除
            dataGetter.CurrentEditMode
                .Where(editMode => editMode != DeployMode)
                .Subscribe(editMode => DestroyNote())
                .AddTo(this.gameObject);

            // ノーツの仮配置
            dataGetter.InteractableColliders.ObserveAdd()
                .Subscribe(collider =>
                {
                    if (dataGetter.CurrentEditMode.Value != DeployMode) { return; }
                    if (TryGetDeployLocation(collider.Value, out var parent, out _)) { UpdateNotePosition(parent); }
                }).AddTo(this.gameObject);

            // 配置ノーツの種類の変更
            dataGetter.DeploymentNoteType
                .Subscribe(noteType =>
                {
                    if (dataGetter.CurrentEditMode.Value != DeployMode) { return; }
                    DestroyNote();
                })
                .AddTo(this.gameObject);
        }

        void Update()
        {
            // 配置モードでない際は返す
            if (dataGetter.CurrentEditMode.Value != DeployMode) { return; }

            // 左クリックで配置
            if (Input.GetMouseButtonDown(0)) { DeployNoteOnClick(); }
        }

        /// <summary>
        /// 配置中のノーツの位置を更新する
        /// </summary>
        private void UpdateNotePosition(Transform deployParent)
        {
            // 配置ノーツが無かったら新規インスタンス化
            if (deployingNote == null) { SpawnNewNote(dataGetter.DeploymentNoteType.Value); }
            // それでもなかったら返す
            if (deployingNote == null) { return; }

            deployingNote.OnMove(deployParent);
            isDeployedTentative = true;
        }

        /// <summary>
        /// 左クリックによるノーツの配置
        /// </summary>
        private void DeployNoteOnClick()
        {
            // カーソルがUI上にあるときは配置しない (ボタンやプレビューを押したとき、裏のレーンに置いてしまわないように)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return; }

            var collider = GetDeployColliderUnderCursor();

            if (collider == null) { return; }
            if (!TryGetDeployLocation(collider, out _, out var address)) { return; }
            if (!isDeployedTentative) { return; }
            if (deployingNoteData == null) { return; }

            deployingNoteData.SetAddress(new AddressWithinRange(address, DeployRangeCount));
            var deployedData = deployingNoteData;

            // データ上の追加
            Record(() => {
                dataGetter.ChartData.Value.AddNote(deployedData);
            },
            // Undoで削除
            () => {
                dataGetter.ChartData.Value.RemoveNote(deployedData);
            });

            Debug.Log($"【配置】:\n{deployedData.Address}");

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

            var noteData = deployingNoteData;
            deployingNote = NoteObjectFactory.Instantiate(noteList, noteData, GetNoteParentTransform);
            if (deployingNote != null)
            {
                deployingNote.OnDestroyListner += () => OnDestroyDeployingNote(noteData);
            }

            isDeployedTentative = false;
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

        /// <summary>
        /// 雛形を複製して、配置用のノーツデータを作る
        /// </summary>
        private IDeployableNoteData GetNoteData(DeploymentNoteType noteType)
        {
            foreach (var data in noteDataList)
            {
                if (data.NoteType == noteType)
                {
                    var copy = data.Copy();
                    SetupNoteData(copy);
                    return copy;
                }
            }

            Debug.LogWarning($"【System】{noteType}に対応する抽象クラスがありません");
            return null;
        }
    }
}
