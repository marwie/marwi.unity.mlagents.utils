using System.Collections.Generic;
using System.Globalization;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

namespace marwi.mlagents.Visualizer
{
    public class ValueDisplay : MonoBehaviour, IDisplayInstance
    {
        [SerializeField]
        private Text nameField;
        [SerializeField]
        private Text valueField;

        
        public string decimalString { get; set; }
        
        public void OnEnable()
        {
            
        }

        public void OnDestroy()
        {
            
        }

        public void OnDisplay(string key, List<float> values)
        {
            nameField.text = key;
            valueField.text = "";
            for (var i = 0; i < values.Count; i++)
            {
                valueField.text += values[i].ToString(decimalString, CultureInfo.InvariantCulture);
                if (i < values.Count - 1)
                    valueField.text += " ";
            }
        }

        public void OnClear()
        {
            valueField.text = "";
        }
    }
}