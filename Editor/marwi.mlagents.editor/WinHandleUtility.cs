using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Debug = UnityEngine.Debug;

namespace marwi.mlagents.editor
{
    public static class WinHandleUtility
    {
        // https://social.msdn.microsoft.com/Forums/de-DE/0e63cc54-1d6d-44eb-a02c-8cb462eef7dd/get-process-id-from-window-handle?forum=netfxcompact
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

        public static Process GetWindowHandleProcess(IntPtr hwnd)
        {
            return GetWindowThreadProcessId(hwnd, out var id) != 0 ? Process.GetProcessById((int) id) : null;
        }
        
        // https://stackoverflow.com/questions/1016823/c-sharp-how-can-i-rename-a-process-window-that-i-started
        [DllImport("user32.dll")]
        static extern int SetWindowTextW(IntPtr hWnd, string windowName);


        // https://stackoverflow.com/questions/19867402/how-can-i-use-enumwindows-to-find-windows-with-a-specific-caption-title
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        // Delegate to filter which windows to include 
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary> Get the text for the window pointed to by hWnd </summary>
        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                var title = builder.ToString();
//                Debug.Log(title);
                return title;
            }

            return String.Empty;
        }

        /// <summary> Find all windows that match the given filter </summary>
        /// <param name="filter"> A delegate that returns true for windows
        ///    that should be returned and false for windows that should
        ///    not be returned </param>
        public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            var found = IntPtr.Zero;
            var windows = new List<IntPtr>();

            EnumWindows((wnd, param) =>
            {
                if (filter(wnd, param))
                {
                    // only add the windows that pass the filter
                    windows.Add(wnd);
                }

                // but return true here so that we iterate all windows
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary> Find all windows that contain the given title text </summary>
        /// <param name="titleText"> The text that the window title must contain. </param>
        public static IEnumerable<IntPtr> FindWindowsWithText(string titleText)
        {
            return FindWindows((wnd, param) => GetWindowText(wnd).Contains(titleText));
        }
    }
}