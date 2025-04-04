using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

namespace ChartEditor
{
    public class BarLine : MonoBehaviour
    {
        [SerializeField] TextMeshPro[] numberTmps;

        IChartEditorDataGetter chartEditorDataGetter;
        BarDataInChart barData;

        /// <summary>
        /// 次の小節線の開始位置
        /// </summary>
        ReactiveProperty<float> nextZ = new ReactiveProperty<float>();
        public IReadOnlyReactiveProperty<float> NextZ => nextZ;

        /// <summary>
        /// BarDataのセット、BarDataによる設定
        /// </summary>
        /// <param name="barData"></param>
        /// <param name="previousBar"></param>
        /// <param name="number"></param>
        public void SetBarData(BarDataInChart barData, BarLine previousBar, IChartEditorDataGetter chartEditorDataGetter, int number)
        {
            this.barData = barData;
            this.chartEditorDataGetter = chartEditorDataGetter;

            // 小節番号の設定
            foreach (TextMeshPro tmp in numberTmps)
            {
                tmp.text = number.ToString();
            }

            Bind(previousBar);
        }

        private void Bind(BarLine previousBar)
        {
            // 前のバーにポジションが変わった時のメソッドを購読
            previousBar?.NextZ
                .Subscribe(AdjustPositionOnChangeNextZ)
                .AddTo(this.gameObject);

            // 小節データに購読
            // N分のM拍子のどちらが変わっても長さは変わる
            barData?.BeatCount
                .Subscribe(beatCount => AdjustPositionOnChangeBarData(beatCount, barData.BeatUnit.Value))
                .AddTo(this.gameObject);

            barData?.BeatUnit
                .Subscribe(beatUnit => AdjustPositionOnChangeBarData(barData.BeatCount.Value, beatUnit))
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 前の小節位置がずれたとき、この小節位置も調整する(数珠繋ぎ)
        /// </summary>
        private void AdjustPositionOnChangeNextZ(float currentZ)
        {
            // このオブジェクトの位置調整
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                currentZ
                );

            float quarterNoteLength = chartEditorDataGetter.ChartViewScale.Value;
            float beatUnit = barData.BeatUnit.Value;
            float beatCount = barData.BeatCount.Value;

            // 次の小節開始位置 = 現在の開始位置 + M分音符の長さ * カウント数
            //                  = 現在の開始位置 + 4分音符の長さ / ( M分 / 4 ) * カウント数
            nextZ.Value = currentZ + quarterNoteLength / (beatUnit / 4f) * beatCount;
        }

        /// <summary>
        /// 小節データが変わった時、次の小節位置を調整する
        /// </summary>
        private void AdjustPositionOnChangeBarData(float beatCount, float beatUnit)
        {
            float quarterNoteLength = chartEditorDataGetter.ChartViewScale.Value;

            // 次の小節開始位置 = 現在の開始位置 + M分音符の長さ * カウント数
            //                  = 現在の開始位置 + 4分音符の長さ / ( M分 / 4 ) * カウント数
            nextZ.Value = transform.position.z + quarterNoteLength / (beatUnit / 4f) * beatCount;
        }
    }
}
