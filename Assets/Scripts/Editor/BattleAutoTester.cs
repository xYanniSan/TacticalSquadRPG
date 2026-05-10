#if UNITY_EDITOR
using System.IO;
using TacticalRPG.ThirdPerson;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalRPG.Editor
{
    /// <summary>
    /// Auto-test runner for the combat loop. Enters play mode, lets the
    /// battle run for N seconds, exits play mode, and reports the path of
    /// the captured log file. Used by the autonomous iteration loop:
    ///
    ///   1. External tool calls TacticalRPG/Combat Test/Run 30s Battle
    ///   2. Editor enters play mode, BattleAutoStop runs the timer
    ///   3. Editor exits play mode → CombatLogger finalizes the log file
    ///   4. External tool reads `Logs/combat-current.log` and analyzes
    ///
    /// Uses EditorPrefs to persist the auto-test config across the domain
    /// reload that happens on play-mode entry.
    /// </summary>
    public static class BattleAutoTester
    {
        private const string PrefKey_Active   = "TacticalRPG.AutoTest.Active";
        private const string PrefKey_Duration = "TacticalRPG.AutoTest.DurationSec";
        public  const string ScenePath        = "Assets/Scenes/GameScene.unity";

        [MenuItem("TacticalRPG/Combat Test/Run 30s Battle")]
        public static void Run30s() => Run(30f);

        [MenuItem("TacticalRPG/Combat Test/Run 60s Battle")]
        public static void Run60s() => Run(60f);

        [MenuItem("TacticalRPG/Combat Test/Stop Auto-Test")]
        public static void StopAutoTest()
        {
            EditorPrefs.SetBool(PrefKey_Active, false);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.ExitPlaymode();
            Debug.Log("[BattleAutoTester] Stopped.");
        }

        public static void Run(float durationSec)
        {
            // Save the open scene first, then load the battle scene.
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            string current = SceneManager.GetActiveScene().path;
            if (current != ScenePath)
            {
                if (!File.Exists(ScenePath))
                {
                    Debug.LogError($"[BattleAutoTester] Scene not found: {ScenePath}");
                    return;
                }
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            EditorPrefs.SetBool(PrefKey_Active, true);
            EditorPrefs.SetFloat(PrefKey_Duration, durationSec);

            Debug.Log($"[BattleAutoTester] Entering play mode for {durationSec:F0}s. " +
                      $"Logs will be written to Logs/combat-current.log.");
            EditorApplication.EnterPlaymode();
        }

        // ── Runtime side: spawned by RuntimeInitializeOnLoad below ──

        public static bool TryConsumeAutoTestRequest(out float durationSec)
        {
            bool active = EditorPrefs.GetBool(PrefKey_Active, false);
            durationSec = EditorPrefs.GetFloat(PrefKey_Duration, 30f);
            if (active)
            {
                // Single-shot: clear the flag so manual play-mode entries don't
                // also auto-exit.
                EditorPrefs.SetBool(PrefKey_Active, false);
            }
            return active;
        }
    }

    /// <summary>
    /// Runtime companion. On play-mode entry, checks if an auto-test was
    /// requested; if so, spawns a coroutine that exits play mode after N seconds.
    /// </summary>
    public static class BattleAutoTesterRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            if (!BattleAutoTester.TryConsumeAutoTestRequest(out float durationSec)) return;

            var go = new GameObject("BattleAutoStop");
            Object.DontDestroyOnLoad(go);
            var stop = go.AddComponent<BattleAutoStop>();
            stop.durationSec = durationSec;
            Debug.Log($"[BattleAutoTester] Auto-test active — exiting play mode in {durationSec:F0}s.");
        }
    }

    public class BattleAutoStop : MonoBehaviour
    {
        public float durationSec = 30f;
        private float _elapsed;

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= durationSec)
            {
                Debug.Log($"[BattleAutoStop] Auto-test complete after {_elapsed:F1}s. Exiting play mode.");
                EditorApplication.ExitPlaymode();
                Destroy(gameObject);
            }
        }
    }
}
#endif
