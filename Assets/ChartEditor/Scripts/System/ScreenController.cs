using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class ScreenController : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;

        [Inject]
        public void Constructor(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            dataGetter?.Resolution.
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
                    Debug.LogWarning($"ySystemz‘Î‰‚·‚é‰ğ‘œ“x‚ªİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ: {resolution}");
                    break;
            }
        }

    }

}
