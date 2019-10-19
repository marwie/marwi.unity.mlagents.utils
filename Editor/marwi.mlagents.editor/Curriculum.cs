using System.Collections.Generic;
using System.Dynamic;
using marwi.mlagents.editor;
using Newtonsoft.Json;
using UnityEngine;

namespace Packages.com.marwi.unity.mlagents.utils.Editor.marwi.mlagents.editor
{
    [CreateAssetMenu(menuName = Namespace.Base + nameof(Curriculum))]
    public class Curriculum : ScriptableObject
    {
        public enum MeasureMode
        {
            Progress = 0,
            Reward = 1
        }

        public MeasureMode Measure;
        public int MinLessonLength = 100;
        public bool SignalSmoothing = true;
        
        public float[] Thresholds = {0f};
        public Param[] Parameters;
        
        [System.Serializable]
        public class Param
        {
            public string Name = "";
            public float[] Values = {0};
        }



        public override string ToString()
        {
            type.measure = Measure == MeasureMode.Progress ? "progress" : "reward";
            type.thresholds = this.Thresholds;
            type.min_lesson_length = MinLessonLength;
            type.signal_smoothing = SignalSmoothing;
            type.parameters = type.parameters ?? new Dictionary<string, float[]>();
            type.parameters.Clear();
            foreach (var param in Parameters)
                type.parameters.Add(param.Name, param.Values);

            return JsonConvert.SerializeObject(type);
        }

        private CurriculumType type;

        [System.Serializable]
        private class CurriculumType
        {
            public string measure;
            public float[] thresholds;
            public int min_lesson_length;
            public bool signal_smoothing;
            public Dictionary<string, float[]> parameters;
        }
        
        
        [ContextMenu(nameof(Print))]
        private void Print()
        {
            Debug.Log(this.ToString());
        }
    }

    /*
     *{
    "measure" : "progress",
    "thresholds" : [0.1, 0.3, 0.5],
    "min_lesson_length" : 100,
    "signal_smoothing" : true,
    "parameters" :
    {
        "big_wall_min_height" : [0.0, 4.0, 6.0, 8.0],
        "big_wall_max_height" : [4.0, 7.0, 8.0, 8.0]
    }
}
     * 
     */
}