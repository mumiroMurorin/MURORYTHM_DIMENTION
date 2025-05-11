using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class LaneController : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ILaneDeployable<BarDataInChart>> barLineDeplayable;
        [SerializeField] GameObject ground;

        IChartEditorOptionGetter optionGetter;
        IChartEditorDataGetter dataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataGetter = dataGetter;
            this.optionGetter = optionGetter;
        }

        void Start()
        {
            Initialize();
            Bind();
        }

        private void Initialize()
        {

        }

        private void Bind()
        {
            // 拡大率
            // グラウンド長さ更新
            dataGetter?.ChartSeconds
                .Subscribe(seconds => UpdateGroundLength(seconds, optionGetter.ChartViewScale.Value))
                .AddTo(this.gameObject);

            optionGetter?.ChartViewScale
                .Subscribe(scale => UpdateGroundLength(dataGetter.ChartSeconds.Value, scale))
                .AddTo(this.gameObject);
        }


        private void UpdateGroundLength(float chartSeconds, float viewScale)
        {
            float chartLength = viewScale * chartSeconds;

            // グラウンドの生成
            ground.transform.localScale = new Vector3(
                ground.transform.localScale.x,
                chartLength,
                ground.transform.localScale.z);

            ground.transform.position = new Vector3(
                ground.transform.position.x,
                ground.transform.position.y,
                ground.transform.localScale.y / 2f
                );
        }
    }

}
