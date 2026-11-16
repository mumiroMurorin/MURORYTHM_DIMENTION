using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// ミニプレビューの上の「スペースに戻る」/「グラウンドに戻る」のボタン (録音 20261117_024505)
    ///
    /// 頂点レイヤーを開いている間だけ出す。押すと、頂点レイヤーに入る前のレイヤーへ戻る (VerticesLayerReturn)。
    /// このコンポーネントは、いつも出ている入れ物に付け、ボタンは、その子に置く (ボタンを隠しても、ここは動き続けるように)。
    /// </summary>
    public class LayerReturnButtonView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text label;

        EditNoteType shownLayer = (EditNoteType)(-1);

        void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
                    VerticesLayerReturn.Return();
                });
            }
        }

        void Update()
        {
            if (button == null) { return; }

            bool isShown = VerticesLayerReturn.CanReturn;
            if (button.gameObject.activeSelf != isShown) { button.gameObject.SetActive(isShown); }
            if (!isShown || label == null) { return; }

            EditNoteType layer = VerticesLayerReturn.ReturnLayer;
            if (layer == shownLayer) { return; }
            shownLayer = layer;
            label.text = layer == EditNoteType.Ground ? "グラウンドに戻る" : "スペースに戻る";
        }
    }
}
