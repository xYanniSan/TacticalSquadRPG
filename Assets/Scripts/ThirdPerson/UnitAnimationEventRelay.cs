using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Sits on the same GameObject as the Animator (the character mesh/rig child).
    /// Forwards Animation Events up to TerrainBattleUnit on the parent.
    /// </summary>
    public class UnitAnimationEventRelay : MonoBehaviour
    {
        private TerrainBattleUnit _unit;

        private void Awake()
        {
            _unit = GetComponentInParent<TerrainBattleUnit>();

            if (_unit == null)
                Debug.LogWarning($"[UnitAnimationEventRelay] No TerrainBattleUnit found in parent of {gameObject.name} (path: {GetPath()})");
            else
                Debug.Log($"[UnitAnimationEventRelay] Found TerrainBattleUnit on {_unit.gameObject.name} from {gameObject.name}");
        }

        // Called by Animation Event on attack clips at the impact frame
        public void OnHitFrame()
        {
            Debug.Log($"[UnitAnimationEventRelay] OnHitFrame fired on {gameObject.name}, unit={(_unit != null ? _unit.gameObject.name : "NULL")}");
            _unit?.OnHitFrame();
        }

        // Called by Animation Event at the last frame of every attack/kick clip
        public void OnAttackEnd()
        {
            _unit?.OnAttackEnd();
        }

        // Called by Animation Event at the last frame of the block clip
        public void OnBlockEnd()
        {
            _unit?.OnBlockEnd();
        }

        // Called by Animation Event at the last frame of the hit-reaction clip
        public void OnHitEnd()
        {
            _unit?.OnHitEnd();
        }

        private string GetPath()
        {
            string path = gameObject.name;
            Transform t = transform.parent;
            while (t != null) { path = t.name + "/" + path; t = t.parent; }
            return path;
        }
    }
}
