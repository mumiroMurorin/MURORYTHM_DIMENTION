using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ChartEditor
{
    public static class DeployableNoteDataUtil
    {
        public static IEnumerable<IDeployableNoteData> OrderedByAddress(this IEnumerable<IDeployableNoteData> source)
        {
            return source
                .OrderBy(v => v.Address.BarIndex)
                .ThenBy(v => v.Address.SubDivisionIndex)
                .ThenBy(v => v.Address.Range[0]);
        }


    }

}

