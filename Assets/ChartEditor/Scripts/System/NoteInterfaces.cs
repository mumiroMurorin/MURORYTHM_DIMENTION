using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public interface IDeployableNoteData
    {
        DeploymentNoteType NoteType { get; }

        IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP { get; }

        IReadOnlyAddressWithinRange Address { get; }

        void SetAddress(IReadOnlyAddressWithinRange address);

        IDeployableNoteData Copy();
    }

    public interface IChainNoteData: IDeployableNoteData
    {
        IConnectableObject NoteObject { get; }

        /// <summary>
        /// 基本チェイン関係はこれに依存
        /// </summary>
        IReadOnlyReactiveProperty<int> ChainIndex { get; }

        public void SetChainIndex(int index);

        void SetNoteObject(IConnectableObject noteObject);
    }

    public interface ITypeChangableNoteData
    {
        IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP { get; }

        void ChangeNoteType();

        void SetNoteType(DeploymentNoteType noteType);
    }

    /// <summary>
    /// Changes note type when note is mirrored
    /// </summary>
    public interface IMirrorTypeChangableNoteData
    {
        void ChangeNoteType();
    }

    public interface IVerticesControlableNoteData
    {
        SpaceVertices SpaceVertices { get; }
    }
}
