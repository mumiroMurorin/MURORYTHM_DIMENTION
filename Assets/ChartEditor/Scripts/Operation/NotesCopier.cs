using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;
using UniRx;
using static UndoRedo.History;

namespace ChartEditor
{
    public class NotesCopier : MonoBehaviour
    {
        [SerializeField] MultiNoteSelector multiNoteSelector;
        [SerializeField] NoteObjectsController noteObjectsController;
        [SerializeField] NoteDeployer noteDeployer;
        [SerializeField] CursorInteracter cursorInteracter;

        IChartEditorDataGetter dataGetter;
        List<IDeployableNoteData> copiedNotes;

        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.Connecting,
             EditMode.EditingBarConfig,
             EditMode.EditingSubDivisionConfig,
             EditMode.Moving,
             EditMode.Scaling
        };

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
        }

        void Update()
        {
            if(dataGetter.EditNoteType.Value != EditNoteType.Ground) { return; }

            // ノーツのコピー
            if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C)) { CopyNotes(); }
            // ノーツの貼り付け
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V)) { PasteVertices(); }
        }

        /// <summary>
        /// ノーツのコピー
        /// </summary>
        private void CopyNotes()
        {
            EditMode currentEditMode = dataGetter.CurrentEditMode.Value;
            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }

            copiedNotes = new List<IDeployableNoteData>(multiNoteSelector.SelectingNotes);
            Debug.Log("【Notes】ノーツをコピー");
        }

        /// <summary>
        /// ノーツの張り付け
        /// </summary>
        private void PasteVertices()
        {
            var currentEditMode = dataGetter.CurrentEditMode.Value;
            var collider = dataGetter.GetInteractableCollider<IDeployableCollider>();

            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }
            if (copiedNotes == null) { return; }
            if (collider == null) { return; }

            var copiedNotesCopy = copiedNotes.Select(x => x.Copy()).OrderedByAddress().ToList();
            var cursorAddress = collider.Address;
            var addressDelta = dataGetter.ChartData.Value.GetAddressDelta(cursorAddress, new AddressInChart(copiedNotesCopy[0].Address));

            // 全選択解除
            multiNoteSelector.DeselectAll();

            foreach (var data in copiedNotesCopy)
            {
                var address = dataGetter.ChartData.Value.AddressAddition(new AddressInChart(data.Address), addressDelta);

                data.SetAddress(new AddressWithinRange(address, data.Address.Range.Count));
                noteDeployer.DeployForNoteData(data);

                // 選択する
                if(!noteObjectsController.DataToObj.GetObject(data).TryGetComponent(out ISelectableNoteObject selectable)) { continue; }
                multiNoteSelector.SelectMulti(selectable);
            }
           
            Debug.Log("【Notes】ノーツを張り付け");
        }
    }

}