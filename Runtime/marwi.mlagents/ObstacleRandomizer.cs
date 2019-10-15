using System.Collections.Generic;
using UnityEngine;

namespace marwi.mlagents
{
    public class ObstacleRandomizer : MonoBehaviour
    {
        public AgentEnvironmentBounds Bounds;

        public List<Transform> Obstacles;
        
        public void Randomize()
        {
            foreach (var obst in Obstacles)
            {
                if (!obst) continue;
                obst.position = Bounds.GetRandomPosition();
                obst.rotation = Quaternion.Euler(0, Mathf.Lerp(-180, 180, Random.value), 0);
            }
            
        }
    }
}