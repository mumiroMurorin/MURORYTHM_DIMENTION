using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class LaneController : MonoBehaviour
    {
        [SerializeField] List<SerializeInterface<ILaneDeployable>> deplayables;

        [SerializeField] GameObject viewCamera;

        [Header("�g��k���̊��x")]
        [SerializeField] float scalingSensitivity = 0.1f;
        [Header("���_�ړ��̊��x")]
        [SerializeField] float moveSensitivity = 0.1f;

        float chartViewScale = 1f;

        void Start()
        {
            Initialize();

        }

        private void Initialize()
        {
            chartViewScale = 1f;

            foreach(SerializeInterface<ILaneDeployable> deployable in deplayables)
            {
                deployable.Value.Scaling(chartViewScale);
            }
        }

        private void Update()
        {
            OperateChartViewScale();
            OperateViewCamera();
        }

        /// <summary>
        /// �g�嗦�̑���
        /// </summary>
        private void OperateChartViewScale()
        {
            var scroll = Input.mouseScrollDelta.y;

            if (Mathf.Abs(scroll) < 0.01f) { return; }
            if (!Input.GetKey(KeyCode.LeftControl)) { return; }

            chartViewScale = Mathf.Clamp(chartViewScale + scroll * scalingSensitivity, 0.1f, float.MaxValue);

            foreach (SerializeInterface<ILaneDeployable> deployable in deplayables)
            {
                deployable.Value.Scaling(chartViewScale);
            }
        }

        /// <summary>
        /// �n�_�̑���
        /// </summary>
        private void OperateViewCamera() 
        {
            var scroll = Input.mouseScrollDelta.y;

            if (Mathf.Abs(scroll) < 0.01f) { return; }
            if (Input.GetKey(KeyCode.LeftControl)) { return; }

            viewCamera.transform.position += Vector3.forward * scroll * moveSensitivity;
        }
    }

}
