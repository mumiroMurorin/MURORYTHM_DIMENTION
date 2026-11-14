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

        /// <summary>
        /// 同じ分線(同じ時刻)に、同じ接続のノーツが2つ以上あるときの順番 (小さいほど先につながる)
        ///
        /// 同じ分線にほかのノーツが無い間は、意味を持たない。
        /// ノーツが別の分線へ動いても値は変わらないので、元の分線へ戻せば元の順番に戻る。
        /// 同じ値のノーツがいる分線へ来たときだけ、いちばん後ろの順番に付け直される (SortedChainNoteDataList)。
        /// </summary>
        IReadOnlyReactiveProperty<int> OrderInSubdivision { get; }

        void SetOrderInSubdivision(int order);

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
