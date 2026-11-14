using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    /// <summary>
    /// クリックされたことを知らせるだけの画面部品
    ///
    /// ボタン(Button)と違って「選択中」の状態にならない。
    /// (ボタンは、クリックした後も選択中のまま残り、Space キーや Enter キーでもう一度押されてしまう。
    ///  譜面エディタでは Space キーを再生に使うので、実行時に作る小さな押しどころにはこちらを使う)
    /// クリックを受け取るには、同じオブジェクトに、クリックを受け付ける画像(Image など)が必要。
    /// </summary>
    public class ClickArea : MonoBehaviour, IPointerClickHandler
    {
        public Action OnClickedListener { get; set; }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            // 左クリックだけ
            if (eventData.button != PointerEventData.InputButton.Left) { return; }

            OnClickedListener?.Invoke();
        }
    }
}
