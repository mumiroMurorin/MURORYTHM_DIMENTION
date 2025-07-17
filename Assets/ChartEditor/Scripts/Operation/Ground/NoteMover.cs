using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.Notes.NotesMoveRecord;

namespace ChartEditor
{
    public class NoteMover : MonoBehaviour
    {
        [Header("レーン(ノーツ)1マス分の横幅")]
        [SerializeField] float noteUnitWidth = 1f;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        INotesDataGetter notesGetter;
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        Dictionary<IMovableObject, AddressDelta> movableAndDelta = new Dictionary<IMovableObject, AddressDelta>();
        List<NoteDataToAddress> previousAddress;
        AddressInChart baseAddress;
        IDeployableCollider lastLocation;
        int pointerToAxisDelta;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter,INotesDataGetter notesGetter)
        {
            this.notesGetter = notesGetter;
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        void LateUpdate()
        {
            // 配置モードのみ
            if (dataGetter.CurrentEditMode.Value != EditMode.Move &&
                dataGetter.CurrentEditMode.Value != EditMode.Moving) { return; }

            if (Input.GetMouseButtonDown(0)) { StartMovingNotes(); }
            else if (Input.GetMouseButton(0)) { MoveNotesOnClick(); } 
            else if (Input.GetMouseButtonUp(0)) { EndMoveNote(); }
        }

        /// <summary>
        /// クリックされた時
        /// </summary>
        private void StartMovingNotes()
        {
            Initialize();

            // 動かせるオブジェクトでなければ返す
            var collider = dataGetter.GetInteractableCollider<IMovableCollider>();
            var movableObject = collider?.Note;

            if (collider == null) { return; }
            if (movableObject == null) { return; }

            previousAddress = new List<NoteDataToAddress>();

            // 基準となるクリックされたノーツ情報を保存
            baseAddress = new AddressInChart(movableObject.Note.NoteData.Address);

            // マウスカーソルからノーツの軸までの差を算出
            float deltaX = cursorInteracter.Value.GetWorldPositionUnderCursor().x - movableObject.Note.transform.position.x;
            pointerToAxisDelta = (int)((deltaX + noteUnitWidth / 2f) / noteUnitWidth);

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var data in notesGetter.SelectingNotes)
            {
                var obj = notesGetter.GetNoteObject(data);
                if (!obj.TryGetComponent(out IMovableObject movable)) { continue; }

                // ノーツオブジェクト側の動作
                movable.OnMoveStart();

                // 差分を保存
                var subDelta = dataGetter.ChartData.Value.GetSubdivisionDelta(new AddressInChart(movable.Note.NoteData.Address), baseAddress);
                var sliderDelta = movable.Note.NoteData.Address.Range[0] - baseAddress.SliderIndex;
                var delta = new AddressDelta(subDelta, (int)sliderDelta, movable.Note.NoteData.Address.Range.Count);

                movableAndDelta.TryAdd(movable, delta);
                previousAddress.Add(new NoteDataToAddress(data, data.Address));
            }

            dataSetter?.SetEditMode(EditMode.Moving);
        }
        
        private void MoveNotesOnClick()
        {
            if (movableAndDelta == null || movableAndDelta.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = dataGetter.GetInteractableCollider<IDeployableCollider>();
            if (deployable == null) { return; }
            if (lastLocation == deployable) { return; }

            // 最新配置場所の更新
            lastLocation = deployable;

            // アドレスの移動
            foreach (var pair in movableAndDelta)
            {
                var newAddress = dataGetter.ChartData.Value.AddressAddition(deployable.Address, pair.Value.SubDivisionDelta);
                newAddress.SetSliderIndex(deployable.Address.SliderIndex + pair.Value.SliderDelta - pointerToAxisDelta);

                MoveNote(pair.Key, new AddressWithinRange(newAddress, pair.Value.RangeCount));
            }
        }

        /// <summary>
        /// ノートを指定されたアドレスに移動させる
        /// </summary>
        /// <param name="movableObject"></param>
        /// <param name="newAddress"></param>
        public void MoveNote(IMovableObject movableObject, AddressWithinRange newAddress)
        {
            var noteData = movableObject.Note.NoteData;
            noteData.SetAddress(newAddress);
            movableObject.OnMove();
        }

        /// <summary>
        /// クリック終了時(移動終了時)
        /// </summary>
        private void EndMoveNote()
        {
            var currentAddress = new List<NoteDataToAddress>();
            foreach (var pair in movableAndDelta)
            {
                pair.Key?.OnMoveEnd();

                var noteData = pair.Key.Note.NoteData;
                currentAddress.Add(new NoteDataToAddress(noteData, noteData.Address));
            }

            // 移動を終えたときはじめて登録
            RecordNotesMoving(previousAddress, currentAddress);

            dataSetter?.SetEditMode(EditMode.Move);

            Initialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            baseAddress = null;
            lastLocation = null;
            pointerToAxisDelta = 0;
            movableAndDelta.Clear();
            previousAddress?.Clear();
        }

        class AddressDelta
        {
            public AddressDelta(int subDelta, int sliderDelta, int rangeCount)
            {
                SubDivisionDelta = subDelta;
                SliderDelta = sliderDelta;
                RangeCount = rangeCount;
            }

            public int SubDivisionDelta { get; set; }
            public int SliderDelta { get; set; }
            public int RangeCount { get; set; }
        }
    }
}
