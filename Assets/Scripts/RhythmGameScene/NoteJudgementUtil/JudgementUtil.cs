using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace JudgementUtil
{
    static public class GroundJudgement
    {
        public static bool IsTouchingSlider(ISliderInputGetter sliderInput, int[] range)
        {
            if (sliderInput == null) { return false; }

            foreach (int index in range)
            {
                if (sliderInput.GetSliderInputReactiveProperty(index).Value) { return true; }
            }

            return false;
        }
    }


}