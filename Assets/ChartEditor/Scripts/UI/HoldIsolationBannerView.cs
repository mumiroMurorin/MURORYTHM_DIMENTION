using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 「ホールドを1本だけ編集中」の表示
    ///
    /// ホールドをダブルクリックして、そのホールドだけを編集している間 (HoldIsolation)、譜面の上に出す。
    /// 戻り方 (Esc キー、何も無い所のダブルクリック) も、ここに書いてある。
    /// 表示は、シーン(EditorCanvas)に最初から置いてある。ここでは、出す・隠すだけを行う。
    /// </summary>
    public class HoldIsolationBannerView : MonoBehaviour
    {
        [Tooltip("表示する部品 (ふだんは隠してある)")]
        [SerializeField] GameObject banner;

        public void SetVisible(bool isVisible)
        {
            if (banner != null && banner.activeSelf != isVisible) { banner.SetActive(isVisible); }
        }
    }
}
