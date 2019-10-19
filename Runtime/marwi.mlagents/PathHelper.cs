using System;
using UnityEngine;

namespace marwi.mlagents.editor
{
    public static class PathHelper
    {
        public static string MakeAbsolute(string relativePath, string @base)
        {
            if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(@base)) return null;
            var baseUri = new Uri(Uri.UnescapeDataString(@base), UriKind.Absolute);
            if (Uri.TryCreate(baseUri, new Uri(relativePath, UriKind.Relative), out Uri absolute))
            {
                var res = Uri.UnescapeDataString(absolute.ToString());
                if (res.StartsWith("file:///"))
                    res = res.Replace("file:///", "");
                return res;
            }

            return null;
        }

        public static string MakeAbsolute(string relativePath)
        {
            return MakeAbsolute(relativePath, Application.dataPath);
        }

        public static string MakeRelative(string absolutePath, string relativeTo)
        {
            if (string.IsNullOrEmpty(absolutePath) || string.IsNullOrEmpty(relativeTo)) return null;
            var uriAbsolute = new Uri(Uri.UnescapeDataString(absolutePath), UriKind.Absolute);
            var uriRelativeTo = new Uri(Uri.UnescapeDataString(relativeTo), UriKind.Absolute);
            var result = Uri.UnescapeDataString(uriRelativeTo.MakeRelativeUri(uriAbsolute).ToString());
            return result;
        }

        public static string MakeRelative(string absolutePath)
        {
            return MakeRelative(absolutePath, Application.dataPath);
        }
    }
}