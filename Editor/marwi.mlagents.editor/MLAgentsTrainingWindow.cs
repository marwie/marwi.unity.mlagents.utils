using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AgentUtils.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
            window.titleContent = new GUIContent("ML-Agents-Training");
            window.Show();
            return window;
        }

        
        private MLAgentsSettings settings;
        private Process trainingsProcess;
        private string[] configurationOptions = new string[0];

        private void OnEnable()
        {
            settings = MLAgentsSettings.GetOrCreateSettings();
            settings.Changed += OnSettingsChanged;
            TryRegainPrevTrainingProcess();
        }

        private void OnSettingsChanged()
        {
            configurationOptions = null;
        }


        private void Log(string msg)
        {
            Debug.Log(msg);
        }

        private void LogWarning(string msg)
        {
            Debug.LogWarning(msg);
        }

        private void OnGUI()
        {
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
                        Log("Copy Brain from \"" + source + "\" to \"" + target + "\"");
                        File.Copy(source, target, true);
                    }
                }

                if (exitAndContinue && !ProcessIsRunning)
                    ContinueTraining();

                AssetDatabase.Refresh();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            if (settings.ActiveConfiguration != null)
            {
                settings.ActiveConfiguration.trainInEditor = EditorGUILayout.ToggleLeft("Start Training in Editor", settings.ActiveConfiguration.trainInEditor);
            }
            
            if(GUILayout.Button("Recover Process"))
                TryRegainPrevTrainingProcess();

            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            if (settings.lastTrainingProcessID != -1)
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Training Process ID: " + settings.lastTrainingProcessID, MessageType.Info);
                GUILayout.Space(10);
            }


//            scroll = EditorGUILayout.BeginScrollView(scroll);
//            var style = EditorStyles.label;
//            style.wordWrap = true;
//            style.richText = true;
//            style.alignment = TextAnchor.UpperLeft;
//            EditorGUILayout.LabelField(messageBuffer.ToString(), style);
//            EditorGUILayout.EndScrollView();

            EditorGUI.EndDisabledGroup();
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
                else LogWarning("Could not create curriculum file");
            }

            return args;
        }


        public bool ProcessIsRunning => trainingsProcess != null && !trainingsProcess.HasExited;

        private void TryRegainPrevTrainingProcess()
        {
            if (this.ProcessIsRunning) return;
            try
            {
                Log($"Try Recover Process {this.settings.lastTrainingProcessID}");
                if (this.settings.lastTrainingProcessID != -1)
                {
                    this.trainingsProcess = Process.GetProcessById(settings.lastTrainingProcessID);
                    if (!this.trainingsProcess.ProcessName.Contains(ML_AGENTS_ARGS_BASE))
                    {
                        Debug.LogWarning($"Recovered Process is not a ml-agents training process {this.trainingsProcess.ProcessName}, {this.trainingsProcess.Id}");
                        this.trainingsProcess = null;
                        return;
                    }
                    
                    RegisterTrainingProcessOutput();
                    Log("<b>Recovered Process: " + this.trainingsProcess.ProcessName + "</b>, " + this.trainingsProcess.Id);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                this.trainingsProcess = null;
                
                // TODO: recover process by name
//                var processlist = Process.GetProcesses();
//                foreach(var proc in processlist){
//                    Debug.Log($"{proc.ProcessName}, {proc.Id}, {proc.MainWindowTitle}");
//                }
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
            this.trainingsProcess = process;
            settings.lastTrainingProcessID = process.Id;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            RegisterTrainingProcessOutput();
            Log("-----------------------");
            Log("Started Training \n" + info.WorkingDirectory + "\n" + info.Arguments);
            Log("Process ID: " + process.Id);
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
            StopProgramByAttachingToItsConsoleAndIssuingCtrlCEvent(trainingsProcess, 1300);
            if (!ProcessIsRunning)
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