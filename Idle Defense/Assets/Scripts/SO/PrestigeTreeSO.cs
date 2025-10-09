using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "IdleDefense/Prestige/Tree")]
public class PrestigeTreeSO : ScriptableObject
{
    public string TreeId;                  // e.g. "BaseMachine_Default"
    public List<PrestigeNodeSO> Nodes;     // author all branches here
}
