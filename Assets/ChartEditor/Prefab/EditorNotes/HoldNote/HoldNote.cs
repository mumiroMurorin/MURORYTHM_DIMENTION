using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class HoldNote : NoteObject
    {

    }

    [System.Serializable]
    public class NoteData_Hold : IChainNoteData, ITypeChangableNoteData
    {
        public NoteData_Hold() { }

        public NoteData_Hold(NoteData_Hold data)
        {
            SetAddress(data.address);
        }


        // 接続番号
        ReactiveProperty<int> chainIndex = new ReactiveProperty<int>(-1);
        public IReadOnlyReactiveProperty<int> ChainIndex => chainIndex;
        public void SetChainIndex(int index)
        {
            chainIndex.Value = index;
        }


        // ノートタイプ
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.Hold);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            if (noteType != DeploymentNoteType.Hold &&
                noteType != DeploymentNoteType.HoldHidden &&
                noteType != DeploymentNoteType.HoldEndUnjudge)
            {
                Debug.LogWarning($"【Note】HoldNoteは {noteType} に対応していません");
                return;
            }

            NoteType = noteType;
        }
        public void ChangeNoteType()
        {
            NoteType = NoteTypeCycle.NextHoldNoteType(NoteType, NoteObject.NextNote.Value == null);
            UpdateNoteType();
        }
        private void UpdateNoteType()
        {
            // 終点ノーツが変なことになってたら元に戻す
            if(NoteObject.NextNote.Value == null && NoteType != DeploymentNoteType.HoldEndUnjudge)
            {
                NoteType = DeploymentNoteType.Hold;
            }

            // 始点ノーツが変なことになってたら元に戻す
            if (NoteObject.BackNote.Value == null) 
            {
                NoteType = DeploymentNoteType.Hold;
            }
        }


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


        public IConnectableObject NoteObject { get; private set; }
        public void SetNoteObject(IConnectableObject noteObject)
        {
            NoteObject = noteObject;
        }


        public IDeployableNoteData Copy()
        {
            return new NoteData_Hold(this);
        }
    }

}