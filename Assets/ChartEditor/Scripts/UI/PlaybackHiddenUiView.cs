using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 全画面プレビューで再生している間に隠す画面部品の一覧 (録音 20261117_024318)
    ///
    /// ログ、操作説明、クイックメニュー、譜面情報、小さいマス (しまったウィンドウ)、編集画面へ戻るボタン、頂点の道具の列
    /// (ログと、全画面プレビューのボタン・道具の列は、それぞれのキャンバスごと隠す)。
    /// 上のメニューバーは隠さない。再生を止めると、それぞれ元の状態に戻る (隠すのは透明にするだけ: UiHideReasons)。
    /// 出し入れは PreviewPlaybackUi が決める。一覧はシーンに置いてある (targets)。
    /// </summary>
    public class PlaybackHiddenUiView : MonoBehaviour
    {
        [Tooltip("再生中に隠す画面部品")]
        [SerializeField] RectTransform[] targets;

        public void SetHidden(bool isHidden)
        {
            if (targets == null) { return; }

            foreach (var target in targets)
            {
                if (target != null) { UiHideReasons.Set(target.gameObject, UiHideReasons.PLAYBACK, isHidden); }
            }
        }
    }
}
