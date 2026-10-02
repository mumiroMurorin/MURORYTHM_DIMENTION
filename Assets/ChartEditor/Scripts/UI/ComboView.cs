using TMPro;
using UnityEngine;

namespace ChartEditor
{
    public class ComboView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tmp;
        [SerializeField] string prefix = "Combo: ";

        public void OnChangeTotalNoteCount(int count)
        {
            if (tmp == null) { return; }

            tmp.text = $"{prefix}{count}";
        }
    }
}
