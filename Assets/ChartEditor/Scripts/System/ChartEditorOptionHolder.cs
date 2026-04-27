using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class ChartEditorOptionHolder : IChartEditorOptionGetter, IChartEditorOptionSetter, INoteSpawnDataOptionGetter, INoteSpawnDataOptionSetter, IVolumeGetter
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

        #region NoteSpeed ノーツ速度

        const int MAX_NOTESPEED = 500;
        const int MIN_NOTESPEED = 20;

        ReactiveProperty<float> noteSpeed = new ReactiveProperty<float>(200f);
        IReadOnlyReactiveProperty<float> INoteSpawnDataOptionGetter.NoteSpeed => noteSpeed;

        void INoteSpawnDataOptionSetter.SetNoteSpeed(float speed)
        {
            noteSpeed.Value = Mathf.Clamp(speed, MIN_NOTESPEED, MAX_NOTESPEED);
        }

        #endregion

        #region Offset オフセット

        const float MAX_OFFSET = 1000f;
        const float MIN_OFFSET = -1000f;

        // オフセット関係
        ReactiveProperty<float> offset = new ReactiveProperty<float>(0);
        public IReadOnlyReactiveProperty<float> OffsetMs => offset;
        public int OffsetDisplay => (int)offset.Value;


        public void SetOffsetMs(float offset)
        {
            this.offset.Value = Mathf.Clamp(offset, MIN_OFFSET, MAX_OFFSET);
        }

        #endregion

        #region SEVolume SE音量

        // SE関係
        ReactiveProperty<float> seVolume = new ReactiveProperty<float>(0.8f);
        public IReadOnlyReactiveProperty<float> SEVolume => seVolume;
        public void SetSEVolume(float value)
        {
            seVolume.Value = Mathf.Clamp01(value);
        }

        // BGM関係
        ReactiveProperty<float> bgmVolume = new ReactiveProperty<float>(0.8f);
        public IReadOnlyReactiveProperty<float> BGMVolume => bgmVolume;
        public void SetBGMVolume(float value)
        {
            bgmVolume.Value = Mathf.Clamp01(value);
        }

        // JUDGEMENTSE関係
        ReactiveProperty<float> judgementSeVolume = new ReactiveProperty<float>(0.8f);
        public IReadOnlyReactiveProperty<float> JudgementSEVolume => judgementSeVolume;
        public void SetJudgementSEVolume(float value)
        {
            judgementSeVolume.Value = Mathf.Clamp01(value);
        }

        #endregion

        #region AutoMode オートモード

        ReactiveProperty<bool> isAutoMode = new ReactiveProperty<bool>();
        public bool IsAutoMode { get { return isAutoMode.Value; } private set { isAutoMode.Value = value; } }
        public IReadOnlyReactiveProperty<bool> IsAutoModeRP => isAutoMode;
        public void SetAutoMode(bool isAutoMode)
        {
            IsAutoMode = isAutoMode;
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

        IReadOnlyReactiveProperty<float> JudgementSEVolume { get; }

        IReadOnlyReactiveProperty<Resolution> Resolution { get; }
    }

    public interface IChartEditorOptionSetter
    {

        void SetLaneDivisionNum(bool isNext);

        void SetChartViewScale(float scale);

        void SetScrollSensitivity(float sensitivity);

        void SetJudgementSEVolume(float value);

        void SetResolution(Resolution resolution);
    }
}

