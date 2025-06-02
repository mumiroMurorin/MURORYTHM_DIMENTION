using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;

namespace ChartEditor
{
    public class BarLineFactory : MonoBehaviour, ILaneDeployable<BarDataInChart>, ILaneDestroyable<BarDataInChart>
    {
        [SerializeField] GameObject barLineObj;

        IChartEditorOptionGetter optionGetter;
        IChartEditorDataGetter dataGetter;
        List<BarLine> barLines = new List<BarLine>();
        int barCount = 0;

        [Inject]
        public void Construct(IChartEditorOptionGetter optionGetter, IChartEditorDataGetter dataGetter)
        {
            this.optionGetter = optionGetter;
            this.dataGetter = dataGetter;
        }

        void ILaneDeployable<BarDataInChart>.Initialize()
        {
            foreach(BarLine barLine in barLines)
            {
                Destroy(barLine.gameObject);
            }

            barLines = new List<BarLine>();
            barCount = 0;
        }

        GameObject ILaneDeployable<BarDataInChart>.Deploy(BarDataInChart barData, Vector3 pos, Transform parent)
        {
            // インスタンス化、設定
            GameObject obj = Instantiate(barLineObj);
            if (parent) { obj.transform.SetParent(parent); }
            obj.transform.localPosition = pos;

            // 生成したラインをリストに格納
            if(obj.TryGetComponent(out BarLine line))
            {
                // 小節の設定
                line.Initialize(barData, barLines.LastOrDefault()?.SubDivisionLast, optionGetter, dataGetter, ++barCount);

                barLines?.Add(line);
            }

            return obj;
        }

        /// <summary>
        /// 最後尾のデータの消去のみ考慮している
        /// 真ん中のデータを消す際は違う処理が必要
        /// </summary>
        /// <param name="lineData"></param>
        void ILaneDestroyable<BarDataInChart>.Destroy(BarDataInChart lineData)
        {
            // lineDataを持つBarLineを探す
            BarLine barLine = null;
            foreach (var bar in barLines)
            {
                if(bar.BarData == lineData) { barLine = bar; break; }
            }

            // 無かったら返す
            if(barLine == null)
            {
                Debug.LogWarning("【System】該当するBarDataが見つかりませんでした");
                return;
            }

            // 最後尾のデータの消去のみ考慮している
            // 真ん中のデータを消す際は違う処理が必要
            barLines.Remove(barLine);
            Destroy(barLine.gameObject);
            barLine = null;

            barCount--;
        }
    }
}
