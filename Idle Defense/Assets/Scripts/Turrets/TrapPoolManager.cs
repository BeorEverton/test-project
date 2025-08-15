using Assets.Scripts.Turrets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;

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
        Debug.Log($"Initializing trap pool with size {poolSize} for turret {gameObject.name}");
        // Clear any existing pool
        foreach (var trap in traps)
        {
            if (trap.visual != null)
                Destroy(trap.visual);
        }
        traps.Clear();

        // Create fresh pool for this turret
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(trapPrefab, transform);
            obj.SetActive(false);
            traps.Add(new Trap { visual = obj, isActive = false });
        }
    }

    public Trap PlaceTrap(Vector3 worldPos, Vector2Int cell, float damage, float delay, 
        float radius, BaseTurret owner, float cellWorldY )
    {
        foreach (var trap in traps)
        {
            if (!trap.isActive)
            {
                trap.visual.transform.position = worldPos;
                trap.visual.SetActive(true);
                trap.cell = cell;
                trap.damage = damage;
                trap.delay = delay;
                trap.radius = radius;
                trap.isActive = true;
                trap.ownerTurret = owner;
                trap.worldY = cellWorldY;
                return trap;
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
