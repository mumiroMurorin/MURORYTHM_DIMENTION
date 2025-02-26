using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SubdivisionLineFactory : MonoBehaviour, ILaneDeployable
    {
        [SerializeField] Transform subdivisionLineParent;
        [SerializeField] GameObject subdivisionLineObj;

        List<SubdivisionLine> subdivisionLines = new List<SubdivisionLine>();

        void ILaneDeployable.Initialize()
        {
            foreach (SubdivisionLine subdivisionLine in subdivisionLines)
            {
                Destroy(subdivisionLine.gameObject);
            }

            subdivisionLines = new List<SubdivisionLine>();
        }

        GameObject ILaneDeployable.Deploy(Vector3 pos)
        {
            GameObject obj = Instantiate(subdivisionLineObj);
            if (subdivisionLineParent) { obj.transform.SetParent(subdivisionLineParent); }
            obj.transform.localPosition = pos;

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out SubdivisionLine line))
            {
                subdivisionLines?.Add(line);
            }

            return obj;
        }

        float currentScale = 1f;
        void ILaneDeployable.Scaling(float scale)
        {
            foreach (SubdivisionLine subdivisionLine in subdivisionLines) 
            {
                Vector3 pos = subdivisionLine.gameObject.transform.position;
                subdivisionLine.gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z * (scale / currentScale));
            }

            currentScale = scale;
        }
    }
}
