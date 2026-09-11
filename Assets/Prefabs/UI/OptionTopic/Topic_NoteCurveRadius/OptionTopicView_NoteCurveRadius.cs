using TMPro;

namespace UIInSelectScene
{
    public class OptionTopicView_NoteCurveRadius : OptionTopicViewBase
    {
        [UnityEngine.SerializeField] TextMeshProUGUI noteCurveRadiusTmp;

        public void OnChangeRadius(int radius)
        {
            noteCurveRadiusTmp.text = radius.ToString();
        }
    }
}
