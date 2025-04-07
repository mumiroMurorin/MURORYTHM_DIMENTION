using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class BarLine : MonoBehaviour, IBarDataGetter, ISubDivisionDataGetter, ILinePositioner
    {
        [SerializeField] BarLineInfoView lineInfo_view;
        [SerializeField] SubdivisionLineInfoView subInfo_view;
        [Header("LineFactories")]
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> beatLineFactory;
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> subdivisionLineFactory;
        [SerializeField] SerializeInterface<ILaneDeployable<SubDivisionDataInBeat>> colliderFactory;

        IChartEditorDataGetter chartEditorDataGetter;
        IReadOnlyReactiveProperty<ILinePositioner> backData;
        int barNumber = 0;

        /// <summary>
        /// 次の小節線の開始位置
        /// </summary>
        ReactiveProperty<float> nextZ = new ReactiveProperty<float>();
        IReadOnlyReactiveProperty<float> ILinePositioner.NextZ => nextZ;

        BarDataInChart barData;
        public BarDataInChart BarData => barData;

        SubDivisionDataInBeat subDivisionData;
        public SubDivisionDataInBeat SubDivisionData => subDivisionData;

        /// <summary>
        /// 分線の最後尾
        /// </summary>
        ReactiveProperty<ILinePositioner> subDivisionLast = new ReactiveProperty<ILinePositioner>();
        public IReadOnlyReactiveProperty<ILinePositioner> SubDivisionLast => subDivisionLast;


        #region Initialize 初期化系

        /// <summary>
        /// BarDataのセット、BarDataによる設定
        /// </summary>
        /// <param name="barData"></param>
        /// <param name="previousBar"></param>
        /// <param name="number"></param>
        public void Initialize(BarDataInChart barData, IReadOnlyReactiveProperty<ILinePositioner> backData, IChartEditorDataGetter chartEditorDataGetter, int number)
        {
            this.barData = barData;
            this.backData = backData;
            this.barNumber = number;
            this.chartEditorDataGetter = chartEditorDataGetter;

            InitializeFactories();
            DeployOtherLine(barData);
            Bind();
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
        private void SetBarLineData(BarDataInChart barData, BarDataInChart backData)
        {
            // 小節番号
            int barNumber = this.barNumber;
            // M
            int beatCount = barNumber == 1 || barData.BeatCount.Value != backData.BeatCount.Value || barData.BeatUnit.Value != backData.BeatUnit.Value ?
                barData.BeatCount.Value : -1;
            // N
            float beatUnit = barNumber == 1 || barData.BeatUnit.Value != backData.BeatUnit.Value || barData.BeatCount.Value != backData.BeatCount.Value ?
                barData.BeatUnit.Value : -1;
            // 分割数
            int divNum = barNumber == 1 || barData.DivisionNum.Value != backData.DivisionNum.Value ?
                barData.DivisionNum.Value : -1;

            lineInfo_view.SetDatas(barNumber, beatCount, beatUnit, divNum);
        }

        /// <summary>
        /// 分線上のデータ更新
        /// </summary>
        /// <param name="barData"></param>
        private void SetSubDivisionLineData(SubDivisionDataInBeat barData, SubDivisionDataInBeat backData)
        {
            // BPM
            float bpm = backData == null || barData.Bpm.Value != backData.Bpm.Value ?
                barData.Bpm.Value : -1;

            subInfo_view.SetDatas(bpm);
        }

        private void BindForBackData(ILinePositioner backData)
        {
            // 前のバーにポジションが変わった時のメソッドを購読
            backData?.NextZ
                .Subscribe(AdjustPositionOnChangeNextZ)
                .AddTo(this.gameObject);

            // 前データが変わった時情報を更新する
            backData?.BarData.BeatCount
                .Subscribe(_ => SetBarLineData(barData, backData.BarData))
                .AddTo(this.gameObject);

            backData?.BarData.BeatUnit
                .Subscribe(_ => SetBarLineData(barData, backData.BarData))
                .AddTo(this.gameObject);

            backData?.BarData.DivisionNum
                .Subscribe(_ => SetBarLineData(barData, backData.BarData))
                .AddTo(this.gameObject);

            backData?.SubDivisionData.Bpm
                .Subscribe(bpm =>
                {
                    SetSubDivisionLineData(subDivisionData, backData.SubDivisionData);
                })
                .AddTo(this.gameObject);
        }

        private void Bind()
        {
            // 前データが変わった際のメソッド
            backData?
                .Subscribe(BindForBackData)
                .AddTo(this.gameObject);

            // 小節データに購読
            // N分のM拍子のどちらが変わっても長さは変わる
            barData?.BeatCount
                .Subscribe(beatCount => SetBarLineData(barData, backData?.Value.BarData))
                .AddTo(this.gameObject);

            barData?.BeatUnit
                .Subscribe(beatUnit =>
                {
                    AdjustPositionOnChangeLineData();
                    SetBarLineData(barData, backData?.Value.BarData);
                })
                .AddTo(this.gameObject);

            // 分割数変化の際はSubDivision数だけ変える
            barData?.DivisionNum
                .Subscribe(_ => SetBarLineData(barData, backData?.Value.BarData))
                .AddTo(this.gameObject);

            // BPM変化
            subDivisionData?.Bpm
                .Subscribe(bpm =>
                {
                    Debug.Log($"BPM {bpm}");
                    AdjustPositionOnChangeLineData();
                    SetSubDivisionLineData(subDivisionData, backData?.Value.SubDivisionData);
                })
                .AddTo(this.gameObject);

            barData.SubDivisionDatas.ObserveCountChanged()
                .Where(count => count == barData.BeatCount.Value * barData.DivisionNum.Value)
                .Subscribe(count => {
                    Debug.Log($"CountChange: {count}");
                    ReDeployOtherLineOnChangeBarData(barData);
                    AdjustPositionOnChangeLineData();
                })
                .AddTo(this.gameObject);
            
        }

        #endregion


        #region CallBack コールバック系

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

        /// <summary>
        /// 前の線位置がずれたとき、この線位置も調整する(数珠繋ぎ)
        /// </summary>
        private void AdjustPositionOnChangeNextZ(float currentZ)
        {
            // このオブジェクトの位置調整
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                currentZ
                );

            float chartLengthParSecond = chartEditorDataGetter.ChartViewScale.Value;
            float beatUnit = barData.BeatUnit.Value;
            float bpm = subDivisionData.Bpm.Value;
            int divNum = barData.DivisionNum.Value;

            // zの追加
            // += 1秒あたりのz距離 * 秒数
            //  = 1秒あたりのz距離 * (60f / bpm) * (4f / beatUnit) / 分割数
            nextZ.Value = currentZ += chartLengthParSecond * (60f / bpm) * (4f / beatUnit) / divNum;
        }

        /// <summary>
        /// 小節データが変わった時、次の小節位置を調整する
        /// </summary>
        private void AdjustPositionOnChangeLineData()
        {
            float chartLengthParSecond = chartEditorDataGetter.ChartViewScale.Value;
            float beatUnit = barData.BeatUnit.Value;
            float bpm = subDivisionData.Bpm.Value;
            int divNum = barData.DivisionNum.Value;

            // zの追加
            // += 1秒あたりのz距離 * 秒数
            //  = 1秒あたりのz距離 * (60f / bpm) * (4f / beatUnit) / 分割数
            nextZ.Value = transform.position.z + chartLengthParSecond * (60f / bpm) * (4f / beatUnit) / divNum;
        }

        #endregion 


        /// <summary>
        /// 拍線、分線、コライダーの設置
        /// </summary>
        /// <param name="barData"></param>
        private void DeployOtherLine(BarDataInChart barData)
        {
            float chartLengthParSecond = chartEditorDataGetter.ChartViewScale.Value;
            float beatUnit = barData.BeatUnit.Value;
            int divNum = barData.DivisionNum.Value;
            float localZ = 0;

            // Debug.Log($"{barData.BeatCount.Value} / {beatUnit}, {divNum}分割");

            // 最初はnull
            GameObject backObj = null;

            // 線の数だけ繰り返す
            for (int i = 0; i < barData.BeatCount.Value; i++)
            {
                for (int j = 0; j < divNum; j++)
                {
                    bool isBarTiming = i == 0 && j == 0;

                    // 分線の生成
                    SubDivisionDataInBeat subDivisionData = barData.SubDivisionDatas[i * divNum + j];
                    ILinePositioner backData = null;

                    if (backObj != null && !backObj.TryGetComponent(out backData)){ return; }
                    backObj = GenerateSubDivisionUnit(subDivisionData, backData, localZ, this.gameObject.transform, j == 0, isBarTiming);

                    // zの追加
                    // += 1秒あたりのz距離 * 秒数
                    //  = 1秒あたりのz距離 * (60f / bpm) * (4f / beatUnit) / 分割数
                    float bpm = subDivisionData.Bpm.Value;
                    localZ += chartLengthParSecond * (60f / bpm) * (4f / beatUnit) / divNum;
                }
            }

            subDivisionLast.Value = backObj.GetComponent<ILinePositioner>();
            this.subDivisionData = barData.SubDivisionDatas[0];
        }

        /// <summary>
        /// 1分線(と拍線)の生成
        /// </summary>
        /// <param name="quarterNoteLength">4分音符あたりの距離</param>
        /// <param name="beatUnit">n/m拍子のM</param>
        /// <param name="divNum">分割数</param>
        /// <param name="currentZ">現Z、参照渡し</param>
        /// <param name="isBeatTiming">拍が打たれる？</param>
        private GameObject GenerateSubDivisionUnit(SubDivisionDataInBeat subData, ILinePositioner backData, float currentZ, Transform parent, bool isBeatTiming, bool isBarTiming)
        {
            // コライダーの設置
            colliderFactory?.Value.Deploy(subData, Vector3.forward * currentZ, parent);

            // 小節線があるため置かない
            // この小節線をリターンする
            if (isBarTiming) { return this.gameObject; }

            GameObject obj;
            if (isBeatTiming)
            {
                obj = beatLineFactory?.Value.Deploy(subData, Vector3.forward * currentZ, parent);
            }
            // 分線
            else
            {
                obj = subdivisionLineFactory?.Value.Deploy(subData, Vector3.forward * currentZ, parent);
            }

            // 初期化
            if (!obj.TryGetComponent(out SubdivisionLine subDivisionLine)) { return null; }
            subDivisionLine.Initialize(subData, backData, chartEditorDataGetter);

            return obj;
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

    public interface IBarDataGetter
    {
        BarDataInChart BarData { get; }
    }

    public interface ILinePositioner
    {
        GameObject gameObject { get; }

        IReadOnlyReactiveProperty<float> NextZ { get; }

        SubDivisionDataInBeat SubDivisionData { get; }

        BarDataInChart BarData { get; }
    }
}
