using System;
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
    public class NoteData_Hold : IChainNoteData, ITypeChangableNoteData, IDisposable
    {
        public NoteData_Hold() { }

        public NoteData_Hold(NoteData_Hold data)
        {
            SetAddress(data.address);
            SetNoteType(data.NoteType);
        }


        // 接続番号
        ReactiveProperty<int> chainIndex = new ReactiveProperty<int>(-1);
        public IReadOnlyReactiveProperty<int> ChainIndex => chainIndex;
        public void SetChainIndex(int index)
        {
            chainIndex.Value = index;
        }


        // ノートタイプ
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.HoldStart);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            if (!IsHoldNoteType(noteType))
            {
                Debug.LogWarning($"【Note】HoldNoteは {noteType} に対応していません");
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
                SetDefaultIfInvalid(DeploymentNoteType.HoldStart, DeploymentNoteType.HoldStart);
                return;
            }

            // 中継点
            if (hasBackNote && hasNextNote)
            {
                SetDefaultIfInvalid(DeploymentNoteType.HoldRelay, DeploymentNoteType.HoldRelay, DeploymentNoteType.HoldMeshRelay);
                return;
            }

            // 終点
            if (hasBackNote && !hasNextNote)
            {
                SetDefaultIfInvalid(DeploymentNoteType.HoldEnd, DeploymentNoteType.HoldEnd, DeploymentNoteType.HoldEndUnjudge);
                return;
            }

            SetDefaultIfInvalid(DeploymentNoteType.HoldStart, DeploymentNoteType.HoldStart);
        }
        private void SetDefaultIfInvalid(DeploymentNoteType defaultType, params DeploymentNoteType[] validTypes)
        {
            if (validTypes.Contains(NoteType)) { return; }

            NoteType = defaultType;
        }
        private bool IsHoldNoteType(DeploymentNoteType noteType)
        {
            return noteType == DeploymentNoteType.HoldStart ||
                   noteType == DeploymentNoteType.HoldRelay ||
                   noteType == DeploymentNoteType.HoldMeshRelay ||
                   noteType == DeploymentNoteType.HoldEnd ||
                   noteType == DeploymentNoteType.HoldEndUnjudge;
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
            return new NoteData_Hold(this);
        }
    }

}