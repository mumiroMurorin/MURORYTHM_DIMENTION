using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class SubdivisionLine : MonoBehaviour, ISubDivisionDataGetter, ILinePositioner
    {
        [SerializeField] SubdivisionLineInfoView lineInfo_view;

        ILinePositioner backData;
        IChartEditorOptionGetter optionGetter;

        /// <summary>
        /// 次の線の開始位置
        /// </summary>
        ReactiveProperty<float> nextZ = new ReactiveProperty<float>();
        IReadOnlyReactiveProperty<float> ILinePositioner.NextZ => nextZ;

        SubDivisionDataInBeat subDivisionData;
        public SubDivisionDataInBeat SubDivisionData => subDivisionData;

        BarDataInChart barData;
        BarDataInChart ILinePositioner.BarData => barData;

        public void Initialize(SubDivisionDataInBeat subDivisionData, ILinePositioner backData, IChartEditorOptionGetter optionGetter)
        {
            this.subDivisionData = subDivisionData;
            this.backData = backData;
            this.barData = backData.BarData;
            this.optionGetter = optionGetter;

            Bind();
        }

        private void Bind()
        {
            // 前のバーにポジションが変わった時のメソッドを購読
            backData?.NextZ
                .Subscribe(AdjustPositionOnChangeNextZ)
                .AddTo(this.gameObject);

            // BPM変化
            subDivisionData?.Bpm
                .Subscribe(value => {
                    AdjustPositionOnChangeLineData();
                    SetSubDivisionLineData(subDivisionData, backData.SubDivisionData);
                })
                .AddTo(this.gameObject);

            // 前のBPMが変わった時
            backData.SubDivisionData?.Bpm
                .Subscribe(value => {
                    SetSubDivisionLineData(subDivisionData, backData.SubDivisionData);
                })
                .AddTo(this.gameObject);
        }


        /// <summary>
        /// 分線上のデータ更新
        /// </summary>
        /// <param name="barData"></param>
        private void SetSubDivisionLineData(SubDivisionDataInBeat subDivisionData, SubDivisionDataInBeat backData)
        {
            // BPM
            float bpm = backData == null || subDivisionData.Bpm.Value != backData.Bpm.Value ?
                subDivisionData.Bpm.Value : -1;

            lineInfo_view.SetDatas(bpm);
        }

        /// <summary>
        /// 前の線位置がずれたとき、この線位置も調整する(数珠繋ぎ)
        /// </summary>
        private void AdjustPositionOnChangeNextZ(float currentZ)
        {
            // このオブジェクトの位置調整
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                currentZ
                );

            float chartLengthParSecond = optionGetter.ChartViewScale.Value;
            float beatUnit = backData.BarData.BeatUnit.Value;
            float bpm = subDivisionData.Bpm.Value;
            int divNum = backData.BarData.DivisionNum.Value;

            // zの追加
            // += 1秒あたりのz距離 * 秒数
            //  = 1秒あたりのz距離 * (60f / bpm) * (4f / beatUnit) / 分割数
            nextZ.Value = currentZ + chartLengthParSecond * (60f / bpm) * (4f / beatUnit) / divNum;
        }

        /// <summary>
        /// 小節データが変わった時、次の小節位置を調整する
        /// </summary>
        private void AdjustPositionOnChangeLineData()
        {
            float chartLengthParSecond = optionGetter.ChartViewScale.Value;
            float beatUnit = backData.BarData.BeatUnit.Value;
            float bpm = subDivisionData.Bpm.Value;
            int divNum = backData.BarData.DivisionNum.Value;

            // zの追加
            // += 1秒あたりのz距離 * 秒数
            //  = 1秒あたりのz距離 * (60f / bpm) * (4f / beatUnit) / 分割数
            nextZ.Value = transform.position.z + chartLengthParSecond * (60f / bpm) * (4f / beatUnit) / divNum;
        }

    }

    public interface ISubDivisionDataGetter
    {
        SubDivisionDataInBeat SubDivisionData { get; }
    }

}
