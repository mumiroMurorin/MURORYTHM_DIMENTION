using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class SubdivisionLine : MonoBehaviour, ISubDivisionDataGetter
    {
        [SerializeField] SubdivisionLineInfoView lineInfo_view;

        SubDivisionDataInBeat thisData;
        SubDivisionDataInBeat backData;

        SubDivisionDataInBeat ISubDivisionDataGetter.SubDivisionData => thisData;

        public void Initialize(SubDivisionDataInBeat thisData, SubDivisionDataInBeat backData)
        {
            this.thisData = thisData;
            this.backData = backData;

            Bind();
        }

        private void Bind()
        {
            // BPM変化
            thisData?.Bpm
                .Subscribe(value => {
                    SetSubDivisionLineData(thisData, backData);
                })
                .AddTo(this.gameObject);

            backData?.Bpm
                .Subscribe(value => {
                    SetSubDivisionLineData(thisData, backData);
                })
                .AddTo(this.gameObject);
        }


        /// <summary>
        /// 分線上のデータ更新
        /// </summary>
        /// <param name="barData"></param>
        private void SetSubDivisionLineData(SubDivisionDataInBeat thisData, SubDivisionDataInBeat backData)
        {
            // BPM
            float bpm = backData == null || thisData.Bpm.Value != backData.Bpm.Value ?
                thisData.Bpm.Value : -1;

            lineInfo_view.SetDatas(bpm);
        }

    }

    public interface ISubDivisionDataGetter
    {
        SubDivisionDataInBeat SubDivisionData { get; }
    }

}
