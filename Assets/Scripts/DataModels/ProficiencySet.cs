using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.DataModels
{
    [Serializable]
    public class ProficiencySet : ISerializationCallbackReceiver
    {
        public Dictionary<ActionType, float>    actionProficiencies    = new Dictionary<ActionType, float>();
        public Dictionary<ElementType, float>   elementProficiencies   = new Dictionary<ElementType, float>();
        public Dictionary<TechniqueType, float> techniqueProficiencies = new Dictionary<TechniqueType, float>();

        [SerializeField] private List<ActionProficiencyEntry>    _serializedActionProficiencies    = new List<ActionProficiencyEntry>();
        [SerializeField] private List<ElementProficiencyEntry>   _serializedElementProficiencies   = new List<ElementProficiencyEntry>();
        [SerializeField] private List<TechniqueProficiencyEntry> _serializedTechniqueProficiencies = new List<TechniqueProficiencyEntry>();

        [Serializable]
        private struct ActionProficiencyEntry
        {
            public ActionType type;
            public float bonus;
        }

        [Serializable]
        private struct ElementProficiencyEntry
        {
            public ElementType type;
            public float bonus;
        }

        [Serializable]
        private struct TechniqueProficiencyEntry
        {
            public TechniqueType type;
            public float bonus;
        }

        public void OnBeforeSerialize()
        {
            _serializedActionProficiencies.Clear();
            foreach (var kvp in actionProficiencies)
                _serializedActionProficiencies.Add(new ActionProficiencyEntry { type = kvp.Key, bonus = kvp.Value });

            _serializedElementProficiencies.Clear();
            foreach (var kvp in elementProficiencies)
                _serializedElementProficiencies.Add(new ElementProficiencyEntry { type = kvp.Key, bonus = kvp.Value });

            _serializedTechniqueProficiencies.Clear();
            foreach (var kvp in techniqueProficiencies)
                _serializedTechniqueProficiencies.Add(new TechniqueProficiencyEntry { type = kvp.Key, bonus = kvp.Value });
        }

        public void OnAfterDeserialize()
        {
            actionProficiencies.Clear();
            foreach (var entry in _serializedActionProficiencies)
                actionProficiencies[entry.type] = entry.bonus;

            elementProficiencies.Clear();
            foreach (var entry in _serializedElementProficiencies)
                elementProficiencies[entry.type] = entry.bonus;

            techniqueProficiencies.Clear();
            foreach (var entry in _serializedTechniqueProficiencies)
                techniqueProficiencies[entry.type] = entry.bonus;
        }

        public float GetProficiencyBonus(ActionType action)
        {
            return actionProficiencies.TryGetValue(action, out float bonus) ? bonus : 1.0f;
        }

        public float GetProficiencyBonus(ElementType element)
        {
            return elementProficiencies.TryGetValue(element, out float bonus) ? bonus : 1.0f;
        }

        public float GetProficiencyBonus(TechniqueType technique)
        {
            return techniqueProficiencies.TryGetValue(technique, out float bonus) ? bonus : 1.0f;
        }
    }
}
