using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class ConfigEditor : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        ReactiveProperty<IRhythmConfigurableSubDivisionCollider> subConfigCollider = new ReactiveProperty<IRhythmConfigurableSubDivisionCollider>();
        public IReadOnlyReactiveProperty<IRhythmConfigurableSubDivisionCollider> SubDivisionConfig => subConfigCollider;

        ReactiveProperty <IRhythmConfigurableBarCollider> barConfigCollider = new ReactiveProperty<IRhythmConfigurableBarCollider>();
        public IReadOnlyReactiveProperty<IRhythmConfigurableBarCollider> BarConfig => barConfigCollider;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        private void Update()
        {
            if(dataGetter.EditNoteType.Value == EditNoteType.Vertices) { return; }
            if(dataGetter.CurrentEditMode.Value != EditMode.EditBarConfig && dataGetter.CurrentEditMode.Value != EditMode.EditSubDivisionConfig) { return; }

            // 左クリック
            if (Input.GetMouseButtonDown(0)) { EditConfig(); }
        }

        /// <summary>
        /// コンフィグの編集
        /// </summary>
        private void EditConfig()
        {
            var subDivisionCollider = dataGetter.GetInteractableCollider<IRhythmConfigurableSubDivisionCollider>();
            var barCollider = dataGetter.GetInteractableCollider<IRhythmConfigurableBarCollider>();

            // エディットモードの変更
            if(subDivisionCollider != null)
            {
                subConfigCollider.Value = subDivisionCollider;
                dataSetter.SetEditMode(EditMode.EditingSubDivisionConfig);
            }
            else if(barCollider != null)
            {
                barConfigCollider.Value = barCollider;
                dataSetter.SetEditMode(EditMode.EditingBarConfig);
            }
        }

        public void ChangeBarConfig(BarConfig barConfig)
        {
            int barIndex = barConfigCollider.Value.BarDataGetter.BarData.BarIndex;
            var previousBarData = barConfigCollider.Value.BarDataGetter.BarData;
            var previousBarConfig = previousBarData.BarConfig;

            // コンフィグが変更できるか調べる
            // 公約数分線上意外にノーツがある場合は変更できない
            int oldCount = previousBarData.SubDivisionDatas.Count;
            int newCount = barConfig.BeatCount * barConfig.DivisionNum;
            for (int i = 0; i < oldCount; i++) 
            {
                // 整数かどうかの判定,分線の位置が被るかどうかの判定
                Debug.Log((newCount * i) % oldCount);
                if ((newCount * i) % oldCount == 0) { continue; }

                // 被らない分線上にノートがあったら警告吐いて終了
                if(previousBarData.SubDivisionDatas[i].NoteDatas.Count > 0) 
                {
                    Debug.Log($"【コンフィグ】対応しない分線上にノーツがあるため変更できません");
                    return;
                }
            }

            // 変更
            Record(() => {
                dataGetter.ChartData.Value.SetBarDataConfig(barIndex, barConfig);
            }, 
            // 元に戻す
            () => {
                dataGetter.ChartData.Value.SetBarDataConfig(barIndex, previousBarConfig);
            });
        }

        public void ChangeSubDivisionConfig(SubdivisionConfig subConfig)
        {
            int barIndex = subConfigCollider.Value.SubDivisionDataGetter.SubDivisionData.BarIndex;
            int subIndex = subConfigCollider.Value.SubDivisionDataGetter.SubDivisionData.SubDivisionIndex;
            var previousSubConfig = subConfigCollider.Value.SubDivisionDataGetter.SubDivisionData.SubConfig;

            // 変更
            Record(() => {
                dataGetter.ChartData.Value.SetSubDivisionConfig(barIndex, subIndex, subConfig);
            },
            // 元に戻す
            () => {
                dataGetter.ChartData.Value.SetSubDivisionConfig(barIndex, subIndex, previousSubConfig);
            });
        }

        public void ResetConfig()
        {
            dataSetter.SetEditMode(EditMode.None);
        }
    }

}