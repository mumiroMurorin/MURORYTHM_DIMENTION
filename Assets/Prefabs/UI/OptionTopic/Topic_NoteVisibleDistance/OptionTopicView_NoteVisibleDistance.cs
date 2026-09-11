using TMPro;

namespace UIInSelectScene
{
    public class OptionTopicView_NoteVisibleDistance : OptionTopicViewBase
    {
        [UnityEngine.SerializeField] TextMeshProUGUI noteVisibleDistanceTmp;

        public void OnChangeDistance(int distance)
        {
            noteVisibleDistanceTmp.text = distance.ToString();
        }
    }
}
