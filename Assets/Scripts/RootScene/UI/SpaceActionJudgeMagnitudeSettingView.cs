using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIInRootScene
{
    public class SpaceActionJudgeMagnitudeSettingView : MonoBehaviour
    {
        [SerializeField] TMP_InputField multiplierInputField;
        [SerializeField] Button decreaseButton;
        [SerializeField] Button increaseButton;
        [SerializeField] float buttonStep = 0.1f;

        public Action<float> OnChangeMultiplierListener { get; set; }

        void Start()
        {
            multiplierInputField?.onEndEdit.AddListener(_ => OnEndEditMultiplier());
            decreaseButton?.onClick.AddListener(() => AddMultiplier(-buttonStep));
            increaseButton?.onClick.AddListener(() => AddMultiplier(buttonStep));
        }

        public void OnChangeMultiplier(float multiplier)
        {
            if (multiplierInputField == null) { return; }

            multiplierInputField.SetTextWithoutNotify(multiplier.ToString("0.00"));
        }

        void OnEndEditMultiplier()
        {
            if (multiplierInputField == null) { return; }
            if (!float.TryParse(multiplierInputField.text, out float multiplier)) { return; }

            OnChangeMultiplierListener?.Invoke(multiplier);
        }

        void AddMultiplier(float delta)
        {
            float current = 1f;
            if (multiplierInputField != null)
            {
                float.TryParse(multiplierInputField.text, out current);
            }

            OnChangeMultiplierListener?.Invoke(current + delta);
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
