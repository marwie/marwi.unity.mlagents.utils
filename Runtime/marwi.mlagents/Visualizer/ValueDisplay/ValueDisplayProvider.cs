using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace marwi.mlagents.Visualizer
{
    public class ValueDisplayProvider : BaseValueDisplayProvider
    {
        public ValueDisplay Template;
        
        public override Visualizsation Type => Visualizsation.PlainValue;
        
        protected override IDisplayInstance GetInstance(RectTransform panel)
        {
            return !Template ? null : Instantiate(Template, panel);
        }
    }
}