# ML Agents Helper

This package aims to make development with [ML-Agents](https://github.com/Unity-Technologies/ml-agents/) quicker and easier.

Features:
- Setup Training Configurations from Unity Project Settings
- Start, Stop, Continue Training from Unity Editor
- Easily copy trained Brains into your Project
- Set "Control" Parameter on Build-Time for Trainings Scenes
- Automatically Update Vector Observations during Development

## Content

### ML-Agents Settings Window
![](./Documentation~/SettingsWindow.png "")

- Active: If true this configuration will be used for training in ML-Agents-Training Window
- Name: well...
- Run ID*: the run-id parameter
- ML-Agents Dir Rel*: Relative Path to your project to ML-Agents root directory
- Config Rel*: Relative Path from ML-Agents root to the yaml configuration file used for training
- Brain Names*: Local Asset Paths to the Brains to be trained, separated by ";" (multi-brain training is not tested yet)
- Executable Rel: (optional) Relative Path to the executable used for training, if none defined or not found training will run in the editor
- Anaconda Env: (optional) Name of your anaconda environment that will activate for training
- Curriculum: (optional) Assign a Curriculum Asset (Create/marwi/ML-Agents/Curriculum) to be used for training. The JSON will be generated when you start training from the Editor and in "curricula/<run-id>/<name_of_brain>.json" (currently only one brain is supported, it uses the first found)

Detect Problems Button:
- Checks if json files in curriculum directory (if defined) match any Brain in your project
- Checks if added brains are LearningBrains (and exist)
 

Currrent Training Process ID: When starting traing from Training Window the Process ID is stored. This ID is used to find the previously started trainings process when UnityEditor recompiles scripts (e.g. when you work on your code while training)

### ML-Agents Training Window
![](./Documentation~/TrainingsWindow.png "")

Use this window to Start, Stop or Continue Training.

Copy Current Brain lets you copy the previously trained brain OR (if clicked during training) will temporarily Stop the training, copy the saved brain in your project and then resume training.


### Automatically Update Vector Observations
Import the "Training Editor Preprocessors" Samples from Unity Package Manager into your project. Then add the "Auto Update Observation" Script to any (or possibly all) of your Agents in your Training Scene. The script checks during ScriptReload (when writing Agent Code) all of your marked Agent's brains get updated. Internally it calls "InitializeAgent" and then "CollectVectorObservations" so make sure your Agent references are all set during Edit-Time.


### Training Scene Marker
Add this script to any GameObject in your scene to Automatically set your Academy "Control" to true when building

### Multi Agent Training Environment Marker
Add this script to any GameObject in your scene to automatically create multiple copy of your training environment when building

### Auto Update Parameters
When using Curriculum Learning this script automatically updates parameter names and lesson-0 values to your Academy when building


## Limitations
- Windows only so far, not tested on MacOS or Linux (feel free to though, I'm happy to fix/help make it work)
- multi brain training is not tested yet
- visual observations are not tested/auto update is not implemented
- imitation learning is not supported yet
- video recorder is not supported yet
- some training parameters are not yet exposed/supported (e.g. help, docker, keep-checkpoints, lesson, no-graphics, num-runs, save-frequency, seed, slow, worker-id)