using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 頂点レイヤーの下の、フィルムストリップの画面部品 (シーンに置いてある。SpaceHoldFilmStrip が探して使う)
    ///
    /// SpaceHoldFilmStripCanvas (重なり順 1: 編集画面 0 より手前、3D編集 5・メニューバー 10 より奥)
    /// ・FilmStrip: 帯の枠 (下のまんなか。見出し、説明、サムネイルを並べる場所 (Viewport / Content)、目盛りの帯、横のスクロールバー)
    /// ・FilmStripMinimized: 帯をしまったときに出す小さいマス (MinimizableWindow。ほかのウィンドウのマスと同じ見た目)
    /// サムネイル・「+」・目盛りの線・目盛りの名前は、数が節の並びで変わるので、Content の中のひな形 (非表示) を、コードで複製して使う。
    /// 帯を出さない間は、キャンバスごと隠しておく。
    /// </summary>
    public class SpaceHoldFilmStripView : MonoBehaviour
    {
        [Tooltip("フィルムストリップのキャンバス (帯を出す間だけ表示する)")]
        [SerializeField] Canvas canvas;
        [Tooltip("見出し (SpaceHold の断面 n / m)")]
        [SerializeField] TextMeshProUGUI titleLabel;
        [Tooltip("サムネイルを並べる場所 (この幅だけ見える)")]
        [SerializeField] RectTransform viewport;
        [Tooltip("サムネイルを並べる入れ物 (左右に送る。幅はコードで決める)")]
        [SerializeField] RectTransform content;
        [Tooltip("ドラッグしている間の、動かす先の印")]
        [SerializeField] Image dropHighlight;

        [Header("ひな形 (非表示。コードで複製する)")]
        [SerializeField] FilmStripItem itemTemplate;
        [SerializeField] EditorUiButton plusTemplate;
        [SerializeField] Image tickTemplate;
        [SerializeField] TextMeshProUGUI rulerLabelTemplate;

        public Canvas Canvas => canvas;
        public TextMeshProUGUI TitleLabel => titleLabel;
        public RectTransform Viewport => viewport;
        public RectTransform Content => content;
        public Image DropHighlight => dropHighlight;
        public FilmStripItem ItemTemplate => itemTemplate;
        public EditorUiButton PlusTemplate => plusTemplate;
        public Image TickTemplate => tickTemplate;
        public TextMeshProUGUI RulerLabelTemplate => rulerLabelTemplate;
    }
}
