using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using System.Threading;

namespace ChartEditor
{
    public class LaneDeployer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ILaneDeployable> barLineFactory;
        [SerializeField] SerializeInterface<ILaneDeployable> beatLineFactory;
        [SerializeField] SerializeInterface<ILaneDeployable> subDivisionLineFactory;
        [SerializeField] Transform lineParent;

        List<LineDataToObject> lines = new List<LineDataToObject>();

        IChartEditorDataGetter dataGetter;
        IChartEditorOptionGetter optionGetter;

        CancellationTokenSource updateLinePosCts;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataGetter = dataGetter;
            this.optionGetter = optionGetter;
        }

        void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 譜面生成
            dataGetter?.ChartData
                .Where(data => data != null)
                .Subscribe(data => {
                    ClearLane();
                    BindForChartData(data);
                    updateLinePosCts?.CancelAndDispose();
                    updateLinePosCts = DelayUtility.Run(0f, () => UpdateLinePos(0, 0));
                })
                .AddTo(this.gameObject);

            // レイヤーチェンジ
            dataGetter.EditNoteType
                .Subscribe(OnChangeLayer)
                .AddTo(this.gameObject);

            // 拡大率操作
            optionGetter.ChartViewScale
                .Subscribe(_ => { UpdateLinePos(0, 0); })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 譜面データに対する購読を行う
        /// </summary>
        /// <param name="chartData"></param>
        private void BindForChartData(ChartData chartData)
        {
            // Collectionの監視は初期化がないので小節線の数だけ繰り返す
            foreach(var bar in chartData.BarDatas)
            {
                BindForBarData(bar);
            }

            chartData?.BarDatas.ObserveAdd()
                .Subscribe(barData => BindForBarData(barData.Value))
                .AddTo(this.gameObject);

            // 小節線追加時
            chartData?.OnAddBarDataListener
                .Subscribe(barIndex => { UpdateLinePos(Mathf.Max(0, barIndex - 1), 0); })
                .AddTo(this.gameObject);

            // BPM変更時
            chartData?.OnChangeBPMListener
                .Subscribe(pair => { UpdateLinePos(pair.Item1, pair.Item2); })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 小節線に対する購読を行う
        /// </summary>
        /// <param name="barData"></param>
        private void BindForBarData(IBarDataGetter barData)
        {
            // BeatUnitの更新 → 位置の更新
            barData?.BeatUnit
                .Skip(1)
                .Subscribe(_ => { UpdateLinePos(Mathf.Max(0, barData.BarIndex - 1), 0); })
                .AddTo(this.gameObject);


            // SubdivisionDatasに購読
            // Collectionの監視は初期化がないので線の数だけ繰り返す
            foreach (var sub in barData.SubDivisionDatas)
            {
                OnAddSubdivision(sub);
                BindForSubdivisionData(sub, Find(sub.BarData.BarIndex, sub.SubDivisionIndex).Item2.Obj.gameObject);
            }

            // 分線データ追加 → 分線オブジェクトのインスタンス化
            barData?.SubDivisionDatas.ObserveAdd()
                .Subscribe(subData => {
                    OnAddSubdivision(subData.Value);
                    BindForSubdivisionData(subData.Value, Find(subData.Value.BarData.BarIndex, subData.Value.SubDivisionIndex).Item2.Obj.gameObject);

                    // ※ループが多いと重たい
                    UpdateLinePos(Mathf.Max(0, barData.BarIndex - 1), 0);
                })
                .AddTo(this.gameObject);

            // 分線データ削除 → 分線オブジェクトも削除
            barData?.SubDivisionDatas.ObserveRemove()
                .Subscribe(subData => {
                    OnRemoveSubdivision(subData.Value);

                    // ※ループが多いと重たい
                    UpdateLinePos(Mathf.Max(0, barData.BarIndex - 1), 0);
                })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 分線に対する購読を行う
        /// </summary>
        /// <param name="subData"></param>
        private void BindForSubdivisionData(ISubDivisionDataGetter subData, GameObject lineObj)
        {
            // BPMの更新 → 表記の更新
            subData.Bpm
                .Where(_ => lineObj != null)
                .Subscribe(bpm => OnChangeBPM(subData.BarData.BarIndex, subData.SubDivisionIndex, bpm))
                .AddTo(this.gameObject)
                .AddTo(lineObj);

            // スピード倍率の更新 → 表記の更新
            subData.SpeedRatio
                .Where(_ => lineObj != null)
                .Subscribe(ratio => OnChangeSpeedRatio(subData.BarData.BarIndex, subData.SubDivisionIndex,ratio))
                .AddTo(this.gameObject)
                .AddTo(lineObj);

            // BeatUnitの更新 → 表記の更新
            subData.BarData.BeatUnit
                .Where(_ => lineObj != null)
                .Subscribe(unit => OnChangeBeatUnit(subData.BarData.BarIndex, subData.SubDivisionIndex, unit))
                .AddTo(this.gameObject)
                .AddTo(lineObj);

            // BeatCountの更新 → 表記の更新
            subData.BarData.BeatCount
                .Where(_ => lineObj != null)
                .Subscribe(count => OnChangeBeatCount(subData.BarData.BarIndex, subData.SubDivisionIndex, count))
                .AddTo(this.gameObject)
                .AddTo(lineObj);

            // DivisionNumの更新 → 表記の更新
            subData.BarData.DivisionNum
                .Where(_ => lineObj != null)
                .Subscribe(divNum => OnChangeDivisionNum(subData.BarData.BarIndex, subData.SubDivisionIndex, divNum))
                .AddTo(this.gameObject)
                .AddTo(lineObj);
        }

        /// <summary>
        /// 分線が追加された時
        /// </summary>
        /// <param name="subData"></param>
        private void OnAddSubdivision(SubDivisionDataInBeat subData)
        {
            DeployableLineObject lineObj;
            var address = new AddressInChart(subData.BarData.BarIndex, subData.SubDivisionIndex, 0);

            // 小節線のインスタンス化
            if (subData.SubDivisionIndex == 0) { lineObj = barLineFactory.Value.Deploy(lineParent); }
            // 拍子線のインスタンス化
            else if (subData.SubDivisionIndex % subData.BarData.DivisionNum.Value == 0) { lineObj = beatLineFactory.Value.Deploy(lineParent); }
            // 分線のインスタンス化
            else { lineObj = subDivisionLineFactory.Value.Deploy(lineParent); }

            // 初期化
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.SetAddress(address);
            lineObj.SetBarNumber(subData.BarData.BarIndex + 1);
            lineObj.SetPlacementLocation(subData.SetPlacementLocation, subData.SetSpaceLocation);
            lineObj.OnChangeLayer(dataGetter.EditNoteType.Value);

            // 挿入する
            var addLine = new LineDataToObject(lineObj, subData);
            int insertIndex;
            for (insertIndex = 0; insertIndex < lines.Count + 1; insertIndex++)
            {
                if (lines.Count <= insertIndex) { break; }

                var targetLine = lines[insertIndex];

                if (targetLine.Data.BarData.BarIndex < subData.BarData.BarIndex) { continue; }
                if (subData.BarData.BarIndex == targetLine.Data.BarData.BarIndex
                    && targetLine.Data.SubDivisionIndex < subData.SubDivisionIndex) { continue; }

                break;
            }

            lines.Insert(insertIndex, addLine);
            var z = insertIndex != 0 ? lines[insertIndex - 1].Obj.gameObject.transform.position.z : 0;
            lineObj.SetPosition(z);
        }

        /// <summary>
        /// 分線が削除された時
        /// </summary>
        /// <param name="subData"></param>
        private void OnRemoveSubdivision(SubDivisionDataInBeat subData)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (line.Data != subData) { continue; }

                // 削除処理
                Destroy(lines[i].Obj.gameObject);
                lines.RemoveAt(i);
                return;
            }
        }

        private void OnChangeBeatCount(int barIndex, int subIndex, int beatCount)
        {
            // 表記の更新
            var pair = Find(barIndex, subIndex);
            var index = pair.Item1;
            if(index < 0) { return; }

            var thisObj = pair.Item2.Obj;
            var backCount = pair.Item1 != 0 ? lines[index - 1].Data.BarData.BeatCount.Value : -1;

            thisObj.OnChangeBeatCount(beatCount, backCount);
        }

        private void OnChangeBeatUnit(int barIndex, int subIndex, float beatUnit)
        {
            // 表記の更新
            var pair = Find(barIndex, subIndex);
            var index = pair.Item1;
            if (index < 0) { return; }

            var thisObj = pair.Item2.Obj;
            var backUnit = pair.Item1 != 0 ? lines[index - 1].Data.BarData.BeatUnit.Value : -1f;

            thisObj.OnChangeBeatUnit(beatUnit, backUnit);
        }

        private void OnChangeDivisionNum(int barIndex, int subIndex, int divisionNum)
        {
            // 表記の更新
            var pair = Find(barIndex, subIndex);
            var index = pair.Item1;
            if (index < 0) { return; }

            var thisObj = pair.Item2.Obj;
            var backDivNum = pair.Item1 != 0 ? lines[index - 1].Data.BarData.DivisionNum.Value : -1;

            thisObj.OnChangeDivisionNum(divisionNum, backDivNum);
        }

        private void OnChangeBPM(int barIndex, int subIndex, float bpm)
        {
            // 表記の更新
            var pair = Find(barIndex, subIndex);
            var index = pair.Item1;
            if (index < 0) { return; }

            var thisObj = pair.Item2.Obj;
            var backBpm = pair.Item1 != 0 ? lines[index - 1].Data.Bpm.Value : -1f;

            thisObj.OnChangeBpm(bpm, backBpm);
        }

        private void OnChangeSpeedRatio(int barIndex, int subIndex, float speedRatio)
        {
            // 表記の更新
            var pair = Find(barIndex, subIndex);
            var index = pair.Item1;
            if (index < 0) { return; }

            var thisObj = pair.Item2.Obj;
            var backSpeedRatio = pair.Item1 != 0 ? lines[index - 1].Data.SpeedRatio.Value : float.MinValue;

            thisObj.OnChangeSpeedRatio(speedRatio, backSpeedRatio);
        }

        /// <summary>
        /// レイヤーが変更されたとき一括で配置場所の位置を変える
        /// </summary>
        /// <param name="editNoteType"></param>
        private void OnChangeLayer(EditNoteType editNoteType)
        {
            foreach(var line in lines)
            {
                line.Obj.OnChangeLayer(editNoteType);
            }
        }

        /// <summary>
        /// Lineの場所を変更する
        /// </summary>
        /// <param name="barNumber"></param>
        /// <param name="subIndex"></param>
        void UpdateLinePos(int barNumber, int subIndex)
        {
            float chartLengthParSecond = optionGetter.ChartViewScale.Value;

            var pair = Find(barNumber, subIndex);
            int startIndex = pair.Item1;
            if(startIndex < 0) { return; }

            float currentZ = pair.Item2.Obj.gameObject.transform.position.z;

            for (int i = startIndex; i < lines.Count; i++)
            {
                var line = lines[i];
                line.Obj.SetPosition(currentZ);

                float bpm = line.Data.Bpm.Value;
                float beatUnit = line.Data.BarData.BeatUnit.Value;
                int beatCount = line.Data.BarData.BeatCount.Value;
                int divNum = line.Data.BarData.DivisionNum.Value;
                float delta = chartLengthParSecond * (60f / bpm) * (4f / beatUnit) / divNum;

                line.Obj.OnChangeSize(delta);

                currentZ += delta;
            }
        }

        /// <summary>
        /// レーン上のオブジェクトをすべて破棄、初期化
        /// </summary>
        private void ClearLane()
        {
            // 親オブジェクトの削除
            foreach(var t in lines)
            {
                Destroy(t.Obj.gameObject);
            }

            lines.Clear();
        }

        private (int,LineDataToObject) Find(int barIndex, int subIndex)
        {
            int index = -1;
            foreach (var line in lines)
            {
                index++;
                if (line.Data.BarData.BarIndex < barIndex) { continue; }
                else if(line.Data.BarData.BarIndex > barIndex)
                {
                    //Debug.Log($"指定された要素が見つかりませんでした: #{barIndex}-{subIndex}");
                    return (-1, null);
                }

                if(line.Data.SubDivisionIndex < subIndex) { continue; }
                else if (line.Data.SubDivisionIndex > subIndex)
                {
                    //Debug.Log($"指定された要素が見つかりませんでした: #{barIndex}-{subIndex}");
                    return (-1, null);
                }

                return (index, line);
            }

            return (-1, null);
        }

        public ISubDivisionDataGetter GetData(DeployableLineObject lineObj)
        {
            foreach(var line in lines) 
            { 
                if(line.Obj == lineObj) { return line.Data; }
            }

            Debug.LogWarning($"【System】データが見つかりませんでした: {lineObj}");
            return null;
        }

        private void OnDestroy()
        {
            updateLinePosCts?.CancelAndDispose();
        }

        [System.Serializable]
        class LineDataToObject
        {
            public LineDataToObject(DeployableLineObject obj, ISubDivisionDataGetter data)
            {
                this.Obj = obj;
                this.Data = data;
            }

            public DeployableLineObject Obj { get; set; }

            public ISubDivisionDataGetter Data { get; set; }
        }
    }

}
