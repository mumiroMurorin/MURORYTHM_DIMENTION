using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// �^�b�`�m�[�c�ɃA�^�b�`�����N���X
    /// </summary>
    public class NoteObject_HoldMeshSuper : NoteObject<NoteData_HoldMeshSuper>
    {
        [Header("mesh�̃}�e���A��(�����莞)")]
        [SerializeField] Material meshMaterialDefault;
        [Header("mesh�̃}�e���A��(�^�b�`��)")]
        [SerializeField] Material meshMaterialTouching;
        [Header("mesh�̃}�e���A��(��^�b�`��)")]
        [SerializeField] Material meshMaterialUntouching;

        NoteData_HoldMeshSuper noteData;
        List<MeshRenderer> meshRenderers;
        bool isJudged;

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_HoldMeshSuper data)
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

            // ���̃z�[���h�m�[�c�̃X�e�[�^�X���ύX
            noteData.noteNext?.SetTouchStatus(isTouching);
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
    public class NoteData_HoldMeshSuper : INoteData
    {
        public NoteType NoteType => NoteType.HoldMesh;

        public float Timing { get; set; }

        public List<TimeToRange> TimeToRanges { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public NoteObject_HoldMesh noteNext { get; set; }
    }

}
