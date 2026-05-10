using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.InputSystem;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Lightweight in-memory combat log.
    /// Attach to any persistent GameObject in the scene (e.g. TerrainBattleManager).
    ///
    /// HOW TO READ THE LOG
    /// ───────────────────
    /// Each line: [T+ss.mmm] [CATEGORY] UnitName — message
    ///
    /// Categories:
    ///   STATE    — UnitCombatState transition
    ///   ROLE     — CombatRole assignment (Attacker/Defender/Free)
    ///   ANIM     — Animation event fired (OnHitFrame, OnAttackEnd, OnBlockEnd, OnHitEnd)
    ///   DMG      — Damage applied, including block/hit type
    ///   INIT     — Initiative change (spend / gain)
    ///   EXCHANGE — Coordinator assigned or completed an exchange
    ///   WARN     — Suspicious / unexpected condition
    ///
    /// COMMON PATTERNS TO LOOK FOR
    /// ────────────────────────────
    /// • Two ROLE[Attacker] lines for different units in the same frame → coordinator leaking simultaneous attackers
    /// • STATE[Execute] on a unit that already has STATE[Execute] without a STATE[Recover] in between → interrupt
    /// • ANIM[OnAttackEnd] missing between STATE[Execute] and STATE[Recover] → event not firing, fallback timer used
    /// • DMG applied while target is STATE[Execute] → damage at wrong time, animation driven by fallback
    /// • ROLE[Free] on a defender before ANIM[OnBlockEnd] → block animation cut short
    /// • WARN lines → check immediately, they flag the exact violation
    /// </summary>
    public class CombatLogger : MonoBehaviour
    {
        public static CombatLogger Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Maximum number of entries kept in memory before oldest are discarded.")]
        [SerializeField] private int maxEntries = 500;

        [Tooltip("Also print every entry to the Unity Console (noisy — disable for performance).")]
        [SerializeField] private bool echoToConsole = false;

        [Header("File output (auto-test pipeline)")]
        [Tooltip("Append every entry in real time to Logs/combat-current.log. Lets external " +
                 "tools (auto-tester) read combat output without pressing L.")]
        [SerializeField] private bool writeToFile = true;
        [Tooltip("Folder under the project root where logs are written. Created if missing.")]
        [SerializeField] private string logFolderRelative = "Logs";

        private readonly List<string> _entries = new List<string>();
        private float _startTime;
        private StreamWriter _fileWriter;
        private string _currentFilePath;
        public string CurrentFilePath => _currentFilePath;

        // ── Categories ───────────────────────────────────────────────
        public const string CAT_STATE    = "STATE   ";
        public const string CAT_ROLE     = "ROLE    ";
        public const string CAT_ANIM     = "ANIM    ";
        public const string CAT_DMG      = "DMG     ";
        public const string CAT_INIT     = "INIT    ";
        public const string CAT_EXCHANGE = "EXCHANGE";
        public const string CAT_WARN     = "WARN    ";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _startTime = Time.time;
            if (writeToFile) OpenLogFile();
        }

        private void OpenLogFile()
        {
            try
            {
                // Resolve project root from Application.dataPath which points
                // to <project>/Assets — go one up.
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string folder = Path.Combine(projectRoot, logFolderRelative);
                Directory.CreateDirectory(folder);

                _currentFilePath = Path.Combine(folder, "combat-current.log");
                _fileWriter = new StreamWriter(_currentFilePath, append: false)
                {
                    AutoFlush = true
                };
                _fileWriter.WriteLine($"=== COMBAT LOG started {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CombatLogger] Could not open log file: {ex.Message}");
                _fileWriter = null;
            }
        }

        private void OnDestroy()
        {
            // Snapshot to a timestamped file for historical analysis, then close.
            if (_fileWriter != null)
            {
                try
                {
                    _fileWriter.WriteLine("=== END ===");
                    _fileWriter.Close();
                    _fileWriter = null;

                    // Copy combat-current.log to a session-stamped filename so a
                    // sequence of runs can be diffed.
                    string projectRoot = Path.GetDirectoryName(Application.dataPath);
                    string folder = Path.Combine(projectRoot, logFolderRelative);
                    string stamp  = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string snapshotPath = Path.Combine(folder, $"combat-{stamp}.log");
                    if (File.Exists(_currentFilePath))
                        File.Copy(_currentFilePath, snapshotPath, overwrite: true);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[CombatLogger] Error finalizing log file: {ex.Message}");
                }
            }
            if (Instance == this) Instance = null;
        }

        // ── Public API ───────────────────────────────────────────────

        public void Log(string category, string unitName, string message)
        {
            float t = Time.time - _startTime;
            int frame = Time.frameCount;
            string entry = $"[T+{t:F3}][F{frame:D5}] [{category}] {unitName,-18} — {message}";

            if (_entries.Count >= maxEntries)
                _entries.RemoveAt(0);

            _entries.Add(entry);

            if (echoToConsole)
                Debug.Log(entry);

            if (_fileWriter != null)
            {
                try { _fileWriter.WriteLine(entry); }
                catch { /* writer may be closed during shutdown */ }
            }
        }

        public void Warn(string unitName, string message)
        {
            Log(CAT_WARN, unitName, "⚠ " + message);
            // Warnings always go to console regardless of echoToConsole
            Debug.LogWarning($"[CombatLogger] {unitName}: {message}");
        }

        // ── Dump ─────────────────────────────────────────────────────

        /// <summary>
        /// Prints the full log to the Unity Console as a single block.
        /// Call from a button or keyboard shortcut in-editor.
        /// </summary>
        [ContextMenu("Dump Log to Console")]
        public void DumpLog()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== COMBAT LOG ({_entries.Count} entries) ===");
            foreach (var e in _entries)
                sb.AppendLine(e);
            sb.AppendLine("=== END ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>Clears the log. Useful to call at the start of each battle.</summary>
        public void Clear()
        {
            _entries.Clear();
            _startTime = Time.time;
        }

        private void Update()
        {
            // Press L in the editor to dump without needing a button
#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
                DumpLog();
#endif
        }
    }
}
