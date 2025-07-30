using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class ScreenController : MonoBehaviour
    {
        IChartEditorOptionGetter optionGetter;

        [Inject]
        public void Constructor(IChartEditorOptionGetter optionGetter)
        {
            this.optionGetter = optionGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            optionGetter?.Resolution.
                Subscribe(ChangeResolution)
                .AddTo(this.gameObject);
        }

        private void ChangeResolution(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.w1920_1080:
                    Screen.SetResolution(1920, 1080, false);
                    break;
                case Resolution.w1280_720:
                    Screen.SetResolution(1280, 720, false);
                    break;
                case Resolution.fullScreen:
                    Screen.SetResolution(1920, 1080, true);
                    break;
                default:
                    Debug.LogWarning($"ÅySystemÅzëŒâûÇ∑ÇÈâëúìxÇ™ê›íËÇ≥ÇÍÇƒÇ¢Ç‹ÇπÇÒ: {resolution}");
                    break;
            }
        }

    }

}
