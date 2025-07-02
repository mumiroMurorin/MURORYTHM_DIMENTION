using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteSelectable : MonoBehaviour, ISelectableNoteObject
    {
        [Tooltip("選択時のアウトライン色")]
        [SerializeField] private ColorSetting outlineColorOnSelect;

        NoteObject noteObject;

        public NoteObject NoteObject => noteObject;

        private void Start()
        {
            noteObject = GetComponent<NoteObject>();
        }

        void ISelectableNoteObject.OnDeselect()
        {
            noteObject.OutlineColors.Remove(outlineColorOnSelect);
        }

        void ISelectableNoteObject.OnSelect()
        {
            noteObject.OutlineColors.Add(outlineColorOnSelect);
        }
    }

}
