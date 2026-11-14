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

        // スペースレイヤーのノーツを表すレーン番号
        const float SPACE_LANE_INDEX = 100f;
        // 選択が変わったか (フレームの終わりに、配置する幅を見直す)
        bool isSelectionChanged;

        [Inject]
        public void ConstructNotes(INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
        }

        protected override void Bind()
        {
            base.Bind();

            // ノーツが1つだけ選択されたとき、配置する幅をそのノーツの幅に合わせる
            // 選択の数が変わるたびにその場で合わせると、複数選択を解除する途中 (1つずつ外れていく) や、削除・貼り付けの途中で
            // 「1つ」の瞬間を通るたびに、幅が書き換わってしまう。
            // ここでは「選択が変わった」ことだけ覚えておき、フレームの終わりに、落ち着いた後の選択を見て決める
            notesGetter.SelectingNotes.ObserveCountChanged()
                .Subscribe(_ => isSelectionChanged = true)
                .AddTo(this.gameObject);
        }

        private void LateUpdate()
        {
            if (!isSelectionChanged) { return; }
            isSelectionChanged = false;

            if (notesGetter.SelectingNotes.Count != 1) { return; }

            // スペースのノーツにはレーンの幅が無いので、配置する幅には使わない
            var lanes = notesGetter.SelectingNotes[0].Address.RangeRP;
            if (lanes.Count == 0 || lanes[0] == SPACE_LANE_INDEX) { return; }

            notesSetter.DeployNoteSize = lanes.Count;
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
