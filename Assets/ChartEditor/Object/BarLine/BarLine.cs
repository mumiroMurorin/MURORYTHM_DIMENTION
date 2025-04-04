using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class BarLine : MonoBehaviour
    {
        [SerializeField] LineInfoView lineInfo_view;
        [Header("LineFactories")]
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> beatLineFactory;
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> subdivisionLineFactory;
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> colliderFactory;

        IChartEditorDataGetter chartEditorDataGetter;
        BarDataInChart barData;
        BarDataInChart backData;
        int barNumber = 0;

        /// <summary>
        /// 次の小節線の開始位置
        /// </summary>
        ReactiveProperty<float> nextZ = new ReactiveProperty<float>();
        public IReadOnlyReactiveProperty<float> NextZ => nextZ;


        #region Initialize 初期化系

        /// <summary>
        /// BarDataのセット、BarDataによる設定
        /// </summary>
        /// <param name="barData"></param>
        /// <param name="previousBar"></param>
        /// <param name="number"></param>
        public void Initialize(BarDataInChart barData, BarDataInChart backData, BarLine previousBar, IChartEditorDataGetter chartEditorDataGetter, int number)
        {
            this.barData = barData;
            this.backData = backData;
            this.barNumber = number;
            this.chartEditorDataGetter = chartEditorDataGetter;

            InitializeFactories();
            SetBarLineData(barData, backData);
            DeployOtherLine(barData);
            Bind(previousBar);
        }

        /// <summary>
        /// 分線の初期化
        /// </summary>
        private void InitializeFactories()
        {
            beatLineFactory?.Value.Initialize();
            subdivisionLineFactory?.Value.Initialize();
            colliderFactory?.Value.Initialize();
        }

        /// <summary>
        /// 小節線上のデータ更新
        /// </summary>
        /// <param name="barData"></param>
        private void SetBarLineData(BarDataInChart thisData, BarDataInChart backData)
        {
            // 小節番号
            int barNumber = this.barNumber;
            // BPM
            float bpm = barNumber == 1 || thisData.SubDivisionDatas[0].Bpm.Value != backData.SubDivisionDatas.Last().Bpm.Value ? 
                thisData.SubDivisionDatas[0].Bpm.Value : -1;
            // M
            int beatCount = barNumber == 1 || thisData.BeatCount.Value != backData.BeatCount.Value || thisData.BeatUnit.Value != backData.BeatUnit.Value ?
                thisData.BeatCount.Value : -1;
            // N
            float beatUnit = barNumber == 1 || thisData.BeatUnit.Value != backData.BeatUnit.Value || thisData.BeatCount.Value != backData.BeatCount.Value ?
                thisData.BeatUnit.Value : -1;
            // 分割数
            int divNum = barNumber == 1 || thisData.DivisionNum.Value != backData.DivisionNum.Value ?
                thisData.DivisionNum.Value : -1;

            lineInfo_view.SetDatas(barNumber, bpm, beatCount, beatUnit, divNum);
        }

        private void Bind(BarLine previousBar)
        {
            // 前のバーにポジションが変わった時のメソッドを購読
            previousBar?.NextZ
                .Subscribe(AdjustPositionOnChangeNextZ)
                .AddTo(this.gameObject);

            // 小節データに購読
            // N分のM拍子のどちらが変わっても長さは変わる
            barData?.BeatCount
                .Subscribe(beatCount => { 
                    AdjustPositionOnChangeBarData(beatCount, barData.BeatUnit.Value);
                    ReDeployOtherLineOnChangeBarData(barData);
                    SetBarLineData(barData, backData);
                })
                .AddTo(this.gameObject);

            barData?.BeatUnit
                .Subscribe(beatUnit => { 
                    AdjustPositionOnChangeBarData(barData.BeatCount.Value, beatUnit);
                    ReDeployOtherLineOnChangeBarData(barData);
                    SetBarLineData(barData, backData);
                })
                .AddTo(this.gameObject);

            // 分割数変化の際はSubDivision数だけ変える
            barData?.DivisionNum
                .Subscribe(_ => { 
                    ReDeployOtherLineOnChangeBarData(barData);
                    SetBarLineData(barData, backData);
                })
                .AddTo(this.gameObject);
        }

        #endregion


        #region CallBack コールバック系

        /// <summary>
        /// 前の小節位置がずれたとき、この小節位置も調整する(数珠繋ぎ)
        /// </summary>
        private void AdjustPositionOnChangeNextZ(float currentZ)
        {
            // このオブジェクトの位置調整
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                currentZ
                );

            float quarterNoteLength = chartEditorDataGetter.ChartViewScale.Value;
            float beatUnit = barData.BeatUnit.Value;
            float beatCount = barData.BeatCount.Value;

            // 次の小節開始位置 = 現在の開始位置 + M分音符の長さ * カウント数
            //                  = 現在の開始位置 + 4分音符の長さ / ( M分 / 4 ) * カウント数
            nextZ.Value = currentZ + quarterNoteLength / (beatUnit / 4f) * beatCount;
        }

        /// <summary>
        /// 小節データが変わった時、次の小節位置を調整する
        /// </summary>
        private void AdjustPositionOnChangeBarData(float beatCount, float beatUnit)
        {
            float quarterNoteLength = chartEditorDataGetter.ChartViewScale.Value;

            // 次の小節開始位置 = 現在の開始位置 + M分音符の長さ * カウント数
            //                  = 現在の開始位置 + 4分音符の長さ / ( M分 / 4 ) * カウント数
            nextZ.Value = transform.position.z + quarterNoteLength / (beatUnit / 4f) * beatCount;
        }

        /// <summary>
        /// 小節データが変わった時、小節内線を更新する
        /// </summary>
        /// <param name="barData"></param>
        private void ReDeployOtherLineOnChangeBarData(BarDataInChart barData)
        {
            // 初期化
            InitializeFactories();
            // 再配置
            DeployOtherLine(barData);
        }

        #endregion 


        /// <summary>
        /// 拍線、分線、コライダーの設置
        /// </summary>
        /// <param name="barData"></param>
        private void DeployOtherLine(BarDataInChart barData)
        {
            float quarterNoteLength = chartEditorDataGetter.ChartViewScale.Value;
            float beatUnit = barData.BeatUnit.Value;
            int divNum = barData.DivisionNum.Value;
            float localZ = 0;

            // 線の数だけ繰り返す
            for (int i = 0; i < barData.BeatCount.Value; i++)
            {
                for (int j = 0; j < divNum; j++)
                {
                    // 分線の生成
                    SubDivisionDataInBeat subDivisionData = barData.SubDivisionDatas[i * divNum + j];
                    GenerateSubDivisionUnit(subDivisionData, localZ, this.gameObject.transform, j == 0, i == 0 && j == 0);

                    // zの追加
                    // += M分音符あたりの距離 / 分割数
                    localZ += quarterNoteLength / (beatUnit / 4f) / divNum;
                }
            }
        }

        /// <summary>
        /// 1分線(と拍線)の生成
        /// </summary>
        /// <param name="quarterNoteLength">4分音符あたりの距離</param>
        /// <param name="beatUnit">n/m拍子のM</param>
        /// <param name="divNum">分割数</param>
        /// <param name="currentZ">現Z、参照渡し</param>
        /// <param name="isBeatTiming">拍が打たれる？</param>
        private void GenerateSubDivisionUnit(SubDivisionDataInBeat subDivisionData, float currentZ, Transform parent, bool isBeatTiming, bool isBarTiming)
        {
            // コライダーの設置
            colliderFactory?.Value.Deploy(subDivisionData, Vector3.forward * currentZ, parent);

            // 小節線があるため置かない
            if (isBarTiming) { return; }
            // 拍線
            else if (isBeatTiming)
            {
                beatLineFactory?.Value.Deploy(subDivisionData, Vector3.forward * currentZ, parent);
            }
            // 分線
            else
            {
                subdivisionLineFactory?.Value.Deploy(subDivisionData, Vector3.forward * currentZ, parent);
            }
        }

        /// <summary>
        /// 小節内で拡大縮小
        /// </summary>
        public void Scaling(float current, float previous)
        {
            // 分線もスケーリング
            beatLineFactory?.Value.Scaling(current, previous);
            subdivisionLineFactory?.Value.Scaling(current, previous);
            colliderFactory?.Value.Scaling(current, previous);
        }
    }
}
