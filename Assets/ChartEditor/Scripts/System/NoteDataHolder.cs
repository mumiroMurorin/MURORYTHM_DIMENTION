using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class NotesDataHolder : INotesDataGetter, INotesDataSetter
    {

        #region Note

        // 選択中ノーツ
        ReactiveCollection<IDeployableNoteData> selectingNotes = new ReactiveCollection<IDeployableNoteData>();
        public IReadOnlyReactiveCollection<IDeployableNoteData> SelectingNotes => selectingNotes;
        public bool TryAddSelectingNotes(IDeployableNoteData data)
        {
            if (selectingNotes.Contains(data)) { return false; }

            selectingNotes.Add(data);
            return true;
        }
        public bool TryRemoveSelectingNotes(IDeployableNoteData data)
        {
            return selectingNotes.Remove(data);
        }
        public void ReselectNotes()
        {
            var list = new List<IDeployableNoteData>();
            foreach (var note in selectingNotes) { list.Add(note); }

            ClearSelectingNotes();

            foreach (var note in list) { TryAddSelectingNotes(note); }
        }
        public void ClearSelectingNotes()
        {
            for (int i = selectingNotes.Count - 1; i >= 0; i--)
            {
                TryRemoveSelectingNotes(selectingNotes[i]);
            }
        }


        // 配置ノーツ幅
        public int DeployNoteSize { get; set; } = 3;
        

        // ノートデータ → ノートオブジェクト
        ReactiveCollection<DataToNoteObject> dataToNoteObject = new ReactiveCollection<DataToNoteObject>();
        public IReadOnlyReactiveCollection<DataToNoteObject> DataToNoteObject => dataToNoteObject;
        public void AddDataToNoteObject(IDeployableNoteData data, NoteObject obj)
        {
            dataToNoteObject.Add(new DataToNoteObject(data, obj));
        }
        public bool RemoveDataToNoteObject(IDeployableNoteData data)
        {
            var dto = dataToNoteObject.FirstOrDefault(n => n.Data == data);
            if (dto == null)
            {
                Debug.LogWarning($"【Note】データに対応するオブジェクトが見つかりませんでした: {data.Address}");
                return false;
            }

            return dataToNoteObject.Remove(dto);
        }
        public NoteObject GetNoteObject(IDeployableNoteData data) { return dataToNoteObject.FirstOrDefault(x => x.Data == data)?.Object; }
        public CompositeDisposable GetNoteDisposable(IDeployableNoteData data) { return dataToNoteObject.FirstOrDefault(x => x.Data == data)?.OnDestroyDisposables; }
        public void ClearDataToNoteObjectList()
        {
            foreach (var pair in dataToNoteObject)
            {
                pair.Object?.Destroy();
            }

            dataToNoteObject.Clear();
        }


        #endregion


        #region ChainNote

        // 接続データ
        Dictionary<int, SortedChainNoteDataList> indexToChainNoteDataList = new Dictionary<int, SortedChainNoteDataList>();
        public void AddChainNote(IChainNoteData addNote)
        {
            // 含まれているとき
            if (indexToChainNoteDataList.TryGetValue(addNote.ChainIndex.Value, out var list)) 
            {
                list.AddChainNoteData(addNote);
            }
            // 新規のとき
            else
            {
                var addList = new SortedChainNoteDataList();
                addList.AddChainNoteData(addNote);
                indexToChainNoteDataList.Add(addNote.ChainIndex.Value, addList);
            }
        }
        public bool RemoveChainNote(IChainNoteData addNote)
        {
            // 含まれてないとき
            if (!indexToChainNoteDataList.TryGetValue(addNote.ChainIndex.Value, out var list)) { return false; }
            bool b = list.RemoveChainNoteData(addNote);

            // 空っぽになったら消す
            if(list.ChainNoteList.Count == 0) { indexToChainNoteDataList.Remove(addNote.ChainIndex.Value); }
            return b;
        }
        public SortedChainNoteDataList GetChainNoteList(int index)
        {
            // 含まれているとき
            if (indexToChainNoteDataList.TryGetValue(index, out var list)) { return list; }
            else { return null; }
        }
        public int GetUsableChainNoteIndex() 
        {
            int i = 0;
            while (indexToChainNoteDataList.ContainsKey(i))
            {
                i++;
            }
            return i;
        }
        public void ClearIndexToChainNoteDataList()
        {
            foreach (var i in indexToChainNoteDataList)
            {
                i.Value.Clear();
            }

            indexToChainNoteDataList.Clear();
        }


        #endregion


        #region Vertex

        // 編集中の頂点ありオブジェクト
        ReactiveProperty<IVerticesControlableNoteData> editingVertices = new ReactiveProperty<IVerticesControlableNoteData>();
        public IReadOnlyReactiveProperty<IVerticesControlableNoteData> EditingVertices => editingVertices;
        public void SetEditingVertices(IVerticesControlableNoteData data)
        {
            if (editingVertices.Value == data) { return; }
            editingVertices.Value = data;
        }

        // 頂点データ → 頂点オブジェクト
        ReactiveCollection<DataToVertexObject> dataToVertexObject = new ReactiveCollection<DataToVertexObject>();
        public IReadOnlyReactiveCollection<DataToVertexObject> DataToVertexObject => dataToVertexObject;
        public VertexObject GetVertexObject(VertexData data) { return dataToVertexObject.FirstOrDefault(x => x.Data == data)?.Object; }
        public VertexObject GetVertexObject(int index) 
        {
            return (index >= 0 && index < dataToVertexObject.Count) ? dataToVertexObject[index].Object : null;
        }
        public bool RemoveVertexDataToObject(VertexData data)
        {
            var dto = dataToVertexObject.FirstOrDefault(n => n.Data == data);
            if (dto == null)
            {
                Debug.LogWarning($"【Vertex】データに対応するオブジェクトが見つかりませんでした: {data.Position}");
                return false;
            }

            return dataToVertexObject.Remove(dto);
        }
        public void InsertVertex(int index, DataToVertexObject data)
        {
            dataToVertexObject.Insert(index, data);
        }
        public void ClearDataToVertexObjectList()
        {
            foreach (var pair in dataToVertexObject)
            {
                pair.Object.Destroy();
            }

            dataToVertexObject.Clear();
        }

        #endregion
    }

    public class DataToNoteObject
    {
        public DataToNoteObject(IDeployableNoteData data, NoteObject obj)
        {
            this.Data = data;
            this.Object = obj;
            this.OnDestroyDisposables = new CompositeDisposable(); 
        }

        public IDeployableNoteData Data { get; set; }

        public NoteObject Object { get; set; }

        public CompositeDisposable OnDestroyDisposables { get; set; }
    }

    public class DataToVertexObject
    {
        public DataToVertexObject(VertexData data, VertexObject obj)
        {
            this.Data = data;
            this.Object = obj;
        }

        public VertexData Data { get; set; }
        public VertexObject Object { get; set; }
    }

    /// <summary>
    /// Address順にソートされることが保証されたChainNoteDataList
    /// </summary>
    public class SortedChainNoteDataList
    {
        ReactiveCollection<IChainNoteData> chainNoteList = new ReactiveCollection<IChainNoteData>();
        public IReadOnlyReactiveCollection<IChainNoteData> ChainNoteList => chainNoteList;

        public void AddChainNoteData(IChainNoteData addData)
        {
            // 挿入するインデックスを調べる
            int insertIndex = 0;
            for(insertIndex = 0; insertIndex < chainNoteList.Count; insertIndex++)
            {
                var current = chainNoteList[insertIndex];
                if (addData.Address.IsEarlierThan(current.Address)) { break; }
            }

            chainNoteList.Insert(insertIndex, addData);
        }

        public bool RemoveChainNoteData(IChainNoteData removeData)
        {
            return chainNoteList.Remove(removeData);
        }

        public void UpdateChainNoteData(IChainNoteData data)
        {
            RemoveChainNoteData(data);
            AddChainNoteData(data);
        }

        public int IndexOf(IChainNoteData targetData)
        {
            return chainNoteList.IndexOf(targetData);
        }

        public void Clear()
        {
            chainNoteList.Clear();
        }
    }

    public interface INotesDataGetter
    {
        IReadOnlyReactiveCollection<IDeployableNoteData> SelectingNotes { get; }

        IReadOnlyReactiveProperty<IVerticesControlableNoteData> EditingVertices { get; }

        IReadOnlyReactiveCollection<DataToNoteObject> DataToNoteObject { get; }

        NoteObject GetNoteObject(IDeployableNoteData data);

        CompositeDisposable GetNoteDisposable(IDeployableNoteData data);

        SortedChainNoteDataList GetChainNoteList(int index);

        int DeployNoteSize { get; }

        void AddChainNote(IChainNoteData addNote);

        bool RemoveChainNote(IChainNoteData addNote);

        int GetUsableChainNoteIndex();

        void ClearIndexToChainNoteDataList();

        IReadOnlyReactiveCollection<DataToVertexObject> DataToVertexObject { get; }

        VertexObject GetVertexObject(VertexData data);

        VertexObject GetVertexObject(int index);
    }

    public interface INotesDataSetter
    {
        bool TryAddSelectingNotes(IDeployableNoteData data);

        bool TryRemoveSelectingNotes(IDeployableNoteData data);

        void ReselectNotes();

        void ClearSelectingNotes();

        void SetEditingVertices(IVerticesControlableNoteData data);

        void AddDataToNoteObject(IDeployableNoteData data, NoteObject obj);

        bool RemoveDataToNoteObject(IDeployableNoteData data);

        void ClearDataToNoteObjectList();

        int DeployNoteSize { set; }

        bool RemoveVertexDataToObject(VertexData data);

        void InsertVertex(int index, DataToVertexObject data);

        void ClearDataToVertexObjectList();
    }
}
