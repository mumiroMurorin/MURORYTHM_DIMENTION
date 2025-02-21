using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Deform;

namespace Refactoring
{
    public class NoteFactory_HoldMeshSuper : NoteFactory<NoteData_HoldMeshSuper>
    {
        [SerializeField] GameObject noteObjectOriginPrefab;

        [Header("mesh�̕�����")]
        [SerializeField] int meshDivisionNum = 10;

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

        public override NoteObject<NoteData_HoldMeshSuper> Spawn(NoteData_HoldMeshSuper data)
        {
            // ����
            NoteObject<NoteData_HoldMeshSuper> note = GenerateNoteInstance(ConvertNoteData(data));

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
        private NoteData_HoldMeshSuper ConvertNoteData(NoteData_HoldMeshSuper data)
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
        private NoteObject<NoteData_HoldMeshSuper> GenerateNoteInstance(NoteData_HoldMeshSuper data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // �m�[�c�I�u�W�F�N�g�𐶐�
            GameObject noteObj = GenerateMeshObject(data);

            // origin�ɂ�������
            noteObj.transform.SetParent(origin.transform);

            // �R���|�[�l���g���擾
            NoteObject<NoteData_HoldMeshSuper> note = origin.GetComponent<NoteObject<NoteData_HoldMeshSuper>>();

            return note;
        }

        /// <summary>
        /// �z�[���h�̃��b�V�������̐���
        /// </summary>
        private GameObject GenerateMeshObject(NoteData_HoldMeshSuper noteData)
        {
            GameObject obj = new GameObject("Mesh");
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            meshFilter.mesh = mesh;

            List<int> triangles = new List<int>();
            List<Vector3> vertices = new List<Vector3>();
            float currentStartZ = 0;
            int currentMeshIndex = 0;

            for (int i = 0; i < noteData.TimeToRanges.Count - 1; i++)
            {
                float length = optionHolder.NoteSpeed * (noteData.TimeToRanges[i + 1].Timing - noteData.TimeToRanges[i].Timing);

                // ���ꂼ��̒[�̃C���f�b�N�X����
                float startLeft = noteData.TimeToRanges[i].Range[0];
                float startRight = noteData.TimeToRanges[i].Range[^1];
                float endLeft = noteData.TimeToRanges[i + 1].Range[0];
                float endRight = noteData.TimeToRanges[i + 1].Range[^1];

                // �X�����v�Z
                float slopeLeft = (endLeft - startLeft) == 0 ? float.PositiveInfinity : length / (endLeft - startLeft);
                float slopeRight = (endRight - startRight) == 0 ? float.PositiveInfinity : length / (endRight - startRight);

                // ���_�C���f�b�N�X���X�g���쐬
                List<float> indexStart = GetMeshPointList(slopeLeft < float.PositiveInfinity && slopeLeft < 0 ? endLeft : startLeft,
                    slopeRight < float.PositiveInfinity && slopeRight > 0 ? endRight + 1 : startRight + 1, meshDivisionNum);

                List<float> indexEnd = GetMeshPointList(slopeLeft < float.PositiveInfinity && slopeLeft > 0 ? startLeft : endLeft,
                   slopeRight < float.PositiveInfinity && slopeRight < 0 ? startRight + 1 : endRight + 1, meshDivisionNum);

                //Debug.Log("indexStart: " + string.Join(",", indexStart.Select(n => n.ToString())));
                //Debug.Log("indexEnd: " + string.Join(",", indexEnd.Select(n => n.ToString())));

                // ���_���X�g�𐶐�
                List<Vector3> verticesStart = GenerateVertices(indexStart, startLeft, startRight + 1, slopeLeft, slopeRight, currentStartZ);
                List<Vector3> verticesEnd = GenerateVertices(indexEnd, endLeft, endRight + 1, slopeLeft, slopeRight, currentStartZ + length);

                //Debug.Log("verticesStart: " + string.Join(",", verticesStart.Select(n => n.ToString())));
                //Debug.Log("verticesEnd: " + string.Join(",", verticesEnd.Select(n => n.ToString())));

                // ���_���X�g�̑��
                vertices.AddRange(verticesStart);
                vertices.AddRange(verticesEnd);

                // �g���C�A���O���C���f�b�N�X�𐶐��A���
                triangles.AddRange(GenerateTriangles(currentMeshIndex, verticesStart.Count, verticesEnd.Count));

                //Debug.Log("triangles: " + string.Join(",", triangles.Select(n => n.ToString())));

                currentStartZ += length;
                currentMeshIndex += verticesStart.Count + verticesEnd.Count;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            obj.AddComponent<Deformable>().AddDeformer(groundDeformer);
            return obj;
        }

        /// <summary>
        /// �͈͓��̃��b�V�����_���X�g��Ԃ�
        /// </summary>
        private List<float> GetMeshPointList(float first, float end, int divNum)
        {
            if (divNum <= 0)
            {
                Debug.LogError("�yNote�z���b�V����������0�ȉ��ł�");
                return new List<float>();
            }

            List<float> list = new List<float>();
            for (float f = first; f <= end; f += 1f / divNum)
            {
                list.Add(f);
            }
            list.Add(end);
            return list.Distinct().OrderBy(x => x).ToList();
        }

        /// <summary>
        /// �w�肵���C���f�b�N�X���X�g���烁�b�V���̒��_���W���v�Z����
        /// </summary>
        private List<Vector3> GenerateVertices(List<float> indices, float left, float right, float slopeLeft, float slopeRight, float baseZ)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (float f in indices)
            {
                float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
                float z = baseZ;

                if (f < left) { z += slopeLeft * (f - left); }
                else if (f > right) { z += slopeRight * (f - right); }

                vertices.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
                //vertices.Add(new Vector3(f, -10, z));  // �f�o�b�O�p
            }
            return vertices;
        }

        /// <summary>
        /// ���b�V���̃g���C�A���O���C���f�b�N�X�𐶐�
        /// </summary>
        private List<int> GenerateTriangles(int startIndex, int countStart, int countEnd)
        {
            List<int> triangles = new List<int>();
            int halfCount = Mathf.Min(countStart, countEnd) - 1;

            for (int i = 0; i < halfCount; i++)
            {
                triangles.Add(startIndex + i + 1);
                triangles.Add(startIndex + i);
                triangles.Add(startIndex + i + countStart);

                triangles.Add(startIndex + i + 1);
                triangles.Add(startIndex + i + countStart);
                triangles.Add(startIndex + i + countStart + 1);
            }
            return triangles;
        }


        /// <summary>
        /// �ʒu�����Ȃ�
        /// </summary>
        private void SetTransform(NoteObject<NoteData_HoldMeshSuper> note, NoteData_HoldMeshSuper data)
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
