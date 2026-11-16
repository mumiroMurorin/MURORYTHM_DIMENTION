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

        void IChartEditorOptionSetter.SelectLaneDivisionNum(int divisionNum)
        {
            // 選べるのは 1・2・4・8・16
            if (divisionNum < 1 || divisionNum > 16 || (divisionNum & (divisionNum - 1)) != 0) { return; }

            laneDivisionNum.Value = divisionNum;
        }

        #endregion

        #region OnionSkin 頂点レイヤーのオニオンスキン

        /// <summary>
        /// オニオンスキンで、前後それぞれ何枚の断面まで重ねて表示するか (0 で表示しない)
        /// </summary>
        public const int MAX_ONION_SKIN_COUNT = 10;
        const int DEFAULT_ONION_SKIN_COUNT = 3;

        ReactiveProperty<int> onionSkinCount = new ReactiveProperty<int>(DEFAULT_ONION_SKIN_COUNT);
        IReadOnlyReactiveProperty<int> IChartEditorOptionGetter.OnionSkinCount => onionSkinCount;

        void IChartEditorOptionSetter.SetOnionSkinCount(int count)
        {
            onionSkinCount.Value = Mathf.Clamp(count, 0, MAX_ONION_SKIN_COUNT);
        }

        #endregion

        #region Scale 拡大率

        const float MAX_SCALE = 100f;
        const float MIN_SCALE = 1f;
        /// <summary>譜面の拡大率の、最初の値 (メニューの「元の大きさ」)</summary>
        public const float DEFAULT_CHART_VIEW_SCALE = 5f;

        /// <summary>
        /// 1秒間のグラウンド長さ
        /// </summary>
        ReactiveProperty<float> chartViewScale = new ReactiveProperty<float>(DEFAULT_CHART_VIEW_SCALE);
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

        #region PlaybackSpeed 再生速度

        // 切り替えられる再生速度 (この順に切り替わり、最後の次は先頭に戻る)
        static readonly float[] playbackSpeeds = { 1f, 0.75f, 0.5f, 0.25f, 0.1f };

        /// <summary>
        /// 楽曲と譜面の再生速度 (1 = 等倍)
        /// </summary>
        ReactiveProperty<float> playbackSpeed = new ReactiveProperty<float>(1f);
        IReadOnlyReactiveProperty<float> IChartEditorOptionGetter.PlaybackSpeed => playbackSpeed;

        void IChartEditorOptionSetter.SwitchPlaybackSpeed()
        {
            int index = System.Array.IndexOf(playbackSpeeds, playbackSpeed.Value);
            playbackSpeed.Value = playbackSpeeds[(index + 1) % playbackSpeeds.Length];
        }

        void IChartEditorOptionSetter.SelectPlaybackSpeed(float speed)
        {
            // 切り替えられる速度の中から選ぶ (それ以外の値は受け付けない)
            foreach (float candidate in playbackSpeeds)
            {
                if (Mathf.Abs(candidate - speed) < 0.001f)
                {
                    playbackSpeed.Value = candidate;
                    return;
                }
            }
        }

        #endregion

        #region NoteSound ノーツ音

        /// <summary>
        /// 再生中にノーツが鳴らす音を、種類に関係なく1種類にそろえるか
        /// </summary>
        ReactiveProperty<bool> isSingleNoteSound = new ReactiveProperty<bool>(false);
        IReadOnlyReactiveProperty<bool> IChartEditorOptionGetter.IsSingleNoteSound => isSingleNoteSound;

        void IChartEditorOptionSetter.SwitchNoteSoundMode()
        {
            isSingleNoteSound.Value = !isSingleNoteSound.Value;
        }

        void IChartEditorOptionSetter.SelectNoteSoundMode(bool isSingle)
        {
            isSingleNoteSound.Value = isSingle;
        }

        #endregion

        #region 延長の設定 譜面を延長するときの、小節数・拍子・分割数

        // 値は、設定するときに、入力できる範囲 (ChartExtendSettings) へ収める
        ReactiveProperty<int> extendBarCount = new ReactiveProperty<int>(ChartExtendSettings.DEFAULT_BAR_COUNT);
        ReactiveProperty<int> extendBeatCount = new ReactiveProperty<int>(ChartExtendSettings.DEFAULT_BEAT_COUNT);
        ReactiveProperty<float> extendBeatUnit = new ReactiveProperty<float>(ChartExtendSettings.DEFAULT_BEAT_UNIT);
        ReactiveProperty<int> extendDivisionNum = new ReactiveProperty<int>(ChartExtendSettings.DEFAULT_DIVISION_NUM);

        IReadOnlyReactiveProperty<int> IChartEditorOptionGetter.ExtendBarCount => extendBarCount;
        IReadOnlyReactiveProperty<int> IChartEditorOptionGetter.ExtendBeatCount => extendBeatCount;
        IReadOnlyReactiveProperty<float> IChartEditorOptionGetter.ExtendBeatUnit => extendBeatUnit;
        IReadOnlyReactiveProperty<int> IChartEditorOptionGetter.ExtendDivisionNum => extendDivisionNum;

        void IChartEditorOptionSetter.SetExtendBarCount(int barCount)
        {
            extendBarCount.Value = Mathf.Clamp(barCount, ChartExtendSettings.MIN_BAR_COUNT, ChartExtendSettings.MAX_BAR_COUNT);
        }

        void IChartEditorOptionSetter.SetExtendBeatCount(int beatCount)
        {
            extendBeatCount.Value = Mathf.Clamp(beatCount, ChartExtendSettings.MIN_BEAT_COUNT, ChartExtendSettings.MAX_BEAT_COUNT);
        }

        void IChartEditorOptionSetter.SetExtendBeatUnit(float beatUnit)
        {
            if (float.IsNaN(beatUnit)) { return; }

            extendBeatUnit.Value = Mathf.Clamp(beatUnit, ChartExtendSettings.MIN_BEAT_UNIT, ChartExtendSettings.MAX_BEAT_UNIT);
        }

        void IChartEditorOptionSetter.SetExtendDivisionNum(int divisionNum)
        {
            extendDivisionNum.Value = Mathf.Clamp(divisionNum, ChartExtendSettings.MIN_DIVISION_NUM, ChartExtendSettings.MAX_DIVISION_NUM);
        }

        #endregion

        #region 画面部品の表示 譜面情報・全体マップ・ログ欄・クイックメニューの各セクションを、表示するかどうか

        // 最初に表示するかどうかは、部品ごとに決まっている (EditorPanelExtension.IsVisibleByDefault)
        // 前回の設定は、EditorPanelSwitcher が起動時に読み込んで、ここへ入れる
        readonly Dictionary<EditorPanel, ReactiveProperty<bool>> panelVisibilities = new Dictionary<EditorPanel, ReactiveProperty<bool>>();

        public IReadOnlyReactiveProperty<bool> GetPanelVisibility(EditorPanel panel)
        {
            return GetPanelVisibilityProperty(panel);
        }

        public void SetPanelVisible(EditorPanel panel, bool isVisible)
        {
            GetPanelVisibilityProperty(panel).Value = isVisible;
        }

        private ReactiveProperty<bool> GetPanelVisibilityProperty(EditorPanel panel)
        {
            if (!panelVisibilities.TryGetValue(panel, out var visibility))
            {
                visibility = new ReactiveProperty<bool>(panel.IsVisibleByDefault());
                panelVisibilities.Add(panel, visibility);
            }

            return visibility;
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

        #region NoteCurveRadius ノーツ軌道半径

        const float MAX_NOTE_CURVE_RADIUS = 10000f;
        const float MIN_NOTE_CURVE_RADIUS = 10f;
        // メニューで変えられる範囲と、1回で変える量 (ゲーム本編の設定と同じ)
        public const float MENU_MIN_NOTE_CURVE_RADIUS = 400f;
        public const float MENU_MAX_NOTE_CURVE_RADIUS = 2000f;
        public const float NOTE_CURVE_RADIUS_STEP = 200f;

        // 【ノーツ軌道】プレビューでもゲーム本編と同じ半径を使用する
        ReactiveProperty<float> noteCurveRadius = new ReactiveProperty<float>(2000f);
        IReadOnlyReactiveProperty<float> INoteSpawnDataOptionGetter.NoteCurveRadius => noteCurveRadius;

        void INoteSpawnDataOptionSetter.SetNoteCurveRadius(float radius)
        {
            noteCurveRadius.Value = Mathf.Clamp(radius, MIN_NOTE_CURVE_RADIUS, MAX_NOTE_CURVE_RADIUS);
        }

        #endregion

        #region NoteVisibleDistance

        const float MAX_NOTE_VISIBLE_DISTANCE = 200f;
        const float MIN_NOTE_VISIBLE_DISTANCE = 20f;
        public const float NOTE_VISIBLE_DISTANCE_STEP = 10f;

        // 判定位置より奥側に表示するノーツの距離
        ReactiveProperty<float> noteVisibleDistance = new ReactiveProperty<float>(100f);
        public IReadOnlyReactiveProperty<float> NoteVisibleDistance => noteVisibleDistance;
        public int NoteVisibleDistanceDisplay => Mathf.RoundToInt(noteVisibleDistance.Value);
        public void SetNoteVisibleDistance(float distance)
        {
            noteVisibleDistance.Value = Mathf.Clamp(
                distance,
                MIN_NOTE_VISIBLE_DISTANCE,
                MAX_NOTE_VISIBLE_DISTANCE);
        }

        bool AddNoteVisibleDistance(int delta)
        {
            if (delta == 0) { return false; }
            if (noteVisibleDistance.Value >= MAX_NOTE_VISIBLE_DISTANCE && delta > 0) { return false; }
            if (noteVisibleDistance.Value <= MIN_NOTE_VISIBLE_DISTANCE && delta < 0) { return false; }

            SetNoteVisibleDistance(noteVisibleDistance.Value + delta * NOTE_VISIBLE_DISTANCE_STEP);
            return true;
        }

        #endregion

        #region SpaceActionJudgeMagnitudeMultiplier

        const float MAX_SPACE_ACTION_JUDGE_MAGNITUDE_MULTIPLIER = 5f;
        const float MIN_SPACE_ACTION_JUDGE_MAGNITUDE_MULTIPLIER = 0.1f;

        ReactiveProperty<float> spaceActionJudgeMagnitudeMultiplier = new ReactiveProperty<float>(1f);
        public IReadOnlyReactiveProperty<float> SpaceActionJudgeMagnitudeMultiplier => spaceActionJudgeMagnitudeMultiplier;
        public void SetSpaceActionJudgeMagnitudeMultiplier(float multiplier)
        {
            spaceActionJudgeMagnitudeMultiplier.Value = Mathf.Clamp(
                multiplier,
                MIN_SPACE_ACTION_JUDGE_MAGNITUDE_MULTIPLIER,
                MAX_SPACE_ACTION_JUDGE_MAGNITUDE_MULTIPLIER);
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
        /// 頂点レイヤーのオニオンスキンで、編集中の断面の前後それぞれ何枚の断面まで重ねて表示するか (0 で表示しない)
        /// </summary>
        IReadOnlyReactiveProperty<int> OnionSkinCount { get; }

        /// <summary>
        /// エディタの拡大倍率、1秒間のUnity長 [z/sec]
        /// </summary>
        IReadOnlyReactiveProperty<float> ChartViewScale { get; }
        /// <summary>全画面プレビューで、判定位置より奥に表示する距離</summary>
        IReadOnlyReactiveProperty<float> NoteVisibleDistance { get; }

        IReadOnlyReactiveProperty<float> ScrollSensitivity { get; }

        /// <summary>
        /// 楽曲と譜面の再生速度 (1 = 等倍)
        /// </summary>
        IReadOnlyReactiveProperty<float> PlaybackSpeed { get; }

        /// <summary>
        /// 再生中にノーツが鳴らす音を、種類に関係なく1種類にそろえるか
        /// </summary>
        IReadOnlyReactiveProperty<bool> IsSingleNoteSound { get; }

        IReadOnlyReactiveProperty<float> JudgementSEVolume { get; }

        IReadOnlyReactiveProperty<Resolution> Resolution { get; }

        /// <summary>
        /// 譜面を延長するときの設定: 延長する小節数、延長する小節の拍子 (分子 / 分母)、分割数
        /// </summary>
        IReadOnlyReactiveProperty<int> ExtendBarCount { get; }
        IReadOnlyReactiveProperty<int> ExtendBeatCount { get; }
        IReadOnlyReactiveProperty<float> ExtendBeatUnit { get; }
        IReadOnlyReactiveProperty<int> ExtendDivisionNum { get; }

        /// <summary>
        /// 画面部品 (譜面情報、全体マップ、ログ欄、クイックメニューの各セクション) を、表示するかどうか
        /// </summary>
        IReadOnlyReactiveProperty<bool> GetPanelVisibility(EditorPanel panel);

        /// <summary>
        /// 楽曲の音量 (0〜1)
        /// </summary>
        IReadOnlyReactiveProperty<float> BGMVolume { get; }
    }

    public interface IChartEditorOptionSetter
    {

        void SetLaneDivisionNum(bool isNext);

        /// <summary>
        /// レーン分割数を、指定した値にする (1・2・4・8・16 のどれか)
        /// </summary>
        void SelectLaneDivisionNum(int divisionNum);

        /// <summary>
        /// オニオンスキンの枚数を変える (0〜MAX_ONION_SKIN_COUNT。範囲の外の値は、端の値になる)
        /// </summary>
        void SetOnionSkinCount(int count);

        void SetChartViewScale(float scale);
        void SetNoteVisibleDistance(float distance);

        void SetScrollSensitivity(float sensitivity);

        /// <summary>
        /// 再生速度を次の段階へ切り替える
        /// </summary>
        void SwitchPlaybackSpeed();

        /// <summary>
        /// 再生速度を、指定した値にする (切り替えられる速度のどれか)
        /// </summary>
        void SelectPlaybackSpeed(float speed);

        /// <summary>
        /// ノーツ音を「種類別」と「単一」で切り替える
        /// </summary>
        void SwitchNoteSoundMode();

        /// <summary>
        /// ノーツ音を、1種類にそろえるか(true)、種類ごとに変えるか(false)、指定する
        /// </summary>
        void SelectNoteSoundMode(bool isSingle);

        void SetJudgementSEVolume(float value);

        void SetResolution(Resolution resolution);

        /// <summary>
        /// 譜面を延長するときの設定を変える (入力できる範囲の外の値は、範囲の端の値になる)
        /// </summary>
        void SetExtendBarCount(int barCount);
        void SetExtendBeatCount(int beatCount);
        void SetExtendBeatUnit(float beatUnit);
        void SetExtendDivisionNum(int divisionNum);

        /// <summary>
        /// 画面部品を、表示する / しない
        /// </summary>
        void SetPanelVisible(EditorPanel panel, bool isVisible);

        /// <summary>
        /// 楽曲の音量を変える (0〜1)
        /// </summary>
        void SetBGMVolume(float value);
    }
}

