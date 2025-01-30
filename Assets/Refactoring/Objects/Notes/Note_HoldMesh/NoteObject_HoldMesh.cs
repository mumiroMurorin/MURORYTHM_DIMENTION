using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// �^�b�`�m�[�c�ɃA�^�b�`�����N���X
    /// </summary>
    public class NoteObject_HoldMesh : NoteObject<NoteData_HoldMesh>
    {
        NoteData_HoldMesh noteData;
        
        bool isJudged;

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_HoldMesh data)
        {
            noteData = data;

            Bind();
        }

        private void Bind()
        {
            if(noteData == null) { return; }


        }

        /// <summary>
        /// �m�[�c���@�\��~����
        /// </summary>
        private void SetDisable()
        {
             this.gameObject.SetActive(false);
            // Destroy(this.gameObject);
        }

        private void Update()
        {

        }
    }

    /// <summary>
    /// (�������ɕK�v�ȕϐ����܂�)�z�[���h���b�V���m�[�c�̃f�[�^
    /// </summary>
    public class NoteData_HoldMesh : INoteData
    {
        public NoteType NoteType => NoteType.HoldMesh;

        public float Timing { get; set; }

        public float EndTiming { get; set; }

        public int[] StartRange { get; set; }

        public int[] EndRange { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }
    }

}
