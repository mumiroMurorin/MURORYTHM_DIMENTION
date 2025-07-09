using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class NoteObjectsBinder : MonoBehaviour
    {
        [SerializeField] DeploymentNoteType defaultNoteType_ground;
        [SerializeField] DeploymentNoteType defaultNoteType_space;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        AddressInChart stackAddress;

        [Inject]
        public void Constructor(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            this.dataSetter = dataSetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 追加された時の挙動
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
    }
}
