using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteMover : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] MultiNoteSelector notesSelector;
        [SerializeField] NoteObjectsController notesController;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        Dictionary<IMovableObject, AddressDelta> movableAndDelta = new Dictionary<IMovableObject, AddressDelta>();
        AddressInChart baseAddress;
        IDeployableCollider lastLocation;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        void Update()
        {
            // 配置モードのみ
            if (dataGetter.CurrentEditMode.Value != EditMode.Move &&
                dataGetter.CurrentEditMode.Value != EditMode.Moving) { return; }

            if (Input.GetMouseButtonDown(0)) { StartMovingNotes(); }
            else if (Input.GetMouseButton(0)) { MoveNotes(); } 
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

            // 基準となるクリックされたノーツ情報を保存
            baseAddress = new AddressInChart(movableObject.Note.NoteData.Address);

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var data in notesSelector.SelectingNotes)
            {
                var obj = notesController.DataToObj.GetObject(data);
                if (!obj.TryGetComponent(out IMovableObject movable)) { continue; }

                // ノーツオブジェクト側の動作
                movable.OnMoveStart();

                // 差分を保存
                var subDelta = dataGetter.ChartData.Value.GetAddressDelta(new AddressInChart(movable.Note.NoteData.Address), baseAddress);
                var sliderDelta = movable.Note.NoteData.Address.Range[0] - baseAddress.SliderIndex;
                var delta = new AddressDelta(subDelta, (int)sliderDelta);

                movableAndDelta.TryAdd(movable, delta);
            }

            dataSetter?.SetEditMode(EditMode.Moving);
        }
        
        /// <summary>
        /// クリック中(移動中)
        /// </summary>
        private void MoveNotes()
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
                var noteData = pair.Key.Note.NoteData;

                var newAddress = dataGetter.ChartData.Value.AddressAddition(deployable.Address, pair.Value.SubDivisionDelta);
                newAddress.SetSliderIndex(deployable.Address.SliderIndex + pair.Value.SliderDelta);
                noteData.SetAddress(new AddressWithinRange(newAddress, noteData.Address.Range.Count));

                pair.Key.OnMove();
            }
        }

        /// <summary>
        /// クリック終了時(移動終了時)
        /// </summary>
        private void EndMoveNote()
        {
            foreach (var pair in movableAndDelta)
            {
                pair.Key?.OnMoveEnd();
            }

            Initialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            baseAddress = null;
            movableAndDelta.Clear();
        }

        class AddressDelta
        {
            public AddressDelta(int subDelta, int sliderDelta)
            {
                SubDivisionDelta = subDelta;
                SliderDelta = sliderDelta;
            }

            public int SubDivisionDelta { get; set; }
            public int SliderDelta { get; set; }
        }
    }
}
