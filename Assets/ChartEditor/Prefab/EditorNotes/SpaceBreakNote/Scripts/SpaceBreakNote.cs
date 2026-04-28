using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class SpaceBreakNote : NoteObject
    {

    }

    [System.Serializable]
    public class NoteData_SpaceBreak : IVerticesControlableNoteData, IDeployableNoteData
    {
        public NoteData_SpaceBreak() {  }

        public NoteData_SpaceBreak(NoteData_SpaceBreak data)
        {
            SetAddress(data.address);
            SetNoteType(data.NoteType);
            SpaceVertices.SetVertices(data.SpaceVertices.Vertices.Select(x => new VertexData(x)).ToList());
        }

        // ノートタイプ
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.SpaceBreak);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            this.noteType.Value = noteType;
            //Debug.LogWarning($"【Note】SpaceHoldNoteは {noteType} に対応していません");
            return;
        }

        // 頂点
        public SpaceVertices SpaceVertices { get; private set; } = new SpaceVertices();

        // アドレス
        AddressWithinRange address;
        public IReadOnlyAddressWithinRange Address => address;
        public void SetAddress(IReadOnlyAddressWithinRange address)
        {
            if (Address == null) { this.address = new AddressWithinRange(address); }
            else
            {
                this.address.SetSameAddress(address);
            }
        }


        public IDeployableNoteData Copy()
        {
            return new NoteData_SpaceBreak(this);
        }
    }
}