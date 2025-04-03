using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class LaneController : MonoBehaviour
    {
        [SerializeField] List<SerializeInterface<ILaneDeployable>> deplayables;
        [SerializeField] GameObject ground;

        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
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
            // ägëÂó¶
            chartEditorDataGetter?.ChartViewScale
                .Pairwise()
                .Subscribe(OnChangeChartViewScale)
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// ägëÂó¶ÇÊÇËägëÂèkè¨ÇçsÇ§
        /// </summary>
        /// <param name="scale"></param>
        private void OnChangeChartViewScale(Pair<float> pairScale)
        {
            // äeê¸
            foreach (SerializeInterface<ILaneDeployable> deployable in deplayables)
            {
                deployable.Value.Scaling(pairScale.Current, pairScale.Previous);
            }

            // ÉOÉâÉEÉìÉh
            ground.transform.localScale = new Vector3(
                ground.transform.localScale.x,
                ground.transform.localScale.y * (pairScale.Current / pairScale.Previous),
                ground.transform.localScale.z);

            ground.transform.position = new Vector3(
                ground.transform.position.x,
                ground.transform.position.y,
                ground.transform.localScale.y / 2f
                );
        }
    }

}
