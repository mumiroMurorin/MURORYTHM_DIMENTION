using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class OperationDescriptionView : MonoBehaviour
    {
        [SerializeField] EditModeToDescription[] descriptionObjects;

        public void OnChangeEditMode(EditMode editMode)
        {
            foreach(var description in descriptionObjects)
            {
                description.CheckAndSet(editMode);
            }
        }

        [System.Serializable]
        class EditModeToDescription
        {
            [SerializeField] EditMode editMode;
            [SerializeField] GameObject obj;

            public void CheckAndSet(EditMode currentEditMode)
            {
                obj.SetActive(editMode == currentEditMode);
            }
        }
    }

}
