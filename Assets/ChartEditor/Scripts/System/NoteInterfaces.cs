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
        /// ÉmÅ[ÉcÇÃà⁄ìÆÅAägëÂèkè¨ÇÃäƒéã
        /// </summary>
        IReadOnlyReactiveCollection<float> Range { get; }

        void SetRange(List<float> range);

        void ChangeRange(float index, bool isRightAnchored);

        void SetAddress(AddressInChart address);

        IGroundNoteData Copy();
    }

    public interface IGroundChainNoteData: IGroundNoteData
    {
        IConnectableObject NoteObject { get; }

        IReadOnlyReactiveProperty<IGroundChainNoteData> NextNote { get; }

        IReadOnlyReactiveProperty<IGroundChainNoteData> BackNote { get; }

        void SetNextNote(IGroundChainNoteData nextNote);

        void SetBackNote(IGroundChainNoteData backNote);

        void AddChainNote(IGroundChainNoteData addNote);

        void RemoveNote();

        void SetNoteObject(IConnectableObject noteObject);
    }

    public interface ITypeChangableNoteData
    {
        IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP { get; }

        void ChangeNoteType(bool isCompulsion);
    }
}
