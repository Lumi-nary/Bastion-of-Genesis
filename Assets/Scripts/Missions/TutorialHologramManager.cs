using System.Collections.Generic;
using UnityEngine;

public class TutorialHologramManager : MonoBehaviour
{
    public static TutorialHologramManager Instance { get; private set; }

    [Header("Building Hologram")]
    [SerializeField] private bool showBuildingHologram = true;
    [SerializeField] private Color hologramColor = new Color(0.25f, 0.85f, 1f, 0.45f);
    [SerializeField] private int hologramSortingOrderOffset = 20;

    [Header("Pulse")]
    [SerializeField] private bool pulseHologram = true;
    [SerializeField] private float pulseScaleAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 2.5f;

    private readonly List<HologramInstance> activeHolograms = new List<HologramInstance>();
    private MissionObjective shownObjective;

    private class HologramInstance
    {
        public GameObject gameObject;
        public Transform transform;
        public Vector3 baseScale;
        public float phase;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        Subscribe();
        Refresh();
    }

    private void Update()
    {
        if (!pulseHologram)
            return;

        float time = Time.unscaledTime * pulseSpeed;
        foreach (HologramInstance hologram in activeHolograms)
        {
            if (hologram?.transform == null)
                continue;

            float scale = 1f + Mathf.Sin(time + hologram.phase) * pulseScaleAmount;
            hologram.transform.localScale = hologram.baseScale * scale;
        }
    }

    private void OnDisable()
    {
        if (TutorialGuideManager.Instance != null)
            TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;

        ClearHolograms();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Subscribe()
    {
        if (TutorialGuideManager.Instance == null)
            return;

        TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;
        TutorialGuideManager.Instance.OnTutorialObjectiveChanged += OnTutorialObjectiveChanged;
    }

    private void OnTutorialObjectiveChanged(MissionObjective objective)
    {
        Refresh();
    }

    public void Refresh()
    {
        MissionObjective objective = TutorialGuideManager.Instance != null ? TutorialGuideManager.Instance.ActiveObjective : null;
        if (shownObjective == objective && activeHolograms.Count > 0)
            return;

        shownObjective = objective;
        RebuildHolograms(objective);
    }

    private void RebuildHolograms(MissionObjective objective)
    {
        ClearHolograms();

        if (!showBuildingHologram || objective == null || GridManager.Instance == null)
            return;

        if (objective.type != ObjectiveType.BuildStructures)
            return;

        if (objective.requiredBuilding == null || objective.requiredBuilding.prefab == null)
            return;

        foreach (Vector2Int startCell in GetValidHologramStartCells(objective))
        {
            Vector2Int centerCell = new Vector2Int(
                startCell.x + objective.requiredBuilding.width / 2,
                startCell.y + objective.requiredBuilding.height / 2);

            GameObject hologram = Instantiate(objective.requiredBuilding.prefab, transform);
            hologram.name = $"{objective.requiredBuilding.buildingName}_TutorialHologram";
            hologram.transform.position = GridManager.Instance.GridToWorldPosition(centerCell);
            PrepareHologram(hologram);
            activeHolograms.Add(new HologramInstance
            {
                gameObject = hologram,
                transform = hologram.transform,
                baseScale = hologram.transform.localScale,
                phase = activeHolograms.Count * 0.75f
            });
        }
    }

    private List<Vector2Int> GetValidHologramStartCells(MissionObjective objective)
    {
        List<Vector2Int> startCells = new List<Vector2Int>();
        if (objective.allowedPlacementCells == null || objective.allowedPlacementCells.Count == 0 || objective.requiredBuilding == null)
            return startCells;

        foreach (Vector2Int candidate in objective.allowedPlacementCells)
        {
            if (FootprintIsAllowed(candidate, objective.requiredBuilding.width, objective.requiredBuilding.height, objective.allowedPlacementCells))
                startCells.Add(candidate);
        }

        return startCells;
    }

    private bool FootprintIsAllowed(Vector2Int startCell, int width, int height, List<Vector2Int> allowedCells)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!allowedCells.Contains(new Vector2Int(startCell.x + x, startCell.y + y)))
                    return false;
            }
        }

        return true;
    }

    private void PrepareHologram(GameObject hologram)
    {
        foreach (Collider2D collider in hologram.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;

        foreach (MonoBehaviour behaviour in hologram.GetComponentsInChildren<MonoBehaviour>())
            behaviour.enabled = false;

        foreach (SpriteRenderer renderer in hologram.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.color = hologramColor;
            renderer.sortingOrder += hologramSortingOrderOffset;
        }
    }

    private void ClearHolograms()
    {
        foreach (HologramInstance hologram in activeHolograms)
        {
            if (hologram?.gameObject != null)
                Destroy(hologram.gameObject);
        }

        activeHolograms.Clear();
    }
}
