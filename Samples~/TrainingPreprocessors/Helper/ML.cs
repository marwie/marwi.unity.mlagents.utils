using System.Collections.Generic;
using MLAgents;
using UnityEngine;

namespace Helper
{
    public static class ML
    {
        private static Academy _academy;
        private static bool _noAcademyFound = false;

        public static Academy Academy()
        {
            if (_noAcademyFound) return null;
            if (_academy) return _academy;
            _academy = Object.FindObjectOfType<Academy>();
            _noAcademyFound = !_academy;
            return _academy;
        }

        public static float GetResetParam(string name)
        {
            var academy = Academy();
            if (!academy)
            {
                Debug.LogError("No Academy present");
                return 0;
            }
            try
            {
                return academy.resetParameters[name];
            }
            catch (KeyNotFoundException keyNotFound)
            {
                Debug.LogError($"ResetParameter \"{name}\" was not found\n{keyNotFound}", Academy());
                return 0;
            }
        }
    }
}
