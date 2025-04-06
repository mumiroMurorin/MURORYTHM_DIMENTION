using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class LaneDeployer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ILaneDeployable<BarDataInChart>> barLineDeplayable;
        [SerializeField] Transform lineParent;
        [SerializeField] GameObject ground;

        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 譜面生成
            chartEditorDataGetter?.ChartData
                .Where(data => data != null)
                .Subscribe(GenerateLane)
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 楽曲の長さとBPMに基づき譜面レーンの生成
        /// </summary>
        /// <param name="musicLength"></param>
        /// <param name="mainBpm"></param>
        private void GenerateLane(ChartData chartData)
        {
            // まず初期化
            ClearLane();

            // 四分音符当たりの距離
            float quarterNoteLength = chartEditorDataGetter.ChartViewScale.Value;
            float currentZ = 0;

            // 小節線の数だけ繰り返す
            for (int i = 0; i < chartData.BarDatas.Count; i++)
            {
                GenerateBarUnit(chartData.BarDatas[i], quarterNoteLength, ref currentZ, lineParent, i);
            }

            // グラウンドの生成
            //ground.transform.localScale = new Vector3(
            //    ground.transform.localScale.x,
            //    chartLength,
            //    ground.transform.localScale.z);

            //ground.transform.position = new Vector3(
            //    ground.transform.position.x,
            //    ground.transform.position.y,
            //    ground.transform.localScale.y / 2f
            //    );
        }

        /// <summary>
        /// 1小節の生成
        /// </summary>
        /// <param name="barData"></param>
        /// <param name="currentZ"></param>
        private void GenerateBarUnit(BarDataInChart barData, float quarterNoteLength, ref float currentZ, Transform parent, int count)
        {
            // 小節線のインスタンス化
            GameObject barObj = barLineDeplayable.Value.Deploy(barData, Vector3.forward * currentZ, parent);
            barObj.name = $"Bar_{count + 1}";
        }

        /// <summary>
        /// レーン上のオブジェクトをすべて破棄、初期化
        /// </summary>
        private void ClearLane()
        {
            barLineDeplayable.Value.Initialize();

            // 親オブジェクトの削除
            foreach(Transform t in lineParent)
            {
                Destroy(t.gameObject);
            }
        }
    }

}
