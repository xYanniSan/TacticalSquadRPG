using System.Collections.Generic;
using System.IO;
using System.Text;
using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Focused logger for the hand-to-hand combat layer. Auto-subscribes
    /// to `BattleH2HPhaseSystem` and `BattleH2HOrchestrator` events so
    /// every phase transition, exchange start/impact/resolution, and
    /// brain decision is captured.
    ///
    /// Output:
    ///   - Ring buffer in memory (last `maxEntries`)
    ///   - Real-time tail to `Logs/h2h-current.log`
    ///   - On `Dump`: copies the full log to the system clipboard AND
    ///     writes `Logs/h2h-<timestamp>.log` for paste-back / archiving
    ///
    /// HOW TO READ
    /// ───────────
    ///   [T+ss.mmm][Fxxxxx] [CATEGORY] UnitName — message
    ///
    /// Categories:
    ///   PHASE   — H2HPhase enter/exit
    ///   EXCH    — Exchange started / impact / resolved
    ///   BRAIN   — Brain decision (commit / disengage / hold)
    ///   IMPACT  — Per-side impact callback (attack played, defender response)
    ///   POS     — Position / target / range snapshot (periodic, opt-in)
    ///   WARN    — Suspicious condition (orchestrator double-fire, missing target, etc.)
    ///
    /// HOTKEY: Press F10 to dump (editor + builds).
    /// </summary>
    public class H2HLogger : MonoBehaviour
    {
        public static H2HLogger Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Max in-memory entries before oldest are discarded.")]
        [SerializeField] private int maxEntries = 1000;
        [Tooltip("Mirror every entry to Unity's Console (noisy — usually off).")]
        [SerializeField] private bool echoToConsole = false;
        [Tooltip("Tail every entry to Logs/h2h-current.log in real time.")]
        [SerializeField] private bool writeToFile = true;
        [Tooltip("How often (seconds) to record a periodic position/range snapshot. 0 = never.")]
        [SerializeField] private float positionSnapshotInterval = 0f;

        [Header("References (auto-found)")]
        [SerializeField] private H2HTrainingDirector _director;
        [SerializeField] private string _logFolderRelative = "Logs";

        // ── Categories ─────────────────────────────────────────────
        public const string CAT_PHASE   = "PHASE  ";
        public const string CAT_EXCH    = "EXCH   ";
        public const string CAT_BRAIN   = "BRAIN  ";
        public const string CAT_IMPACT  = "IMPACT ";
        public const string CAT_POS     = "POS    ";
        public const string CAT_WARN    = "WARN   ";
        public const string CAT_LOCO    = "LOCO   ";

        private readonly List<string> _entries = new List<string>();
        private float _startTime;
        private float _nextSnapshotAt;
        private StreamWriter _fileWriter;
        private string _currentFilePath;

        public string CurrentFilePath => _currentFilePath;

        // ── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _startTime = Time.time;
            if (writeToFile) OpenLogFile();
        }

        private void Start()
        {
            if (_director == null) _director = FindAnyObjectByType<H2HTrainingDirector>();
            if (_director == null)
            {
                Warn("setup", "no H2HTrainingDirector in scene — logger will sit idle");
                return;
            }
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            CloseLogFile(snapshotOnClose: true);
            if (Instance == this) Instance = null;
        }

        private void Subscribe()
        {
            if (_director.Phases != null)
            {
                _director.Phases.OnPhaseEnter += HandlePhaseEnter;
                _director.Phases.OnPhaseExit  += HandlePhaseExit;
            }
            if (_director.Orchestrator != null)
            {
                _director.Orchestrator.OnExchangeStarted  += HandleExchangeStarted;
                _director.Orchestrator.OnExchangeImpact   += HandleExchangeImpact;
                _director.Orchestrator.OnExchangeResolved += HandleExchangeResolved;
            }
            Log("setup", $"logger subscribed to director ({_director.AllUnits?.Count ?? 0} units registered)");
        }

        private void Unsubscribe()
        {
            if (_director == null) return;
            if (_director.Phases != null)
            {
                _director.Phases.OnPhaseEnter -= HandlePhaseEnter;
                _director.Phases.OnPhaseExit  -= HandlePhaseExit;
            }
            if (_director.Orchestrator != null)
            {
                _director.Orchestrator.OnExchangeStarted  -= HandleExchangeStarted;
                _director.Orchestrator.OnExchangeImpact   -= HandleExchangeImpact;
                _director.Orchestrator.OnExchangeResolved -= HandleExchangeResolved;
            }
        }

        // ── Event handlers ─────────────────────────────────────────

        private void HandlePhaseEnter(MonoBehaviour unit, H2HPhase phase, string reason)
        {
            Log(CAT_PHASE, NameOf(unit), $"→ {phase,-12} ({reason})");
        }

        private void HandlePhaseExit(MonoBehaviour unit, H2HPhase phase, string reason)
        {
            Log(CAT_PHASE, NameOf(unit), $"   {phase,-12} exit ({reason})");
        }

        private void HandleExchangeStarted(BattleH2HOrchestrator.ExchangeHandle h)
        {
            string atk = NameOf(h.attacker as MonoBehaviour);
            string def = NameOf(h.defender as MonoBehaviour);
            int hits = h.combo != null ? h.combo.hits.Count : 0;
            string name = h.combo != null ? h.combo.name : "(no combo)";
            float firstImpact = (h.impactAt != null && h.impactAt.Length > 0) ? Mathf.Max(0f, h.impactAt[0] - Time.time) : 0f;
            float lenIn = Mathf.Max(0f, h.endsAt - Time.time);
            Log(CAT_EXCH, $"{atk}→{def}",
                $"START combo='{name}' hits={hits} archetype={h.archetype} firstImpactIn={firstImpact:F2}s len={lenIn:F2}s");
        }

        private void HandleExchangeImpact(BattleH2HOrchestrator.ExchangeHandle h, int hitIndex)
        {
            string atk = NameOf(h.attacker as MonoBehaviour);
            string def = NameOf(h.defender as MonoBehaviour);
            string tail = h.defenderCountered ? "COUNTER" : "hit-or-react";
            string attackId = (h.combo != null && hitIndex < h.combo.hits.Count) ? h.combo.hits[hitIndex].attackId : "?";
            Log(CAT_IMPACT, $"{atk}→{def}", $"impact #{hitIndex} attack='{attackId}' result={tail}");
        }

        private void HandleExchangeResolved(BattleH2HOrchestrator.ExchangeHandle h)
        {
            string atk = NameOf(h.attacker as MonoBehaviour);
            string def = NameOf(h.defender as MonoBehaviour);
            string name = h.combo != null ? h.combo.name : "(no combo)";
            Log(CAT_EXCH, $"{atk}→{def}",
                $"RESOLVE combo='{name}' counterSwap={h.defenderCountered}");
        }

        // ── External: locomotion driver hook ──────────────────────
        /// <summary>
        /// Called by `KuboldLocomotionDriver` whenever it picks a new clip,
        /// so the locomotion decision stream lands in the same dump stream
        /// as phase / brain / impact events. `playedId` is the id that
        /// actually started playing (may differ from `desiredId` if a
        /// fallback was used). Pass `null` for `playedId` when no clip in
        /// the fallback chain resolved.
        /// </summary>
        public void LogLocomotion(MonoBehaviour unit, H2HPhase phase, float speed, string direction, string desiredId, string playedId)
        {
            string fb = (playedId != null && playedId != desiredId) ? $" (fallback)" : string.Empty;
            string played = playedId ?? "<MISS>";
            Log(CAT_LOCO, NameOf(unit),
                $"phase={phase,-12} speed={speed:F2}m/s dir={direction,-12} → '{desiredId}' played='{played}'{fb}");
        }

        // ── Periodic snapshot ──────────────────────────────────────

        private void Update()
        {
            HandleHotkey();

            if (positionSnapshotInterval > 0f && Time.time >= _nextSnapshotAt)
            {
                _nextSnapshotAt = Time.time + positionSnapshotInterval;
                if (_director?.AllUnits != null)
                {
                    foreach (var u in _director.AllUnits)
                    {
                        if (u == null) continue;
                        var phase = u.Phases != null ? u.Phases.GetPhase(u) : H2HPhase.NotEngaged;
                        Vector3 vel = u.CC != null ? u.CC.velocity : Vector3.zero;
                        vel.y = 0f;
                        Log(CAT_POS, u.DisplayName,
                            $"phase={phase,-12} pos={Format(u.transform.position)} vel={vel.magnitude:F2}m/s hp={u.CurrentHp:F0}/{u.MaxHp:F0} sp={u.CurrentSpeed:F0}");
                    }
                }
            }
        }

        private void HandleHotkey()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f10Key.wasPressedThisFrame) Dump();
            // Shift+F10 clears
            if (kb.f10Key.wasPressedThisFrame && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
                Clear();
        }

        // ── Public API ─────────────────────────────────────────────

        public void Log(string category, string source, string message)
        {
            float t = Time.time - _startTime;
            int frame = Time.frameCount;
            string entry = $"[T+{t:F3}][F{frame:D5}] [{category}] {source,-18} — {message}";

            if (_entries.Count >= maxEntries) _entries.RemoveAt(0);
            _entries.Add(entry);

            if (echoToConsole) Debug.Log(entry);
            if (_fileWriter != null)
            {
                try { _fileWriter.WriteLine(entry); }
                catch { /* writer may be closing */ }
            }
        }

        public void Log(string source, string message) => Log("INFO   ", source, message);

        public void Warn(string source, string message)
        {
            Log(CAT_WARN, source, "⚠ " + message);
            Debug.LogWarning($"[H2HLogger] {source}: {message}");
        }

        /// <summary>Dump full buffer to console, write to a stamped file,
        /// and copy to system clipboard so the user can paste back.</summary>
        [ContextMenu("Dump Log")]
        public void Dump()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== H2H LOG ({_entries.Count} entries, T+{Time.time - _startTime:F1}s) ===");
            foreach (var e in _entries) sb.AppendLine(e);
            sb.AppendLine("=== END ===");
            string text = sb.ToString();

            Debug.Log(text);
            GUIUtility.systemCopyBuffer = text;

            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string folder = Path.Combine(projectRoot, _logFolderRelative);
                Directory.CreateDirectory(folder);
                string stamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string path = Path.Combine(folder, $"h2h-{stamp}.log");
                File.WriteAllText(path, text);
                Debug.Log($"[H2HLogger] Snapshot written to {path} and copied to clipboard.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[H2HLogger] Could not write snapshot file: {ex.Message}");
            }
        }

        public void Clear()
        {
            _entries.Clear();
            _startTime = Time.time;
            Debug.Log("[H2HLogger] Cleared.");
        }

        // ── File output ────────────────────────────────────────────

        private void OpenLogFile()
        {
            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string folder = Path.Combine(projectRoot, _logFolderRelative);
                Directory.CreateDirectory(folder);
                _currentFilePath = Path.Combine(folder, "h2h-current.log");
                _fileWriter = new StreamWriter(_currentFilePath, append: false) { AutoFlush = true };
                _fileWriter.WriteLine($"=== H2H LOG started {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[H2HLogger] Could not open log file: {ex.Message}");
                _fileWriter = null;
            }
        }

        private void CloseLogFile(bool snapshotOnClose)
        {
            if (_fileWriter == null) return;
            try
            {
                _fileWriter.WriteLine("=== END ===");
                _fileWriter.Close();
                _fileWriter = null;
                if (snapshotOnClose && File.Exists(_currentFilePath))
                {
                    string projectRoot = Path.GetDirectoryName(Application.dataPath);
                    string folder = Path.Combine(projectRoot, _logFolderRelative);
                    string stamp  = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string snapshotPath = Path.Combine(folder, $"h2h-{stamp}.log");
                    File.Copy(_currentFilePath, snapshotPath, overwrite: true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[H2HLogger] Error finalizing log file: {ex.Message}");
            }
        }

        // ── Helpers ────────────────────────────────────────────────

        private static string NameOf(MonoBehaviour unit)
        {
            if (unit == null) return "(null)";
            if (unit is H2HUnit h) return h.DisplayName;
            return unit.name;
        }

        private static string Format(Vector3 v) => $"({v.x:F2},{v.y:F2},{v.z:F2})";
    }
}
