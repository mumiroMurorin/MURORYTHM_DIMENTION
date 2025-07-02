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

        [Inject]
        public void Constructor(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.optionGetter = optionGetter;
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
