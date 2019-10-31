using System.Collections.Generic;

namespace marwi.mlagents.Visualizer
{
    public interface IDisplayInstance
    {
        string decimalString { get; set; }
        bool enabled { get; set; }
        void OnDestroy();
        void OnDisplay(string key, List<float> values);
        void OnClear();
    }
}