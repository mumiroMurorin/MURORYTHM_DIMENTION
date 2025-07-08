using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class NotesDataHolder : INotesDataGetter, INotesDataSetter
    {
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
        public void ClearSelectingNotes()
        {
            for (int i = selectingNotes.Count - 1; i >= 0; i--)
            {
                TryRemoveSelectingNotes(selectingNotes[i]);
            }
        }


        // ノートデータ→ノートオブジェクト
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


        // 編集中の頂点ありオブジェクト
        ReactiveProperty<IVerticesControlableNoteData> editingVertices = new ReactiveProperty<IVerticesControlableNoteData>();
        public IReadOnlyReactiveProperty<IVerticesControlableNoteData> EditingVertices => editingVertices;
        public void SetEditingVertices(IVerticesControlableNoteData data)
        {
            if (editingVertices.Value == data) { return; }
            editingVertices.Value = data;
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

    public interface INotesDataGetter
    {
        IReadOnlyReactiveCollection<IDeployableNoteData> SelectingNotes { get; }

        IReadOnlyReactiveProperty<IVerticesControlableNoteData> EditingVertices { get; }

        IReadOnlyReactiveCollection<DataToNoteObject> DataToNoteObject { get; }

        NoteObject GetNoteObject(IDeployableNoteData data);
    }

    public interface INotesDataSetter
    {
        bool TryAddSelectingNotes(IDeployableNoteData data);

        bool TryRemoveSelectingNotes(IDeployableNoteData data);

        void ClearSelectingNotes();

        void SetEditingVertices(IVerticesControlableNoteData data);

        void AddDataToNoteObject(IDeployableNoteData data, NoteObject obj);

        bool RemoveDataToNoteObject(IDeployableNoteData data);
    }
}
