using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SuperInseto
{
    public sealed class EnvironmentalPuzzle : MonoBehaviour
    {
        [SerializeField] DestructibleObject[] destroyedObjects;
        [SerializeField] BioelectricReceiver[] poweredNodes;
        [SerializeField] UnityEvent onSolved = new UnityEvent();
        [SerializeField] int conditionsMet;
        [SerializeField] int totalConditions;
        [SerializeField] bool isSolved;
        bool started, subscribed, configured;
        public bool IsSolved => isSolved;
        public int ConditionsMet => conditionsMet;
        public int TotalConditions => totalConditions;
        public event Action Solved;

        // Scene authors can instead serialize conditions and UnityEvent outputs directly in the Inspector.
        public void Configure(DestructibleObject[] destruction, BioelectricReceiver[] power, UnityAction consequence)
        {
            if (isActiveAndEnabled || started || configured)
                throw new InvalidOperationException("Configure a new puzzle once, before enabling its GameObject.");
            configured = true; destroyedObjects = destruction; poweredNodes = power;
            if (consequence != null) onSolved.AddListener(consequence);
        }
        void OnEnable() { Subscribe(); if (started) Evaluate(); }
        void Start() { started = true; Evaluate(); } // Door/source Awake methods must finish before resolving.
        void OnDisable() { Unsubscribe(); }
        void Subscribe()
        {
            if (subscribed || isSolved) return;
            subscribed = true;
            if (destroyedObjects != null) foreach (var source in destroyedObjects) if (source) source.Destroyed += Evaluate;
            if (poweredNodes != null) foreach (var source in poweredNodes) if (source) source.Powered += Evaluate;
        }
        void Unsubscribe()
        {
            if (!subscribed) return;
            subscribed = false;
            if (destroyedObjects != null) foreach (var source in destroyedObjects) if (source) source.Destroyed -= Evaluate;
            if (poweredNodes != null) foreach (var source in poweredNodes) if (source) source.Powered -= Evaluate;
        }
        void Evaluate()
        {
            if (!started || isSolved || !isActiveAndEnabled) return;
            conditionsMet = 0;
            totalConditions = (destroyedObjects?.Length ?? 0) + (poweredNodes?.Length ?? 0);
            bool valid = totalConditions > 0;
            var unique = new HashSet<UnityEngine.Object>(); // Only on condition events, never per frame.
            if (destroyedObjects != null) foreach (var source in destroyedObjects)
            {
                if (!source || !unique.Add(source)) { valid = false; continue; }
                if (source.IsDestroyed) conditionsMet++;
            }
            if (poweredNodes != null) foreach (var source in poweredNodes)
            {
                if (!source || !unique.Add(source)) { valid = false; continue; }
                if (source.IsPowered) conditionsMet++;
            }
            if (!valid || conditionsMet != totalConditions) return;
            isSolved = true; Unsubscribe(); // Remains solved throughout this Play, including disable/enable.
            onSolved.Invoke(); Solved?.Invoke();
        }
    }
}
