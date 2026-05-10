using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Live debug overlay drawn above each unit. Surfaces the AI brain's
    /// current state — archetype, intent, stance, exchange phase, active CC —
    /// so combat behaviour is visible without combing the log. Auto-attached
    /// by `TerrainBattleUnit.Initialize`, mirroring `SpeedBarUI`.
    ///
    /// Toggle visibility with the H key (default on).
    /// </summary>
    public class CombatOverlayUI : MonoBehaviour
    {
        private static bool s_visible = true;

        private TerrainBattleUnit _unit;
        private UnitBrainAI       _brain;
        private BattleSpeedSystem _speed;
        private BattleMovementSystem _movement;
        private BattleStatusEffectSystem _status;
        private BattleExchangeCoordinator _exchange;
        private BattleCombatEngine _engine;

        public void Initialize(TerrainBattleUnit unit)
        {
            _unit     = unit;
            _brain    = unit.GetComponent<UnitBrainAI>();
            var mgr   = TerrainBattleManager.Instance;
            if (mgr != null)
            {
                _speed    = mgr.Speed;
                _movement = mgr.Movement;
                _status   = mgr.StatusEffects;
                _exchange = mgr.ExchangeCoordinator;
                _engine   = mgr.CombatEngine;
            }
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
            {
                s_visible = !s_visible;
            }
        }

        private void OnGUI()
        {
            if (!s_visible) return;
            if (_unit == null || _unit.IsDead) return;
            if (Camera.main == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 2.6f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f) return;

            float dist = Vector3.Distance(Camera.main.transform.position, transform.position);
            if (dist > 30f) return;

            const float boxWidth  = 220f;
            const float lineHeight = 14f;
            float x = screenPos.x - boxWidth / 2f;
            float y = Screen.height - screenPos.y - 70f;

            // Lines composed lazily per draw — cheap given <10 units.
            string archetype = _brain != null ? _brain.LoadoutArchetype.ToString() : "?";
            MovementIntent intent = _movement != null ? _movement.GetIntent(_unit) : MovementIntent.Hold;
            string stanceName = _brain != null && _brain.Stance != null ? _brain.Stance.displayName : "—";
            float speed = _speed != null ? _speed.GetSpeed(_unit) : 0f;
            SpeedBand band = _speed != null ? _speed.GetSpeedBand(_unit) : SpeedBand.Engaged;

            // Phase: lookup by attacker reference; if this unit isn't an attacker,
            // try the existing role/state for context.
            string phaseLabel = _exchange != null ? _exchange.GetExchangePhase(_unit).ToString() : "None";
            string statusLine = BuildStatusLine();

            // Background box
            int lineCount = 6;
            var bgRect = new Rect(x, y, boxWidth, lineHeight * lineCount + 6f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                new Color(0f, 0f, 0f, 0.55f), 0f, 4f);

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                alignment = TextAnchor.UpperCenter
            };
            labelStyle.normal.textColor = Color.white;

            // Engine state takes priority over the legacy CombatState
            // (which freezes at "Decide" once the engine takes over).
            string stateLabel;
            UnitMoveExecution exec = _engine != null ? _engine.GetState(_unit) : null;
            if (_brain != null && _brain.EngineControlled && exec != null && exec.currentMove != null)
                stateLabel = $"{exec.currentMove.id} f{exec.framesElapsed} {exec.phase}";
            else
                stateLabel = $"{_unit.CombatState}, {_unit.CombatRole}";

            float ly = y + 3f;
            DrawLine($"<b>{_unit.Unit?.DisplayName}</b>  ({stateLabel})", x, ly, boxWidth, lineHeight, labelStyle);
            ly += lineHeight;
            DrawLine($"<color=#9cc>arch</color> {archetype}   <color=#9cc>stance</color> {stanceName}", x, ly, boxWidth, lineHeight, labelStyle);
            ly += lineHeight;
            DrawLine($"<color=#9cc>intent</color> {intent}    <color=#9cc>band</color> {band}", x, ly, boxWidth, lineHeight, labelStyle);
            ly += lineHeight;
            DrawLine($"<color=#9cc>speed</color> {speed:F0}  <color=#9cc>energy</color> {_unit.Unit?.currentEnergy:F0}", x, ly, boxWidth, lineHeight, labelStyle);
            ly += lineHeight;
            DrawLine($"<color=#9cc>exchange</color> {phaseLabel}", x, ly, boxWidth, lineHeight, labelStyle);
            ly += lineHeight;
            if (!string.IsNullOrEmpty(statusLine))
                DrawLine(statusLine, x, ly, boxWidth, lineHeight, labelStyle);
        }

        private string BuildStatusLine()
        {
            if (_status == null) return "";
            var parts = new System.Collections.Generic.List<string>(2);
            foreach (CCEffectType t in System.Enum.GetValues(typeof(CCEffectType)))
            {
                if (t == CCEffectType.None) continue;
                if (_status.Has(_unit, t))
                    parts.Add($"<color=#fc6>{t}</color>");
            }
            return parts.Count > 0 ? "CC: " + string.Join(" ", parts) : "";
        }

        private void DrawLine(string text, float x, float y, float w, float h, GUIStyle style)
        {
            // GUI.skin.label doesn't honor richText by default; reset per draw.
            var rich = new GUIStyle(style) { richText = true };
            GUI.Label(new Rect(x, y, w, h), text, rich);
        }
    }
}
