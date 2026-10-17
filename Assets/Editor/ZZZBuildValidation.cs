using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor.Validation
{
    /// <summary>
    /// W0 Windows 候选 Demo 构建入口，显式传入场景而不修改 Build Settings。
    /// </summary>
    public static class ZZZBuildValidation
    {
        public static void BuildWindowsDevelopment()
        {
            string outputDirectory = Path.GetFullPath("docs/评估/2026-10-13/evidence/w0/windows-development");
            Directory.CreateDirectory(outputDirectory);
            int exitCode = 1;
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Test_Player.unity" },
                    locationPathName = Path.Combine(outputDirectory, "ZZZ.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                var evidence = new BuildEvidence
                {
                    unityVersion = Application.unityVersion,
                    scene = options.scenes[0],
                    outputPath = summary.outputPath,
                    result = summary.result.ToString(),
                    totalErrors = summary.totalErrors,
                    totalWarnings = summary.totalWarnings,
                    totalSize = summary.totalSize,
                    durationSeconds = summary.totalTime.TotalSeconds
                };
                File.WriteAllText(Path.Combine(outputDirectory, "build-summary.json"), JsonUtility.ToJson(evidence, true));
                File.WriteAllLines(Path.Combine(outputDirectory, "build-messages.txt"),
                    CollectMessages(report));
                exitCode = summary.result == BuildResult.Succeeded && summary.totalErrors == 0 ? 0 : 1;
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(outputDirectory, "build-exception.txt"), exception.ToString());
                Debug.LogException(exception);
            }
            finally
            {
                EditorApplication.Exit(exitCode);
            }
        }

        private static System.Collections.Generic.IEnumerable<string> CollectMessages(BuildReport report)
        {
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages)
                {
                    yield return $"[{message.type}] {step.name}: {message.content}";
                }
            }
        }

        [Serializable]
        private sealed class BuildEvidence
        {
            public string unityVersion;
            public string scene;
            public string outputPath;
            public string result;
            public int totalErrors;
            public int totalWarnings;
            public ulong totalSize;
            public double durationSeconds;
        }
    }
}
