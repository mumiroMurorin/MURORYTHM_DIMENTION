using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class JudgementAnimation : MonoBehaviour
    {
        Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            UpdateRotation();
        }

        private void UpdateRotation()
        {
            if (mainCamera == null) { return; }

            // �J�����̕������v�Z
            Vector3 directionToCamera = transform.position - mainCamera.transform.position;

            // Y���̉�]�݂̂��l��
            directionToCamera.x = 0;

            Quaternion currentRotation = transform.rotation;

            // ��]��K�p
            if (directionToCamera.sqrMagnitude > 0.01f) // �������قڃ[���łȂ����m�F
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = Quaternion.Euler(
                    targetRotation.eulerAngles.x,
                    targetRotation.eulerAngles.y,
                    currentRotation.eulerAngles.z
                );
            }
        }

        public void Destroy()
        {
            Destroy(this.gameObject);
        }

    }

}
