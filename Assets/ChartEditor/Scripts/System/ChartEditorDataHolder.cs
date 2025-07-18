using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace ChartEditor
{
    public class ChartEditorDataHolder : IChartEditorDataGetter, IChartEditorDataSetter
    {
        #region Chart 譜面関係

        ReactiveProperty<ChartData> chartData = new ReactiveProperty<ChartData>();
        public IReadOnlyReactiveProperty<ChartData> ChartData => chartData;
        public void SetChartData(ChartData chartData)
        {
            // リセット
            if (this.chartData != null && this.chartData.Value != null && this.chartData.Value.BarDatas != null) 
            { 
                this.chartData.Value.RemoveBar(this.chartData.Value.BarDatas.Count);
            }

            this.chartData.Value = chartData;
        }

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
            //Debug.Log($"Change Edit Mode: {currentEditMode.Value}"); 
        }

        // 編集ノーツタイプ
        ReactiveProperty<EditNoteType> editNoteType = new ReactiveProperty<EditNoteType>(EditNoteType.Ground);
        IReadOnlyReactiveProperty<EditNoteType> IChartEditorDataGetter.EditNoteType => editNoteType;
        public void SetEditNoteType(EditNoteType editNoteType)
        {
            if (this.editNoteType.Value == editNoteType) { return; }

            this.editNoteType.Value = editNoteType;
            Debug.Log($"Change Edit Note Type: {this.editNoteType.Value}");
        }
        void IChartEditorDataSetter.SwitchEditNoteType()
        {
            EditNoteType current = editNoteType.Value;
            EditNoteType next = EditNoteType.Ground;
            switch (current)
            {
                case EditNoteType.Ground:
                    next = EditNoteType.Space;
                    break;

                case EditNoteType.Space:
                    next = EditNoteType.Ground;
                    break;

                case EditNoteType.Vertices:
                    next = EditNoteType.Space;
                    break;
            }
            SetEditNoteType(next);
        }

        #endregion

        #region ノーツデータ関係

        // 配置中のノーツタイプ
        ReactiveProperty<DeploymentNoteType> deploymentNoteType = new ReactiveProperty<DeploymentNoteType>(ChartEditor.DeploymentNoteType.TouchNote);
        IReadOnlyReactiveProperty<DeploymentNoteType> IChartEditorDataGetter.DeploymentNoteType => deploymentNoteType;
        void IChartEditorDataSetter.SetNoteType(DeploymentNoteType noteType)
        {
            deploymentNoteType.Value = noteType;
        }

        // インタラクトされているコライダーたち
        ReactiveCollection<IInteractableCollider> interactableColliders = new ReactiveCollection<IInteractableCollider>();
        public IReadOnlyReactiveCollection<IInteractableCollider> InteractableColliders => interactableColliders;
        public T GetInteractableCollider<T>() where T : IInteractableCollider
        {
            foreach (var col in interactableColliders)
            {
                if (col is T matched) { return matched; }
            }

            return default;
        }
        public void SetInteractableColliders(IInteractableCollider[] colliders)
        {
            interactableColliders.Clear();

            if(colliders == null) { return; }
            foreach (var col in colliders)
            {
                interactableColliders.Add(col);
            }
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

        IReadOnlyReactiveProperty<EditNoteType> EditNoteType { get; }

        IReadOnlyReactiveCollection<IInteractableCollider> InteractableColliders { get; }

        T GetInteractableCollider<T>() where T : IInteractableCollider;

        IReadOnlyReactiveProperty<DeploymentNoteType> DeploymentNoteType { get; }

        IReadOnlyReactiveProperty<PlayMode> PlayMode { get; }

        IReadOnlyReactiveProperty<float> PlaybackProgress { get; }

        IReadOnlyReactiveProperty<float> Offset { get; }

        IReadOnlyReactiveProperty<AudioClip> Music { get; }

    }

    public interface IChartEditorDataSetter
    {
        public void ChangeChartLength(int delta);

        public void SetChartData(ChartData chartData);

        public void SetChartSeconds(float seconds);

        void SetEditMode(EditMode editMode);

        void SetEditNoteType(EditNoteType editNoteType);

        void SwitchEditNoteType();

        void SetNoteType(DeploymentNoteType noteType);

        void SetInteractableColliders(IInteractableCollider[] colliders);

        void SetPlayMode(PlayMode playMode);

        void SetPlaybackProgress(float value);

        void SetOffset(float offset);

        void SetMusic(AudioClip clip);
    }
}
