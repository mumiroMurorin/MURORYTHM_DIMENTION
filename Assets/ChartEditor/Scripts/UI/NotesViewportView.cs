using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NotesViewportView : MonoBehaviour
    {
        [SerializeField] GameObject obj;

        public void OnChangeEditMode(EditMode mode)
        {
            obj.SetActive(mode == EditMode.Deploy);
        }
    }

}
