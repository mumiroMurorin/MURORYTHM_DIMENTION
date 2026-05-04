using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshGenerate;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class VerticesMeshController : MonoBehaviour
    {
        [SerializeField] VertexObjectsController verticesController;
        [SerializeField] Transform meshParent;

        [SerializeField] Material spaceHoldMeshMaterial;
        [SerializeField] Material spaceBreakMeshMaterial;

        MeshFilter centerMeshFilter;
        MeshRenderer centerMeshRenderer;

        INotesDataSetter notesSetter;
        INotesDataGetter notesGetter;

        [Inject]
        public void Constructor(INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
        }

        private void Start()
        {
            GenerateCenterMeshParent();
            Bind();
        }

        private void Bind()
        {
            verticesController?.OnAddVertexListener.Subscribe(_ => UpdateMesh()).AddTo(this.gameObject);
            verticesController?.OnRemoveVertexListener.Subscribe(_ => UpdateMesh()).AddTo(this.gameObject);
            verticesController?.OnClearVertexListener.Subscribe(_ => UpdateMesh()).AddTo(this.gameObject);
            verticesController?.OnChangePositionListener.Subscribe(_ => UpdateMesh()).AddTo(this.gameObject);
            verticesController?.EditingNoteType.Subscribe(ChangeMaterial).AddTo(this.gameObject);
        }

        /// <summary>
        /// 形が変わった時などメッシュを更新する
        /// </summary>
        private void UpdateMesh()
        {
            // センターメッシュ
            List<Vector3> positions = new List<Vector3>();
            for(int i = 0; i < notesGetter.DataToVertexObject.Count; i++)
            {
                Vector3 vertexPos = notesGetter.GetVertexObject(i).gameObject.transform.localPosition;
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
            centerMeshRenderer = obj.AddComponent<MeshRenderer>();
            centerMeshFilter = obj.AddComponent<MeshFilter>();

            obj.transform.SetParent(meshParent);
            obj.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// メッシュのマテリアル変更
        /// </summary>
        /// <param name="type"></param>
        private void ChangeMaterial(DeploymentNoteType type)
        {
            if (type == DeploymentNoteType.SpaceHoldStart ||
                type == DeploymentNoteType.SpaceHoldRelay ||
                type == DeploymentNoteType.SpaceHoldMeshRelay ||
                type == DeploymentNoteType.SpaceHoldEnd)
            {
                centerMeshRenderer.material = spaceHoldMeshMaterial;
            }
            else if (type == DeploymentNoteType.SpaceBreak)
            {
                centerMeshRenderer.material = spaceBreakMeshMaterial;
            }
        }
    }

}