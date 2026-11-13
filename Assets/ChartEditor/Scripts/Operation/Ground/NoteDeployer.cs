using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    /// <summary>
    /// グラウンドノーツの配置 (レーン上のマスに、指定した横幅で置く)
    /// </summary>
    public class NoteDeployer : NoteDeployerBase
    {
        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;

        protected override EditMode DeployMode => EditMode.Deploy;

        protected override int DeployRangeCount => notesGetter.DeployNoteSize;

        [Inject]
        public void ConstructNotes(INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
        }

        protected override void Bind()
        {
            base.Bind();

            // ノーツが単選択されたとき、配置サイズを変える
            notesGetter.SelectingNotes.ObserveCountChanged()
                .Where(count => count == 1)
                .Subscribe(_ => {
                    notesSetter.DeployNoteSize = notesGetter.SelectingNotes[0].Address.Range.Count;
                })
                .AddTo(this.gameObject);
        }

        protected override IInteractableCollider GetDeployColliderUnderCursor()
        {
            return dataGetter.GetInteractableCollider<IDeployableCollider>();
        }

        protected override bool TryGetDeployLocation(IInteractableCollider collider, out Transform parent, out AddressInChart address)
        {
            if (collider is IDeployableCollider deployable)
            {
                parent = deployable.deployParent;
                address = deployable.Address;
                return true;
            }

            parent = null;
            address = null;
            return false;
        }

        protected override void SetupNoteData(IDeployableNoteData noteData)
        {
            // 仮ノーツを、配置する横幅で表示する
            var address = noteData.Address;
            var range = Enumerable.Range(0, notesGetter.DeployNoteSize).Select(x => (float)x).ToList();

            noteData.SetAddress(new AddressWithinRange(address.BarIndex, address.SubDivisionIndex, range));
        }
    }
}
