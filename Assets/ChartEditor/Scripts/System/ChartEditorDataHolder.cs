using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class ChartEditorDataHolder : IChartEditorDataGetter, IChartEditorDataSetter
    {
        #region Chart 譜面関係

        ReactiveProperty<ChartData> chartData = new ReactiveProperty<ChartData>();

        public IReadOnlyReactiveProperty<ChartData> ChartData => chartData;

        public void InitializeChartData()
        {
            if(music.Value == null) { return; }

            chartData.Value = new ChartData(music.Value.length, 256);
        }

        #endregion

        #region EditMode エディットモード関係

        // エディットモード
        ReactiveProperty<EditMode> currentEditMode = new ReactiveProperty<EditMode>(EditMode.None);
        IReadOnlyReactiveProperty<EditMode> IChartEditorDataGetter.CurrentEditMode => currentEditMode;

        void IChartEditorDataSetter.SetEditMode(EditMode editMode) 
        {
            if(currentEditMode.Value == editMode) { return; }

            currentEditMode.Value = editMode;
            if (!autoEditMode.Value) { Debug.Log($"Change Edit Mode: {currentEditMode.Value}"); }
        }


        // 自動エディット設定モード
        ReactiveProperty<bool> autoEditMode = new ReactiveProperty<bool>();
        IReadOnlyReactiveProperty<bool> IChartEditorDataGetter.AutoEditMode => autoEditMode;

        void IChartEditorDataSetter.SetAutoEditMode(bool isEnable)
        {
            if (autoEditMode.Value == isEnable) { return; }

            autoEditMode.Value = isEnable;
            Debug.Log($"Change Auto Edit Mode: {autoEditMode.Value}");
        }


        #endregion

        #region DeploymentNoteType 配置ノーツ関係

        ReactiveProperty<DeploymentNoteType> deploymentNoteType = new ReactiveProperty<DeploymentNoteType>(ChartEditor.DeploymentNoteType.TouchNote);
        IReadOnlyReactiveProperty<DeploymentNoteType> IChartEditorDataGetter.DeploymentNoteType => deploymentNoteType;
        void IChartEditorDataSetter.SetNoteType(DeploymentNoteType noteType)
        {
            deploymentNoteType.Value = noteType;
            Debug.Log($"Change Deployment Note: {deploymentNoteType.Value}");
        }

        #endregion

        #region Deploy 配置場所

        ReactiveProperty<IDeployableCollider> deployableCollider = new ReactiveProperty<IDeployableCollider>();
        IReadOnlyReactiveProperty<IDeployableCollider> IChartEditorDataGetter.DeployableCollider => deployableCollider;

        void IChartEditorDataSetter.SetDeployableCollider(IDeployableCollider collider)
        {
            if(deployableCollider.Value == collider) { return; }
            deployableCollider.Value = collider;
        }

        #endregion

        #region BarConfig 小節線に対する設定

        ReactiveProperty<IRhythmConfigurableBarCollider> rhythmConfigurableBar = new ReactiveProperty<IRhythmConfigurableBarCollider>();
        IReadOnlyReactiveProperty<IRhythmConfigurableBarCollider> IChartEditorDataGetter.RhythmConfigurableBar => rhythmConfigurableBar;

        void IChartEditorDataSetter.SetRhythmConfigurableBar(IRhythmConfigurableBarCollider rCollider)
        {
            if (rhythmConfigurableBar.Value == rCollider) { return; }
            rhythmConfigurableBar.Value = rCollider;
        }

        #endregion

        #region SubDivisionConfig 分線(拍線)に対する設定

        ReactiveProperty<IRhythmConfigurableSubDivisionCollider> rhythmConfigurableSubDivision = new ReactiveProperty<IRhythmConfigurableSubDivisionCollider>();
        IReadOnlyReactiveProperty<IRhythmConfigurableSubDivisionCollider> IChartEditorDataGetter.RhythmConfigurableSubDivision => rhythmConfigurableSubDivision;

        void IChartEditorDataSetter.SetRhythmConfigurableSubDivision(IRhythmConfigurableSubDivisionCollider sCollider)
        {
            if (rhythmConfigurableSubDivision.Value == sCollider) { return; }
            rhythmConfigurableSubDivision.Value = sCollider;
        }

        #endregion

        #region ChartEdit 譜面の編集系

        // ノーツの再配置
        ReactiveProperty<IMovableObject> movableObject= new ReactiveProperty<IMovableObject>();
        IReadOnlyReactiveProperty<IMovableObject> IChartEditorDataGetter.MovableObject => movableObject;

        void IChartEditorDataSetter.SetMovableObject(IMovableObject mObject)
        {
            if (movableObject.Value == mObject) { return; }

            movableObject.Value = mObject;
        }

        // ノーツの拡大縮小
        bool isRightAnchored;
        bool IChartEditorDataGetter.IsRightAnchored => isRightAnchored;

        ReactiveProperty<IScalableObject> scalableObject = new ReactiveProperty<IScalableObject>();
        IReadOnlyReactiveProperty<IScalableObject> IChartEditorDataGetter.ScalableObject => scalableObject;

        void IChartEditorDataSetter.SetScalableObject(IScalableObject sObject, bool isRightAnchored)
        {
            if(scalableObject.Value == sObject) { return; }

            scalableObject.Value = sObject;
            this.isRightAnchored = isRightAnchored;
        }

        // ノーツの削除
        ReactiveProperty<IDestroyableObject> destroyableObject = new ReactiveProperty<IDestroyableObject>();
        IReadOnlyReactiveProperty<IDestroyableObject> IChartEditorDataGetter.DestroyableObject => destroyableObject;
        void IChartEditorDataSetter.SetDestroyableObject(IDestroyableObject dObject)
        {
            if (destroyableObject.Value == dObject) { return; }

            destroyableObject.Value = dObject;
        }

        #endregion

        #region PlayMode プレイモード関係

        ReactiveProperty<PlayMode> playMode = new ReactiveProperty<PlayMode>(PlayMode.Stop);
        IReadOnlyReactiveProperty<PlayMode> IChartEditorDataGetter.PlayMode => playMode;

        void IChartEditorDataSetter.SetPlayMode(PlayMode playMode)
        {
            this.playMode.Value = playMode;
            Debug.Log($"Change Play Mode: {this.playMode.Value}");
        }

        #endregion

        #region PlaybackProgress 再生位置

        ReactiveProperty<float> playbackProgress = new ReactiveProperty<float>(0);
        IReadOnlyReactiveProperty<float> IChartEditorDataGetter.PlaybackProgress => playbackProgress;

        void IChartEditorDataSetter.SetPlaybackProgress(float value)
        {
            playbackProgress.Value = Mathf.Clamp01(value);
        }

        #endregion

        #region DivNum レーン分割数

        ReactiveProperty<int> laneDivisionNum = new ReactiveProperty<int>(4);
        IReadOnlyReactiveProperty<int> IChartEditorDataGetter.LaneDivisionNum => laneDivisionNum;

        void IChartEditorDataSetter.SetLaneDivisionNum(bool isNext)
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

        /// <summary>
        /// 1秒間のグラウンド長さ
        /// </summary>
        ReactiveProperty<float> chartViewScale = new ReactiveProperty<float>(5f);
        IReadOnlyReactiveProperty<float> IChartEditorDataGetter.ChartViewScale => chartViewScale;

        void IChartEditorDataSetter.SetChartViewScale(float scale)
        {
            chartViewScale.Value = Mathf.Clamp(scale, 0.1f, float.MaxValue);
        }

        #endregion

        #region Music 再生音楽

        ReactiveProperty<AudioClip> music = new ReactiveProperty<AudioClip>();
        IReadOnlyReactiveProperty<AudioClip> IChartEditorDataGetter.Music => music;

        void IChartEditorDataSetter.SetMusic(AudioClip clip)
        {
            music.Value = clip;
        }

        #endregion

        #region Offset オフセット

        ReactiveProperty<float> offset = new ReactiveProperty<float>(0);
        IReadOnlyReactiveProperty<float> IChartEditorDataGetter.Offset => offset;

        void IChartEditorDataSetter.SetOffset(float offset)
        {
            this.offset.Value = offset;
        }

        #endregion
    }

    public interface IChartEditorDataGetter
    {
        IReadOnlyReactiveProperty<ChartData> ChartData { get; }

        IReadOnlyReactiveProperty<EditMode> CurrentEditMode { get; }

        IReadOnlyReactiveProperty<bool> AutoEditMode { get; }

        IReadOnlyReactiveProperty<IRhythmConfigurableBarCollider> RhythmConfigurableBar { get; }

        IReadOnlyReactiveProperty<IRhythmConfigurableSubDivisionCollider> RhythmConfigurableSubDivision { get; }

        IReadOnlyReactiveProperty<DeploymentNoteType> DeploymentNoteType { get; }

        IReadOnlyReactiveProperty<IDeployableCollider> DeployableCollider { get; }

        IReadOnlyReactiveProperty<IMovableObject> MovableObject { get; }

        bool IsRightAnchored { get; }

        IReadOnlyReactiveProperty<IScalableObject> ScalableObject { get; }

        IReadOnlyReactiveProperty<IDestroyableObject> DestroyableObject { get; }

        IReadOnlyReactiveProperty<PlayMode> PlayMode { get; }

        IReadOnlyReactiveProperty<float> PlaybackProgress { get; }

        IReadOnlyReactiveProperty<int> LaneDivisionNum { get; }

        /// <summary>
        /// エディタの拡大倍率、1秒間のUnity長 [z/sec]
        /// </summary>
        IReadOnlyReactiveProperty<float> ChartViewScale { get; }

        IReadOnlyReactiveProperty<float> Offset { get; }

        IReadOnlyReactiveProperty<AudioClip> Music { get; }

    }

    public interface IChartEditorDataSetter
    {
        public void InitializeChartData();

        void SetEditMode(EditMode editMode);

        void SetAutoEditMode(bool isEnable);

        void SetNoteType(DeploymentNoteType noteType);

        void SetRhythmConfigurableBar(IRhythmConfigurableBarCollider rCollider);

        void SetRhythmConfigurableSubDivision(IRhythmConfigurableSubDivisionCollider rCollider);

        void SetDeployableCollider(IDeployableCollider collider);

        void SetMovableObject(IMovableObject mObject);

        void SetScalableObject(IScalableObject sObject, bool isRightAnchored);

        void SetDestroyableObject(IDestroyableObject dObject);

        void SetPlayMode(PlayMode playMode);

        void SetPlaybackProgress(float value);

        void SetLaneDivisionNum(bool isNext);

        void SetChartViewScale(float scale);

        void SetOffset(float offset);

        void SetMusic(AudioClip clip);
    }
}
