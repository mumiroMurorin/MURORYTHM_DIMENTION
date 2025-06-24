using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

namespace UIInRootScene
{
    public class HandInfoView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI originPositionLeftTmp;
        [SerializeField] TextMeshProUGUI originPositionRightTmp;
        [SerializeField] TextMeshProUGUI normalizedPositionLeftTmp;
        [SerializeField] TextMeshProUGUI normalizedPositionRightTmp;
        [SerializeField] TextMeshProUGUI velocityLeftTmp;
        [SerializeField] TextMeshProUGUI velocityRightTmp;
        [SerializeField] Button flipButton;

        public Action OnPushFlipButtonListner { get; set; }
        private void Start()
        {
            flipButton.onClick.AddListener(OnPushFlipButton);
        }

        public void OnChangeRightHandOriginPosition(Vector3 pos)
        {
            originPositionRightTmp.text = pos.ToString();
        }

        public void OnChangeLeftHandOriginPosition(Vector3 pos)
        {
            originPositionLeftTmp.text = pos.ToString();
        }

        public void OnChangeRightHandNormalizedPosition(Vector3 pos)
        {
            normalizedPositionRightTmp.text = pos.ToString();
        }

        public void OnChangeLeftHandNormalizedPosition(Vector3 pos)
        {
            normalizedPositionLeftTmp.text = pos.ToString();
        }

        public void OnChangeRightHandVelocity(Vector3 velocity)
        {
            velocityRightTmp.text = velocity.ToString();
        }

        public void OnChangeLeftHandVelocity(Vector3 velocity)
        {
            velocityLeftTmp.text = velocity.ToString();
        }

        private void OnPushFlipButton()
        {
            OnPushFlipButtonListner?.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }

    }
}
