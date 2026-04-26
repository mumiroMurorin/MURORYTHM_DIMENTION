using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class LaneController : MonoBehaviour
    {
        [SerializeField] GameObject laneDivisionLineParent;
        [SerializeField] GameObject divisionLayerObjParent;
        [SerializeField] GameObject groundParent;
        [SerializeField] GameObject[] laneDivisionLines;

        IChartEditorOptionGetter optionGetter;
        IChartEditorDataGetter dataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataGetter = dataGetter;
            this.optionGetter = optionGetter;
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
            // グラウンド長さ更新
            dataGetter?.ChartSeconds
                .Subscribe(seconds => UpdateGroundLength(seconds, optionGetter.ChartViewScale.Value))
                .AddTo(this.gameObject);

            // 拡大率
            optionGetter?.ChartViewScale
                .Subscribe(scale => UpdateGroundLength(dataGetter.ChartSeconds.Value, scale))
                .AddTo(this.gameObject);

            // レーン分割線の表示
            optionGetter?.LaneDivisionNum
                .Subscribe(SetLaneDivisionLine)
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// グラウンド長を調整
        /// </summary>
        /// <param name="chartSeconds"></param>
        /// <param name="viewScale"></param>
        private void UpdateGroundLength(float chartSeconds, float viewScale)
        {
            float chartLength = viewScale * chartSeconds;

            // グラウンド長のセット
            groundParent.transform.localScale = new Vector3(
                groundParent.transform.localScale.x,
                groundParent.transform.localScale.y,
                chartLength);

            // 分割線のセット
            laneDivisionLineParent.transform.localScale = new Vector3(
                laneDivisionLineParent.transform.localScale.x,
                laneDivisionLineParent.transform.localScale.y,
                chartLength);

            // 分割レイヤーオブジェクト長のセット
            divisionLayerObjParent.transform.localScale = new Vector3(
                divisionLayerObjParent.transform.localScale.x,
                divisionLayerObjParent.transform.localScale.y,
                chartLength);
        }

        /// <summary>
        /// 分割線の表示非表示
        /// </summary>
        /// <param name="divNum"></param>
        private void SetLaneDivisionLine(int divNum)
        {
            if (laneDivisionLines == null) { return; }
            if (laneDivisionLines.Length != 17) { return; }

            for (int i = 0; i < 16; i++)
            {
                laneDivisionLines[i].SetActive(i % (16 / divNum) == 0);
            }

            laneDivisionLines[16].SetActive(true);
        }
    }
}
