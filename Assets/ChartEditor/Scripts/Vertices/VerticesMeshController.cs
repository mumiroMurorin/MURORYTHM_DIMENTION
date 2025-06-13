using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshGenerate;
using VContainer;

namespace ChartEditor
{
    public class VerticesMeshController : MonoBehaviour
    {
        [SerializeField] VerticesController verticesController;
        [SerializeField] Transform meshParent;
        [SerializeField] Material centerMeshMaterial;

        MeshFilter centerMeshFilter;

        private void Start()
        {
            GenerateCenterMeshParent();
            SetEvent();
        }

        private void SetEvent()
        {
            verticesController.OnAddVertexListner += UpdateMesh;
            verticesController.OnRemoveVertexListner += UpdateMesh;
            verticesController.OnClearVertexListner += UpdateMesh;
            verticesController.OnChangePositionListner += UpdateMesh;
        }

        /// <summary>
        /// 形が変わった時などメッシュを更新する
        /// </summary>
        private void UpdateMesh()
        {
            // センターメッシュ
            List<Vector3> positions = new List<Vector3>();
            foreach (var pair in verticesController.DataToObj)
            {
                Vector3 vertexPos = pair.Value.gameObject.transform.localPosition;
                positions.Add(new Vector3(vertexPos.x, vertexPos.y, vertexPos.z));
            }

            if (positions.Count < 3) { return; }

            Mesh centerMesh = MeshGenerator.GenerateMesh(positions);
            centerMeshFilter.mesh = centerMesh;
        }

        /// <summary>
        /// センターメッシュの生成
        /// </summary>
        private void GenerateCenterMeshParent()
        {
            GameObject obj = new GameObject("CenterMesh");
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            centerMeshFilter = obj.AddComponent<MeshFilter>();

            meshRenderer.material = centerMeshMaterial;

            obj.transform.SetParent(meshParent);
            obj.transform.localPosition = Vector3.zero;
        }
    }

}