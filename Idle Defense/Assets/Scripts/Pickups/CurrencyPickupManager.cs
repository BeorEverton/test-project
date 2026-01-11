using Assets.Scripts.Enemies;
using Assets.Scripts.Systems;
using Assets.Scripts.WaveSystem; // EnemySpawner + OnEnemyDeathEventArgs
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyPickupManager : MonoBehaviour
{
    public static CurrencyPickupManager Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private CurrencyPickup pickupPrefab;
    [SerializeField] private int initialPoolSize = 256;

    [Header("Visuals")]
    [SerializeField] private Sprite scrapsSprite;
    [SerializeField] private Sprite blackSteelSprite;
    [SerializeField] private Sprite crimsonCoreSprite;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetimeSeconds = 2.5f;
    [SerializeField] private float fadeOutDurationSeconds = 0.25f;

    [Header("Pickup Animation")]
    [SerializeField] private float travelDurationSeconds = 0.35f;
    [SerializeField] private float travelScaleUp = 1.25f;

    [Header("Grid")]
    [SerializeField] private float pickupCellSize = 1.0f;
    [SerializeField] private bool debugDrawGrid = false;
    [SerializeField] private Color debugCellColor = new Color(1f, 1f, 0f, 0.2f);
    [SerializeField] private Color debugMouseCellColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("Mouse Projection")]
    [Tooltip("Y height (world space) of the plane where the mouse is projected for hover detection, e.g. ground height.")]
    [SerializeField] private float mouseGroundY = .2f;
    [Tooltip("Extra height above ground where pickups appear and where the mouse grid lives.")]
    [SerializeField] private float pickupHeightOffset = 0.1f;

    [Header("UI Targets (world positions)")]
    [SerializeField] private Transform scrapsTargetWorld;
    [SerializeField] private Transform blackSteelTargetWorld;
    [SerializeField] private Transform crimsonCoreTargetWorld;

    private readonly List<CurrencyPickup> _active = new List<CurrencyPickup>(1024);
    private readonly Queue<CurrencyPickup> _pool = new Queue<CurrencyPickup>(256);

    // grid: cell -> list of pickups in that cell
    private readonly Dictionary<Vector2Int, List<CurrencyPickup>> _pickupGrid =
        new Dictionary<Vector2Int, List<CurrencyPickup>>();

    private Camera _cam;
    private Vector2Int _lastMouseCell;
    private bool _hasLastMouseCell;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _cam = Camera.main;
        Prewarm();
    }

    private void OnEnable()
    {
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void Prewarm()
    {
        if (pickupPrefab == null)
        {
            Debug.LogWarning("CurrencyPickupManager: Pickup prefab is not assigned.");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CurrencyPickup p = Instantiate(pickupPrefab, transform);
            p.ResetForPool();
            _pool.Enqueue(p);
        }
    }

    private CurrencyPickup GetFromPool()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        CurrencyPickup p = Instantiate(pickupPrefab, transform);
        p.ResetForPool();
        return p;
    }

    public void RecyclePickup(CurrencyPickup pickup)
    {
        if (pickup == null)
            return;

        // Remove from grid
        if (_pickupGrid.TryGetValue(pickup.GridPos, out var list))
        {
            list.Remove(pickup);
            if (list.Count == 0)
                _pickupGrid.Remove(pickup.GridPos);
        }

        _active.Remove(pickup);

        pickup.ResetForPool();
        _pool.Enqueue(pickup);
    }

    private void HandleEnemyDeath(object sender, EnemySpawner.OnEnemyDeathEventArgs e)
    {
        // Money already added by GameManager; we only spawn visual pickups.
        if (pickupPrefab == null)
            return;

        Sprite sprite = GetSpriteForCurrency(e.CurrencyType);
        if (sprite == null)
            return;

        // We need the world position of the enemy death; if you haven't yet,
        // extend OnEnemyDeathEventArgs to include WorldPosition and pass it here.
        Vector3 worldPos = GetDeathWorldPosition(sender, e);
        worldPos = new Vector3(worldPos.x, worldPos.y + pickupHeightOffset, worldPos.z); // slight offset above ground

        Vector2Int cell = GetGridPosition(worldPos);

        CurrencyPickup pickup = GetFromPool();
        
        pickup.Initialize(this, e.CurrencyType, e.Amount, worldPos, sprite, cell);

        _active.Add(pickup);

        if (!_pickupGrid.TryGetValue(cell, out var list))
        {
            list = new List<CurrencyPickup>(4);
            _pickupGrid[cell] = list;
        }
        list.Add(pickup);
    }

    private Sprite GetSpriteForCurrency(Currency currency)
    {
        switch (currency)
        {
            case Currency.Scraps:
                return scrapsSprite;
            case Currency.BlackSteel:
                return blackSteelSprite;
            case Currency.CrimsonCore:
                return crimsonCoreSprite;
            default:
                return scrapsSprite;
        }
    }

    private Transform GetTargetTransform(Currency currency)
    {
        switch (currency)
        {
            case Currency.Scraps:
                return scrapsTargetWorld;
            case Currency.BlackSteel:
                return blackSteelTargetWorld;
            case Currency.CrimsonCore:
                return crimsonCoreTargetWorld;
            default:
                return scrapsTargetWorld;
        }
    }

    private void Update()
    {
        if (_active.Count == 0)
            return;

        float dt = Time.deltaTime;
        TickLifetimes(dt);
        HandleMouseHover();
    }

    private void TickLifetimes(float dt)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            CurrencyPickup p = _active[i];
            if (p == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            if (!p.Collected)
                p.TickLifetime(dt, maxLifetimeSeconds, fadeOutDurationSeconds);
        }
    }

    private void HandleMouseHover()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null)
                return;
        }

        // Ray from camera through mouse position
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

        // Plane representing the ground (y = mouseGroundY)
        // Mouse should hit the same plane where pickups live: ground + offset
        float planeY = mouseGroundY + pickupHeightOffset;
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));


        if (!groundPlane.Raycast(ray, out float distance))
        {
            // Mouse doesn't hit the plane (shouldn't really happen), so bail out
            return;
        }

        // Actual world position under the mouse on the ground plane
        Vector3 world = ray.GetPoint(distance);

        // Use this world position for grid cell lookup (x / z)
        Vector2Int cell = GetGridPosition(world);


        if (_hasLastMouseCell && cell == _lastMouseCell)
            return;

        _lastMouseCell = cell;
        _hasLastMouseCell = true;

        if (!_pickupGrid.TryGetValue(cell, out var list))
            return;

        Transform target = null;
        // All coins in that cell share the same currency type? Not guaranteed,
        // but we can individually choose their target.
        for (int i = 0; i < list.Count; i++)
        {
            CurrencyPickup p = list[i];
            if (p == null || p.Collected)
                continue;

            target = GetTargetTransform(p.CurrencyType);
            Vector3 targetPos = (target != null) ? target.position : p.transform.position;

            p.CollectViaManager(targetPos, travelDurationSeconds, travelScaleUp, fadeOutDurationSeconds);
        }
    }

    private Vector2Int GetGridPosition(Vector3 worldPos)
    {
        // grid.x = world.x / cellSize, grid.y = Depth(z) / cellSize
        float size = Mathf.Max(0.0001f, pickupCellSize);
        int gx = Mathf.FloorToInt(worldPos.x / size);
        int gy = Mathf.FloorToInt(worldPos.z / size);
        return new Vector2Int(gx, gy);
    }

    private Vector3 GetCellCenterWorld(Vector2Int cell, float groundY)
    {
        float size = Mathf.Max(0.0001f, pickupCellSize);
        float cx = cell.x * size + size * 0.5f;
        float cz = cell.y * size + size * 0.5f;
        return new Vector3(cx, groundY, cz);
    }

    // If you extend OnEnemyDeathEventArgs with WorldPosition, just return that.
    private Vector3 GetDeathWorldPosition(object sender, EnemySpawner.OnEnemyDeathEventArgs e)
    {
        return e.WorldPosition;
    }

    public void OnPickupCollected(
    CurrencyPickup pickup,
    Vector3 targetWorldPos,
    float travelDuration,
    float scaleUp,
    float fadeOutDuration)
    {
        if (pickup == null)
            return;

        // If we're in delayed mode, add currency now
        if (GameManager.Instance != null && GameManager.Instance.AwardCurrencyOnPickup && pickup.Amount > 0)
        {
            GameManager.Instance.AddCurrency(pickup.CurrencyType, pickup.Amount);
            pickup.Amount = 0;
        }

        // Then play the travel animation towards the UI and recycle on complete
        pickup.PlayPickupPop(
            riseDistance: 0.5f,
            duration: travelDurationSeconds,
            scaleUp: travelScaleUp,
            fadeDuration: fadeOutDurationSeconds
        );

    }

    public void OnPickupExpired(CurrencyPickup pickup, float fadeOutDuration)
    {
        if (pickup == null)
            return;

        
        // Auto-collect currency on timeout if we're awarding on pickup
        if (GameManager.Instance != null && GameManager.Instance.AwardCurrencyOnPickup && pickup.Amount > 0)
        {
            GameManager.Instance.AddCurrency(pickup.CurrencyType, pickup.Amount);
            pickup.Amount = 0;
        }

        // Simple fade out, then recycle
        pickup.PlayFadeOutOnly(fadeOutDuration);
    }


    private void OnDrawGizmosSelected()
    {
        if (!debugDrawGrid)
            return;

        Gizmos.matrix = Matrix4x4.identity;

        // Draw occupied cells
        Gizmos.color = debugCellColor;
        foreach (var kvp in _pickupGrid)
        {
            Vector2Int cell = kvp.Key;
            Vector3 center = GetCellCenterWorld(cell, mouseGroundY);
            Vector3 size = new Vector3(pickupCellSize * 0.9f, 0.1f, pickupCellSize * 0.9f);
            Gizmos.DrawCube(center, size);
        }

        // Draw mouse cell
        if (_hasLastMouseCell)
        {
            Gizmos.color = debugMouseCellColor;
            Vector3 c = GetCellCenterWorld(_lastMouseCell, mouseGroundY);
            Vector3 s = new Vector3(pickupCellSize, 0.1f, pickupCellSize);
            Gizmos.DrawCube(c, s);
        }
    }
}
