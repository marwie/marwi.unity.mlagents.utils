using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AgentUtils.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace marwi.mlagents.editor
{
    public class MLAgentsTrainingWindow : EditorWindow
    {
        private MLAgentsSettings settings;

        private Process trainingsProcess;

        [MenuItem("marwi/ML-Agents-Training")]
        public static MLAgentsTrainingWindow OpenWindow()
        {
            var window = GetWindow<MLAgentsTrainingWindow>();
            window.titleContent = new GUIContent("ML-Agents-Training");
            window.Show();
            return window;
        }

        public static bool IsOpen => GetWindow<MLAgentsTrainingWindow>() != null;

        private void OnEnable()
        {
            settings = MLAgentsSettings.GetOrCreateSettings();
            TryRegainPrevTrainingProcess();
        }

        private StringBuilder messageBuffer = new StringBuilder();
        private Vector2 scroll;

        private void Log(string msg)
        {
            messageBuffer.Insert(0, msg + "\n");
        }


        private void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(!settings.HasActiveConfiguration || !settings.ActiveConfiguration.CanTrain);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Settings")) MLAgentsSettingsRegister.OpenSettings();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(settings.ActiveConfiguration != null ? settings.ActiveConfiguration?.name : "None", new GUIStyle(EditorStyles.boldLabel){fontSize = 12});
            GUILayout.Space(6);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(ProcessIsRunning);
            if (GUILayout.Button("Start")) StartTraining();
            if (GUILayout.Button("Continue")) ContinueTraining();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!ProcessIsRunning);
            if (GUILayout.Button("Stop")) StopTrainingProcess();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Copy Current Brain"))
            {
                var exitAndContinue = ProcessIsRunning;
                if (exitAndContinue)
                {
                    StopTrainingProcess();
                }

                foreach (var brains in settings.ActiveConfiguration.EachBrainPaths())
                {
                    var source = brains.modelPathAbsolute;
                    var target = brains.assetPathAbsolute;
                    Log("Copy Brain from \"" + source + "\" to \"" + target + "\"");
                    File.Copy(source, target, true);
                }
                AssetDatabase.Refresh();

                if (exitAndContinue && !ProcessIsRunning)
                    ContinueTraining();
            }

            EditorGUI.EndDisabledGroup();

//

            if (GUILayout.Button("Clear Log"))
            {
                messageBuffer.Clear();
            }

            EditorGUILayout.Space();
            if (settings.lastTrainingProcessID != -1)
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Current Training Process ID: " + settings.lastTrainingProcessID, MessageType.Info);
                GUILayout.Space(10);
            }
            EditorGUILayout.Space();

            
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var style = EditorStyles.label;
            style.wordWrap = true;
            style.richText = true;
            style.alignment = TextAnchor.UpperLeft;
            EditorGUILayout.LabelField(messageBuffer.ToString(), style);
            EditorGUILayout.EndScrollView();

            EditorGUI.EndDisabledGroup();
        }

        private void StartTraining()
        {
            StartTrainingProcess(GetTrainingArguments());
        }
        
        private void ContinueTraining()
        {
            StartTrainingProcess(GetTrainingArguments() + " --load");
        }

        private string GetTrainingArguments()
        {
            var args = $@"mlagents-learn {settings.ActiveConfiguration.ConfigParam} --env={settings.ActiveConfiguration.ExecuteableParam} --train";
            // prepend anaconda
            if (!string.IsNullOrWhiteSpace(settings.ActiveConfiguration.anacondaEnvironmentName))
                args = $"activate {settings.ActiveConfiguration.anacondaEnvironmentName} && " + args;
            if (!string.IsNullOrWhiteSpace(settings.ActiveConfiguration.runID))
                args += $" --run-id={settings.ActiveConfiguration.runID}";
            if (settings.ActiveConfiguration.CurriculumExists)
                args += $" --curriculum={settings.ActiveConfiguration.CurriculumParam}";
            return args;
        }



        private bool ProcessIsRunning => trainingsProcess != null && !trainingsProcess.HasExited;

        private void TryRegainPrevTrainingProcess()
        {
            if (this.ProcessIsRunning) return;
            try
            {
                if (this.settings.lastTrainingProcessID != -1)
                {
                    this.trainingsProcess = Process.GetProcessById(settings.lastTrainingProcessID);
                    RegisterTrainingProcessOutput();
                    Log("Resolved Process: " + this.trainingsProcess.ProcessName + ", " + this.trainingsProcess.Id);
                }
            }
            catch (Exception)
            {
//                Debug.LogError(e);
//                settings.lastTrainingProcessID = -1;
                this.trainingsProcess = null;
                // ignored
            }
        }

        private void StartTrainingProcess(string args)
        {
            StopTrainingProcess();
            var process = new Process();
            var info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.WorkingDirectory = settings.ActiveConfiguration.AbsolutePathToMlAgentsDir;
            info.Arguments = $"/K {args}";
            info.UseShellExecute = true;
            info.RedirectStandardInput = !info.UseShellExecute;
            info.RedirectStandardOutput = !info.UseShellExecute;
            info.RedirectStandardError = !info.UseShellExecute;
            info.ErrorDialog = true;
            if (!info.UseShellExecute)
            {
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
            }

            process.StartInfo = info;
            process.Start();
            settings.lastTrainingProcessID = process.Id;
            this.trainingsProcess = process;
            RegisterTrainingProcessOutput();
            Log("-----------------------");
            Log("Started Training \n" + info.WorkingDirectory + "\n" + info.Arguments);
        }

        private void RegisterTrainingProcessOutput()
        {
            if (this.trainingsProcess == null) return;
            this.trainingsProcess.OutputDataReceived -= OnOutput;
            this.trainingsProcess.ErrorDataReceived -= OnError;

            if (this.trainingsProcess.StartInfo.RedirectStandardOutput)
            {
                Log("Begin Output Read");
                this.trainingsProcess.BeginOutputReadLine();
                this.trainingsProcess.OutputDataReceived += OnOutput;
            }

            if (this.trainingsProcess.StartInfo.RedirectStandardError)
            {
                Log("Begin Error Read");
                this.trainingsProcess.BeginErrorReadLine();
                this.trainingsProcess.ErrorDataReceived += OnError;
            }

//            this.Log(this.trainingsProcess.StandardOutput.ReadToEnd());
        }

        private void StopTrainingProcess()
        {
            if (!ProcessIsRunning) return;
//            trainingsProcess.CloseMainWindow();
            StopProgramByAttachingToItsConsoleAndIssuingCtrlCEvent(trainingsProcess);
            trainingsProcess = null;
            settings.lastTrainingProcessID = -1;
            Log("Stopped Training");
        }

        private void OnOutput(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Log(e.Data);
        }

        private void OnError(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                var msg = e.Data;
                if (msg.StartsWith("INFO")) msg = "<b>" + msg + "</b>";
                else msg = "<color=#777>" + msg + "</color>";
                Log(msg);
            }
        }


        // http://stanislavs.org/stopping-command-line-applications-programatically-with-ctrl-c-events-from-net/#comment-2880

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        // Delegate type to be used as the Handler Routine for SCCH
        delegate Boolean ConsoleCtrlDelegate(CtrlTypes CtrlType);

        // Enumerated type for the control messages sent to the handler routine
        private enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, uint dwProcessGroupId);

        private static void StopProgramByAttachingToItsConsoleAndIssuingCtrlCEvent(Process process, int waitForExitTimeout = 3000)
        {
            if (!AttachConsole((uint) process.Id))
            {
                return;
            }

            // Disable Ctrl-C handling for our program
            SetConsoleCtrlHandler(null, true);

            // Sent Ctrl-C to the attached console
            GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);

            // Wait for the graceful end of the process.
            // If the process will not exit in time specified by 'waitForExitTimeout', the process will be killed
            using (new Timer((dummy =>
            {
                if (!process.HasExited) process.Kill();
            }), null, waitForExitTimeout, Timeout.Infinite))
            {
                // Must wait here. If we don't wait and re-enable Ctrl-C handling below too fast, we might terminate ourselves.
                process.WaitForExit();
            }

            FreeConsole();

            // Re-enable Ctrl-C handling or any subsequently started programs will inherit the disabled state.
            SetConsoleCtrlHandler(null, false);
        }

//        public static void StopProgramByAttachingToItsConsoleAndIssuingCtrlCEvent(Process process)
//        {
//            //This does not require the console window to be visible.
//            if (!AttachConsole((uint) process.Id)) return;
//            
//            //Disable Ctrl-C handling for our program
//            SetConsoleCtrlHandler(null, true);
//            GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);
//
//            //Must wait here. If we don't and re-enable Ctrl-C handling below too fast, we might terminate ourselves.
//            process.WaitForExit();
//
//            FreeConsole();
//
//            //Re-enable Ctrl-C handling or any subsequently started programs will inherit the disabled state.
//            SetConsoleCtrlHandler(null, false);
//        }
    }
}