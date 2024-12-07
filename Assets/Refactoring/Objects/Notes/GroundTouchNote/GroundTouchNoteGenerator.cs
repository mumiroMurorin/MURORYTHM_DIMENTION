using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    public class GroundTouchNoteGenerator : MonoBehaviour, IGroundNoteGenerator
    {
        [SerializeField] GameObject noteObjectOriginPrefab;
        [SerializeField] Deformer groundBendDeformer;

        [Header("�}�X�ɉ������m�[�c�^�C��")]
        [SerializeField] GameObject singleTilePrefab;
        [SerializeField] GameObject rightEdgeTilePrefab;
        [SerializeField] GameObject centerTilePrefab;
        [SerializeField] GameObject leftEdgeTilePrefab;

        public GroundTouchNoteObject GenerateNote(IGroundNoteGenerationData generationData)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // �m�[�c�I�u�W�F�N�g�𐶐�����origin�ɂ�������
            GenerateNoteObject(generationData.NoteLaneWidth).transform.SetParent(origin.transform);

            origin.transform.eulerAngles = generationData.NoteEulerAngles;

            return origin.GetComponent<GroundTouchNoteObject>();
        }

        /// <summary>
        /// �m�[�c�^�C����g�ݍ��킹�ăm�[�c�𐶐�
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private GameObject GenerateNoteObject(int size) 
        {
            Vector3 pos, rot;
            GameObject pre = new GameObject("NoteObject");   //�܂Ƃߖ��̃I�u�W�F�N�g����

            // 1�}�X������
            for (int i = 0; i < size; i++)
            {
                // ���|�W�V�����Ɗp�x�̌v�Z
                pos = new Vector3(10 * Mathf.Cos((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 10 * Mathf.Sin((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 0);
                rot = new Vector3(0, 0, ((size - 1) / 2f - i) * 11.25f);

                // 1�}�X�m�[�g�̎�
                if (size == 1) { Instantiate(singleTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
                // �m�[�g���[�̎�
                else if (i == 0) { Instantiate(leftEdgeTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
                // �m�[�g�E�[�̎�
                else if (i == size - 1) { Instantiate(rightEdgeTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
                // �m�[�g���̎�
                else { Instantiate(centerTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
            }

            // Deform�̐ݒ�
            foreach (Transform t in pre.transform)
            {
                Deformable d = t.GetComponentInChildren<Deformable>();
                d.AddDeformer(groundBendDeformer);
            }

            return pre;
        }

    }

}
