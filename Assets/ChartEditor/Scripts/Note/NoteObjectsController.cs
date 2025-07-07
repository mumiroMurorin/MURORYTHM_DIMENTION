using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class NoteObjectsController : MonoBehaviour
    {
        DataToNoteObjectList dataToObj = new DataToNoteObjectList();
        public DataToNoteObjectList DataToObj => dataToObj;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        IChartEditorOptionGetter optionGetter;
        AddressInChart stackAddress;

        [Inject]
        public void Constructor(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.optionGetter = optionGetter;
        }

        public void AddNote(IDeployableNoteData data, NoteObject obj)
        {
            DataToObj.List.Add(new DataToNoteObject(data, obj));
            BindForNoteAddress(data);
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

    public class DataToNoteObjectList
    {
        List<DataToNoteObject> list = new List<DataToNoteObject>();

        public List<DataToNoteObject> List { get { return list; } }

        public bool Remove(IDeployableNoteData data)
        {
            var dto = list.Find(n => n.Data == data);
            if (dto == null)
            {
                Debug.LogWarning($"【Note】データに対応するオブジェクトが見つかりませんでした: {data.Address}");
                return false;
            }

            return list.Remove(dto);
        }

        public NoteObject GetObject(IDeployableNoteData data) { return list.Find(x => x.Data == data)?.Object; }

        public void Clear()
        {
            foreach (var pair in list)
            {
                pair.Object.Destroy();
            }

            list.Clear();
        }
    }

    public class DataToNoteObject
    {
        public DataToNoteObject(IDeployableNoteData data, NoteObject obj)
        {
            this.Data = data;
            this.Object = obj;
        }

        public IDeployableNoteData Data { get; set; }
        public NoteObject Object { get; set; }
    }
}
