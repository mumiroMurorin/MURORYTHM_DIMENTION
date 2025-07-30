using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

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
                    Initialze();
                    BindForChartData(data);
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

        private void BindForChartData(ChartData chartData)
        {
            // Collectionの監視は初期化がないので小節線の数だけ繰り返す
            for (int i = 0; i < chartData.BarDatas.Count; i++)
            {
                OnAddLane(chartData.BarDatas[i]);
            }

            chartData?.BarDatas.ObserveAdd()
                .Subscribe(barData => {
                    OnAddLane(barData.Value);
                    BindForBarData(barData.Value);
                })
                .AddTo(this.gameObject);

            chartData?.BarDatas.ObserveRemove()
                .Subscribe(barData => {
                    OnRemoveLastLane(barData.Value.SubDivisionDatas.Count);
                })
                .AddTo(this.gameObject);
        }

        private void BindForBarData(IBarDataGetter barData)
        {
            // BeatUnitの更新
            barData?.BeatUnit
                .Skip(1)
                .Subscribe(_ => { OnChangeBeatUnit(barData.BarIndex); })
                .AddTo(this.gameObject);

            // SubdivisionDatasに購読
            barData?.SubDivisionDatas.ObserveAdd()
                .Subscribe(subData => BindForSubdivisionData(subData.Value))
                .AddTo(this.gameObject);
        }

        private void BindForSubdivisionData(ISubDivisionDataGetter subData)
        {
            // BPMの更新
            subData?.Bpm
                .Skip(1)
                .Subscribe(_ => OnChangeBPM(subData.BarData.BarIndex, subData.SubDivisionIndex))
                .AddTo(this.gameObject);
        }

        private void Initialze()
        {
            ClearLane();
        }

        /// <summary>
        /// 小節線が追加された時
        /// </summary>
        /// <param name="barData"></param>
        private void OnAddLane(IBarDataGetter barData)
        {
            DeployableLineObject lineObj;
            for (int i = 0; i < barData.SubDivisionDatas.Count; i++)
            {
                var subData = barData.SubDivisionDatas[i];
                var address = new AddressInChart(barData.BarIndex, i, 0);

                // 小節線のインスタンス化
                if (i == 0) { lineObj = barLineFactory.Value.Deploy(lineParent); }
                // 拍子線のインスタンス化
                else if (i % barData.BeatCount.Value == 0) { lineObj = beatLineFactory.Value.Deploy(lineParent); }
                // 分線のインスタンス化
                else { lineObj = subDivisionLineFactory.Value.Deploy(lineParent); }

                // 初期化
                lineObj.transform.localPosition = Vector3.zero;
                lineObj.SetAddress(address);
                lineObj.SetPlacementLocation(subData.SetPlacementLocation, subData.SetSpaceLocation);
                lineObj.OnChangeLayer(dataGetter.EditNoteType.Value);
                lines.Add(new LineDataToObject(lineObj, subData));
            }

            // 一つ前の分線から位置調整を行う
            UpdateLinePos(Mathf.Max(0, barData.BarIndex - 1), 0);
        }

        /// <summary>
        /// 最後の小節線が削除された時
        /// </summary>
        /// <param name="lineCount"></param>
        private void OnRemoveLastLane(int lineCount)
        {
            for(int i = 0; i < lineCount; i++)
            {
                Destroy(lines[lines.Count - 1].Obj.gameObject);
                lines.RemoveAt(lines.Count - 1);
            }
        }

        private void OnChangeBeatUnit(int barIndex)
        {
            UpdateLinePos(Mathf.Max(0, barIndex - 1), 0);
        }

        private void OnChangeBPM(int barIndex, int subIndex)
        {
            UpdateLinePos(Mathf.Max(0, barIndex - 1), 0);
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
                    Debug.Log($"指定された要素が見つかりませんでした: #{barIndex}-{subIndex}");
                    return (-1, null);
                }

                if(line.Data.SubDivisionIndex < subIndex) { continue; }
                else if (line.Data.SubDivisionIndex > subIndex)
                {
                    Debug.Log($"指定された要素が見つかりませんでした: #{barIndex}-{subIndex}");
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

            Debug.LogWarning($"【System】データが見つかりませんでした");
            return null;
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
