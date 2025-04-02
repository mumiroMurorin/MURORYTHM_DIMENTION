using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor 
{
    public class NoteSEPlayer : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (chartEditorDataGetter.PlayMode.Value != PlayMode.Play) { return; }
            if (!other.TryGetComponent(out NoteSoundCollider soundCollider)) { return; }

            soundCollider.PlaySE();
        }
    }
}

