using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace Refactoring
{
    /// <summary>
    /// �^�b�`�m�[�c�ɃA�^�b�`�����N���X
    /// </summary>
    public class NoteObject_HoldMesh : NoteObject<NoteData_HoldMesh>
    {
        [Header("mesh�̃}�e���A��(�����莞)")]
        [SerializeField] Material meshMaterialDefault;
        [Header("mesh�̃}�e���A��(�^�b�`��)")]
        [SerializeField] Material meshMaterialTouching;
        [Header("mesh�̃}�e���A��(��^�b�`��)")]
        [SerializeField] Material meshMaterialUntouching;

        NoteData_HoldMesh noteData;
        List<MeshRenderer> meshRenderers;

        List<int> judgeRange = new List<int>();
        bool isJudged;

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_HoldMesh data)
        {
            noteData = data;

            // �}�e���A���̐ݒ�
            meshRenderers = new List<MeshRenderer>();
            foreach(Transform child in this.gameObject.transform)
            {
                if (child.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    meshRenderers.Add(meshRenderer);
                    meshRenderer.material = meshMaterialDefault;
                }
            }

            Bind();
        }

        private void Bind()
        {
            if(noteData == null) { return; }


        }

        private void Update()
        {
            if (noteData == null) { return; }
            if (noteData.Timer.Time < noteData.Timing) { return; }

            UpdateJudgeRange();
            UpdateTouchStatus();
        }

        /// <summary>
        /// �m�[�c�̔���͈͂��X�V����
        /// </summary>
        private void UpdateJudgeRange()
        {
            if (noteData.Timer == null) { return; }

            // ���ԊO����
            if (noteData.TimeToRanges[0].Timing > noteData.Timer.Time) { return; }
            if (noteData.TimeToRanges[^1].Timing < noteData.Timer.Time) { return; }

            // ���z�[���h�m�[�c�̂ǂ̎��Ԃ𔻒肵�Ă���̂����ׂ�
            TimeToRange former = new TimeToRange();
            TimeToRange latter = new TimeToRange();
            for(int i = 0; i < noteData.TimeToRanges.Count; i++)
            {
                if (noteData.TimeToRanges[i].Timing > noteData.Timer.Time) { continue; }
                if (noteData.TimeToRanges[i + 1].Timing < noteData.Timer.Time) { continue; }

                former = noteData.TimeToRanges[i];
                latter = noteData.TimeToRanges[i + 1];
            }

            // ����͈͂̌v�Z
            float t0 = former.Timing;
            float t1 = latter.Timing;
            float x0 = former.Range[0];
            float x1 = latter.Range[0];
            float t = noteData.Timer.Time;

            float startRange = x1 - x0 != 0 ?
                (t - t1) * (x1 - x0) / (t1 - t0) + x1 :
                former.Range[0];

            x0 = former.Range[^1];
            x1 = latter.Range[^1];

            float endRange = x1 - x0 != 0 ?
                (t - t1) * (x1 - x0) / (t1 - t0) + x1 :
                former.Range[^1];

            judgeRange = Enumerable.Range((int)startRange, (int)Mathf.Ceil(endRange) - (int)startRange + 1).ToList();

            //Debug.Log($"Range: {startRange} , {endRange}");
            //Debug.Log("judgeRange: " + string.Join(",", judgeRange.Select(n => n.ToString())));
        }

        /// <summary>
        /// �^�b�`������X�V����
        /// </summary>
        private void UpdateTouchStatus()
        {
            if (noteData.Timer == null) { return; }

            // ����͈͓��̃X���C�_�[���͂𒲂ׂ�
            foreach(int index in judgeRange)
            {
                if (!noteData.SliderInput.GetSliderInputReactiveProperty(index).Value) { continue; }

                // �����ǂ�����������Ă����琬��
                SetTouchStatus(true);
                return;
            }

            // �ǂ���������Ă��Ȃ������玸�s
            SetTouchStatus(false);

            return;
        }

        /// <summary>
        /// �^�b�`����Ă��邩�ǂ����Ń}�e���A����ύX����
        /// </summary>
        /// <param name="isTouching"></param>
        public void SetTouchStatus(bool isTouching)
        {
            foreach(MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.material = isTouching ? meshMaterialTouching : meshMaterialUntouching;
            }
        }

        /// <summary>
        /// �m�[�c���@�\��~����
        /// </summary>
        private void SetDisable()
        {
             this.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// (�������ɕK�v�ȕϐ����܂�)�z�[���h���b�V���m�[�c�̃f�[�^
    /// </summary>
    public class NoteData_HoldMesh : INoteData
    {
        public NoteType NoteType => NoteType.HoldMesh;

        public float Timing { get; set; }

        public List<TimeToRange> TimeToRanges { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }
    }

}
