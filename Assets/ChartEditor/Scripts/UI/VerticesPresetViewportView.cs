using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class VerticesPresetViewportView : MonoBehaviour
    {
        [SerializeField] GameObject obj;

        public void OnChangeEditNoteType(EditNoteType editNoteType)
        {
            if (editNoteType != EditNoteType.Vertices) { obj.SetActive(false); }
            else { obj.SetActive(true); }
        }
    }
}
