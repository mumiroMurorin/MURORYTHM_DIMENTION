using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SpaceDeployableUnit : MonoBehaviour, ILayerAffectable, IScaleAffectable, IAddressSettable
    {
        [Header("それぞれの高さ")]
        [SerializeField] float heightOnGroundEditMode = 0.1f;
        [SerializeField] float heightOnSpaceEditMode = 17.1f;

        [SerializeField] float maxHeight = 0.075f;
        [SerializeField] float minHeight = 0.05f;
        [SerializeField] float minZ = 0.1f;
        [SerializeField] float maxZ = 50f;

        [SerializeField] BoxCollider boxCollider;

        const int SPACE_INDEX = 100;

        public AddressInChart Address { get; private set; }

        /// <summary>
        /// レイヤーチェンジ → 地と宙の位置反転
        /// </summary>
        /// <param name="editNoteType"></param>
        public void OnChangeLayer(EditNoteType editNoteType)
        {
            Vector3 pos = this.gameObject.transform.position;
            switch (editNoteType)
            {
                case EditNoteType.Ground:
                    this.gameObject.transform.position = new Vector3(pos.x, heightOnGroundEditMode, pos.z);
                    break;
                case EditNoteType.Space:
                    this.gameObject.transform.position = new Vector3(pos.x, heightOnSpaceEditMode, pos.z);
                    break;
            }
        }

        /// <summary>
        /// 配置サイズ(奥行)の変更
        /// </summary>
        /// <param name="z"></param>
        public void OnChangeSize(float z)
        {
            float height = Mathf.Lerp(minHeight, maxHeight, 1f - (Mathf.Clamp(z, minZ, maxZ) - minZ) / (maxZ - minZ));
            boxCollider.size = new Vector3(boxCollider.size.x, height, z);
        }

        public void SetAddress(IReadOnlyAddressInChart address)
        {
            Address = new AddressInChart(address.BarIndex, address.SubDivisionIndex, SPACE_INDEX);
        }

        public Transform GetNoteDeployableUnitTransform()
        {
            return this.transform;
        }
    }

}