using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class NoteObjectsBinder : MonoBehaviour
    {
        // SubclassSelectorを自作クラスの中にいれると上手く動作しないので苦肉の策
        [Tooltip("ノートデータ(抽象クラス)")]
        [SerializeReference, SubclassSelector] IDeployableNoteData[] noteDataList;
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
            // DataToNoteObjectに項目が追加された時の挙動
            notesGetter.DataToNoteObject.ObserveAdd()
                .Subscribe(data => { 
                    BindForNoteAddress(data.Value.Data);

                    // 譜面データへのノーツデータの追加
                    dataGetter.ChartData.Value.AddNote(data.Value.Data);
                })
                .AddTo(this.gameObject);

            // 削除された時の挙動
            notesGetter.DataToNoteObject.ObserveRemove()
                .Subscribe(data => {
                    // 譜面データからノーツデータの削除
                    dataGetter.ChartData.Value.RemoveNote(data.Value.Data);
                })
                .AddTo(this.gameObject);

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
                    BindForChartData(chart);
                })
                .AddTo(this.gameObject);
        }

        private void BindForNoteAddress(IDeployableNoteData data)
        {
            // ノーツデータの移動
            // 小節線移動時
            data.Address.BarIndexRP
                .Pairwise()
                .Subscribe(pair => {
                    var oldAddress = new AddressInChart(pair.Previous, data.Address.SubDivisionIndex, data.Address.Range[0]);
                    var newAddress = new AddressInChart(data.Address);

                    if (!dataGetter.ChartData.Value.IsExistAddressInChart(oldAddress))
                    {
                        if (stackAddress == null)
                        {
                            Debug.LogError("スタックアドレスがnullかつ元アドレスが正規でありません");
                            return;
                        }

                        oldAddress = stackAddress;
                        stackAddress = null;
                    }
                    if (!dataGetter.ChartData.Value.IsExistAddressInChart(newAddress))
                    {
                        if (stackAddress != null)
                        {
                            Debug.LogError("スタックアドレスがnullでないかつ新アドレスが正規でありません");
                            return;
                        }

                        stackAddress = oldAddress;
                        return;
                    }

                    dataGetter.ChartData.Value.ChangeNoteAddress(data, oldAddress, newAddress);
                })
                .AddTo(this.gameObject);

            // 分線移動時
            data.Address.SubDivisionIndexRP
                .Pairwise()
                .Subscribe(pair => {
                    var oldAddress = new AddressInChart(data.Address.BarIndex, pair.Previous, data.Address.Range[0]);
                    var newAddress = new AddressInChart(data.Address);

                    if (!dataGetter.ChartData.Value.IsExistAddressInChart(oldAddress))
                    {
                        if (stackAddress == null)
                        {
                            Debug.LogError("スタックアドレスがnullかつ元アドレスが正規でありません");
                            return;
                        }

                        oldAddress = stackAddress;
                        stackAddress = null;
                    }
                    if (!dataGetter.ChartData.Value.IsExistAddressInChart(newAddress))
                    {
                        if (stackAddress != null)
                        {
                            Debug.LogError("スタックアドレスがnullでないかつ新アドレスが正規でありません");
                            return;
                        }

                        stackAddress = oldAddress;
                        return;
                    }

                    dataGetter.ChartData.Value.ChangeNoteAddress(data, oldAddress, newAddress);
                })
                .AddTo(this.gameObject);
        }

        private void BindForChartData(ChartData chartData)
        {
            // 既にあるデータにバインド
            foreach(var bar in chartData?.BarDatas)
            {
                BindForBarData(bar);
            }

            // 小節線データが追加された時
            chartData?.BarDatas.ObserveAdd()
                .Subscribe(bar => {
                    BindForBarData(bar.Value);
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
                    // DataToObjリストから全て削除
                    foreach (var note in sub.Value.NoteDatas)
                    {
                        notesSetter.RemoveDataToNoteObject(note);
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
                    // DataToObjリストから削除
                    notesSetter.RemoveDataToNoteObject(note.Value);
                })
                .AddTo(this.gameObject);
        }

        private void OnAddNoteData(IDeployableNoteData note)
        {
            // オブジェクトを配置
            var obj = InstantiateNoteObject(note);
            // DataToObjリストに追加
            notesSetter.AddDataToNoteObject(note, obj.Note);
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
}
