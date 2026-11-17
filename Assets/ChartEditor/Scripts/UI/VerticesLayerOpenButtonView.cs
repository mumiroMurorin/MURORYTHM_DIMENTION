using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// ミニプレビューの上の段の左の、「頂点レイヤーで編集」のボタン (録音 20261117_195031)
    ///
    /// スペースのレイヤーで、SpaceHold・SpaceBreak (頂点を持つノーツ) を選んでいる間に出す。押すと、そのノーツを頂点レイヤーで開く
    /// (ノーツのダブルクリックと同じ)。いくつも選んでいるとき (つながった SpaceHold を全体で選んだときなど) は、いちばん前の部分を開く。
    /// 同じ場所の「スペースに戻る」(LayerReturnButtonView) は頂点レイヤーでだけ出るので、重ならない。
    /// このコンポーネントは、いつも出ている入れ物に付け、ボタンは、その子に置く (ボタンを隠しても、ここは動き続けるように)。
    /// </summary>
    public class VerticesLayerOpenButtonView : MonoBehaviour
    {
        [SerializeField] Button button;

        void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
                    SpaceHold3DEditor.Instance?.OpenSpaceSelectionInVerticesLayer();
                });
            }
        }

        void Update()
        {
            if (button == null) { return; }

            var editor = SpaceHold3DEditor.Instance;
            bool isShown = editor != null && editor.CanOpenSpaceSelectionInVerticesLayer();
            if (button.gameObject.activeSelf != isShown) { button.gameObject.SetActive(isShown); }
        }
    }
}
