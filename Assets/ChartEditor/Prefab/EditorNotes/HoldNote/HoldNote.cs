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
            // 同じ分線に並んだノーツをまとめて複製したとき、並びの順番が保たれるように
            SetOrderInSubdivision(data.OrderInSubdivision.Value);
            curveControl = data.curveControl;
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
            new[] { DeploymentNoteType.HoldStart, DeploymentNoteType.DivineHoldStart },
            new[] { DeploymentNoteType.HoldRelay, DeploymentNoteType.HoldMeshRelay },
            new[] { DeploymentNoteType.HoldEnd, DeploymentNoteType.HoldEndUnjudge });

        // ノートタイプ
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.HoldStart);
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

            // 譜面の読み込み中は、前後のつながりが確定するまで待つ (途中の状態で種類を変えてしまわないように)
            if (ChainNoteTypeCorrection.TryDefer(this, UpdateNoteType)) { return; }

            bool hasBackNote = NoteObject.BackNote.Value != null;
            bool hasNextNote = NoteObject.NextNote.Value != null;

            // 前後のつながりから決まる役割(始点・中継点・終点)に合わない種類のときは、
            // その役割で最後に使っていた種類にする (一度も使っていなければ、標準の種類)
            var resolved = typeMemory.Resolve(hasBackNote, hasNextNote, NoteType);
            bool isForced = false;
            // ダイナミックノーツが載っているノーツは、HoldMeshRelay にしない (中継点の役割なら、HoldRelay にする)
            if (resolved == DeploymentNoteType.HoldMeshRelay && DynamicNoteAttachments.GetDynamicOn(this) != null)
            {
                resolved = DeploymentNoteType.HoldRelay;
                isForced = true;
            }
            // 載っているノーツが終点になるときは、判定なしの終点にする (録音 20261118_010356)
            if (resolved == DeploymentNoteType.HoldEnd && DynamicNoteAttachments.GetDynamicOn(this) != null)
            {
                resolved = DeploymentNoteType.HoldEndUnjudge;
                isForced = true;
            }
            // ダイナミックノーツのために変えた種類は、そのノーツが選んだ種類としては覚えない
            // (元に戻すなどでダイナミックノーツが外れたあと、また終点になったときに、判定なしのままにならないように)
            if (isForced) { noteType.Value = resolved; }
            else { NoteType = resolved; }
        }
        private bool IsHoldNoteType(DeploymentNoteType noteType)
        {
            return noteType == DeploymentNoteType.HoldStart ||
                   noteType == DeploymentNoteType.DivineHoldStart ||
                   noteType == DeploymentNoteType.HoldRelay ||
                   noteType == DeploymentNoteType.HoldMeshRelay ||
                   noteType == DeploymentNoteType.HoldEnd ||
                   noteType == DeploymentNoteType.HoldEndUnjudge;
        }


        // このノーツ (節) と、次の節との間を曲げる、制御点の位置。動かしていないときは null (まっすぐ)
        // x: 2つの節をまっすぐ結んだ線から、横へレーンいくつぶん離れているか (プラスが右)
        // y: この節から次の節までの長さの、何割のところにあるか (0〜1)     (計算は HoldCurveMath)
        // 編集している間だけ覚えている。譜面ファイルには保存しない
        Vector2? curveControl;
        public Vector2? CurveControl => curveControl;

        public void SetCurveControl(Vector2? control)
        {
            curveControl = control;
        }

        /// <summary>
        /// 制御点の左右を入れ替える (ノーツを左右に反転したときに呼ぶ。2回呼ぶと元に戻る)
        /// </summary>
        public void MirrorCurveControl()
        {
            if (curveControl.HasValue) { curveControl = new Vector2(-curveControl.Value.x, curveControl.Value.y); }
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