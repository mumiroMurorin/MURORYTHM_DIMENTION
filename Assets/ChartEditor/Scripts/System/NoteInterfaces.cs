using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public interface IGroundNoteData
    {
        DeploymentNoteType NoteType { get; }

        AddressInChart Address { get; }

        /// <summary>
        /// ƒm[ƒc‚ÌˆÚ“®AŠg‘åk¬‚ÌŠÄ‹
        /// </summary>
        IReadOnlyReactiveCollection<float> Range { get; }

        void SetRange(List<float> range);

        void ChangeRange(float index, bool isRightAnchored);

        void SetAddress(AddressInChart address);

        IGroundNoteData Copy();
    }

    public interface IGroundChainNoteData: IGroundNoteData
    {
        IGroundNoteData NextNote { get; }

        void SetNextNote(IGroundNoteData nextNote);
    }
}
