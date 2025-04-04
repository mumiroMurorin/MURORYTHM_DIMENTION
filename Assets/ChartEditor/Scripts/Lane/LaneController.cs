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
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> beatLineDeployable;
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> subdivisionLineDeployable;
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> colliderDeployableGroup;
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
            // 拡大率
            chartEditorDataGetter?.ChartViewScale
                .Pairwise()
                .Subscribe(OnChangeChartViewScale)
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 拡大率より拡大縮小を行う
        /// </summary>
        /// <param name="scale"></param>
        private void OnChangeChartViewScale(Pair<float> pairScale)
        {
            // 各線でスケーリング
            barLineDeplayable?.Value.Scaling(pairScale.Current, pairScale.Previous);
            beatLineDeployable?.Value.Scaling(pairScale.Current, pairScale.Previous);
            subdivisionLineDeployable?.Value.Scaling(pairScale.Current, pairScale.Previous);
            colliderDeployableGroup?.Value.Scaling(pairScale.Current, pairScale.Previous);

            // グラウンド
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
