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
        [SerializeField] GameObject[] laneDivisionLines;
 
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorOptionGetter chartEditorOptionGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionGetter chartEditorOptionGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorOptionGetter = chartEditorOptionGetter;
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

            // レーン分割線の表示
            chartEditorOptionGetter?.LaneDivisionNum
                .Subscribe(SetLaneDivisionLine)
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

            // 小節線の数だけ繰り返す
            for (int i = 0; i < chartData.BarDatas.Count; i++)
            {
                GenerateBarUnit(chartData.BarDatas[i], lineParent, i);
            }

            float chartLength = chartEditorOptionGetter.ChartViewScale.Value * chartEditorDataGetter.Music.Value.length;

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

        /// <summary>
        /// 1小節の生成
        /// </summary>
        /// <param name="barData"></param>
        /// <param name="currentZ"></param>
        private void GenerateBarUnit(BarDataInChart barData, Transform parent, int count)
        {
            // 小節線のインスタンス化
            GameObject barObj = barLineDeplayable.Value.Deploy(barData, Vector3.zero, parent);
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

        /// <summary>
        /// 分割線の表示非表示
        /// </summary>
        /// <param name="divNum"></param>
        private void SetLaneDivisionLine(int divNum)
        {
            if(laneDivisionLines == null) { return; }
            if(laneDivisionLines.Length != 17) { return; }

            for (int i = 0; i < 16; i++) 
            {
                laneDivisionLines[i].SetActive(i % (16 / divNum) == 0);
            }

            laneDivisionLines[16].SetActive(true);
        }
    }

}
