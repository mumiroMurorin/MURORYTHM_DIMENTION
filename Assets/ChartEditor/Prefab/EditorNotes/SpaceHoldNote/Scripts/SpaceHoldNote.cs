using System;
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
    public class NoteData_SpaceHold : IChainNoteData, ITypeChangableNoteData, IVerticesControlableNoteData, IDisposable
    {
        public NoteData_SpaceHold() {  }

        public NoteData_SpaceHold(NoteData_SpaceHold data)
        {
            SetAddress(data.address);
            SetNoteType(data.NoteType);
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
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.SpaceHoldStart);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            if (!IsSpaceHoldNoteType(noteType))
            {
                Debug.LogWarning($"【Note】SpaceHoldNoteは {noteType} に対応していません");
                return;
            }

            NoteType = noteType;
        }
        public void ChangeNoteType()
        {
            NoteType = NoteTypeCycle.NextNoteType(NoteType);
            UpdateNoteType();
        }
        private void UpdateNoteType()
        {
            if (NoteObject == null) { return; }

            bool hasBackNote = NoteObject.BackNote.Value != null;
            bool hasNextNote = NoteObject.NextNote.Value != null;

            // 始点
            if (!hasBackNote && hasNextNote)
            {
                SetDefaultIfInvalid(DeploymentNoteType.SpaceHoldStart, DeploymentNoteType.SpaceHoldStart);
                return;
            }

            // 中継点
            if (hasBackNote && hasNextNote)
            {
                SetDefaultIfInvalid(DeploymentNoteType.SpaceHoldRelay, DeploymentNoteType.SpaceHoldRelay, DeploymentNoteType.SpaceHoldMeshRelay);
                return;
            }

            // 終点
            if (hasBackNote && !hasNextNote)
            {
                SetDefaultIfInvalid(DeploymentNoteType.SpaceHoldEnd, DeploymentNoteType.SpaceHoldEnd);
                return;
            }

            SetDefaultIfInvalid(DeploymentNoteType.SpaceHoldStart, DeploymentNoteType.SpaceHoldStart);
        }
        private void SetDefaultIfInvalid(DeploymentNoteType defaultType, params DeploymentNoteType[] validTypes)
        {
            if (validTypes.Contains(NoteType)) { return; }

            NoteType = defaultType;
        }
        private bool IsSpaceHoldNoteType(DeploymentNoteType noteType)
        {
            return noteType == DeploymentNoteType.SpaceHoldStart ||
                   noteType == DeploymentNoteType.SpaceHoldRelay ||
                   noteType == DeploymentNoteType.SpaceHoldMeshRelay ||
                   noteType == DeploymentNoteType.SpaceHoldEnd;
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
        IDisposable noteConnectionDisposable;
        public void SetNoteObject(IConnectableObject noteObject)
        {
            noteConnectionDisposable?.Dispose();

            NoteObject = noteObject;
            if (NoteObject == null) { return; }

            noteConnectionDisposable = Observable.Merge(
                    NoteObject.BackNote.Skip(1).AsUnitObservable(),
                    NoteObject.NextNote.Skip(1).AsUnitObservable()
                )
                .ThrottleFrame(1)
                .Subscribe(_ => UpdateNoteType());
        }

        public void Dispose()
        {
            noteConnectionDisposable?.Dispose();
            noteConnectionDisposable = null;
        }


        public IDeployableNoteData Copy()
        {
            return new NoteData_SpaceHold(this);
        }
    }
}