using MLAgents;
using UnityEngine;

namespace MLAgents
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


    }
}
