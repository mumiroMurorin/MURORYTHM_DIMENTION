using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SubdivisionLineFactory : MonoBehaviour, ILaneDeployable<SubDivisionDataInBeat>
    {
        [SerializeField] GameObject subdivisionLineObj;

        List<SubdivisionLine> subdivisionLines = new List<SubdivisionLine>();

        void ILaneDeployable<SubDivisionDataInBeat>.Initialize()
        {
            foreach (SubdivisionLine subdivisionLine in subdivisionLines)
            {
                Destroy(subdivisionLine.gameObject);
            }

            subdivisionLines = new List<SubdivisionLine>();
        }

        GameObject ILaneDeployable<SubDivisionDataInBeat>.Deploy(SubDivisionDataInBeat subDivisionData, Vector3 pos,Transform parent)
        {
            GameObject obj = Instantiate(subdivisionLineObj);
            if (parent) { obj.transform.SetParent(parent); }
            obj.transform.localPosition = pos;

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out SubdivisionLine line))
            {
                subdivisionLines?.Add(line);
            }

            return obj;
        }
    }
}
