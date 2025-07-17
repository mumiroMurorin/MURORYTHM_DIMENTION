using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class ConfigEditor : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

        ReactiveProperty<IRhythmConfigurableSubDivisionCollider> subDivisionConfig = new ReactiveProperty<IRhythmConfigurableSubDivisionCollider>();
        public IReadOnlyReactiveProperty<IRhythmConfigurableSubDivisionCollider> SubDivisionConfig => subDivisionConfig;

        ReactiveProperty <IRhythmConfigurableBarCollider> barConfig = new ReactiveProperty<IRhythmConfigurableBarCollider>();
        public IReadOnlyReactiveProperty<IRhythmConfigurableBarCollider> BarConfig => barConfig;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.dataGetter = dataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
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
                chartEditorDataSetter.SetEditMode(EditMode.EditingSubDivisionConfig);
            }
            else if(barCollider != null)
            {
                chartEditorDataSetter.SetEditMode(EditMode.EditingBarConfig);
            }
            else
            {
                return;
            }

            // コライダーを保存、発火
            subDivisionConfig.Value = subDivisionCollider;
            barConfig.Value = barCollider;
        }

        public void ResetConfig()
        {
            subDivisionConfig.Value = null;
            barConfig.Value = null;

            chartEditorDataSetter.SetEditMode(EditMode.None);
        }
    }

}