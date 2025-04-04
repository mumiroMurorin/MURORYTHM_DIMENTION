using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class BeatLineFactory : MonoBehaviour, ILaneDeployable<SubDivisionDataInBeat>
    {
        [SerializeField] GameObject beatLineObj;

        List<BeatLine> beatLines = new List<BeatLine>();

        void ILaneDeployable<SubDivisionDataInBeat>.Initialize()
        {
            foreach (BeatLine beatLine in beatLines)
            {
                Destroy(beatLine.gameObject);
            }

            beatLines = new List<BeatLine>();
        }

        GameObject ILaneDeployable<SubDivisionDataInBeat>.Deploy(SubDivisionDataInBeat subDivisionData, Vector3 pos, Transform parent)
        {
            GameObject obj = Instantiate(beatLineObj);
            if (parent) { obj.transform.SetParent(parent); }
            obj.transform.localPosition = pos;

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out BeatLine line))
            {
                beatLines?.Add(line);
            }

            return obj;
        }

        void ILaneDeployable<SubDivisionDataInBeat>.Scaling(float current, float previous)
        {
            foreach (BeatLine beatLine in beatLines)
            {
                Vector3 pos = beatLine.gameObject.transform.position;
                beatLine.gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z * (current / previous));
            }
        }
    }
}
