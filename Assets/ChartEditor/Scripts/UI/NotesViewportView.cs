using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NotesViewportView : MonoBehaviour
    {
        [SerializeField] GameObject obj;

        public void OnChangeEditMode(EditMode mode, EditNoteType editNoteType)
        {
            if (editNoteType == EditNoteType.Vertices) { obj.SetActive(false); }
            else if (mode == EditMode.Connecting) { obj.SetActive(false); }
            else { obj.SetActive(true); }
        }
    }

}
