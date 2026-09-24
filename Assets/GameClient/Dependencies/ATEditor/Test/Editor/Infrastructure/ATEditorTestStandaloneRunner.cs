using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Test
{
    /// <summary>
    /// ATEditor 自动化测试脱机执行套件
    /// 既可通过 Unity Editor Test Runner 运行，也可通过菜单项一键在控制台运行并输出完整诊断报告
    /// </summary>
    public static class ATEditorTestStandaloneRunner
    {
        public struct TestResult
        {
            public string FixtureName;
            public string TestName;
            public bool Success;
            public string Message;
            public long ElapsedMs;
        }

        [MenuItem("ATEditor/Tests/Run All Playback Tests", priority = 100)]
        public static void RunAllTestsMenu()
        {
            RunAllTests(logToConsole: true);
        }

        public static List<TestResult> RunAllTests(bool logToConsole = true)
        {
            var results = new List<TestResult>();
            var testFixtures = new List<Type>
            {
                typeof(ProcessLifecycleTests),
                typeof(ActionRunnerTickEdgeTests),
                typeof(ActionRunnerConcurrencyTests),
                typeof(ProcessContextTests),
                typeof(ProcessFactoryPoolTests)
            };

            int passedCount = 0;
            int failedCount = 0;
            var totalStopwatch = Stopwatch.StartNew();

            if (logToConsole)
            {
                UnityEngine.Debug.Log("<color=#00e676><b>[ATEditor.Test] 开始执行全量运行时自动化测试...</b></color>");
            }

            foreach (var fixtureType in testFixtures)
            {
                var fixtureInstance = Activator.CreateInstance(fixtureType);
                var setUpMethod = fixtureType.GetMethod("SetUp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var tearDownMethod = fixtureType.GetMethod("TearDown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var testMethods = fixtureType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (var method in testMethods)
                {
                    if (method.GetCustomAttribute<TestAttribute>() == null) continue;

                    var testSw = Stopwatch.StartNew();
                    bool testSuccess = false;
                    string testMessage = "PASSED";

                    try
                    {
                        setUpMethod?.Invoke(fixtureInstance, null);
                        method.Invoke(fixtureInstance, null);
                        testSuccess = true;
                        passedCount++;
                    }
                    catch (TargetInvocationException ex)
                    {
                        var inner = ex.InnerException ?? ex;
                        if (inner.GetType().Name == "SuccessException" || inner.Message.Contains("Pass"))
                        {
                            testSuccess = true;
                            passedCount++;
                            testMessage = $"PASSED ({inner.Message})";
                        }
                        else
                        {
                            testSuccess = false;
                            failedCount++;
                            testMessage = $"FAILED: {inner.Message}\n{inner.StackTrace}";
                        }
                    }
                    catch (Exception ex)
                    {
                        testSuccess = false;
                        failedCount++;
                        testMessage = $"FAILED: {ex.Message}\n{ex.StackTrace}";
                    }
                    finally
                    {
                        try
                        {
                            tearDownMethod?.Invoke(fixtureInstance, null);
                        }
                        catch (Exception tdEx)
                        {
                            UnityEngine.Debug.LogError($"[ATEditor.Test] TearDown 异常: {tdEx.Message}");
                        }
                        testSw.Stop();
                    }

                    var res = new TestResult
                    {
                        FixtureName = fixtureType.Name,
                        TestName = method.Name,
                        Success = testSuccess,
                        Message = testMessage,
                        ElapsedMs = testSw.ElapsedMilliseconds
                    };
                    results.Add(res);

                    if (logToConsole)
                    {
                        if (testSuccess)
                        {
                            UnityEngine.Debug.Log($"<color=#69f0ae>✔ [{fixtureType.Name}] {method.Name}</color> ({testSw.ElapsedMilliseconds}ms)");
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($"<color=#ff5252>✘ [{fixtureType.Name}] {method.Name}</color> ({testSw.ElapsedMilliseconds}ms)\n{testMessage}");
                        }
                    }
                }
            }

            totalStopwatch.Stop();
            if (logToConsole)
            {
                string summaryColor = failedCount == 0 ? "#00e676" : "#ff5252";
                UnityEngine.Debug.Log($"<color={summaryColor}><b>[ATEditor.Test] 执行完毕: 总计 {results.Count} 个用例, 通过: {passedCount}, 失败: {failedCount}, 总耗时: {totalStopwatch.ElapsedMilliseconds}ms</b></color>");
            }

            return results;
        }
    }
}
