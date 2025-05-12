using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class ChartLengthCalculater : MonoBehaviour
    {
        Dictionary<BarDataInChart, float> barDataToSeconds = new Dictionary<BarDataInChart, float>();
        IChartEditorDataSetter dataSetter;
        IChartEditorDataGetter dataGetter;

        [Inject]
        public void Construct(IChartEditorDataSetter dataSetter, IChartEditorDataGetter dataGetter)
        {
            this.dataSetter = dataSetter;
            this.dataGetter = dataGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            dataGetter.ChartData?
                .Subscribe(BindForChart)
                .AddTo(this.gameObject);
        }

        private void BindForChart(ChartData chartData)
        {
            if(chartData == null) { return; }

            barDataToSeconds = new Dictionary<BarDataInChart, float>();

            chartData.BarDatas.ObserveAdd()
                .Subscribe(data => {
                    OnAddBarInChart(data.Value);
                    BindForBar(data.Value);
                })
                .AddTo(this.gameObject);

            chartData.BarDatas.ObserveRemove()
                .Subscribe(data => {
                    OnRemoveBarInChart(data.Value);
                })
                .AddTo(this.gameObject);
        }

        private void BindForBar(BarDataInChart barData)
        {
            if(barData == null) { return; }

            // è¨êﬂÉfÅ[É^ì‡ÇÃïœçXÇ…ëŒÇ∑ÇÈçwì«
            barData.SubDivisionDatas.ObserveAdd()
                .Subscribe(sub => {
                    BindForSubDivisionData(sub.Value, barData);
                    OnChangeValueInBarData(barData);
                })
                .AddTo(this.gameObject);

            barData.BeatUnit
                .Subscribe(_ => OnChangeValueInBarData(barData))
                .AddTo(this.gameObject);

            foreach (var sub in barData.SubDivisionDatas)
            {
                BindForSubDivisionData(sub, barData);
            }
        }

        private void BindForSubDivisionData(SubDivisionDataInBeat subData, BarDataInChart barData)
        {
            if(subData == null) { return; }
            if(barData == null) { return; }

            subData.Bpm.Subscribe(_ => { OnChangeValueInBarData(barData); })
                   .AddTo(this.gameObject);
        }

        public void OnAddBarInChart(BarDataInChart barData)
        {
            float seconds = CalcTimeInBar(barData);
            barDataToSeconds.Add(barData, seconds);

            UpdateChartSeconds();
        }

        public void OnRemoveBarInChart(BarDataInChart barData)
        {
            barDataToSeconds.Remove(barData);
            UpdateChartSeconds();
        }

        public void OnChangeValueInBarData(BarDataInChart barData)
        {
            float seconds = CalcTimeInBar(barData);

            if (!barDataToSeconds.Remove(barData)) 
            {
                Debug.LogWarning("ÅySystemÅzäYìñÇ∑ÇÈè¨êﬂÉfÅ[É^Ç™å©Ç¬Ç©ÇËÇ‹ÇπÇÒÇ≈ÇµÇΩ");
                return;
            }
            barDataToSeconds.Add(barData, seconds);

            UpdateChartSeconds();
        }

        /// <summary>
        /// ïàñ éûä‘ÇÃçXêV
        /// </summary>
        private void UpdateChartSeconds()
        {
            float seconds = 0;
            foreach (var pair in barDataToSeconds)
            {
                seconds += pair.Value;
            }

            dataSetter.SetChartSeconds(seconds);
        }

        /// <summary>
        /// àÍè¨êﬂÇ†ÇΩÇËÇÃéûä‘(ïb)Çï‘Ç∑
        /// </summary>
        /// <param name="barData"></param>
        /// <returns></returns>
        private float CalcTimeInBar(BarDataInChart barData)
        {
            float seconds = 0;
            float beatUnit = barData.BeatUnit.Value;
            int divNum = barData.DivisionNum.Value;

            foreach (var subData in barData.SubDivisionDatas)
            {
                seconds += CalcTimeInSubDivision(subData, beatUnit, divNum);
            }

            return seconds;
        }

        /// <summary>
        /// àÍï™êﬂÇ†ÇΩÇËÇÃéûä‘(ïb)Çï‘Ç∑
        /// </summary>
        /// <param name="sub"></param>
        /// <param name="beatCount"></param>
        /// <param name="beatUnit"></param>
        /// <param name="divNum"></param>
        /// <returns></returns>
        private float CalcTimeInSubDivision(SubDivisionDataInBeat sub, float beatUnit, int divNum)
        {
            return (60f / sub.Bpm.Value) * (4f / beatUnit) / divNum;
        }
    }

}