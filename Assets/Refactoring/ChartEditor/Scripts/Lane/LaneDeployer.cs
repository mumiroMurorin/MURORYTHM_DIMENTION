using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class LaneDeployer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ILaneDeployable> barLineDeplayable;
        [SerializeField] SerializeInterface<ILaneDeployable> beatLineDeployable;
        [SerializeField] SerializeInterface<ILaneDeployable> subdivisionLineDeployable;
        [SerializeField] SerializeInterface<ILaneDeployable> colliderDeployableGroup;

        void Start()
        {
            // ƒeƒXƒg
            for (int i = 0; i < 100; i += 10) 
            {
                barLineDeplayable.Value.Deploy(new Vector3(0, 0, i));

                for (float j = 0; j < 10f; j += 2.5f)
                {
                    if (j != 0) { beatLineDeployable.Value.Deploy(new Vector3(0, 0, i + j)); }

                    for (float k = 0; k < 2.5f; k += 0.625f)
                    {
                        if (k != 0) { subdivisionLineDeployable.Value.Deploy(new Vector3(0, 0, i + j + k)); }
                        colliderDeployableGroup.Value.Deploy(new Vector3(0, 0, i + j + k));
                    }
                }
            }

            colliderDeployableGroup.Value.Deploy(new Vector3(0, 0, 100));
            barLineDeplayable.Value.Deploy(new Vector3(0, 0, 100));
        }
    }

}
