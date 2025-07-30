using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableGroup : MonoBehaviour, ILayerAffectable, IScaleAffectable, IAddressSettable
    {
        [Header("それぞれの高さ")]
        [SerializeField] float heightOnGroundEditMode = 17.1f;
        [SerializeField] float heightOnSpaceEditMode = 0.1f;

        [SerializeField] NoteDeployableUnit[] units;

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
            foreach (var unit in units)
            {
                unit.ChangeSize(z);
            }
        }

        /// <summary>
        /// 担当アドレスのセット
        /// </summary>
        /// <param name="address"></param>
        public void SetAddress(IReadOnlyAddressInChart address)
        {
            foreach (var unit in units)
            {
                unit.SetAddress(address.BarIndex, address.SubDivisionIndex);
            }
        }

        public Transform[] GetNoteDeployableUnitTransforms()
        {
            List<Transform> transforms = new List<Transform>();

            foreach(var unit in units)
            {
                transforms.Add(unit.transform);
            }

            return transforms.ToArray();
        }
    }
}