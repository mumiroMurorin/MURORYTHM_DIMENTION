using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class LaneDeployer : MonoBehaviour
    {
        [Tooltip("1小節の長さ(拡大率1のとき)")]
        [SerializeField] float lengthInBarLine = 10f;
        [SerializeField] SerializeInterface<ILaneDeployable> barLineDeplayable;
        [SerializeField] SerializeInterface<ILaneDeployable> beatLineDeployable;
        [SerializeField] SerializeInterface<ILaneDeployable> subdivisionLineDeployable;
        [SerializeField] SerializeInterface<ILaneDeployable> colliderDeployableGroup;
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
            // 楽曲
            chartEditorDataGetter?.Music
                .Subscribe(music => {
                    if(music == null) { return; }
                    if(chartEditorDataGetter.MainBpm.Value <= 0) { return; }

                    GenerateLane(music.length, chartEditorDataGetter.MainBpm.Value);
                })
                .AddTo(this.gameObject);

            // メインBPM
            chartEditorDataGetter?.MainBpm
                .Subscribe(bpm => {
                    if (chartEditorDataGetter.Music.Value == null) { return; }
                    if (bpm <= 0) { return; }

                    GenerateLane(chartEditorDataGetter.Music.Value.length, bpm);
                })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 楽曲の長さとBPMに基づき譜面レーンの生成
        /// </summary>
        /// <param name="musicLength"></param>
        /// <param name="mainBpm"></param>
        private void GenerateLane(float musicLength, float mainBpm)
        {
            // まず初期化
            ClearLane();

            // レーンの生成
            // 小節線の数 (曲の長さ[分] × 1分間中の小節数[個])
            int barLineNum = (int)Mathf.Ceil((musicLength / 60f) * (mainBpm / 4f));
            // 1小節の長さ
            float lengthInBarLine = this.lengthInBarLine * chartEditorDataGetter.ChartViewScale.Value;

            // 楽曲の長さを超えるまで繰り返す
            // 1小節 (4分音符 × 4)
            for (int i = 0; i < barLineNum; i++)
            {
                // 小節線のインスタンス化
                barLineDeplayable.Value.Deploy(new Vector3(0, 0, i * lengthInBarLine));

                // 1拍 (bpmは1小節内の4分音符の数)
                for (int j = 0; j < 4; j++)
                {
                    // 拍線と被るため0は除外
                    if (j != 0) { beatLineDeployable.Value.Deploy(new Vector3(0, 0, (i + j / 4f) * lengthInBarLine)); }

                    // 16分
                    for (int k = 0; k < 4; k++)
                    {
                        // 16分線
                        if (k != 0) { subdivisionLineDeployable.Value.Deploy(new Vector3(0, 0, (i + j / 4f + k / 16f) * lengthInBarLine)); }
                        // 設置コライダー
                        colliderDeployableGroup.Value.Deploy(new Vector3(0, 0, (i + j / 4f + k / 16f) * lengthInBarLine));
                    }
                }
            }

            // グラウンドの生成
            ground.transform.localScale = new Vector3(
                ground.transform.localScale.x,
                barLineNum * lengthInBarLine,
                ground.transform.localScale.z);

            ground.transform.position = new Vector3(
                ground.transform.position.x,
                ground.transform.position.y,
                ground.transform.localScale.y / 2f
                );
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
        }
    }

}
