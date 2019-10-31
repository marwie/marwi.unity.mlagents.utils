using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AgentUtils.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace marwi.mlagents.editor
{
    public class MLAgentsTrainingWindow : EditorWindow
    {
        public static bool IsOpen => GetWindow<MLAgentsTrainingWindow>() != null;

        [MenuItem(Namespace.Base + "/Open Training Window")]
        public static MLAgentsTrainingWindow OpenWindow()
        {
            var window = GetWindow<MLAgentsTrainingWindow>();
            window.titleContent = new GUIContent("ML-Agents-Training", "");
            window.Show();
            return window;
        }


        private MLAgentsSettings settings;
        private Process trainingsProcess;
        private string[] configurationOptions = new string[0];

        private List<string> brainInfo = new List<string>();
        
        private void OnEnable()
        {
            settings = MLAgentsSettings.GetOrCreateSettings();
            settings.Changed += OnSettingsChanged;
            TryRegainPrevTrainingProcess();
            ProcessRecoverLoop();
            UpdateBrainSaveDates();
        }

        private async void ProcessRecoverLoop()
        {
            while (this)
            {
                await Task.Delay(3000);
                if (!ProcessIsRunning)
                {
                    TryRegainPrevTrainingProcess();
                }
            }
        }

        private void OnSettingsChanged()
        {
            configurationOptions = null;
        }

        private Vector2 scroll;

        private void OnGUI()
        {
        
//            float width = Screen.width * 30 / 160;
//            float height = Screen.width * 38 / 160 - Screen.height * 1 / 25;
//            scroll = EditorGUILayout.BeginScrollView(scroll, false, false, GUILayout.Width(width), GUILayout.Height(height));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Settings")) MLAgentsSettingsRegister.OpenSettings();
            EditorGUILayout.EndHorizontal();


            GUILayout.Space(6);
            EditorGUILayout.LabelField("Training", EditorStyles.boldLabel);

            if (configurationOptions == null || settings.Configurations.Count + 1 != configurationOptions.Length)
            {
                configurationOptions = new string[settings.Configurations.Count + 1];
                configurationOptions[0] = "None";
                for (var i = 0; i < settings.Configurations.Count; i++)
                    configurationOptions[i + 1] = settings.Configurations[i].name;
            }

            EditorGUI.BeginChangeCheck();
            var selection = EditorGUILayout.Popup(settings.ActiveConfiguration != null ? settings.Configurations.IndexOf(settings.ActiveConfiguration) + 1 : 0,
                configurationOptions);
            if (EditorGUI.EndChangeCheck())
            {
                settings.SetActiveExclusive(selection - 1);
                EditorUtility.SetDirty(settings);
                UpdateBrainSaveDates();
            }

            // ReSharper disable once PossibleNullReferenceException
            EditorGUI.BeginDisabledGroup(!settings.HasActiveConfiguration || !settings.ActiveConfiguration.CanTrain);

            if (SceneManager.sceneCount > 1)
            {
                if (GUILayout.Button("Load Play Scene")) TrainingsUtility.OpenPlayScenesAdditive();
                if (GUILayout.Button("Load Training Scene")) TrainingsUtility.OpenTrainingScenesAdditive();
                GUILayout.Space(10);
            }


            EditorGUI.BeginDisabledGroup(ProcessIsRunning);
            if (GUILayout.Button("Build and Start"))
            {
                var report = TrainingsUtility.MakeTrainingsBuild(settings);
                if (report.summary.result == BuildResult.Succeeded)
                    StartTraining();
            }

            if (GUILayout.Button("Start New")) StartTraining();
            if (GUILayout.Button("Continue")) ContinueTraining();
            EditorGUI.EndDisabledGroup();


//            EditorGUI.BeginDisabledGroup(ProcessIsRunning);
//            if (GUILayout.Button("Train in Editor")) StartTraining(true);
//            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!ProcessIsRunning);
            if (GUILayout.Button("Stop")) StopTrainingProcess();
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Copy Current Brain"))
            {
                var exitAndContinue = ProcessIsRunning;
                if (exitAndContinue)
                {
                    StopTrainingProcess();
                }

                if (settings.ActiveConfiguration != null)
                {
                    foreach (var (source, target) in settings.ActiveConfiguration.EnumerateBrainModelPaths())
                    {
                        Debug.Log("Copy Brain from \"" + source + "\" to \"" + target + "\"");
                        File.Copy(source, target, true);
                    }
                }

                if (exitAndContinue && !ProcessIsRunning)
                    ContinueTraining();

                AssetDatabase.Refresh();
            }

            foreach (var brainInfo in brainInfo)
            {
                EditorGUILayout.LabelField(brainInfo, EditorStyles.centeredGreyMiniLabel);
            }


            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();


            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            if (settings.ActiveConfiguration != null)
            {
                settings.ActiveConfiguration.trainInEditor = EditorGUILayout.ToggleLeft("Start Training in Editor", settings.ActiveConfiguration.trainInEditor);
            }


//            if (GUILayout.Button("Remove Process"))
//            {
//                this.trainingsProcess = null;
//                this.settings.lastTrainingProcessID = -1;
//            }

            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            if (this.ProcessIsRunning)
            {
                var infoStr = "Training Process ID: " + settings.lastTrainingProcessID + "\n" + settings.lastTrainingsProcessArgs;
                EditorGUILayout.HelpBox(infoStr, MessageType.Info);
            }

            if (!this.ProcessIsRunning && settings.lastTrainingsProcessArgs != null)
            {
                if (GUILayout.Button("Recover Process"))
                {
                    TryRegainPrevTrainingProcess();
                    if (!ProcessIsRunning)
                        Debug.Log("Could not recover Trainings Process \"" + settings.lastTrainingsProcessArgs +"\"");
                }
            }

            GUILayout.Space(5);


//            scroll = EditorGUILayout.BeginScrollView(scroll);
//            var style = EditorStyles.label;
//            style.wordWrap = true;
//            style.richText = true;
//            style.alignment = TextAnchor.UpperLeft;
//            EditorGUILayout.LabelField(messageBuffer.ToString(), style);
//            EditorGUILayout.EndScrollView();

            EditorGUI.EndDisabledGroup();
//            EditorGUILayout.EndScrollView();
        }

        private void StartTraining(bool inEditor = false)
        {
            StartTrainingProcess(GetTrainingArguments(inEditor));
        }

        private void ContinueTraining(bool inEditor = false)
        {
            StartTrainingProcess(GetTrainingArguments(inEditor) + " --load");
        }

        public const string ML_AGENTS_ARGS_BASE = "mlagents-learn";

        private string GetTrainingArguments(bool inEditor = false)
        {
            var args = $@"{ML_AGENTS_ARGS_BASE} {settings.ActiveConfiguration.ConfigParam} --train";
            // prepend anaconda
            if (!string.IsNullOrWhiteSpace(settings.ActiveConfiguration.anacondaEnvironmentName))
                args = $"activate {settings.ActiveConfiguration.anacondaEnvironmentName} && {args}";

            if (!inEditor && !settings.ActiveConfiguration.trainInEditor && settings.ActiveConfiguration.ExecuteableExists)
                args = $"{args} --env={settings.ActiveConfiguration.ExecuteableParam}";

            if (!string.IsNullOrWhiteSpace(settings.ActiveConfiguration.runID))
                args += $" --run-id={settings.ActiveConfiguration.runID}";
            if (settings.ActiveConfiguration.CurriculumExists)
            {
                if (settings.ActiveConfiguration.TryCreateCurriculumFileAndGetPathParam(out var curriculumParam))
                    args += $" --curriculum={curriculumParam}";
                else Debug.LogWarning("Could not create curriculum file");
            }

            return args;
        }


        public bool ProcessIsRunning => trainingsProcess != null && !trainingsProcess.HasExited;

        private void TryRegainPrevTrainingProcess()
        {
            if (this.ProcessIsRunning) return;

            if (this.settings.lastTrainingProcessID > 0)
            {
                try
                {
                    if (this.settings.lastTrainingProcessID != -1)
                    {
                        this.trainingsProcess = Process.GetProcessById(this.settings.lastTrainingProcessID);
                        if (!this.trainingsProcess.HasExited)
                        {
                            RegisterTrainingProcessOutput();
                            Debug.Log("<b>Recovered Process: " + this.trainingsProcess.ProcessName + "</b> from ID " + this.trainingsProcess.Id);
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                    this.trainingsProcess = null;
                    this.settings.lastTrainingProcessID = -1;
                }
            }


            var windows = WinHandleUtility.FindWindowsWithText(ML_AGENTS_ARGS_BASE);
            try
            {
                foreach (var w in windows)
                {
                    var windowTitle = WinHandleUtility.GetWindowText(w);
                    // for some reason the console app has to spaces after mlagents-learn
                    windowTitle = windowTitle.Replace("  ", " ");
                    if (!windowTitle.Contains(settings.lastTrainingsProcessArgs))
                        continue;
                    var process = WinHandleUtility.GetWindowHandleProcess(w);
                    this.settings.lastTrainingProcessID = process.Id;
                    this.trainingsProcess = process;
                    Debug.Log("<b>Recovered Process: " + windowTitle + "</b>; " + this.trainingsProcess.Id);
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            // ReSharper disable once PossibleMultipleEnumeration
            var windowsFound = windows.ToArray();
            if (windowsFound.Length > 0)
            {
                Debug.Log($"Failed to recover training, found {windowsFound} matching windows to {settings.lastTrainingsProcessArgs}");
                foreach (var window in windowsFound)
                {
                    Debug.Log(WinHandleUtility.GetWindowText(window));
                }
            }
        }

        private void StartTrainingProcess(string mlargs)
        {
            StopTrainingProcess();
            var process = new Process();
            var info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.WorkingDirectory = settings.ActiveConfiguration.AbsolutePathToMlAgentsDir;
            info.Arguments = $"/K {mlargs}";
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
            this.trainingsProcess = process;

            settings.lastTrainingProcessID = process.Id;

            const string argsSeparator = " && ";
            var argsToSave = mlargs;
            argsToSave = argsToSave.Replace("--load", "");
            argsToSave = argsToSave.Replace("  ", " ");
            argsToSave = argsToSave.Trim();
            if (argsToSave.Contains(argsSeparator))
                settings.lastTrainingsProcessArgs =
                    argsToSave.Substring(argsToSave.LastIndexOf(argsSeparator, StringComparison.Ordinal) + argsSeparator.Length);
            else
                settings.lastTrainingsProcessArgs = argsToSave;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            RegisterTrainingProcessOutput();

            Debug.Log($"<b>Started Training</b> id= {process.Id} args= {info.Arguments}");
        }


        private void RegisterTrainingProcessOutput()
        {
            if (this.trainingsProcess == null) return;
            this.trainingsProcess.OutputDataReceived -= OnOutput;
            this.trainingsProcess.ErrorDataReceived -= OnError;

            if (this.trainingsProcess.StartInfo.RedirectStandardOutput)
            {
                Debug.Log("Begin Output Read");
                this.trainingsProcess.BeginOutputReadLine();
                this.trainingsProcess.OutputDataReceived += OnOutput;
            }

            if (this.trainingsProcess.StartInfo.RedirectStandardError)
            {
                Debug.Log("Begin Error Read");
                this.trainingsProcess.BeginErrorReadLine();
                this.trainingsProcess.ErrorDataReceived += OnError;
            }

//            this.Log(this.trainingsProcess.StandardOutput.ReadToEnd());
        }

        private void StopTrainingProcess()
        {
            if (!ProcessIsRunning) return;
//            trainingsProcess.CloseMainWindow();
            StopProgramByAttachingToItsConsoleAndIssuingCtrlCEvent(trainingsProcess, 1300);
            if (!ProcessIsRunning)
                Debug.Log("Stopped Training");
            UpdateBrainSaveDates();
        }

        private void UpdateBrainSaveDates()
        {
            brainInfo.Clear();
            if (settings.ActiveConfiguration == null) return;
            foreach (var (modelPathAbsolute, assetPathAbsolute) in settings.ActiveConfiguration.EnumerateBrainModelPaths())
            {
                var info = new FileInfo(modelPathAbsolute);
                var time = info.LastWriteTime.ToString("G");
                brainInfo.Add(info.Name + ": " + time);
            }
        }

        private void OnOutput(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Debug.Log(e.Data);
        }

        private void OnError(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                var msg = e.Data;
                if (msg.StartsWith("INFO")) msg = "<b>" + msg + "</b>";
                else msg = "<color=#777>" + msg + "</color>";
                Debug.Log(msg);
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