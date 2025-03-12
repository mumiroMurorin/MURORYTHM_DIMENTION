using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class BarLineFactory : MonoBehaviour, ILaneDeployable
    {
        [SerializeField] Transform barLineParent;
        [SerializeField] GameObject barLineObj;

        List<BarLine> barLines = new List<BarLine>();
        int barCount = 0;

        void ILaneDeployable.Initialize()
        {
            foreach(BarLine barLine in barLines)
            {
                Destroy(barLine.gameObject);
            }

            barLines = new List<BarLine>();
            barCount = 0;
        }

        GameObject ILaneDeployable.Deploy(Vector3 pos)
        {
            GameObject obj = Instantiate(barLineObj);
            if (barLineParent) { obj.transform.SetParent(barLineParent); }
            obj.transform.localPosition = pos;

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if(obj.TryGetComponent(out BarLine line))
            {
                barLines?.Add(line);
                // è¨êﬂî‘çÜÇÃê›íË
                line.SetBarNumber(++barCount);
            }

            return obj;
        }

        float currentScale = 1f;
        void ILaneDeployable.Scaling(float scale)
        {
            foreach (BarLine barLine in barLines)
            {
                Vector3 pos = barLine.gameObject.transform.position;
                barLine.gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z * (scale / currentScale));
            }

            currentScale = scale;
        }
    }
}
