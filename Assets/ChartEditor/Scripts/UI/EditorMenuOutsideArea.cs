using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    /// <summary>
    /// 画面上のメニューを開いている間、メニューの外側(画面全体)を覆う、見えない板
    ///
    /// ・メニューの外を押したら、メニューを閉じる
    /// ・メニューを開いている間、裏にあるボタンなどが押されないようにする
    /// 同じオブジェクトに、クリックを受け付ける透明な画像(Image)が必要。
    /// </summary>
    public class EditorMenuOutsideArea : MonoBehaviour, IPointerDownHandler
    {
        /// <summary>
        /// 押されたとき (マウスのどのボタンでも)
        /// </summary>
        public Action OnPressedListener { get; set; }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            OnPressedListener?.Invoke();
        }
    }
}
