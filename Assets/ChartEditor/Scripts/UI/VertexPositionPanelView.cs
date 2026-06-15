using System;
using TMPro;
using UnityEngine;

namespace ChartEditor
{
    public class VertexPositionPanelView : MonoBehaviour
    {
        [SerializeField] TMP_InputField inputFieldX;
        [SerializeField] TMP_InputField inputFieldY;

        public event Action OnBeginEditListener;
        public event Action OnEndEditListener;
        public event Action<float> OnXValueChangedListener;
        public event Action<float> OnYValueChangedListener;

        void Awake()
        {
            if (inputFieldX != null)
            {
                inputFieldX.onSelect.AddListener(_ => OnBeginEditListener?.Invoke());
                inputFieldX.onValueChanged.AddListener(OnXValueChanged);
                inputFieldX.onEndEdit.AddListener(_ => OnEndEditListener?.Invoke());
            }

            if (inputFieldY != null)
            {
                inputFieldY.onSelect.AddListener(_ => OnBeginEditListener?.Invoke());
                inputFieldY.onValueChanged.AddListener(OnYValueChanged);
                inputFieldY.onEndEdit.AddListener(_ => OnEndEditListener?.Invoke());
            }
        }

        public void SetInteractable(bool value)
        {
            if (inputFieldX != null) { inputFieldX.interactable = value; }
            if (inputFieldY != null) { inputFieldY.interactable = value; }
        }

        public void SetPosition(Vector2 position, int decimalDigits)
        {
            var format = $"F{Mathf.Clamp(decimalDigits, 0, 9)}";

            if (inputFieldX != null)
            {
                inputFieldX.SetTextWithoutNotify(position.x.ToString(format));
            }

            if (inputFieldY != null)
            {
                inputFieldY.SetTextWithoutNotify(position.y.ToString(format));
            }
        }

        public void Clear()
        {
            if (inputFieldX != null) { inputFieldX.SetTextWithoutNotify(string.Empty); }
            if (inputFieldY != null) { inputFieldY.SetTextWithoutNotify(string.Empty); }
        }

        void OnXValueChanged(string value)
        {
            if (!float.TryParse(value, out var parsed)) { return; }
            OnXValueChangedListener?.Invoke(parsed);
        }

        void OnYValueChanged(string value)
        {
            if (!float.TryParse(value, out var parsed)) { return; }
            OnYValueChangedListener?.Invoke(parsed);
        }
    }
}
