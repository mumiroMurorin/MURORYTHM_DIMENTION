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
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> beatLineDeployable;
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> subdivisionLineDeployable;
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> colliderDeployableGroup;
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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                ChartData data = chartEditorDataGetter.ChartData.Value;
                data.BarDatas[2].SetBeatUnit(data.BarDatas[2].BeatUnit.Value - 1);
                data.BarDatas[3].SetBeatUnit(data.BarDatas[3].BeatUnit.Value - 1);
                data.BarDatas[4].SetBeatUnit(data.BarDatas[4].BeatUnit.Value - 2);
                data.BarDatas[5].SetBeatUnit(data.BarDatas[5].BeatUnit.Value - 2);
                data.BarDatas[6].SetBeatUnit(data.BarDatas[6].BeatUnit.Value - 3);
                data.BarDatas[7].SetBeatUnit(data.BarDatas[7].BeatUnit.Value - 3);
            }
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

            float beatUnit = barData.BeatUnit.Value;
            int divNum = barData.DivisionNum.Value;
            float addZ = 0;

            // 線の数だけ繰り返す
            for (int i = 0; i < barData.BeatCount.Value; i++) 
            {
                for (int j = 0; j < divNum; j++)
                {
                    // 分線の生成
                    SubDivisionDataInBeat subDivisionData = barData.SubDivisionDatas[i * divNum + j];
                    GenerateSubDivisionUnit(subDivisionData, quarterNoteLength, beatUnit, divNum, ref addZ, barObj.transform, j == 0);
                }
            }

            currentZ += addZ;
        }

        /// <summary>
        /// 1分線(と拍線)の生成
        /// </summary>
        /// <param name="quarterNoteLength">4分音符あたりの距離</param>
        /// <param name="beatUnit">n/m拍子のM</param>
        /// <param name="divNum">分割数</param>
        /// <param name="currentZ">現Z、参照渡し</param>
        /// <param name="isBeatTiming">拍が打たれる？</param>
        private void GenerateSubDivisionUnit(SubDivisionDataInBeat subDivisionData, float quarterNoteLength, float beatUnit, int divNum, ref float currentZ, Transform parent, bool isBeatTiming = false)
        {
            if (isBeatTiming)
            {
                // 拍線
                beatLineDeployable.Value.Deploy(subDivisionData, Vector3.forward * currentZ, parent);
            }
            else
            {
                // ただの分線
                subdivisionLineDeployable.Value.Deploy(subDivisionData, Vector3.forward * currentZ, parent);
            }

            // コライダーの設置
            colliderDeployableGroup.Value.Deploy(subDivisionData, Vector3.forward * currentZ, parent);

            // zの追加
            // += M分音符あたりの距離 / 分割数
            currentZ += quarterNoteLength / (beatUnit / 4f) / divNum;
        }

        /// <summary>
        /// レーン上のオブジェクトをすべて破棄、初期化
        /// </summary>
        private void ClearLane()
        {
            barLineDeplayable.Value.Initialize();
            beatLineDeployable.Value.Initialize();
            subdivisionLineDeployable.Value.Initialize();
            colliderDeployableGroup.Value.Initialize();

            // 親オブジェクトの削除
            foreach(Transform t in lineParent)
            {
                Destroy(t.gameObject);
            }
        }
    }

}
