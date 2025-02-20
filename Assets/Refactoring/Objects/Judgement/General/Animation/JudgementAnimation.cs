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

            // カメラの方向を計算
            Vector3 directionToCamera = transform.position - mainCamera.transform.position;

            // Y軸の回転のみを考慮
            directionToCamera.x = 0;

            Quaternion currentRotation = transform.rotation;

            // 回転を適用
            if (directionToCamera.sqrMagnitude > 0.01f) // 長さがほぼゼロでないか確認
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
