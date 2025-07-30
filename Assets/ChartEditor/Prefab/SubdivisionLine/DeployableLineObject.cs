using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ChartEditor
{

    public abstract class DeployableLineObject : MonoBehaviour, ILayerAffectable, IScaleAffectable, IAddressSettable
    {
        [SerializeField] NoteDeployableGroup groundDeployable;
        [SerializeField] SpaceDeployableUnit spaceDeployable;

        public virtual void SetBarNumber(int barNumber) { }

        public virtual void OnChangeBpm(float bpm, float backBpm) { }

        public virtual void OnChangeBeatCount(int beatCount, int backCount) { }

        public virtual void OnChangeBeatUnit(float beatUnit, float backUnit) { }

        public virtual void OnChangeDivisionNum(int divNum, int backNum) { }

        /// <summary>
        /// 配置コライダーに担当アドレスを伝える
        /// </summary>
        /// <param name="address"></param>
        public void SetAddress(IReadOnlyAddressInChart address)
        {
            groundDeployable?.SetAddress(address);
            spaceDeployable?.SetAddress(address);
        }

        public void SetPlacementLocation(Action<Transform[]> registerGroundLocates, Action<Transform> registerSpaceLocate)
        {
            registerGroundLocates?.Invoke(groundDeployable.GetNoteDeployableUnitTransforms());
            registerSpaceLocate?.Invoke(spaceDeployable.GetNoteDeployableUnitTransform());
        }

        public void SetPosition(float z)
        {
            var pos = this.gameObject.transform.position;
            this.gameObject.transform.position = new Vector3(pos.x, pos.y, z);
        }

        /// <summary>
        /// レイヤーチェンジ → 地と宙の位置反転
        /// </summary>
        /// <param name="editNoteType"></param>
        public void OnChangeLayer(EditNoteType editNoteType)
        {
            groundDeployable?.OnChangeLayer(editNoteType);
            spaceDeployable?.OnChangeLayer(editNoteType);
        }

        /// <summary>
        /// それぞれの配置レンジを調整
        /// </summary>
        /// <param name="z"></param>
        public void OnChangeSize(float z)
        {
            groundDeployable?.OnChangeSize(z);
            spaceDeployable?.OnChangeSize(z);
        }
    }

}