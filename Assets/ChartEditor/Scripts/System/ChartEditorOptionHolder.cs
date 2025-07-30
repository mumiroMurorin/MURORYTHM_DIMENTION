using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class ChartEditorOptionHolder : IChartEditorOptionGetter, IChartEditorOptionSetter
    {

        #region DivNum レーン分割数

        ReactiveProperty<int> laneDivisionNum = new ReactiveProperty<int>(4);
        IReadOnlyReactiveProperty<int> IChartEditorOptionGetter.LaneDivisionNum => laneDivisionNum;

        void IChartEditorOptionSetter.SetLaneDivisionNum(bool isNext)
        {
            int num = laneDivisionNum.Value;

            // 最大値の時1に戻す
            if (num >= 16 && isNext) { laneDivisionNum.Value = 1; }
            // 最小値の時16にする
            else if (num <= 1 && !isNext) { laneDivisionNum.Value = 16; }
            // *2
            else if (isNext && num > 0 && (num & (num - 1)) == 0) { laneDivisionNum.Value = num * 2; }
            // ÷2
            else if (!isNext && num > 0 && (num & (num - 1)) == 0) { laneDivisionNum.Value = num / 2; }
            // それ以外の時1に戻す
            else { laneDivisionNum.Value = 1; }
        }

        #endregion

        #region Scale 拡大率

        const float MAX_SCALE = 100f;
        const float MIN_SCALE = 1f;

        /// <summary>
        /// 1秒間のグラウンド長さ
        /// </summary>
        ReactiveProperty<float> chartViewScale = new ReactiveProperty<float>(5f);
        IReadOnlyReactiveProperty<float> IChartEditorOptionGetter.ChartViewScale => chartViewScale;

        void IChartEditorOptionSetter.SetChartViewScale(float scale)
        {
            chartViewScale.Value = Mathf.Clamp(scale, MIN_SCALE, MAX_SCALE);
        }

        #endregion

        #region Sensitivity 移動感度

        ReactiveProperty<float> scrollSensitivity = new ReactiveProperty<float>(0.5f);
        IReadOnlyReactiveProperty<float> IChartEditorOptionGetter.ScrollSensitivity => scrollSensitivity;

        void IChartEditorOptionSetter.SetScrollSensitivity(float sensitivity)
        {
            scrollSensitivity.Value = Mathf.Clamp01(sensitivity);
        }

        #endregion

        #region スクリーンサイズ

        ReactiveProperty<Resolution> resolution = new ReactiveProperty<Resolution>(ChartEditor.Resolution.w1920_1080);
        public IReadOnlyReactiveProperty<Resolution> Resolution => resolution;
        public void SetResolution(Resolution resolution)
        {
            this.resolution.Value = resolution;
        }

        #endregion
    }

    public interface IChartEditorOptionGetter
    {

        IReadOnlyReactiveProperty<int> LaneDivisionNum { get; }

        /// <summary>
        /// エディタの拡大倍率、1秒間のUnity長 [z/sec]
        /// </summary>
        IReadOnlyReactiveProperty<float> ChartViewScale { get; }

        IReadOnlyReactiveProperty<float> ScrollSensitivity { get; }

        IReadOnlyReactiveProperty<Resolution> Resolution { get; }
    }

    public interface IChartEditorOptionSetter
    {

        void SetLaneDivisionNum(bool isNext);

        void SetChartViewScale(float scale);

        void SetScrollSensitivity(float sensitivity);

        void SetResolution(Resolution resolution);
    }
}

