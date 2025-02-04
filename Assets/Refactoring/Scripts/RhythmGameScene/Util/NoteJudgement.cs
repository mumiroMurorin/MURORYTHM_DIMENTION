using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace NoteJudgement
{
    static public class DynamicNote
    {
        /// <summary>
        /// ���͂��ꂽVector������臒l�𒴂��Ă��邩�Ԃ�
        /// </summary>
        /// <param name="thresHold"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool JudgeThreshold(Vector3 thresHold, Vector3 input)
        {
            // XYZ�ǂꂩ��臒l�𒴂��Ă����true
            return JudgeThresholdValue(thresHold.x, input.x) ||
                JudgeThresholdValue(thresHold.y, input.y) ||
                JudgeThresholdValue(thresHold.z, input.z);
        }

        /// <summary>
        /// �v�f�̒l�Ŕ���
        /// </summary>
        /// <param name="valueThreshold"></param>
        /// <param name="valueInput"></param>
        /// <returns></returns>
        private static bool JudgeThresholdValue(float valueThreshold, float valueInput)
        {
            if (valueThreshold == 0f) { return false; }
            if (valueThreshold > 0 && valueInput < valueThreshold) { return false; }
            if (valueThreshold < 0 && valueInput > valueThreshold) { return false; }

            return true;
        }

        /// <summary>
        /// ����(0,1)�Ƃ���x�N�g������ɔ����臒l���Z�o����
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        static public Vector3 CalcJudgementThresHold(float radius, int index, Vector3 rotationVector, Vector3? center = null)
        {
            // �~�ォ�璆�S�Ɍ����Ẵx�N�g�����Z�o
            Vector3 directionToCenter = GetDirectionToCenter(radius, index, center);

            // ��]�p���Z�o
            float rad = GetRadianXY(rotationVector);

            return RotateVector(directionToCenter, rad);
        }

        public static float GetRadianXY(Vector2 direction)
        {
            // Mathf.Atan2(y, x) ���g���ă��W�A�����擾
            float angle = Mathf.Atan2(direction.y, direction.x);

            return angle;
        }

        /// <summary>
        /// ���a r �Ɗp�x theta ���璆�S�Ɍ������x�N�g�����擾
        /// </summary>
        /// <param name="r"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static Vector3 GetDirectionToCenter(float r, int index, Vector3? center = null)
        {
            center = center ?? Vector3.zero;

            float radian = (180f + (index + 0.5f) * 11.25f) * Mathf.Deg2Rad;
            float x = r * Mathf.Cos(radian) + center.Value.x;
            float y = r * Mathf.Sin(radian) + center.Value.y;
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// �x�N�g���� rotationAngle ��������]������
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="rotationAngle"></param>
        /// <returns></returns>
        public static Vector3 RotateVector(Vector3 vector, float rotationAngle)
        {
            float radian = rotationAngle * Mathf.Deg2Rad;

            // 2D ��]�s���K�p (Z���𒆐S�ɉ�])
            float rotatedX = vector.x * Mathf.Cos(radian) - vector.y * Mathf.Sin(radian);
            float rotatedY = vector.x * Mathf.Sin(radian) + vector.y * Mathf.Cos(radian);

            return new Vector3(rotatedX, rotatedY, vector.z); // Z���͂��̂܂�
        }

        /// <summary>
        /// 2��TimeToPosition����x�N�g�������߂�
        /// </summary>
        /// <param name="timeToPosition1"></param>
        /// <param name="timeToPosition2"></param>
        /// <returns></returns>
        public static Vector3 CalculateVelocity((float, Vector3)timeToPosition1, (float, Vector3) timeToPosition2)
        {
            float t1 = timeToPosition1.Item1 < timeToPosition2.Item1 ? timeToPosition1.Item1 : timeToPosition2.Item1;
            float t2 = timeToPosition1.Item1 < timeToPosition2.Item1 ? timeToPosition2.Item1 : timeToPosition1.Item1;
            Vector3 p1 = timeToPosition1.Item1 < timeToPosition2.Item1 ? timeToPosition1.Item2 : timeToPosition2.Item2;
            Vector3 p2 = timeToPosition1.Item1 < timeToPosition2.Item1 ? timeToPosition2.Item2 : timeToPosition1.Item2;

            return (p2 - p1) / Mathf.Abs(t2 - t1);
        }
    }
}