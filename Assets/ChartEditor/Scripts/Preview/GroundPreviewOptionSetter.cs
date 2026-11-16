using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class GroundPreviewOptionSetter : MonoBehaviour
    {
        [SerializeField] GameObject[] divisionLines;

        IChartEditorOptionGetter optionGetter;
        INoteSpawnDataOptionGetter spawnOptionGetter;
        // 全画面プレビューの舞台 (ゲーム本編のもの) のグラウンド。ゲーム本編の設定は届かないので、ここから渡す
        GroundOptionSetter stageGround;

        [Inject]
        public void Construct(IChartEditorOptionGetter optionGetter, INoteSpawnDataOptionGetter spawnOptionGetter)
        {
            this.optionGetter = optionGetter;
            this.spawnOptionGetter = spawnOptionGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            optionGetter?.LaneDivisionNum
                .Subscribe(SetDivisionLines)
                .AddTo(this.gameObject);

            // 奥行きの表示範囲と、グラウンドのカーブ (録音 20261117_051155: ゲーム本編と同じ設定を、プレビューでも)
            stageGround = FindObjectOfType<GroundOptionSetter>(true);
            var noteVisibility = FindObjectOfType<PreviewNoteVisibilityController>(true);
            optionGetter?.NoteVisibleDistance
                .Subscribe(distance =>
                {
                    if (stageGround != null) { stageGround.ApplyVisibleDistance(distance); }
                    if (noteVisibility != null) { noteVisibility.SetVisibleAheadDistance(distance); }
                })
                .AddTo(this.gameObject);
            spawnOptionGetter?.NoteCurveRadius
                .Subscribe(radius => { if (stageGround != null) { stageGround.ApplyCurveRadius(radius); } })
                .AddTo(this.gameObject);
        }

        private void SetDivisionLines(int divNum)
        {
            if (divisionLines.Length != 17) { return; }

            for (int i = 0; i < divisionLines.Length; i++)
            {
                if (i == 0 || i == 16)
                {
                    divisionLines[i].SetActive(true);
                }
                else if (i % (16 / divNum) == 0)
                {
                    divisionLines[i].SetActive(true);
                }
                else
                {
                    divisionLines[i].SetActive(false);
                }
            }
        }
    }

}