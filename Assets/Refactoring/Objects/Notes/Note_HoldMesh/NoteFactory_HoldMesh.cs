using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Deform;

namespace Refactoring
{
    public class NoteFactory_HoldMesh : NoteFactory<NoteData_HoldMesh>
    {
        [SerializeField] GameObject noteObjectOriginPrefab;

        [Header("mesh�̕�����")]
        [SerializeField] int meshDivisionNum = 10;
        [Header("mesh�̃}�e���A��(�����莞)")]
        [SerializeField] Material meshMaterialDefault;
        [Header("mesh�̃}�e���A��(�^�b�`��)")]
        [SerializeField] Material meshMaterialTouched;
        [Header("mesh�̃}�e���A��(��^�b�`��)")]
        [SerializeField] Material meshMaterialNoneTouched;

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

        public override NoteObject<NoteData_HoldMesh> Spawn(NoteData_HoldMesh data)
        {
            // ����
            NoteObject<NoteData_HoldMesh> note = GenerateNoteInstance(ConvertNoteData(data));

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
        private NoteData_HoldMesh ConvertNoteData(NoteData_HoldMesh data)
        {
            // �m�[�c�f�[�^�ɂ��낢��ǉ�
            data.SliderInput = this.sliderInputGetter;
            data.Timer = this.timer;

            return data;
        }

        /// <summary>
        /// �m�[�c���C���X�^���X�����ĕԂ�
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private NoteObject<NoteData_HoldMesh> GenerateNoteInstance(NoteData_HoldMesh data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // �m�[�c�I�u�W�F�N�g�𐶐�
            GameObject noteObj = GenerateMeshObject(data);

            // origin�ɂ�������
            noteObj.transform.SetParent(origin.transform);

            // �R���|�[�l���g���擾
            NoteObject<NoteData_HoldMesh> note = origin.GetComponent<NoteObject<NoteData_HoldMesh>>();

            return note;
        }

        /// <summary>
        /// �z�[���h�̃��b�V�������̐���
        /// </summary>
        private GameObject GenerateMeshObject(NoteData_HoldMesh noteData)
        {
            GameObject obj = new GameObject("Mesh");
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh;

            // Hold�̒���
            float length = optionHolder.NoteSpeed * (noteData.EndTiming - noteData.Timing);

            // �m�[�g�[�̂��ꂼ���index
            int startLeft = noteData.StartRange[0];
            int startRight = noteData.StartRange[noteData.StartRange.Length - 1];
            int endLeft = noteData.EndRange[0];
            int endRight = noteData.EndRange[noteData.EndRange.Length - 1];

            // ��2�ӂ̌X���𓱏o
            float slopeLeft = length / (endLeft - startLeft);
            float slopeRight = length / (endRight - startRight);

            // �m�[�g�[�`�[�܂łɓ��镪���C���f�b�N�X
            List<float> index_f = new List<float>();
            List<float> index_l = new List<float>();

            // ----- �����C���f�b�N�X���X�g�Ɋe���b�V�����_��ǉ� -----
            // --- StartRange�ɒǉ� ---
            float firstIndex = startLeft;
            float endIndex = startRight + 1;

            // ���ӂ̌X�������̏ꍇ�C���f�b�N�X��ύX �u�_�v
            if (slopeLeft < float.PositiveInfinity && slopeLeft < 0) 
            {
                index_f.Add(startLeft);
                index_l.Add(startLeft);
                firstIndex = endLeft;
            }

            // �E�ӂ̌X�������̏ꍇ�C���f�b�N�X��ύX �u�^�v
            if (slopeRight < float.PositiveInfinity && slopeRight > 0) 
            {
                index_f.Add(endRight + 1);
                index_l.Add(startRight + 1);
                endIndex = endRight + 1;
            }

            // ���b�V�������̊e�_�𓱏o�A���
            index_f.AddRange(GetMeshPointList(firstIndex, endIndex, meshDivisionNum));

            // --- EndRange�ɒǉ� ---
            firstIndex = endLeft;
            endIndex = endRight + 1;

            // ���ӂ̌X�������̏ꍇ�C���f�b�N�X��ύX �u�^�v
            if (slopeLeft < float.PositiveInfinity && slopeLeft > 0)
            {
                index_l.Add(endLeft);
                index_f.Add(endLeft);
                firstIndex = startLeft;
            }

            // �E�ӂ̌X�������̏ꍇ�C���f�b�N�X��ύX �u�_�v
            if (slopeRight < float.PositiveInfinity && slopeRight < 0) 
            {
                index_l.Add(endRight + 1);
                index_f.Add(endRight + 1);
                endIndex = startRight + 1;
            }

            // ���b�V�������̊e�_�𓱏o�A���
            index_l.AddRange(GetMeshPointList(firstIndex, endIndex, meshDivisionNum)); //���X�g�ɑ��

            index_f.Sort();                         //�\�[�g
            index_l.Sort();                         //�\�[�g
            index_f = index_f.Distinct().ToList();  //�d���v�f�폜
            index_l = index_l.Distinct().ToList();  //�d���v�f�폜
            if (slopeLeft < float.PositiveInfinity && slopeLeft != 0) { index_l.Remove(firstIndex); }  //�X����0�łȂ��ꍇ�A�ŏ��̒l���폜
            if (slopeRight < float.PositiveInfinity && slopeRight != 0) { index_f.Remove(endIndex); }  //�X����0�łȂ��ꍇ�A�Ō�̒l���폜

            // ----- �_�̍��W�𓱏o -----
            List<Vector3> vertices_f = new List<Vector3>();   //�O�m�[�g���_���W���X�g
            List<Vector3> vertices_l = new List<Vector3>();   //��m�[�g���_���W���X�g

            // Index������W�𓱏o(Start)
            foreach (float f in index_f)
            {
                float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
                float z = 0;

                // �΂ߍ��ӂ̏ꍇ �u�_�v
                if (f < startLeft) { z = slopeLeft * (f - endLeft) + length; }
                // �΂߉E�ӂ̏ꍇ �u�^�v
                else if (f > startRight + 1){ z = slopeRight * (f - startRight - 1); }

                vertices_f.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
            }

            // Index������W�𓱏o(End)
            foreach (float f in index_l)
            {
                float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
                float z = length;

                // �΂ߍ��ӂ̏ꍇ �u�^�v
                if (f < endLeft) { z = slopeLeft * (f - startLeft); }
                // �΂߉E�ӂ̏ꍇ �u�_�v
                else if (f > endRight + 1) { z = slopeRight * (f - endRight - 1) + length; }
                vertices_l.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
            }

            // ���b�V�������ԍ��̊���U��
            List<int> triangles = new List<int>();         // ���b�V�������ԍ�
            List<Vector3> vertices = new List<Vector3>();  // ���b�V������(�_)���W

            // ����
            vertices.AddRange(vertices_f);
            vertices.AddRange(vertices_l);

            int num = 0;
            int harfCount = vertices.Count % 2 != 0 && slopeLeft >= float.PositiveInfinity ?
                (vertices.Count - 1) / 2 - 1 : (vertices.Count - 1) / 2;

            // ���b�V��triangle���v�Z
            while (num + 1 <= harfCount)
            {
                triangles.Add(num + 1);
                triangles.Add(num);
                triangles.Add(num + 1 + harfCount);

                if (num + 2 + harfCount >= vertices.Count) { break; }
                triangles.Add(num + 1 + harfCount);
                triangles.Add(num + 2 + harfCount);
                triangles.Add(++num);
            }

            // �E�ӂ̌X���̂�0(����)�̂Ƃ��A�Ō�̃��b�V����ǉ�
            if (slopeLeft >= float.PositiveInfinity && slopeRight < float.PositiveInfinity)
            {
                triangles.Add(num + 1 + harfCount);
                triangles.Add(num + 2 + harfCount);
                triangles.Add(num);
            }

            // ���b�V���̓_�Ɩʂ�ݒ肵�čČv�Z
            mesh.vertices = vertices.ToArray(); //���
            mesh.triangles = triangles.ToArray(); //���
            mesh.RecalculateNormals();

            //Debug.Log("���_list: " + string.Join(",", vertices_f.Select(n => n.ToString())));
            //Debug.Log("���b�V��list: " + string.Join(",", triangles.Select(n => n.ToString())));

            // �}�e���A���̐ݒ�
            meshRenderer.material = meshMaterialDefault;

            // Deform�̐ݒ�
            Deformable d = obj.AddComponent<Deformable>();
            d.AddDeformer(groundDeformer);

            return obj;
        }

        /// <summary>
        /// �͈͓��̃��b�V�����_���X�g��Ԃ�(end��+1���邱�Ƃ�Y��Ȃ��悤��)
        /// </summary>
        /// <param name="first"></param>
        /// <param name="end"></param>
        /// <param name="div_num"></param>
        /// <returns></returns>
        private List<float> GetMeshPointList(float first, float end, int div_num)
        {
            if (div_num == 0)
            {
                Debug.LogError("�yNote�z���b�V����������0�ł�");
                return null;
            }

            List<float> list = new List<float>();
            float f = 0;
            while (f < end)
            {
                if (first < f) { list.Add(f); }
                f += 1f / div_num;
            }
            list.Add(first);
            list.Add(end);
            list.Sort();
            return list;
        }

        /// <summary>
        /// �ʒu�����Ȃ�
        /// </summary>
        private void SetTransform(NoteObject<NoteData_HoldMesh> note, NoteData_HoldMesh data)
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

    public static class MeshGenerator
    {
        /// <summary>
        /// ���b�V���𐶐����ĕԂ�
        /// </summary>
        /// <returns></returns>
        public static Mesh GenerateMesh(List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = new Mesh();

            mesh.vertices = vertices.ToArray(); //���
            mesh.triangles = triangles.ToArray(); //���
            mesh.RecalculateNormals();

            return mesh;
        }
    }

}
