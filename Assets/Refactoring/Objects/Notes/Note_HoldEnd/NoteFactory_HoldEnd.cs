using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    public class NoteFactory_HoldEnd : NoteFactory<NoteData_HoldEnd>
    {
        [SerializeField] GameObject noteObjectOriginPrefab;

        [Header("�}�X�ɉ������m�[�c�^�C��")]
        [SerializeField] GameObject singleTilePrefab;
        [SerializeField] GameObject rightEdgeTilePrefab;
        [SerializeField] GameObject centerTilePrefab;
        [SerializeField] GameObject leftEdgeTilePrefab;

        INoteSpawnDataOptionHolder optionHolder;
        ISliderInputGetter sliderInputGetter;
        IJudgementRecorder judgementRecorder;
        ITimeGetter timer;
        GameObject groundObject;
        Deformer groundDeformer;

        public override void Initialize(NoteFactoryInitializingData initializingData)
        {
            this.optionHolder = initializingData.OptionHolder;
            this.groundObject = initializingData.GroundObject;
            this.groundDeformer = initializingData.GroundDeformer;
            this.sliderInputGetter = initializingData.SliderInputGetter;
            this.judgementRecorder = initializingData.JudgementRecorder;
            this.timer = initializingData.Timer;
        }

        public override NoteObject<NoteData_HoldEnd> Spawn(NoteData_HoldEnd data)
        {
            // ����
            NoteObject<NoteData_HoldEnd> note = GenerateNoteInstance(ConvertNoteData(data));

            // �ʒu����
            SetTransform(note, data);

            // ������
            note.Initialize(data);

            return note;
        }

        /// <summary>
        /// �m�[�g�f�[�^�ɂ���Ȃ����ǉ�
        /// </summary>
        /// <param name="data"></param>
        private NoteData_HoldEnd ConvertNoteData(NoteData_HoldEnd data)
        {
            // �m�[�c�f�[�^�ɂ��낢��ǉ�
            data.SliderInput = this.sliderInputGetter;
            data.Timer = this.timer;
            data.JudgementRecorder = this.judgementRecorder;

            return data;
        }

        /// <summary>
        /// �m�[�c���C���X�^���X�����ĕԂ�
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private NoteObject<NoteData_HoldEnd> GenerateNoteInstance(NoteData_HoldEnd data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // �m�[�c�I�u�W�F�N�g�𐶐�
            GameObject noteObj = GenerateNoteObject(data.Range.Length);

            // origin�ɂ�������
            noteObj.transform.SetParent(origin.transform);

            // �p�x(���[��)����
            noteObj.transform.eulerAngles = new Vector3(0, 0, CalcNoteTransform.NoteAngle(data.Range));

            // �R���|�[�l���g���擾
            NoteObject<NoteData_HoldEnd> note = origin.GetComponent<NoteObject<NoteData_HoldEnd>>();

            return note;
        }

        /// <summary>
        /// �m�[�c�^�C����g�ݍ��킹�ăm�[�c�𐶐�
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private GameObject GenerateNoteObject(int size)
        {
            Vector3 pos, rot;
            GameObject pre = new GameObject("NoteObjects");   //�܂Ƃߖ��̃I�u�W�F�N�g����

            // 1�}�X������
            for (int i = 0; i < size; i++)
            {
                // ���|�W�V�����Ɗp�x�̌v�Z
                
                //pos = new Vector3(10 * Mathf.Cos((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 10 * Mathf.Sin((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 0);
                float radian = (5.625f * (2 * i - size + 1) - 90f) * Mathf.Deg2Rad;
                pos = new Vector3(10 * Mathf.Cos(radian), 10 * Mathf.Sin(radian), 0);
                rot = new Vector3(0, 0, ((size - 1) / 2f - (size - i - 1)) * 11.25f);

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
                d.AddDeformer(groundDeformer);
            }

            return pre;
        }

        /// <summary>
        /// �ʒu�����Ȃ�
        /// </summary>
        private void SetTransform(NoteObject<NoteData_HoldEnd> note, NoteData_HoldEnd data)
        {
            // �ʒu�̒���
            note.transform.position = new Vector3(
                note.transform.position.x,
                note.transform.position.y,
                optionHolder.NoteSpeed * data.Timing
                );

            // �����n�ʂ�e�o�^
            note.transform.SetParent(groundObject.transform);
        }
    }

}
