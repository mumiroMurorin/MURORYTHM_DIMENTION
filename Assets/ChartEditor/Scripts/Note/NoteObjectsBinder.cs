using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class NoteObjectsBinder : MonoBehaviour
    {
        [SerializeField] NoteTypeToNoteObjectList noteList;

        [SerializeField] DeploymentNoteType defaultNoteType_ground;
        [SerializeField] DeploymentNoteType defaultNoteType_space;

        [SerializeField] NoteConnecter noteConnecter;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataSetter notesSetter;
        INotesDataGetter notesGetter;

        AddressInChart stackAddress;

        [Inject]
        public void Constructor(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            this.dataSetter = dataSetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 編集レイヤーが変更された時、配置ノートタイプと編集モードを変更する
            dataGetter.EditNoteType
                .Subscribe(type => {
                    switch (type)
                    {
                        case EditNoteType.Ground:
                            if (!dataGetter.TryGetCachedNoteType(EditNoteType.Ground, out var groundType))
                            {
                                groundType = defaultNoteType_ground;
                            }
                            dataSetter.SetNoteType(groundType);
                            dataSetter.SetEditMode(EditMode.Deploy);
                            break;
                        case EditNoteType.Space:
                            if (!dataGetter.TryGetCachedNoteType(EditNoteType.Space, out var spaceType))
                            {
                                spaceType = defaultNoteType_space;
                            }
                            dataSetter.SetNoteType(spaceType);
                            dataSetter.SetEditMode(EditMode.SpaceDeploy);
                            break;
                    }
                })
                .AddTo(this.gameObject);

            // 譜面データが追加された時
            dataGetter?.ChartData
                .Subscribe(chart => {
                    // 頂点編集中だった場合、編集対象が無くなるのでレイヤーを戻す
                    LeaveVerticesLayer();

                    // データクリア
                    notesSetter.ClearDataToNoteObjectList();
                    notesGetter.ClearIndexToChainNoteDataList();
                    noteConnecter.ReleaseAllNotes();

                    // 購読
                    BindForChartData(chart);
                })
                .AddTo(this.gameObject);
        }

        private void BindForChartData(ChartData chartData)
        {
            if (chartData == null) { return; }

            chartData.OnAddNoteListener += OnAddNoteData;
            chartData.OnRemoveNoteListener += OnRemoveNoteData;

            // 小節線データが削除された時
            chartData?.BarDatas.ObserveRemove()
                .Subscribe(bar => {
                    foreach(var sub in bar.Value.SubDivisionDatas)
                    {
                        foreach (var note in sub.NoteDatas)
                        {
                            OnRemoveNoteData(note);
                        }
                    }
                })
                .AddTo(this.gameObject);
        }

        private void BindForNoteAddress(IDeployableNoteData data,GameObject noteObj, CompositeDisposable disposable = default)
        {
            data.Address.BarIndexRP
                .Pairwise()
                .Subscribe(pair => {
                    var oldAddress = new AddressInChart(pair.Previous, data.Address.SubDivisionIndex, data.Address.Range[0]);
                    var newAddress = new AddressInChart(data.Address);

                    if (!dataGetter.ChartData.Value.IsExistAddressInChart(oldAddress))
                    {
                        if (stackAddress == null)
                        {
                            Debug.LogError("【Note】スタックアドレスがありません");
                            return;
                        }

                        oldAddress = stackAddress;
                        stackAddress = null;
                    }
                    if (!dataGetter.ChartData.Value.IsExistAddressInChart(newAddress))
                    {
                        if (stackAddress != null)
                        {
                            Debug.LogError("【Note】スタックアドレスが既に存在します");
                            return;
                        }

                        stackAddress = oldAddress;
                        return;
                    }

                    dataGetter.ChartData.Value.ChangeNoteAddress(data, oldAddress, newAddress);
                })
                .AddTo(this.gameObject)
                .AddTo(noteObj)
                .AddTo(disposable);

            data.Address.SubDivisionIndexRP
                .Pairwise()
                .Subscribe(pair =>
                {
                    var oldAddress = new AddressInChart(data.Address.BarIndex, pair.Previous, data.Address.Range[0]);
                    var newAddress = new AddressInChart(data.Address);

                    if (!dataGetter.ChartData.Value.IsExistAddressInChart(oldAddress))
                    {
                        if (stackAddress == null)
                        {
                            Debug.LogError($"【Note】スタックアドレスがありません: {oldAddress}");
                            return;
                        }

                        oldAddress = stackAddress;
                        stackAddress = null;
                    }
                    if (!dataGetter.ChartData.Value.IsExistAddressInChart(newAddress))
                    {
                        if (stackAddress != null)
                        {
                            Debug.LogError("【Note】スタックアドレスが既に存在します");
                            return;
                        }

                        stackAddress = oldAddress;
                        return;
                    }

                    dataGetter.ChartData.Value.ChangeNoteAddress(data, oldAddress, newAddress);
                })
                .AddTo(this.gameObject)
                .AddTo(noteObj)
                .AddTo(disposable);
        }

        /// <summary>
        /// ノーツがデータ上に追加された時、オブジェクトを配置する
        /// </summary>
        /// <param name="note"></param>
        private void OnAddNoteData(IDeployableNoteData note)
        {
            // オブジェクトのインスタンス化
            // チェインノーツは、データに残っている接続番号で接続し直す
            var obj = NoteObjectFactory.Instantiate(noteList, note, GetNoteParentTransform,
                chainData => noteConnecter.ConnectNote(chainData, chainData.ChainIndex.Value));
            if (obj == null) { return; }

            // 配置
            Transform parent = dataGetter.ChartData.Value.GetPlacementLocation(new AddressInChart(note.Address));
            obj.OnMove(parent);
            obj.OnDeploy();

            // DataToObjリストに追加
            notesSetter.AddDataToNoteObject(note, obj.Note);

            // バインド
            BindForNoteAddress(note, obj.Note.gameObject, notesGetter.GetNoteDisposable(note));
        }

        /// <summary>
        /// ノーツがデータ上から削除された時、オブジェクトを削除する
        /// </summary>
        /// <param name="note"></param>
        private void OnRemoveNoteData(IDeployableNoteData note)
        {
            if (note is System.IDisposable disposable) { disposable.Dispose(); }

            // オブジェクトの削除
            var noteObject = notesGetter.GetNoteObject(note);
            if (noteObject == null || !noteObject.TryGetComponent(out IDestroyableObject destroyableObject)) { return; }

            // コネクトの解除 (接続番号はデータに残すので、Undoで再配置すれば元の接続に戻る)
            if(note is IChainNoteData chainData)
            {
                notesGetter.RemoveChainNote(chainData);
                noteConnecter.ReleaseNote(chainData);
            }

            // 購読の解除
            notesGetter.GetNoteDisposable(note).Dispose();

            // 選択の解除 (オブジェクトとの対応が残っているうちに行う)
            notesSetter.TryRemoveSelectingNotes(note);

            // 頂点編集中のノートだった場合、編集を終了する
            if (note is IVerticesControlableNoteData verticesNote && notesGetter.EditingVertices.Value == verticesNote)
            {
                LeaveVerticesLayer();
                notesSetter.SetEditingVertices(null);
            }

            // データの削除
            notesSetter.RemoveDataToNoteObject(note);

            destroyableObject.OnDestroy();
        }

        /// <summary>
        /// 頂点編集レイヤーにいる場合、スペースレイヤーへ戻す
        /// </summary>
        private void LeaveVerticesLayer()
        {
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return; }

            dataSetter.SetEditNoteType(EditNoteType.Space);
        }

        /// <summary>
        /// アドレス → 設置場所
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private Transform GetNoteParentTransform(IReadOnlyAddressWithinRange address)
        {
            return dataGetter.ChartData.Value.GetPlacementLocation(new AddressInChart(address));
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
