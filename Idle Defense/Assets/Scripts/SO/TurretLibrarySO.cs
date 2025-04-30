// Assets/Scripts/SO/TurretLibrarySO.cs
using Assets.Scripts.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "IdleDefense/Turret Library")]
public class TurretLibrarySO : ScriptableObject
{
    [Serializable]
    public struct Pair
    {
        public TurretType type;      // enum key
        public TurretInfoSO info;      // original stats
        public GameObject prefab;    // prefab with BaseTurret component
    }

    public List<Pair> items;

    Dictionary<TurretType, Pair> _map;

    void OnEnable() => _map = items.ToDictionary(p => p.type, p => p);

    public TurretInfoSO GetInfo(TurretType t) => _map[t].info;
    public GameObject GetPrefab(TurretType t) => _map[t].prefab;
}
