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

        public void ChangeChartLength(int delta)
        {
            if(ChartData.Value == null)
            {
                chartData.Value = new ChartData(0);
            }

            if (delta > 0) { chartData.Value.AddBar(delta); }
            else if(delta < 0) { chartData.Value.RemoveBar(Mathf.Abs(delta)); }
        }

        // 譜面長さ
        ReactiveProperty<float> chartSeconds = new ReactiveProperty<float>(0);
        public IReadOnlyReactiveProperty<float> ChartSeconds => chartSeconds;

        public void SetChartSeconds(float seconds)
        {
            if(seconds < 0) { return; }
            chartSeconds.Value = seconds;
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

        // ノーツの接続
        ReactiveProperty<IConnectableObject> connectableObject = new ReactiveProperty<IConnectableObject>();
        IReadOnlyReactiveProperty<IConnectableObject> IChartEditorDataGetter.ConnectableObject => connectableObject;
        void IChartEditorDataSetter.SetConnectableObject(IConnectableObject cObject)
        {
            if (connectableObject.Value == cObject) { return; }

            connectableObject.Value = cObject;
        }

        // ノーツタイプの変更
        ReactiveProperty<IChangableObject> changableObject = new ReactiveProperty<IChangableObject>();
        IReadOnlyReactiveProperty<IChangableObject> IChartEditorDataGetter.ChangableObject => changableObject;
        void IChartEditorDataSetter.SetChangableObject(IChangableObject cObject)
        {
            if (changableObject.Value == cObject) { return; }

            changableObject.Value = cObject;
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

        IReadOnlyReactiveProperty<float> ChartSeconds { get; }

        IReadOnlyReactiveProperty<EditMode> CurrentEditMode { get; }

        IReadOnlyReactiveProperty<bool> AutoEditMode { get; }

        IReadOnlyReactiveProperty<IRhythmConfigurableBarCollider> RhythmConfigurableBar { get; }

        IReadOnlyReactiveProperty<IRhythmConfigurableSubDivisionCollider> RhythmConfigurableSubDivision { get; }

        IReadOnlyReactiveProperty<DeploymentNoteType> DeploymentNoteType { get; }

        IReadOnlyReactiveProperty<IDeployableCollider> DeployableCollider { get; }

        IReadOnlyReactiveProperty<IMovableObject> MovableObject { get; }

        bool IsRightAnchored { get; }

        IReadOnlyReactiveProperty<IScalableObject> ScalableObject { get; }

        IReadOnlyReactiveProperty<IConnectableObject> ConnectableObject { get; }

        IReadOnlyReactiveProperty<IChangableObject> ChangableObject { get; }

        IReadOnlyReactiveProperty<IDestroyableObject> DestroyableObject { get; }

        IReadOnlyReactiveProperty<PlayMode> PlayMode { get; }

        IReadOnlyReactiveProperty<float> PlaybackProgress { get; }

        IReadOnlyReactiveProperty<float> Offset { get; }

        IReadOnlyReactiveProperty<AudioClip> Music { get; }

    }

    public interface IChartEditorDataSetter
    {
        public void ChangeChartLength(int delta);

        public void SetChartSeconds(float seconds);

        void SetEditMode(EditMode editMode);

        void SetAutoEditMode(bool isEnable);

        void SetNoteType(DeploymentNoteType noteType);

        void SetRhythmConfigurableBar(IRhythmConfigurableBarCollider rCollider);

        void SetRhythmConfigurableSubDivision(IRhythmConfigurableSubDivisionCollider rCollider);

        void SetDeployableCollider(IDeployableCollider collider);

        void SetMovableObject(IMovableObject mObject);

        void SetScalableObject(IScalableObject sObject, bool isRightAnchored);

        void SetConnectableObject(IConnectableObject cObject);

        void SetChangableObject(IChangableObject cObject);

        void SetDestroyableObject(IDestroyableObject dObject);

        void SetPlayMode(PlayMode playMode);

        void SetPlaybackProgress(float value);

        void SetOffset(float offset);

        void SetMusic(AudioClip clip);
    }
}
