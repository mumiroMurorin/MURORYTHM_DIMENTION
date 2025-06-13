using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(VertexObject))]
    public class VertexSelectable : MonoBehaviour, ISelectableVertexObject
    {
        [Tooltip("選択時のアウトライン色")]
        [SerializeField] private Color outlineColorOnSelect;

        VertexObject vertexObject;

        public VertexObject VertexObject => vertexObject;

        private void Start()
        {
            vertexObject = GetComponent<VertexObject>();
        }

        void ISelectableVertexObject.OnDeselect()
        {
            vertexObject.SetOutlineActive(false);
        }

        void ISelectableVertexObject.OnSelect()
        {
            vertexObject.SetOutlineColor(outlineColorOnSelect, true);
            vertexObject.SetOutlineActive(true);
        }
    }

}
