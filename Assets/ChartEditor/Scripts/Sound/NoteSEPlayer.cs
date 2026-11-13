using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteSEPlayer : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorOptionGetter optionGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionGetter optionGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.optionGetter = optionGetter;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (chartEditorDataGetter.PlayMode.Value != PlayMode.Play) { return; }
            if (!other.TryGetComponent(out NoteSoundCollider soundCollider)) { return; }

            // 設定に応じて、ノーツの種類ごとの音か、全ノーツ共通の音を鳴らす
            bool isSingleSound = optionGetter != null && optionGetter.IsSingleNoteSound.Value;
            soundCollider.PlaySE(isSingleSound);
        }
    }
}
