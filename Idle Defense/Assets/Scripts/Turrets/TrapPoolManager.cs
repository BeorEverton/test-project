using Assets.Scripts.Turrets;
using System.Collections.Generic;
using UnityEngine;

public class TrapPoolManager : MonoBehaviour
{
    public static TrapPoolManager Instance;

    private List<Trap> traps = new List<Trap>();

    public bool hasAnyTrapActive => traps.Exists(trap => trap.isActive);

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Initializes the trap pool for a specific turret setup.
    /// Should be called by the turret when it spawns.
    /// </summary>
    public void InitializePool(GameObject trapPrefab, int poolSize)
    {
        

        // Non-destructive: only top up if we don't have enough entries
        int need = Mathf.Max(0, poolSize - traps.Count);
        for (int i = 0; i < need; i++)
        {
            var obj = Instantiate(trapPrefab, transform);
            obj.SetActive(false);
            traps.Add(new Trap { visual = obj, isActive = false });
        }
    }


    public Trap PlaceTrap(Vector3 worldPos, Vector2Int cell, float damage, float delay,
        float radius, BaseTurret owner, float cellWorldY)
    {
        
        foreach (var trap in traps)
        {
            
            if (!trap.isActive)
            {
                

                // --- ADD: upgrade to LB trap if owner is null ---
                Trap chosen = trap;
                if (owner == null && !(trap is LimitBreakTrap))
                {
                    // Swap the list entry with a LimitBreakTrap, preserving the visual
                    int idx = traps.IndexOf(trap);
                    var lb = new LimitBreakTrap();
                    lb.visual = trap.visual;
                    traps[idx] = lb;
                    chosen = lb;
                }
                // -----------------------------------------------

                chosen.visual.transform.position = worldPos;
                chosen.visual.SetActive(true);
                chosen.cell = cell;
                chosen.damage = damage;
                chosen.delay = delay;
                chosen.radius = radius;
                chosen.isActive = true;
                if (owner)
                    chosen.ownerTurret = owner;
                chosen.worldY = cellWorldY;
                return chosen;
            }

        }
        return null;
    }


    public Trap GetTrapAtCell(Vector2Int cell)
    {
        foreach (var trap in traps)
        {
            if (trap.isActive && trap.cell == cell)
                return trap;
        }
        return null;
    }

    public void DisableTrap(Trap trap)
    {
        trap.isActive = false;
        trap.visual.SetActive(false);
    }
}
