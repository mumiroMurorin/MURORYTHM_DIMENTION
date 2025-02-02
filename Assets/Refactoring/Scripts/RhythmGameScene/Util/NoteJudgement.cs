using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        /// Vector�̊e�v�f�̍ő�l��臒l�ɂȂ�
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        static public Vector3 CalcJudgementThresHold(Vector3 vector,int[] range)
        {
            Vector3 maxVector = 

            float rad = GetRadianXY(vector);
            return new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), vector.z);
        }

        public static float GetRadianXY(Vector2 direction)
        {
            // Mathf.Atan2(y, x) ���g���ă��W�A�����擾
            float angle = Mathf.Atan2(direction.y, direction.x);

            return angle;
        }
    }
}