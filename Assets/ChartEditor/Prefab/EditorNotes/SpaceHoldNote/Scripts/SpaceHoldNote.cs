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
            // 同じ分線に並んだノーツをまとめて複製したとき、並びの順番が保たれるように
            SetOrderInSubdivision(data.OrderInSubdivision.Value);
            SpaceVertices.SetVertices(data.SpaceVertices.Vertices.Select(x => new VertexData(x)).ToList());
        }


        // 接続番号 
        ReactiveProperty<int> chainIndex = new ReactiveProperty<int>(-1);
        public IReadOnlyReactiveProperty<int> ChainIndex => chainIndex;
        public void SetChainIndex(int index)
        {
            chainIndex.Value = index;
        }

        // 同じ分線に、同じ接続のノーツが2つ以上あるときの順番 (小さいほど先につながる)
        ReactiveProperty<int> orderInSubdivision = new ReactiveProperty<int>(0);
        public IReadOnlyReactiveProperty<int> OrderInSubdivision => orderInSubdivision;
        public void SetOrderInSubdivision(int order)
        {
            orderInSubdivision.Value = order;
        }


        // 役割(始点・中継点・終点)ごとに、最後に使っていた種類
        // つながりが変わって役割が変わると、種類もその役割のものに付け替わる。元の役割に戻ったときに、
        // 覚えておいた種類へ戻せるようにする (操作を元に戻しても、メッシュ中継点などが標準の種類のままにならないように)
        readonly ChainNoteTypeMemory typeMemory = new ChainNoteTypeMemory(
            new[] { DeploymentNoteType.SpaceHoldStart },
            new[] { DeploymentNoteType.SpaceHoldRelay, DeploymentNoteType.SpaceHoldMeshRelay },
            new[] { DeploymentNoteType.SpaceHoldEnd });

        // ノートタイプ
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.SpaceHoldStart);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set
            {
                noteType.Value = value;
                // その種類を、対応する役割(始点・中継点・終点)で最後に使っていた種類として覚えておく
                typeMemory.Remember(value);
            }
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

            // 譜面の読み込み中は、前後のつながりが確定するまで待つ (途中の状態で種類を変えてしまわないように)
            if (ChainNoteTypeCorrection.TryDefer(this, UpdateNoteType)) { return; }

            bool hasBackNote = NoteObject.BackNote.Value != null;
            bool hasNextNote = NoteObject.NextNote.Value != null;

            // 前後のつながりから決まる役割(始点・中継点・終点)に合わない種類のときは、
            // その役割で最後に使っていた種類にする (一度も使っていなければ、標準の種類)
            NoteType = typeMemory.Resolve(hasBackNote, hasNextNote, NoteType);
        }
        private bool IsSpaceHoldNoteType(DeploymentNoteType noteType)
        {
            return noteType == DeploymentNoteType.SpaceHoldStart ||
                   noteType == DeploymentNoteType.SpaceHoldRelay ||
                   noteType == DeploymentNoteType.SpaceHoldMeshRelay ||
                   noteType == DeploymentNoteType.SpaceHoldEnd;
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