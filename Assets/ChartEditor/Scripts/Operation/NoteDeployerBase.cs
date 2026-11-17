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
        // ノーツの上に置く種類 (ダイナミックノーツ) で、今、仮ノーツを出しているノーツ
        IDeployableNoteData hostUnderCursor;

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

        /// <summary>
        /// レーンのマスではなく、ほかのノーツの上に置く種類か (ダイナミックノーツ)
        /// </summary>
        protected virtual bool IsHostPlacement(DeploymentNoteType noteType) => false;

        /// <summary>
        /// ノーツの上に置く種類のとき: カーソルの下の、置き先のノーツ (置けないときは null と、その理由。ノーツが無いときは理由も null)
        /// </summary>
        protected virtual IDeployableNoteData FindHostUnderCursor(out string refusal)
        {
            refusal = null;
            return null;
        }

        /// <summary>
        /// ノーツの上に置く種類のとき: 置き先のノーツに、ノーツを置く (元に戻せるように登録する)
        /// </summary>
        /// <returns>置いたとき true</returns>
        protected virtual bool DeployOnHost(IDeployableNoteData host, IDeployableNoteData noteData) => false;

        /// <summary>
        /// ノーツの上に置く種類のとき: 何も無いレーンのマスをクリックしたときに、ログに出す文
        /// </summary>
        protected virtual string HostRequiredMessage => string.Empty;

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
                    // ホールドを1本だけ編集している間は、仮ノーツも出さない
                    if (HoldIsolation.IsActive) { return; }
                    // 置くノーツを選んでいない間も、出さない (録音 20261118_010533)
                    if (DeploymentNoteChoice.IsNone) { return; }
                    // ノーツの上に置く種類は、レーンのマスには出さない (Update で、カーソルの下のノーツに出す)
                    if (IsHostPlacement(dataGetter.DeploymentNoteType.Value)) { return; }
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

            // ホールドを1本だけ編集している間は、どのノーツも置けない
            // (配置のツールを、ボタンやショートカットキーで選んだときも同じ。出ている仮ノーツは消す)
            if (HoldIsolation.IsActive)
            {
                DestroyNote();
                return;
            }

            // 置くノーツを選んでいない間は、何も置かない (仮ノーツも消す。録音 20261118_010533)
            if (DeploymentNoteChoice.IsNone)
            {
                DestroyNote();
                return;
            }

            // ノーツの上に置く種類 (ダイナミックノーツ): カーソルの下のノーツに仮ノーツを出し、クリックでそのノーツに載せる
            if (IsHostPlacement(dataGetter.DeploymentNoteType.Value))
            {
                UpdateHostGhost();
                if (Input.GetMouseButtonDown(0) && !RangeSelectGesture.IsForced(dataGetter)) { DeployOnHostOnClick(); }
                return;
            }

            // 左クリックで配置 (Shift を押しながらのドラッグは範囲選択なので、置かない)
            if (Input.GetMouseButtonDown(0) && !RangeSelectGesture.IsForced(dataGetter)) { DeployNoteOnClick(); }
        }

        /// <summary>
        /// ノーツの上に置く種類: カーソルの下の、置けるノーツに仮ノーツを重ねる (幅も、そのノーツと同じ)
        /// </summary>
        private void UpdateHostGhost()
        {
            var host = FindHostUnderCursor(out _);
            if (host == null)
            {
                hostUnderCursor = null;
                DestroyNote();
                return;
            }

            if (deployingNote == null) { SpawnNewNote(dataGetter.DeploymentNoteType.Value); }
            if (deployingNote == null || deployingNoteData == null) { return; }

            if (!DynamicNoteLinker.IsSameAddress(host.Address, deployingNoteData.Address)) { deployingNoteData.SetAddress(host.Address); }

            var parent = GetNoteParentTransform(host.Address);
            if (parent == null) { return; }

            deployingNote.OnMove(parent);
            hostUnderCursor = host;
            isDeployedTentative = true;
        }

        /// <summary>
        /// ノーツの上に置く種類: クリックしたノーツに載せる
        /// </summary>
        private void DeployOnHostOnClick()
        {
            // カーソルがUI上にあるときは配置しない
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return; }

            var host = FindHostUnderCursor(out string refusal);
            if (host == null)
            {
                // ノーツの上だが置けない (HoldMeshRelay など) ときは、その理由。何も無いレーンのマスのときは、置ける場所
                if (!string.IsNullOrEmpty(refusal)) { Debug.Log($"【配置】{refusal}"); }
                else if (GetDeployColliderUnderCursor() != null && !string.IsNullOrEmpty(HostRequiredMessage)) { Debug.Log($"【配置】{HostRequiredMessage}"); }
                return;
            }
            if (!isDeployedTentative || deployingNoteData == null) { return; }

            if (!DeployOnHost(host, deployingNoteData)) { return; }

            hostUnderCursor = null;
            DestroyNote();
            SpawnNewNote(dataGetter.DeploymentNoteType.Value);
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
