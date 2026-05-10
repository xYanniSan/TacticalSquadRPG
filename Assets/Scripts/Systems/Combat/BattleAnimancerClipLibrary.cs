using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Maps logical animation roles (idle / walk / punch / ...) to Animancer
    /// TransitionAssets. Lets gameplay code request "play 'punch'" without
    /// caring which underlying clip is wired in.
    ///
    /// Used by KuboldClipTester for the Phase-2 retargeting bring-up and
    /// later by BattleAnimancerDriver as the canonical source of clips when
    /// move-engine moves call PlayClip(name).
    /// </summary>
    [CreateAssetMenu(menuName = "TacticalRPG/Combat/Animancer Clip Library",
                     fileName = "BattleAnimancerClipLibrary")]
    public class BattleAnimancerClipLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            [Tooltip("Logical role — 'idle', 'walk_forward', 'punch', etc.")]
            public string id;

            [Tooltip("Animancer transition that plays this role.")]
            public TransitionAsset transition;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<string, TransitionAsset> _lookup;

        public IReadOnlyList<Entry> Entries => _entries;

        public TransitionAsset Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureLookup();
            _lookup.TryGetValue(id, out var asset);
            return asset;
        }

        public bool TryGet(string id, out TransitionAsset asset)
        {
            asset = Get(id);
            return asset != null;
        }

        public void Set(string id, TransitionAsset asset)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].id == id)
                {
                    var e = _entries[i];
                    e.transition = asset;
                    _entries[i] = e;
                    _lookup = null;
                    return;
                }
            }
            _entries.Add(new Entry { id = id, transition = asset });
            _lookup = null;
        }

        private void EnsureLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<string, TransitionAsset>(_entries.Count);
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (!string.IsNullOrEmpty(e.id) && e.transition != null)
                    _lookup[e.id] = e.transition;
            }
        }

        private void OnValidate() { _lookup = null; }
    }
}
