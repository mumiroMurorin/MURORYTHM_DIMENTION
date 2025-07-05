using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public interface IDeployableNoteData
    {
        DeploymentNoteType NoteType { get; }

        AddressWithinRange Address { get; }

        void SetAddress(AddressWithinRange address);

        IDeployableNoteData Copy();
    }

    public interface IChainNoteData: IDeployableNoteData
    {
        IConnectableObject NoteObject { get; }

        IReadOnlyReactiveProperty<IChainNoteData> NextNote { get; }

        IReadOnlyReactiveProperty<IChainNoteData> BackNote { get; }

        void SetNextNote(IChainNoteData nextNote, bool isUpdateNoteType = true);

        void SetBackNote(IChainNoteData backNote, bool isUpdateNoteType = true);

        void AddChainNote(IChainNoteData addNote, bool isUpdateNoteType = true);

        void RemoveNote();

        void SetNoteObject(IConnectableObject noteObject);
    }

    public interface ITypeChangableNoteData
    {
        IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP { get; }

        void ChangeNoteType();

        void SetNoteType(DeploymentNoteType noteType);
    }

    public interface IVerticesControlableNoteData
    {
        SpaceHoldVertices SpaceHoldVertices { get; }
    }
}
