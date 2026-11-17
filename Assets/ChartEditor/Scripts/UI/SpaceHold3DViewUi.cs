using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold の3D編集の、3Dの絵を映す画面部品 (シーンに置いてある。SpaceHold3DView が探して使う)
    ///
    /// SpaceHold3DCanvas (重なり順 5: フィルムストリップ 1 より手前、3D編集の道具の列 6・メニューバー 10 より奥)
    /// ・Root: 下地 (不透明。メニューバーより下の全体)。その中に、3Dの絵 (Viewport)、つまみと文字の置き場 (Handles)、
    ///   下のまんなかの文字 (Info: 選んでいる節、Status: 最後の操作)、上の見出し (Banner)、右上の視点のウィンドウ (ViewPanel)
    /// ・TopStrip: メニューバーの右の、メニューバーと同じ高さの所をふさぐ下地
    /// つまみと文字は、数が決まっていないので、Handles の中のひな形 (非表示) を、コードで複製して使う。
    /// 3D編集を開いていない間は、キャンバスごと隠しておく。
    /// </summary>
    public class SpaceHold3DViewUi : MonoBehaviour
    {
        [Tooltip("3D編集の画面部品のキャンバス (開いている間だけ出す)")]
        [SerializeField] Canvas canvas;
        [Tooltip("3Dの絵 (カメラの絵を、コードで貼る)")]
        [SerializeField] RawImage viewportImage;
        [Tooltip("つまみと文字の置き場 (中身は、コードでひな形を複製して作る)")]
        [SerializeField] RectTransform handleLayer;
        [Tooltip("つまみのひな形 (非表示)")]
        [SerializeField] Image handleTemplate;
        [Tooltip("3Dの中の文字のひな形 (非表示)")]
        [SerializeField] TextMeshProUGUI labelTemplate;
        [Tooltip("丸いつまみの絵 (四角いつまみは、絵なし)")]
        [SerializeField] Sprite circleSprite;
        [Tooltip("下のまんなかの文字: 選んでいる節")]
        [SerializeField] TextMeshProUGUI infoLabel;
        [Tooltip("下のまんなかの文字: 最後の操作")]
        [SerializeField] TextMeshProUGUI statusLabel;

        [Header("右上: 視点のウィンドウ")]
        [SerializeField] SpaceHold3DViewGizmo gizmo;
        [Tooltip("透視 / 平行 (押すたびに切り替える。文字は、今の見え方)")]
        [SerializeField] EditorUiButton projectionButton;
        [Tooltip("全体を映す (F)")]
        [SerializeField] EditorUiButton frameButton;
        [Tooltip("奥行きの縮尺を小さくする")]
        [SerializeField] EditorUiButton depthDownButton;
        [Tooltip("奥行きの縮尺を大きくする")]
        [SerializeField] EditorUiButton depthUpButton;
        [Tooltip("今の奥行きの縮尺 (x1 など)")]
        [SerializeField] TextMeshProUGUI depthScaleLabel;

        public Canvas Canvas => canvas;
        public RawImage ViewportImage => viewportImage;
        public RectTransform HandleLayer => handleLayer;
        public Image HandleTemplate => handleTemplate;
        public TextMeshProUGUI LabelTemplate => labelTemplate;
        public Sprite CircleSprite => circleSprite;
        public TextMeshProUGUI InfoLabel => infoLabel;
        public TextMeshProUGUI StatusLabel => statusLabel;

        public SpaceHold3DViewGizmo Gizmo => gizmo;
        public EditorUiButton ProjectionButton => projectionButton;
        public EditorUiButton FrameButton => frameButton;
        public EditorUiButton DepthDownButton => depthDownButton;
        public EditorUiButton DepthUpButton => depthUpButton;
        public TextMeshProUGUI DepthScaleLabel => depthScaleLabel;
    }
}
