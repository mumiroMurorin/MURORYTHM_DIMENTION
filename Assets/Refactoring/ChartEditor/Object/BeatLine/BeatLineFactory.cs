using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class BeatLineFactory : MonoBehaviour, ILaneDeployable
    {
        [SerializeField] Transform beatLineParent;
        [SerializeField] GameObject beatLineObj;

        List<BeatLine> beatLines = new List<BeatLine>();

        void ILaneDeployable.Initialize()
        {
            foreach (BeatLine beatLine in beatLines)
            {
                Destroy(beatLine.gameObject);
            }

            beatLines = new List<BeatLine>();
        }

        GameObject ILaneDeployable.Deploy(Vector3 pos)
        {
            GameObject obj = Instantiate(beatLineObj);
            if (beatLineParent) { obj.transform.SetParent(beatLineParent); }
            obj.transform.localPosition = pos;

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out BeatLine line))
            {
                beatLines?.Add(line);
            }

            return obj;
        }

        float currentScale = 1f;
        void ILaneDeployable.Scaling(float scale)
        {
            foreach (BeatLine beatLine in beatLines)
            {
                Vector3 pos = beatLine.gameObject.transform.position;
                beatLine.gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z * (scale / currentScale));
            }

            currentScale = scale;
        }
    }
}
