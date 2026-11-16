using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// ミニプレビューの「プレビュー」のボタンの横の、「3Dで編集」のボタン (録音 20261116_225326)
    ///
    /// 頂点レイヤーで SpaceHold の節を編集している間と、スペースのレイヤーで2つ以上の部品からなる SpaceHold を選んでいる間に出す
    /// (録音 20261117_013612)。押すと、その SpaceHold を3D編集 (SpaceHold3DEditor) で開く。
    /// (前はフィルムストリップの見出しにあった)
    /// このコンポーネントは、いつも出ている入れ物に付け、ボタンは、その子に置く (ボタンを隠しても、ここは動き続けるように)。
    /// </summary>
    public class SpaceHold3DOpenButtonView : MonoBehaviour
    {
        [SerializeField] Button button;

        void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
                    var editor = SpaceHold3DEditor.Instance;
                    if (editor == null) { return; }
                    if (editor.CanOpenFromVerticesLayer()) { editor.OpenFromVerticesLayer(); }
                    else { editor.OpenFromSpaceLayerSelection(); }
                });
            }
        }

        void Update()
        {
            if (button == null) { return; }

            var editor = SpaceHold3DEditor.Instance;
            bool isShown = editor != null && (editor.CanOpenFromVerticesLayer() || editor.CanOpenFromSpaceLayerSelection());
            if (button.gameObject.activeSelf != isShown) { button.gameObject.SetActive(isShown); }
        }
    }
}
