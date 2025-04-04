using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;

namespace ChartEditor
{
    public class BarLineFactory : MonoBehaviour, ILaneDeployable<BarDataInChart>
    {
        [SerializeField] GameObject barLineObj;

        IChartEditorDataGetter chartEditorDataGetter;
        List<BarLine> barLines = new List<BarLine>();
        BarDataInChart backData;
        int barCount = 0;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
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
                line.Initialize(barData, backData, barLines.LastOrDefault(), chartEditorDataGetter, ++barCount);

                barLines?.Add(line);
            }

            // データを保存
            backData = barData;

            return obj;
        }

        void ILaneDeployable<BarDataInChart>.Scaling(float current, float previous)
        {
            foreach (BarLine barLine in barLines)
            {
                barLine.Scaling(current, previous);

                Vector3 pos = barLine.gameObject.transform.localPosition;
                barLine.gameObject.transform.localPosition = new Vector3(pos.x, pos.y, pos.z * (current / previous));
            }
        }
    }
}
