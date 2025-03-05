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

        [Header("mesh��1���[�����̕�����")]
        [SerializeField] int meshHorizontalDivisionNum = 10;

        [Header("�O�p�`�̍ő卂��(����)")]
        [SerializeField] float maxTriangleLength = 0.5f;

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
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            meshFilter.mesh = mesh;

            List<int> triangles = new List<int>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            float currentStartZ = 0;
            float maxLength = optionHolder.NoteSpeed * (noteData.TimeToRanges[^1].Timing - noteData.TimeToRanges[0].Timing);
            int currentMeshIndex = 0;

            for (int i = 0; i < noteData.TimeToRanges.Count - 1; i++)
            {
                float length = optionHolder.NoteSpeed * (noteData.TimeToRanges[i + 1].Timing - noteData.TimeToRanges[i].Timing);

                //for(float )

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
                    slopeRight < float.PositiveInfinity && slopeRight > 0 ? endRight + 1 : startRight + 1, meshHorizontalDivisionNum);

                List<float> indexEnd = GetMeshPointList(slopeLeft < float.PositiveInfinity && slopeLeft > 0 ? startLeft : endLeft,
                   slopeRight < float.PositiveInfinity && slopeRight < 0 ? startRight + 1 : endRight + 1, meshHorizontalDivisionNum);

                // ���_���X�g�𐶐�
                List<Vector3> verticesStart = GenerateVertices(indexStart, startLeft, startRight + 1, slopeLeft, slopeRight, currentStartZ);
                List<Vector3> verticesEnd = GenerateVertices(indexEnd, endLeft, endRight + 1, slopeLeft, slopeRight, currentStartZ + length);

                // ���_���X�g�̑��
                vertices.AddRange(verticesStart);
                vertices.AddRange(verticesEnd);

                // UV���W�̐���,���
                List<Vector2> uvListStart = GetUVPositionList(verticesStart, currentStartZ, maxLength);
                List<Vector2> uvListEnd = GetUVPositionList(verticesEnd, currentStartZ + length, maxLength);
                uvs.AddRange(uvListStart);
                uvs.AddRange(uvListEnd);

                // �g���C�A���O���C���f�b�N�X�𐶐��A���
                triangles.AddRange(GenerateTriangles(currentMeshIndex, verticesStart.Count, verticesEnd.Count));

                currentStartZ += length;
                currentMeshIndex += verticesStart.Count + verticesEnd.Count;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
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
        /// �������C����UV���_���W�𐶐�
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private List<Vector2> GetUVPositionList(List<Vector3> vertices, float baseZ, float length)
        {
            List<Vector2> uvList = new List<Vector2>();

            Vector3 firstMatch = vertices.FirstOrDefault(v => Mathf.Approximately(v.z, baseZ));
            Vector3 lastMatch = vertices.LastOrDefault(v => Mathf.Approximately(v.z, baseZ));
            float minX = firstMatch.x;
            float maxX = lastMatch.x;

            foreach (Vector3 pos in vertices)
            {
                Vector2 uv = new Vector2();
                uv.x = Mathf.Clamp((pos.x - minX) / (maxX - minX), 0f, 1f);
                uv.y = pos.z / length;
                uvList.Add(uv);
            }

            return uvList;
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
}
