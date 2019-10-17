using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using marwi.mlagents.editor;
using UnityEditor;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace AgentUtils.Editor
{
    public class MLAgentsSettings : ScriptableObject
    {
        #region Settings Creation BoilerPlate

        private static bool SettingsExist => !string.IsNullOrWhiteSpace(m_relativeSettingsPath) && File.Exists(ToAbsolutePath(m_relativeSettingsPath));
        private static string ToAbsolutePath(string relativePath) => Application.dataPath + "/../" + relativePath;
        private static string ToAbsoluteDirectory(string path) => Path.GetDirectoryName(ToAbsolutePath(path));

        private static string m_relativeSettingsPath;

        private static string settingsFullPath
        {
            get
            {
                if (!SettingsExist)
                {
                    var settingsInstances = AssetDatabase.FindAssets("t:" + nameof(MLAgentsSettings));
                    foreach (var guid in settingsInstances)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        m_relativeSettingsPath = path;
//                        Debug.Log($"Resolved ML-Agents Settings at \"{m_relativeSettingsPath}\"");
                        break;
                    }

                    if (!SettingsExist)
                    {
                        var scriptGuids = AssetDatabase.FindAssets(nameof(MLAgentsSettings));
                        foreach (var guid in scriptGuids)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            if (path.EndsWith(".cs"))
                            {
                                m_relativeSettingsPath = $"{path.Substring(0, path.LastIndexOf('/'))}/ML-Agents-Settings.asset";
                                Debug.Log($"Create ML Agents Settings at \"{m_relativeSettingsPath}\"");
                                break;
                            }
                        }
                    }
                }


                return m_relativeSettingsPath;
            }
        }

        public static MLAgentsSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MLAgentsSettings>(settingsFullPath);
            if (settings != null) return settings;


            settings = CreateInstance<MLAgentsSettings>();
            settings.OnCreate();

            // make sure full path exists:
            var directory = ToAbsoluteDirectory(m_relativeSettingsPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            AssetDatabase.CreateAsset(settings, m_relativeSettingsPath);
            AssetDatabase.SaveAssets();

            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        #endregion

//        private void OnEnable()
//        {
//            EditorApplication.wantsToQuit += OnBeforeQuit;
//        }
//
//        private void OnDisable()
//        {
//            EditorApplication.wantsToQuit -= OnBeforeQuit;
//        }
//
//        private bool OnBeforeQuit()
//        {
//            AssetDatabase.SaveAssets();
//            return true;
//        }

        private void OnCreate()
        {
            lastTrainingProcessID = -1;
        }

        [SerializeField] public int lastTrainingProcessID = -1;

        public bool HasActiveConfiguration => ActiveConfiguration != null;
        public TrainingsConfiguration ActiveConfiguration => Configurations.FirstOrDefault(c => c.isActive);

        [SerializeField] public List<TrainingsConfiguration> Configurations;


        public void AddConfiguration()
        {
            this.Configurations.Add(new TrainingsConfiguration());
        }

        public void RemoveConfiguration(TrainingsConfiguration config)
        {
            this.Configurations.Remove(config);
        }

        public void SetActiveExclusive(TrainingsConfiguration config)
        {
            foreach (var c in Configurations)
                c.isActive = c == config;
        }

        public void SetActiveExclusive(int index)
        {
            for (var i = 0; i < Configurations.Count; i++) Configurations[i].isActive = i == index;
        }
    }

    [System.Serializable]
    public class TrainingsConfiguration : ICloneable
    {
        public bool foldout = true;
        public string name = "New ML Configuration";
        public bool isActive = false;
        public string relPathToMLAgentsDir;
        public string relPathToExecutable;
        public string relPathToConfig;
        public string brainNames;

        public string runID = "default";
        public string anacondaEnvironmentName;
        public string relPathToCurriculum;

        [NonSerialized] public readonly List<string> detectedProblems = new List<string>();

        public object Clone()
        {
            return new TrainingsConfiguration()
            {
                foldout = foldout,
                name = name,
                isActive = false,
                relPathToMLAgentsDir = relPathToMLAgentsDir,
                relPathToExecutable = relPathToExecutable,
                relPathToConfig = relPathToConfig,
                runID = runID,
                anacondaEnvironmentName = anacondaEnvironmentName,
                relPathToCurriculum = relPathToCurriculum,
            };
        }

        public bool CanTrain => MLAgentsDirExists && ConfigExists && HasValidRunID && !HasInvalidBrain();

        public string ConfigParam
        {
            get
            {
                // this feels hacky but we need to remove the directory name of the agent installation folder
                if (!string.IsNullOrWhiteSpace(relPathToConfig) && relPathToConfig.Length > 1)
                    return relPathToConfig.Substring(relPathToConfig.IndexOf("/", StringComparison.Ordinal) + 1);
                return null;
            }
        }

        public string ExecuteableParam
        {
            get
            {
                // this feels hacky but we need to remove the directory name of the agent installation folder
                if (!string.IsNullOrWhiteSpace(relPathToExecutable) && relPathToExecutable.Length > 1)
                    return relPathToExecutable.Substring(relPathToExecutable.IndexOf("/", StringComparison.Ordinal) + 1);
                return null;
            }
        }

        public string CurriculumParam
        {
            get
            {
                // this feels hacky but we need to remove the directory name of the agent installation folder
                if (!string.IsNullOrWhiteSpace(relPathToCurriculum) && relPathToCurriculum.Length > 1)
                    return relPathToCurriculum.Substring(relPathToCurriculum.IndexOf("/", StringComparison.Ordinal) + 1);
                return null;
            }
        }

        public string AbsolutePathToMlAgentsDir => PathHelper.MakeAbsolute(relPathToMLAgentsDir);
        public string AbsolutePathToExecuteable => PathHelper.MakeAbsolute(relPathToExecutable, AbsolutePathToMlAgentsDir);
        public string AbsolutePathToCurriculumDirectory => PathHelper.MakeAbsolute(relPathToCurriculum, AbsolutePathToMlAgentsDir);

        public bool MLAgentsDirExists => Directory.Exists(AbsolutePathToMlAgentsDir);
        public bool ExecuteableExists => File.Exists(AbsolutePathToExecuteable) && AbsolutePathToExecuteable.EndsWith(".exe");
        public bool ConfigExists => File.Exists(PathHelper.MakeAbsolute(relPathToConfig, AbsolutePathToMlAgentsDir));
        public bool CurriculumExists => Directory.Exists(AbsolutePathToCurriculumDirectory);
        public bool HasValidRunID => !string.IsNullOrWhiteSpace(runID);

        public void AddBrain(string absolutePathToBrain)
        {
            if (string.IsNullOrEmpty(absolutePathToBrain) || !absolutePathToBrain.EndsWith(".asset")) return;
            var assetPath = PathHelper.MakeRelative(absolutePathToBrain, Application.dataPath);
            if (string.IsNullOrEmpty(brainNames) || !brainNames.Contains(assetPath))
                brainNames = string.IsNullOrWhiteSpace(brainNames) ? assetPath : brainNames + ";" + assetPath;
//            var brainName
        }

//        public void RemoveBrain(string absolutePathToBrain)
//        {
//            Debug.Log("TODO, find name from path and remove from list");
//            Debug.Log(absolutePathToBrain);
//        }



        public IEnumerable<(string modelPathAbsolute, string assetPathAbsolute)> EachBrainPaths()
        {
            if (!string.IsNullOrWhiteSpace(this.brainNames))
            {
                var brainPaths = brainNames.Split(';');
                foreach (var brain in brainPaths)
                {
                    if (string.IsNullOrWhiteSpace(brain)) continue;
                    var absoluteAssetPath = PathHelper.MakeAbsolute(brain, Application.dataPath);
                    if (!File.Exists(absoluteAssetPath))
                    {
                        Debug.LogWarning("Could not find Brain at " + absoluteAssetPath);
                        continue;
                    }

                    var brainName = brain.Substring(brain.LastIndexOf("/", StringComparison.Ordinal) + 1).Replace(".asset", "");
                    var pathToBrain = Path.Combine(AbsolutePathToMlAgentsDir, "models", runID + "-0", brainName + ".nn");
                    if (File.Exists(pathToBrain))
                        yield return (pathToBrain, absoluteAssetPath.Replace(".asset", ".nn"));
                    else Debug.LogWarning("Could not find Brain at " + pathToBrain);
                }
            }
        }

        public IEnumerable<string> InvalidBrainInfos()
        {
            if (!string.IsNullOrWhiteSpace(this.brainNames))
            {
                var brainPaths = brainNames.Split(';');
                foreach (var brain in brainPaths)
                {
                    if (string.IsNullOrWhiteSpace(brain)) continue;
                    var type = AssetDatabase.GetMainAssetTypeAtPath(brain);
                    if (type == null) yield return brain;
                    if (type != null && type.Name != "LearningBrain") yield return brain;
                }
            }
            else yield return "No Brain added";
        }

        public bool HasInvalidBrain()
        {
            return InvalidBrainInfos().Any();
        }

        public void DetectProblems()
        {
            detectedProblems.Clear();
            if (CurriculumExists)
            {
                var curriculumDirectory = AbsolutePathToCurriculumDirectory;
                var curriculumInfo = new DirectoryInfo(curriculumDirectory);
                var foundBrain = false;
                foreach (var file in curriculumInfo.EnumerateFiles())
                {
                    if (foundBrain) break;
                    if (file.Extension == ".json")
                    {
                        // get curriculum name and check if we have a brain
                        var name = Path.GetFileNameWithoutExtension(file.FullName);
                        var paths = AssetDatabase.FindAssets("").Select(AssetDatabase.GUIDToAssetPath);

                        foreach (var p in paths)
                        {
                            if (foundBrain) break;
                            var type = AssetDatabase.GetMainAssetTypeAtPath(p);
                            // because ML-Agents is not a package we can not reference it in our asmdef SO we have to check by string
                            // if the asset is a ml agents brain
                            if (type.Name == "LearningBrain")
                            {
                                var assetName = p.Substring(p.LastIndexOf("/", StringComparison.Ordinal) + 1).Replace(".asset", "");
                                if (assetName == name)
                                    foundBrain = true;
                            }
                        }
                    }
                }

                if (!foundBrain)
                    detectedProblems.Add($"Curriculum: No matching brain found for \"{curriculumInfo.Name}\"");
            }

            foreach (var brain in InvalidBrainInfos())
                detectedProblems.Add("Invalid Brain: " + brain);

            if (detectedProblems.Count <= 0)
                Debug.Log("No Problems detected for \"" + name + "\"");
        }
    }

    public static class GUIColorHelper
    {
        private static Color previousBackgroundColor;

        public static int SetBackgroundColor(Color color, bool predicate)
        {
            if (!predicate) return -1;
            previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            return 0;
        }

        public static void ResetBackgroundColor(int index)
        {
            if (index == 0)
                GUI.backgroundColor = previousBackgroundColor;
        }
    }

    // ReSharper disable once UnusedMember.Global
    internal static class MLAgentsSettingsRegister
    {
        public static string SettingsPath => "Project/Marwi.ML-Agents";
        
        public static void OpenSettings() =>  SettingsService.OpenProjectSettings(SettingsPath);
        
        [SettingsProvider]
        public static SettingsProvider CreateMLAgentsSettings()
        {
            var provider = new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "ML-Agents",
                deactivateHandler = () =>
                {
                    var obj = MLAgentsSettings.GetSerializedSettings();
                    var settings = obj?.targetObject as MLAgentsSettings;
                    if (settings != null)
                    {
                        if (EditorUtility.IsDirty(settings))
                            AssetDatabase.SaveAssets();
                    }
                },
                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] {"Number", "Some String"})
            };

            provider.guiHandler = GUIHandler;
            return provider;
        }

        private static Texture plusIcon = EditorGUIUtility.FindTexture("Toolbar Plus");

        private static readonly List<TrainingsConfiguration> removeBuffer = new List<TrainingsConfiguration>();

        private static void GUIHandler(string searchContext)
        {
            var obj = MLAgentsSettings.GetSerializedSettings();
            var settings = obj?.targetObject as MLAgentsSettings;

            if (settings != null)
            {
                Undo.RecordObject(settings, "ML-Agents Training Settings");

                EditorGUI.BeginChangeCheck();

                for (var index = 0; index < settings.Configurations.Count; index++)
                {
                    var config = settings.Configurations[index];
                    EditorGUILayout.BeginHorizontal();
                    config.foldout = EditorGUILayout.Foldout(config.foldout, config.isActive ? config.name + " (Active)" : config.name,
                        config.isActive ? new GUIStyle(EditorStyles.foldout) {fontStyle = FontStyle.Bold} : EditorStyles.foldout);

                    EditorGUI.BeginDisabledGroup(config.isActive);
                    if (GUILayout.Button("Set Active", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
                        settings.SetActiveExclusive(config);
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button("Clone", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
                        settings.Configurations.Add(config.Clone() as TrainingsConfiguration);
                    if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
                    {
                        var wantDelete = EditorUtility.DisplayDialog("Delete Configuration " + config.name, "Do you really want to delete " + config.name,
                            "Yes Delete",
                            "Wait what");
                        if (wantDelete)
                            removeBuffer.Add(config);
                    }

                    EditorGUILayout.EndHorizontal();
                    if (config.foldout)
                    {
                        ++EditorGUI.indentLevel;

                        EditorGUILayout.BeginHorizontal();
//                        GUILayout.FlexibleSpace();


                        EditorGUILayout.EndHorizontal();

                        var wasActive = config.isActive;
                        config.isActive = EditorGUILayout.Toggle("Active", config.isActive);
                        if (!wasActive && config.isActive)
                            settings.SetActiveExclusive(config);
                        config.name = EditorGUILayout.TextField("Name", config.name);

                        var bgIdent = 0;

                        using (var change = new EditorGUI.ChangeCheckScope())
                        {
                            bgIdent = GUIColorHelper.SetBackgroundColor(new Color(1, .8f, .8f), !config.HasValidRunID);
                            config.runID = EditorGUILayout.TextField("Run ID", config.runID);
                            GUIColorHelper.ResetBackgroundColor(bgIdent);
                            if (change.changed)
                                config.runID = config.runID.Replace(" ", "-");
                        }

                        EditorGUILayout.BeginHorizontal();
                        bgIdent = GUIColorHelper.SetBackgroundColor(new Color(1, .8f, .8f), !config.MLAgentsDirExists);
                        config.relPathToMLAgentsDir = EditorGUILayout.TextField("ML Agents Dir Rel.", config.relPathToMLAgentsDir);
                        GUIColorHelper.ResetBackgroundColor(bgIdent);

//                        var folder = EditorGUIUtility.FindTexture("Folder Icon");
                        if (GUILayout.Button("Open", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))//, GUILayout.Height(20), GUILayout.Width(24)))
                        {
                            Process.Start(config.AbsolutePathToMlAgentsDir);
                        }
                        
                        if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                        {
                            var path = PathHelper.MakeAbsolute(config.relPathToMLAgentsDir);
                            var selectedPath = EditorUtility.OpenFolderPanel("Select ML-Agents Root Directory",
                                Directory.Exists(path) ? path : Application.dataPath,
                                "");
                            if (Directory.Exists(selectedPath))
                                config.relPathToMLAgentsDir = PathHelper.MakeRelative(selectedPath);
                        }


                        EditorGUILayout.EndHorizontal();

                      

                        EditorGUILayout.BeginHorizontal();
                        bgIdent = GUIColorHelper.SetBackgroundColor(new Color(1, .8f, .8f), !config.ConfigExists);
                        config.relPathToConfig = EditorGUILayout.TextField("Config Rel.", config.relPathToConfig);
                        GUIColorHelper.ResetBackgroundColor(bgIdent);
                        EditorGUI.BeginDisabledGroup(!config.MLAgentsDirExists);
                        if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                        {
                            var path = PathHelper.MakeAbsolute(config.relPathToConfig, config.AbsolutePathToMlAgentsDir);
                            var selectedPath = EditorUtility.OpenFilePanel("Select Trainings Config",
                                Directory.Exists(path) ? path : config.AbsolutePathToMlAgentsDir,
                                "yaml");
                            if (File.Exists(selectedPath))
                                config.relPathToConfig = PathHelper.MakeRelative(selectedPath, config.AbsolutePathToMlAgentsDir);
                        }

                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();


                        EditorGUILayout.BeginHorizontal();
                        bgIdent = GUIColorHelper.SetBackgroundColor(new Color(1, .8f, .8f), config.HasInvalidBrain());
                        config.brainNames = EditorGUILayout.TextField("Brain Names", config.brainNames);
                        GUIColorHelper.ResetBackgroundColor(bgIdent);
                        if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
                        {
                            var selectedPath = EditorUtility.OpenFilePanel("Add Brain", Application.dataPath, "asset");
                            if (File.Exists(selectedPath))
                                config.AddBrain(selectedPath);
                        }

//                        if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
//                        {
//                            var selectedPath = EditorUtility.OpenFilePanel("Select Brain", Application.dataPath, "asset");
//                            if (File.Exists(selectedPath))
//                                config.RemoveBrain(selectedPath);
//                        }
                        EditorGUILayout.EndHorizontal();


                        EditorGUILayout.LabelField("Optional", EditorStyles.miniBoldLabel);
                        
                        EditorGUILayout.BeginHorizontal();
                        bgIdent = GUIColorHelper.SetBackgroundColor(new Color(1, .8f, .8f), !string.IsNullOrWhiteSpace(config.relPathToExecutable) && !config.ExecuteableExists);
                        config.relPathToExecutable =
                            EditorGUILayout.TextField("Executable Rel.", config.relPathToExecutable, new GUIStyle(EditorStyles.textField));
                        GUIColorHelper.ResetBackgroundColor(bgIdent);
                        EditorGUI.BeginDisabledGroup(!config.MLAgentsDirExists);
                        if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                        {
                            var path = PathHelper.MakeAbsolute(config.relPathToExecutable, config.AbsolutePathToMlAgentsDir);
                            var selectedPath = EditorUtility.OpenFilePanel("Select Trainings Executable",
                                Directory.Exists(path) ? path : config.AbsolutePathToMlAgentsDir,
                                "exe");
                            if (File.Exists(selectedPath))
                                config.relPathToExecutable = PathHelper.MakeRelative(selectedPath, config.AbsolutePathToMlAgentsDir);
                        }

                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();
                        
                        config.anacondaEnvironmentName = EditorGUILayout.TextField("Anaconda Env.", config.anacondaEnvironmentName);

                        EditorGUILayout.BeginHorizontal();
                        bgIdent = GUIColorHelper.SetBackgroundColor(new Color(1, .8f, .8f),
                            !config.CurriculumExists && !string.IsNullOrWhiteSpace(config.relPathToCurriculum));
                        config.relPathToCurriculum = EditorGUILayout.TextField("Curriculum Dir Rel.", config.relPathToCurriculum);
                        GUIColorHelper.ResetBackgroundColor(bgIdent);
                        EditorGUI.BeginDisabledGroup(!config.MLAgentsDirExists);
                        if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                        {
                            var path = PathHelper.MakeAbsolute(config.relPathToCurriculum, config.AbsolutePathToMlAgentsDir);
                            var selectedPath = EditorUtility.OpenFolderPanel("Select Curriculum Directory",
                                Directory.Exists(path) ? path : config.AbsolutePathToMlAgentsDir,
                                "");
                            if (Directory.Exists(selectedPath))
                                config.relPathToCurriculum = PathHelper.MakeRelative(selectedPath, config.AbsolutePathToMlAgentsDir);
                        }

                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();


                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Detect Problems", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
                            config.DetectProblems();
                        EditorGUILayout.EndHorizontal();


                        foreach (var problem in config.detectedProblems) EditorGUILayout.HelpBox(problem, MessageType.Warning);

                        --EditorGUI.indentLevel;

                        if (index < settings.Configurations.Count - 1)
                        {
                            GUILayout.Space(15);
                        }
                    }
                }

                foreach (var toBeRemoved in removeBuffer)
                    settings.RemoveConfiguration(toBeRemoved);
                removeBuffer.Clear();


                GUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Settings"))
                {
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("Add Configuration")) settings.AddConfiguration();
                EditorGUILayout.EndHorizontal();


                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(settings);
                }

                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Current Training Process ID: " + settings.lastTrainingProcessID, MessageType.Info);
                if (GUILayout.Button("Open Training Window"))
                    MLAgentsTrainingWindow.OpenWindow();
                GUILayout.Space(10);
            }
        }
    }
}