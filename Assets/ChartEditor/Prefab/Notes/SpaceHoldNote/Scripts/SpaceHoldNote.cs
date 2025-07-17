using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class SpaceHoldNote : NoteObject
    {

    }

    [System.Serializable]
    public class NoteData_SpaceHold : IChainNoteData, ITypeChangableNoteData, IVerticesControlableNoteData
    {
        public NoteData_SpaceHold() {  }

        public NoteData_SpaceHold(NoteData_SpaceHold data)
        {
            SetAddress(data.address);
            SpaceHoldVertices.SetVertices(data.SpaceHoldVertices.Vertices.Select(x => new VertexData(x)).ToList());
        }


        // 接続番号 
        ReactiveProperty<int> chainIndex = new ReactiveProperty<int>(-1);
        public IReadOnlyReactiveProperty<int> ChainIndex => chainIndex;
        public void SetChainIndex(int index)
        {
            chainIndex.Value = index;
        }


        // ノートタイプ
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.SpaceHold);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            if (noteType != DeploymentNoteType.SpaceHold &&
                noteType != DeploymentNoteType.SpaceHoldHidden)
            {
                Debug.LogWarning($"【Note】SpaceHoldNoteは {noteType} に対応していません");
                return;
            }

            NoteType = noteType;
        }
        public void ChangeNoteType(bool isDone)
        {
            // 始点終点は必ず判定あり
            if (NoteObject.NextNote.Value == null || NoteObject.BackNote.Value == null) { return; }

            switch (NoteType)
            {
                // 可視 → 不可視(中継点のみ)
                case DeploymentNoteType.SpaceHold:
                    if (isDone) { NoteType = DeploymentNoteType.SpaceHoldHidden; }
                    else { NoteType = DeploymentNoteType.SpaceHoldHidden; }
                    break;
                // 不可視 → 可視
                case DeploymentNoteType.SpaceHoldHidden:
                    if (isDone) { NoteType = DeploymentNoteType.SpaceHold; }
                    else { NoteType = DeploymentNoteType.SpaceHold; }
                    break;
            }

            UpdateNoteType();
        }
        private void UpdateNoteType()
        {
            // 始点ノーツが変なことになってたら元に戻す
            if (NoteObject.BackNote.Value == null)
            {
                NoteType = DeploymentNoteType.SpaceHold;
            }
        }

        // 頂点
        public SpaceHoldVertices SpaceHoldVertices { get; private set; } = new SpaceHoldVertices();

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
            return new NoteData_SpaceHold(this);
        }
    }
}