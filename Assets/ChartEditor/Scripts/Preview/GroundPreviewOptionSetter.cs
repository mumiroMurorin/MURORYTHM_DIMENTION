using UnityEngine;
using UniRx;
using VContainer;
using Deform;

namespace ChartEditor
{
    /// <summary>
    /// 全画面プレビューの舞台 (ゲーム本編の Stage) のグラウンドの設定
    /// (エディタの舞台からは、ゲーム本編の GroundOptionSetter を外してある。代わりにここが、エディタの設定から決める)
    /// </summary>
    public class GroundPreviewOptionSetter : MonoBehaviour
    {
        [SerializeField] GameObject[] divisionLines;
        [Tooltip("グラウンドを曲げる部品 (ゲーム本編の GroundOptionSetter と同じもの)")]
        [SerializeField] BendDeformer bendDeformer;
        [Tooltip("グラウンドのメッシュの親 (奥行きの表示範囲で切るもの)")]
        [SerializeField] Transform groundObjectRoot;
        [SerializeField, Min(0f)] float visibleDistancePadding;

        IChartEditorOptionGetter optionGetter;
        INoteSpawnDataOptionGetter spawnOptionGetter;
        // カーブと奥行きの表示範囲 (ゲーム本編と同じ処理)
        GroundShaper shaper;

        [Inject]
        public void Construct(IChartEditorOptionGetter optionGetter, INoteSpawnDataOptionGetter spawnOptionGetter)
        {
            this.optionGetter = optionGetter;
            this.spawnOptionGetter = spawnOptionGetter;
        }

        private void Start()
        {
            shaper = new GroundShaper(bendDeformer, groundObjectRoot, visibleDistancePadding);
            shaper.InitializeVisibility();
            if (bendDeformer == null || groundObjectRoot == null)
            {
                Debug.LogWarning("【プレビュー】グラウンドの部品がつながっていないので、カーブと奥行きの表示範囲をグラウンドに反映できません");
            }
            Bind();
        }

        private void Bind()
        {
            optionGetter?.LaneDivisionNum
                .Subscribe(divNum => GroundShaper.SetDivisionLines(divisionLines, divNum))
                .AddTo(this.gameObject);

            // 奥行きの表示範囲と、カーブの半径 (録音 20261117_051155 / 162506: ゲーム本編と同じように、グラウンドも)
            var noteVisibility = FindObjectOfType<PreviewNoteVisibilityController>(true);
            optionGetter?.NoteVisibleDistance
                .Subscribe(distance =>
                {
                    shaper.SetVisibleDistance(distance);
                    if (noteVisibility != null) { noteVisibility.SetVisibleAheadDistance(distance); }
                })
                .AddTo(this.gameObject);
            spawnOptionGetter?.NoteCurveRadius
                .Subscribe(shaper.SetCurveRadius)
                .AddTo(this.gameObject);
        }

        private void OnDestroy()
        {
            shaper?.Dispose();
        }
    }

}
