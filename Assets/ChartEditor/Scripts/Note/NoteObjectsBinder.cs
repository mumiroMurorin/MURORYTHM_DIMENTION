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
            // 編集レイヤーが変更された時、配置ノートタイプを変更する
            dataGetter.EditNoteType
                .Subscribe(type => {
                    switch (type)
                    {
                        case EditNoteType.Ground:
                            dataSetter.SetNoteType(defaultNoteType_ground);
                            break;
                        case EditNoteType.Space:
                            dataSetter.SetNoteType(defaultNoteType_space);
                            break;
                    }
                })
                .AddTo(this.gameObject);

            // 譜面データが追加された時
            dataGetter?.ChartData
                .Subscribe(chart => {
                    // リセット
                    if (chart != null && chart.BarDatas != null) 
                    {
                        foreach (var bar in chart?.BarDatas)
                        {
                            foreach (var sub in bar.SubDivisionDatas)
                            {
                                foreach (var note in sub.NoteDatas)
                                {
                                    OnRemoveNoteData(note);
                                }
                            }
                        }
                    }

                    // 購読
                    BindForChartData(chart);
                })
                .AddTo(this.gameObject);
        }

        private void BindForChartData(ChartData chartData)
        {
            // 既にあるデータにバインド
            if(chartData != null && chartData.BarDatas != null)
            {
                foreach (var bar in chartData?.BarDatas)
                {
                    BindForBarData(bar);
                }
            }

            // 小節線データが追加された時
            chartData?.BarDatas.ObserveAdd()
                .Subscribe(bar => {
                    BindForBarData(bar.Value);
                })
                .AddTo(this.gameObject);

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

        private void BindForBarData(BarDataInChart barData)
        {
            // 既にあるデータにバインド
            foreach (var sub in barData.SubDivisionDatas)
            {
                BindForSubdivisionData(sub);
            }

            // 分線データが追加された時
            barData?.SubDivisionDatas.ObserveAdd()
                .Subscribe(sub => {
                    BindForSubdivisionData(sub.Value);
                })
                .AddTo(this.gameObject);

            // 分線データが削除された時
            barData?.SubDivisionDatas.ObserveRemove()
                .Subscribe(sub => {
                    foreach (var note in sub.Value.NoteDatas)
                    {
                        OnRemoveNoteData(note);
                    }
                })
                .AddTo(this.gameObject);
        }

        private void BindForSubdivisionData(SubDivisionDataInBeat subData)
        {
            // 既にあるデータにバインド
            foreach(var note in subData.NoteDatas)
            {
                OnAddNoteData(note);
            }

            // ノートが追加された時
            subData?.NoteDatas.ObserveAdd()
                .Subscribe(note => {
                    OnAddNoteData(note.Value);
                })
                .AddTo(this.gameObject);

            // ノートが削除された時
            subData?.NoteDatas.ObserveRemove()
                .Subscribe(note => {
                    OnRemoveNoteData(note.Value);
                })
                .AddTo(this.gameObject);
        }

        private void BindForNoteAddress(IDeployableNoteData data)
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
                .AddTo(this.gameObject);

            data.Address.SubDivisionIndexRP
                .Pairwise()
                .Subscribe(pair => {
                    var oldAddress = new AddressInChart(data.Address.BarIndex, pair.Previous, data.Address.Range[0]);
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
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// ノーツがデータ上に追加された時、オブジェクトを配置する
        /// </summary>
        /// <param name="note"></param>
        private void OnAddNoteData(IDeployableNoteData note)
        {
            // オブジェクトのインスタンス化
            var obj = InstantiateNoteObject(note);

            // 配置
            Transform parent = dataGetter.ChartData.Value.GetPlacementLocation(new AddressInChart(note.Address));
            obj.OnMove(parent);
            obj.OnDeploy();

            // DataToObjリストに追加
            notesSetter.AddDataToNoteObject(note, obj.Note);

            // バインド
            BindForNoteAddress(note);
        }

        /// <summary>
        /// ノーツがデータ上から削除された時、オブジェクトを削除する
        /// </summary>
        /// <param name="note"></param>
        private void OnRemoveNoteData(IDeployableNoteData note)
        {
            // オブジェクトの削除
            var noteObject = notesGetter.GetNoteObject(note);
            if (noteObject == null || !noteObject.TryGetComponent(out IDestroyableObject destroyableObject)) { return; }

            destroyableObject.OnDestroy();

            // データの削除
            notesSetter.RemoveDataToNoteObject(note);
        }

        /// <summary>
        /// ノートのインスタンス化
        /// </summary>
        /// <param name="noteData"></param>
        /// <returns></returns>
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

            return deployable;
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
