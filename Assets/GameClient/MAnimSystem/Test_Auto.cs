using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.MAnimSystem
{
    /// <summary>
    /// Fully automatic regression suite for MAnimSystem.
    /// Put this on the same GameObject that has AnimComponent.
    /// </summary>
    public class Test_Auto : MonoBehaviour
    {
        [Header("References")]
        public AnimComponent animComponent;

        [Header("Required Clips")]
        public AnimationClip clip1;
        public AnimationClip clip2;
        public AnimationClip clip3;

        [Header("Optional Clip")]
        public AnimationClip clip4;

        [Header("Run Options")]
        public bool runOnStart = true;
        public bool loopRun = false;
        public float loopInterval = 1f;
        public bool verbose = true;
        public bool failOnUnityError = true;
        public int randomSeed = 20260302;

        [Header("Switch Settings")]
        public float fadeDuration = 0.08f;
        public int lowFreqSwitchCount = 12;
        public float lowFreqInterval = 0.12f;
        public int highFreqSwitchCount = 120;
        public float highFreqInterval = 0.01f;
        public int sameClipReentryCount = 80;
        public int pressureSwitchCount = 240;
        public int randomSwitchCount = 160;
        public float fadeDurationMismatchLong = 0.8f;
        public float fadeDurationMismatchShort = 0.02f;
        public float fadeDurationMismatchObserveTime = 0.25f;

        [Header("Transition Duration Test")]
        public float transitionDurationTestA = 0.12f;
        public float transitionDurationTestB = 0.35f;
        public float transitionDurationTolerance = 0.06f;
        public float transitionDurationTimeout = 2f;

        [Header("Speed Test")]
        public float speedProbeDuration = 0.16f;
        public float speedPauseMaxDelta = 0.02f;
        public float speedResumeMinDelta = 0.02f;
        public float speedFastMultiplierMin = 1.2f;

        [Header("Thresholds")]
        public int maxConnectedAfterLowFreq = 8;
        public int maxConnectedAfterHighFreq = 12;
        public int maxConnectedAfterSameClip = 10;
        public int maxConnectedAfterPressure = 14;
        public int maxConnectedAfterSettle = 6;
        public float minReuseRateAfterBurst = 0.4f;

        private bool _isRunning;
        private bool _isCapturingLog;
        private int _passCount;
        private int _failCount;
        private int _runtimeErrorCount;

        private readonly List<string> _failMessages = new List<string>();
        private readonly List<string> _runtimeErrors = new List<string>();
        private System.Random _rng;

        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunSuiteLoop());
            }
        }

        private void OnDestroy()
        {
            StopLogCapture();
        }

        [ContextMenu("Run Auto Test")]
        public void RunAutoTest()
        {
            if (_isRunning)
            {
                return;
            }

            StartCoroutine(RunSuiteLoop());
        }

        private IEnumerator RunSuiteLoop()
        {
            do
            {
                yield return StartCoroutine(RunSuiteOnce());
                if (loopRun)
                {
                    yield return new WaitForSeconds(loopInterval);
                }
            }
            while (loopRun);
        }

        private IEnumerator RunSuiteOnce()
        {
            if (_isRunning)
            {
                yield break;
            }

            _isRunning = true;
            _passCount = 0;
            _failCount = 0;
            _runtimeErrorCount = 0;
            _failMessages.Clear();
            _runtimeErrors.Clear();
            _rng = new System.Random(randomSeed);

            StartLogCapture();
            Log("========== MAnimSystem Auto Test Begin ==========");

            if (!ValidateSetup())
            {
                PrintSummary();
                StopLogCapture();
                _isRunning = false;
                yield break;
            }

            // Reset graph state before each full run for deterministic results.
            animComponent.Initialize();
            animComponent.ClearPlayGraph();
            animComponent.InitializeGraph();
            animComponent.SetLayerSpeed(0, 1f);
            animComponent.ResetPoolMetricsCounters();

            yield return StartCoroutine(RunCase("Basic Play/Fade", CaseBasicPlayFade()));
            yield return StartCoroutine(RunCase("Transition Duration Accuracy", CaseTransitionDurationAccuracy()));
            yield return StartCoroutine(RunCase("Layer Speed Control", CaseLayerSpeedControl()));
            yield return StartCoroutine(RunCase("Fade Duration Mismatch Transition", CaseFadeDurationMismatchTransition()));
            yield return StartCoroutine(RunCase("Scheduled Event Chain", CaseScheduledEventChain()));
            yield return StartCoroutine(RunCase("BlendTree1D State", CaseBlendTree1DState()));
            yield return StartCoroutine(RunCase("BlendTree2D State", CaseBlendTree2DState()));
            yield return StartCoroutine(RunCase("Low Frequency Switching", CaseLowFrequencySwitching()));
            yield return StartCoroutine(RunCase("High Frequency Switching", CaseHighFrequencySwitching()));
            yield return StartCoroutine(RunCase("Same Clip Reentry", CaseSameClipReentry()));
            yield return StartCoroutine(RunCase("Multi-Clip Pressure", CaseMultiClipPressure()));
            yield return StartCoroutine(RunCase("Randomized Burst", CaseRandomizedBurst()));
            yield return StartCoroutine(RunCase("Settle & Leak Check", CaseSettleAndLeakCheck()));
            yield return StartCoroutine(RunCase("Unity Error/Exception Check", CaseRuntimeErrorCheck()));

            PrintSummary();
            StopLogCapture();
            _isRunning = false;
        }

        private bool ValidateSetup()
        {
            if (animComponent == null)
            {
                animComponent = GetComponent<AnimComponent>();
            }

            if (animComponent == null)
            {
                Fail("AnimComponent is missing.");
                return false;
            }

            if (clip1 == null || clip2 == null || clip3 == null)
            {
                Fail("clip1/clip2/clip3 are required.");
                return false;
            }

            if (animComponent.Animator == null)
            {
                Fail("AnimComponent.Animator is missing.");
                return false;
            }

            return true;
        }

        private IEnumerator RunCase(string caseName, IEnumerator routine)
        {
            int failBefore = _failCount;
            float begin = Time.realtimeSinceStartup;
            yield return StartCoroutine(routine);
            float elapsed = Time.realtimeSinceStartup - begin;

            if (_failCount == failBefore)
            {
                _passCount++;
                Log($"[PASS] {caseName} ({elapsed:F3}s)");
            }
            else
            {
                LogError($"[FAIL] {caseName} ({elapsed:F3}s)");
            }
        }

        private IEnumerator CaseBasicPlayFade()
        {
            animComponent.Play(clip2, 0f, true);
            yield return new WaitForSeconds(0.02f);
            AssertTrue(animComponent.GetCurrentClip() == clip2, "Basic play failed: current clip is not clip2.");

            animComponent.Play(clip3, fadeDuration, true);
            yield return new WaitForSeconds(Mathf.Max(0.12f, fadeDuration * 1.6f));
            AssertTrue(animComponent.GetCurrentClip() == clip3, "Basic fade failed: current clip is not clip3.");
        }

        private IEnumerator CaseTransitionDurationAccuracy()
        {
            animComponent.SetLayerSpeed(0, 1f);

            yield return StartCoroutine(VerifyTransitionDurationOnce(transitionDurationTestA, "A"));
            yield return StartCoroutine(VerifyTransitionDurationOnce(transitionDurationTestB, "B"));
        }

        private IEnumerator CaseScheduledEventChain()
        {
            bool eventTriggered = false;
            AnimState state = animComponent.Play(clip1, fadeDuration, true);
            AssertTrue(state != null, "Scheduled event test failed: Play(clip1) returned null.");
            if (state == null)
            {
                yield break;
            }

            float triggerTime = Mathf.Min(1.6999998f, Mathf.Max(0.02f, clip1.length * 0.7f));
            state.AddScheduledEvent(triggerTime, _ =>
            {
                eventTriggered = true;
                animComponent.Play(clip2, fadeDuration, true);
            });

            float timeout = triggerTime + 1f;
            float elapsed = 0f;
            while (elapsed < timeout && !eventTriggered)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            AssertTrue(eventTriggered, "Scheduled event was not triggered.");
            yield return new WaitForSeconds(0.08f);
            AssertTrue(animComponent.GetCurrentClip() == clip2, "Scheduled event did not switch to clip2.");
        }

        private IEnumerator VerifyTransitionDurationOnce(float requestedDuration, string label)
        {
            if (requestedDuration <= 0f)
            {
                Fail($"Transition duration test {label} has invalid requestedDuration={requestedDuration:F4}.");
                yield break;
            }

            animComponent.Play(clip1, 0f, true);
            yield return null;
            yield return null;

            AnimState target = animComponent.Play(clip2, requestedDuration, true);
            AssertTrue(target != null, $"Transition duration test {label} failed: Play(clip2) returned null.");
            if (target == null)
            {
                yield break;
            }

            float start = Time.realtimeSinceStartup;
            float fadeInDoneElapsed = -1f;
            float fadeOutDoneElapsed = -1f;

            while (Time.realtimeSinceStartup - start < transitionDurationTimeout)
            {
                float elapsed = Time.realtimeSinceStartup - start;
                if (fadeInDoneElapsed < 0f && target.Weight >= 0.999f)
                {
                    fadeInDoneElapsed = elapsed;
                }

                AnimComponentPoolMetrics metrics = animComponent.GetPoolMetrics();
                if (fadeOutDoneElapsed < 0f &&
                    metrics.TotalFadingStates == 0 &&
                    metrics.TotalConnectedStates <= 1 &&
                    animComponent.GetCurrentClip() == clip2)
                {
                    fadeOutDoneElapsed = elapsed;
                }

                if (fadeInDoneElapsed >= 0f && fadeOutDoneElapsed >= 0f)
                {
                    break;
                }

                yield return null;
            }

            AssertTrue(fadeInDoneElapsed >= 0f,
                $"Transition duration test {label} timeout: fade-in completion not reached within {transitionDurationTimeout:F2}s.");
            AssertTrue(fadeOutDoneElapsed >= 0f,
                $"Transition duration test {label} timeout: fade-out cleanup completion not reached within {transitionDurationTimeout:F2}s.");

            if (fadeInDoneElapsed >= 0f)
            {
                float inDiff = Mathf.Abs(fadeInDoneElapsed - requestedDuration);
                AssertTrue(inDiff <= transitionDurationTolerance,
                    $"Transition duration test {label} fade-in mismatch: requested={requestedDuration:F4}, actual={fadeInDoneElapsed:F4}, diff={inDiff:F4}, tolerance={transitionDurationTolerance:F4}");
            }

            if (fadeOutDoneElapsed >= 0f)
            {
                float outTolerance = transitionDurationTolerance + Mathf.Max(0.01f, Time.deltaTime);
                float outDiff = Mathf.Abs(fadeOutDoneElapsed - requestedDuration);
                AssertTrue(outDiff <= outTolerance,
                    $"Transition duration test {label} fade-out mismatch: requested={requestedDuration:F4}, actual={fadeOutDoneElapsed:F4}, diff={outDiff:F4}, tolerance={outTolerance:F4}");
            }
        }

        private IEnumerator CaseFadeDurationMismatchTransition()
        {
            animComponent.ResetPoolMetricsCounters();

            animComponent.Play(clip1, fadeDurationMismatchLong, true);
            yield return new WaitForSeconds(0.02f);
            animComponent.Play(clip2, fadeDurationMismatchShort, true);
            yield return new WaitForSeconds(fadeDurationMismatchObserveTime);

            AnimComponentPoolMetrics metrics = animComponent.GetPoolMetrics();
            AssertTrue(animComponent.GetCurrentClip() == clip2, "Fade mismatch test failed: current clip is not clip2.");
            AssertTrue(metrics.TotalConnectedStates <= 1,
                $"Fade mismatch test failed: stale fading state detected. connected={metrics.TotalConnectedStates}");
        }

        private IEnumerator CaseLayerSpeedControl()
        {
            animComponent.SetLayerSpeed(0, 1f);
            AnimState state = animComponent.Play(clip1, 0f, true);
            AssertTrue(state != null, "Speed test failed: Play(clip1) returned null.");
            if (state == null)
            {
                yield break;
            }

            state = animComponent.Play(clip1, 0f, true);
            yield return new WaitForSeconds(speedProbeDuration);
            float normalStart = state.Time;

            animComponent.SetLayerSpeed(0, 0f);
            state = animComponent.Play(clip1, 0f, true);
            float pauseStart = state.Time;
            yield return new WaitForSeconds(speedProbeDuration);
            float pauseEnd = state.Time;
            AssertTrue(Mathf.Abs(pauseEnd - pauseStart) <= speedPauseMaxDelta,
                $"Speed pause failed: delta={Mathf.Abs(pauseEnd - pauseStart):F4}, threshold={speedPauseMaxDelta:F4}");

            animComponent.SetLayerSpeed(0, 1f);
            state = animComponent.Play(clip1, 0f, true);
            normalStart = state.Time;
            yield return new WaitForSeconds(speedProbeDuration);
            float normalEnd = state.Time;
            float normalAdvance = normalEnd - normalStart;
            AssertTrue(normalAdvance >= speedResumeMinDelta,
                $"Speed resume failed: normalAdvance={normalAdvance:F4}, threshold={speedResumeMinDelta:F4}");

            animComponent.SetLayerSpeed(0, 2f);
            state = animComponent.Play(clip1, 0f, true);
            float fastStart = state.Time;
            yield return new WaitForSeconds(speedProbeDuration);
            float fastEnd = state.Time;
            float fastAdvance = fastEnd - fastStart;
            AssertTrue(fastAdvance >= Mathf.Max(speedResumeMinDelta, normalAdvance * speedFastMultiplierMin),
                $"Speed accelerate failed: fastAdvance={fastAdvance:F4}, normalAdvance={normalAdvance:F4}, minMultiplier={speedFastMultiplierMin:F2}");

            animComponent.SetLayerSpeed(0, 1f);
        }

        private IEnumerator CaseBlendTree1DState()
        {
            var blendState = animComponent.CreateBlendTree1DState(new[]
            {
                new BlendTree1DChild(clip1, 0f),
                new BlendTree1DChild(clip2, 0.5f),
                new BlendTree1DChild(clip3, 1f)
            }, 0f);

            animComponent.Play(blendState, fadeDuration, true);
            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < 40; i++)
            {
                blendState.Parameter = Mathf.PingPong(i * 0.08f, 1f);
                yield return null;
            }

            AssertTrue(animComponent.GetCurrentState() == blendState, "BlendTree1D failed: current state mismatch.");

            animComponent.Play(clip2, fadeDuration, true);
            yield return new WaitForSeconds(0.3f);
            var metrics = animComponent.GetPoolMetrics();
            AssertTrue(metrics.TotalConnectedStates <= maxConnectedAfterHighFreq,
                $"BlendTree1D cleanup overflow: connected={metrics.TotalConnectedStates}, threshold={maxConnectedAfterHighFreq}");
        }

        private IEnumerator CaseBlendTree2DState()
        {
            var blendState = animComponent.CreateBlendTree2DState(new[]
            {
                new BlendTree2DChild(clip1, new Vector2(-1f, 0f)),
                new BlendTree2DChild(clip2, new Vector2(1f, 0f)),
                new BlendTree2DChild(clip3, new Vector2(0f, 1f))
            }, Vector2.zero);

            animComponent.Play(blendState, fadeDuration, true);
            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < 50; i++)
            {
                float t = i / 50f;
                float angle = t * Mathf.PI * 2f;
                blendState.Parameter = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                yield return null;
            }

            AssertTrue(animComponent.GetCurrentState() == blendState, "BlendTree2D failed: current state mismatch.");

            animComponent.Play(clip3, fadeDuration, true);
            yield return new WaitForSeconds(0.3f);
            var metrics = animComponent.GetPoolMetrics();
            AssertTrue(metrics.TotalConnectedStates <= maxConnectedAfterHighFreq,
                $"BlendTree2D cleanup overflow: connected={metrics.TotalConnectedStates}, threshold={maxConnectedAfterHighFreq}");
        }

        private IEnumerator CaseLowFrequencySwitching()
        {
            animComponent.ResetPoolMetricsCounters();

            for (int i = 0; i < lowFreqSwitchCount; i++)
            {
                AnimationClip clip = (i & 1) == 0 ? clip2 : clip3;
                animComponent.Play(clip, fadeDuration, true);
                yield return new WaitForSeconds(lowFreqInterval);
            }

            yield return new WaitForSeconds(0.2f);
            AnimComponentPoolMetrics metrics = animComponent.GetPoolMetrics();
            AssertTrue(metrics.TotalConnectedStates <= maxConnectedAfterLowFreq,
                $"Low-frequency switch overflow: connected={metrics.TotalConnectedStates}, threshold={maxConnectedAfterLowFreq}");
            AssertTrue(metrics.TotalReusedStateCount > 0, "Low-frequency switch had zero reuse hits.");
        }

        private IEnumerator CaseHighFrequencySwitching()
        {
            animComponent.ResetPoolMetricsCounters();

            for (int i = 0; i < highFreqSwitchCount; i++)
            {
                AnimationClip clip = (i & 1) == 0 ? clip2 : clip3;
                animComponent.Play(clip, fadeDuration, true);
                yield return new WaitForSeconds(highFreqInterval);
            }

            yield return new WaitForSeconds(0.5f);
            AnimComponentPoolMetrics metrics = animComponent.GetPoolMetrics();
            AssertTrue(metrics.TotalConnectedStates <= maxConnectedAfterHighFreq,
                $"High-frequency switch overflow: connected={metrics.TotalConnectedStates}, threshold={maxConnectedAfterHighFreq}");
            AssertTrue(metrics.TotalReuseHitRate >= minReuseRateAfterBurst,
                $"High-frequency reuse is too low: reuseRate={metrics.TotalReuseHitRate:P1}, threshold={minReuseRateAfterBurst:P1}");
        }

        private IEnumerator CaseSameClipReentry()
        {
            animComponent.ResetPoolMetricsCounters();

            for (int i = 0; i < sameClipReentryCount; i++)
            {
                animComponent.Play(clip2, fadeDuration, true);
                yield return new WaitForSeconds(highFreqInterval);
            }

            yield return new WaitForSeconds(0.4f);
            AnimComponentPoolMetrics metrics = animComponent.GetPoolMetrics();
            AssertTrue(animComponent.GetCurrentClip() == clip2, "Same-clip reentry failed: current clip is not clip2.");
            AssertTrue(metrics.TotalConnectedStates <= maxConnectedAfterSameClip,
                $"Same-clip reentry overflow: connected={metrics.TotalConnectedStates}, threshold={maxConnectedAfterSameClip}");
            AssertTrue(metrics.TotalReusedStateCount > 0, "Same-clip reentry had zero reuse hits.");
        }

        private IEnumerator CaseMultiClipPressure()
        {
            animComponent.ResetPoolMetricsCounters();
            AnimationClip[] clips = GetClipSet();

            for (int i = 0; i < pressureSwitchCount; i++)
            {
                AnimationClip clip = clips[i % clips.Length];
                animComponent.Play(clip, fadeDuration, true);
                yield return new WaitForSeconds(highFreqInterval);
            }

            yield return new WaitForSeconds(0.8f);
            AnimComponentPoolMetrics metrics = animComponent.GetPoolMetrics();
            AssertTrue(metrics.TotalConnectedStates <= maxConnectedAfterPressure,
                $"Multi-clip pressure overflow: connected={metrics.TotalConnectedStates}, threshold={maxConnectedAfterPressure}");
            AssertTrue(metrics.TotalDestroyedStateCount >= 0, "Invalid metrics: destroyed state count is negative.");
        }

        private IEnumerator CaseRandomizedBurst()
        {
            animComponent.ResetPoolMetricsCounters();
            AnimationClip[] clips = GetClipSet();

            for (int i = 0; i < randomSwitchCount; i++)
            {
                AnimationClip clip = clips[_rng.Next(clips.Length)];
                float localFade = Mathf.Lerp(0f, fadeDuration * 1.5f, (float)_rng.NextDouble());
                float localWait = Mathf.Lerp(0f, highFreqInterval * 2f, (float)_rng.NextDouble());

                animComponent.Play(clip, localFade, true);
                if (localWait <= 0f)
                {
                    yield return null;
                }
                else
                {
                    yield return new WaitForSeconds(localWait);
                }
            }

            yield return new WaitForSeconds(0.8f);
            AnimComponentPoolMetrics metrics = animComponent.GetPoolMetrics();
            AssertTrue(metrics.TotalConnectedStates <= maxConnectedAfterPressure,
                $"Randomized burst overflow: connected={metrics.TotalConnectedStates}, threshold={maxConnectedAfterPressure}");
            AssertTrue(metrics.TotalReuseHitRate >= minReuseRateAfterBurst * 0.8f,
                $"Randomized burst reuse is too low: reuseRate={metrics.TotalReuseHitRate:P1}");
        }

        private IEnumerator CaseSettleAndLeakCheck()
        {
            animComponent.Play(clip2, fadeDuration, true);
            yield return new WaitForSeconds(1f);

            AnimComponentPoolMetrics metrics = animComponent.GetPoolMetrics();
            AssertTrue(metrics.TotalConnectedStates <= maxConnectedAfterSettle,
                $"Settle check failed: connected={metrics.TotalConnectedStates}, threshold={maxConnectedAfterSettle}");
            AssertTrue(metrics.TotalFadingStates <= 1,
                $"Settle check failed: fading state count={metrics.TotalFadingStates}");
        }

        private IEnumerator CaseRuntimeErrorCheck()
        {
            if (!failOnUnityError)
            {
                yield break;
            }

            if (_runtimeErrorCount > 0)
            {
                string detail = _runtimeErrors.Count > 0
                    ? "\n" + string.Join("\n----\n", _runtimeErrors)
                    : string.Empty;
                Fail($"Captured {_runtimeErrorCount} Unity Error/Exception logs during suite.{detail}");
            }
        }

        private AnimationClip[] GetClipSet()
        {
            return clip4 != null
                ? new[] { clip1, clip2, clip3, clip4 }
                : new[] { clip1, clip2, clip3 };
        }

        private void StartLogCapture()
        {
            if (_isCapturingLog)
            {
                return;
            }

            Application.logMessageReceived += HandleUnityLog;
            _isCapturingLog = true;
        }

        private void StopLogCapture()
        {
            if (!_isCapturingLog)
            {
                return;
            }

            Application.logMessageReceived -= HandleUnityLog;
            _isCapturingLog = false;
        }

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            if (!_isRunning || !failOnUnityError)
            {
                return;
            }

            if (type != LogType.Error && type != LogType.Exception)
            {
                return;
            }

            if (!string.IsNullOrEmpty(condition) && condition.Contains("[Test_Auto]"))
            {
                return;
            }

            _runtimeErrorCount++;
            if (_runtimeErrors.Count < 8)
            {
                _runtimeErrors.Add($"[{type}] {condition}\n{stackTrace}");
            }
        }

        private void AssertTrue(bool condition, string failMessage)
        {
            if (!condition)
            {
                Fail(failMessage);
            }
        }

        private void Fail(string message)
        {
            _failCount++;
            _failMessages.Add(message);
            LogError("[ASSERT] " + message);
        }

        private void PrintSummary()
        {
            string poolReport = animComponent != null
                ? animComponent.GetPoolMetricsReport(true)
                : "[MAnimSystem.Pool] AnimComponent is null";

            if (_failCount == 0)
            {
                Log($"========== AUTO TEST PASS: pass={_passCount}, fail={_failCount} ==========\n{poolReport}");
            }
            else
            {
                string joined = string.Join("\n", _failMessages);
                LogError($"========== AUTO TEST FAIL: pass={_passCount}, fail={_failCount} ==========\n{joined}\n{poolReport}");
            }
        }

        private void Log(string message)
        {
            if (verbose)
            {
                Debug.Log("[Test_Auto] " + message);
            }
        }

        private void LogError(string message)
        {
            Debug.LogError("[Test_Auto] " + message);
        }
    }
}
