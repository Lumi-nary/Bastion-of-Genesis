using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialGuideManager : MonoBehaviour
{
    public static TutorialGuideManager Instance { get; private set; }

    public event Action<MissionObjective> OnTutorialObjectiveChanged;

    public MissionObjective ActiveObjective { get; private set; }
    public static bool IsTutorialEnabled => SettingsManager.Instance == null ||
        SettingsManager.Instance.CurrentSettings == null ||
        SettingsManager.Instance.CurrentSettings.tutorialEnabled;
    public bool HasActiveHardGate => IsTutorialEnabled && !IsTutorialGuidanceBlocked && ActiveObjective != null && ActiveObjective.isTutorialStep && ActiveObjective.gateMode == TutorialGateMode.HardGate;
    public string CurrentInstruction => IsTutorialEnabled && !IsTutorialGuidanceBlocked && ActiveObjective != null ? ActiveObjective.tutorialInstruction : string.Empty;
    public TutorialTargetPanel CurrentTargetPanel => HasActiveHardGate ? ActiveObjective.targetPanel : TutorialTargetPanel.None;
    public TutorialTargetAction CurrentTargetAction => HasActiveHardGate ? ActiveObjective.targetAction : TutorialTargetAction.None;
    public bool HasCameraLock => IsTutorialEnabled && !IsTutorialGuidanceBlocked && ActiveObjective != null && ActiveObjective.isTutorialStep && ActiveObjective.focusCameraOnTarget;
    public float CurrentCameraZoom => HasCameraLock ? ActiveObjective.tutorialCameraZoom : 0f;
    public bool IsTutorialGuidanceBlocked => MissionChapterManager.Instance != null &&
        (!MissionChapterManager.Instance.IsMissionActive ||
        MissionChapterManager.Instance.IsObjectiveDialogueBlockingTutorial);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureRuntimeObjectsBeforeSceneLoad()
    {
        EnsureRuntimeObjects();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeObjectsAfterSceneLoad()
    {
        EnsureRuntimeObjects();
    }

    public static void EnsureRuntimeObjects()
    {
        if (Instance == null)
        {
            GameObject guideObject = new GameObject("TutorialGuideManager");
            guideObject.AddComponent<TutorialGuideManager>();
        }

        if (TutorialHologramManager.Instance == null)
        {
            GameObject hologramObject = new GameObject("TutorialHologramManager");
            hologramObject.AddComponent<TutorialHologramManager>();
        }

        if (CanShowTutorialOverlay() && FindFirstObjectByType<TutorialOverlayUI>() == null)
        {
            GameObject overlayObject = new GameObject("TutorialOverlayUI");
            overlayObject.AddComponent<TutorialOverlayUI>();
        }
    }

    public static bool CanShowTutorialOverlay()
    {
        return IsTutorialEnabled &&
            MissionChapterManager.Instance != null &&
            MissionChapterManager.Instance.CurrentMission != null;
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
    }

    private void Start()
    {
        Subscribe();
        RefreshActiveObjective();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Subscribe()
    {
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionStarted -= OnMissionStarted;
            MissionChapterManager.Instance.OnMissionStarted += OnMissionStarted;
            MissionChapterManager.Instance.OnObjectiveCompleted -= OnObjectiveCompleted;
            MissionChapterManager.Instance.OnObjectiveCompleted += OnObjectiveCompleted;
            MissionChapterManager.Instance.OnObjectiveDialogueStateChanged -= OnObjectiveDialogueStateChanged;
            MissionChapterManager.Instance.OnObjectiveDialogueStateChanged += OnObjectiveDialogueStateChanged;
        }

        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingPlaced -= OnBuildingPlaced;
            BuildingManager.Instance.OnBuildingPlaced += OnBuildingPlaced;
        }

        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnTechResearched -= OnTechResearched;
            ResearchManager.Instance.OnTechResearched += OnTechResearched;
        }

        Building.OnAnyWorkerAssigned -= OnWorkerAssigned;
        Building.OnAnyWorkerAssigned += OnWorkerAssigned;
    }

    private void Unsubscribe()
    {
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionStarted -= OnMissionStarted;
            MissionChapterManager.Instance.OnObjectiveCompleted -= OnObjectiveCompleted;
            MissionChapterManager.Instance.OnObjectiveDialogueStateChanged -= OnObjectiveDialogueStateChanged;
        }

        if (BuildingManager.Instance != null)
            BuildingManager.Instance.OnBuildingPlaced -= OnBuildingPlaced;

        if (ResearchManager.Instance != null)
            ResearchManager.Instance.OnTechResearched -= OnTechResearched;

        Building.OnAnyWorkerAssigned -= OnWorkerAssigned;
    }

    private void OnMissionStarted(MissionData mission)
    {
        RefreshActiveObjective();
    }

    private void OnObjectiveCompleted(MissionObjective objective)
    {
        RefreshActiveObjective();
    }

    private void OnObjectiveDialogueStateChanged()
    {
        RefreshActiveObjective();
    }

    private void OnBuildingPlaced(Building building)
    {
        RefreshActiveObjective();
    }

    private void OnWorkerAssigned(Building building, WorkerData workerData)
    {
        RefreshActiveObjective();
    }

    private void OnTechResearched(TechnologyData tech)
    {
        RefreshActiveObjective();
    }

    public void RefreshActiveObjective()
    {
        MissionObjective nextObjective = IsTutorialEnabled && !IsTutorialGuidanceBlocked ? FindActiveTutorialObjective() : null;
        if (ActiveObjective == nextObjective)
            return;

        ActiveObjective = nextObjective;
        OnTutorialObjectiveChanged?.Invoke(ActiveObjective);
    }

    public void DisableTutorial()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.CurrentSettings != null)
        {
            SettingsData updatedSettings = SettingsManager.Instance.CurrentSettings.Clone();
            updatedSettings.tutorialEnabled = false;
            SettingsManager.Instance.UpdateSettings(updatedSettings);
            SettingsManager.Instance.SaveSettings();
        }

        RefreshActiveObjective();
    }

    private MissionObjective FindActiveTutorialObjective()
    {
        MissionData mission = MissionChapterManager.Instance != null ? MissionChapterManager.Instance.CurrentMission : null;
        return FindActiveTutorialObjective(mission);
    }

    public static MissionObjective FindActiveTutorialObjective(MissionData mission)
    {
        if (!IsTutorialEnabled)
            return null;

        if (mission == null || mission.objectives == null)
            return null;

        foreach (MissionObjective objective in mission.objectives)
        {
            if (objective == null || objective.isCompleted || objective.isOptional)
                continue;

            return objective.isTutorialStep ? objective : null;
        }

        return null;
    }

    public bool CanSelectBuilding(BuildingData buildingData)
    {
        if (!HasActiveHardGate)
            return true;

        if (ActiveObjective.type != ObjectiveType.BuildStructures)
            return true;

        return ActiveObjective.requiredBuilding == null || ActiveObjective.requiredBuilding == buildingData;
    }

    public bool CanOpenPanel(UIManager.PanelKind panelKind)
    {
        if (!HasActiveHardGate || ActiveObjective.targetPanel == TutorialTargetPanel.None)
            return true;

        return ActiveObjective.targetPanel == ToTutorialTargetPanel(panelKind);
    }

    public bool IsTargetPanel(UIManager.PanelKind panelKind)
    {
        return HasActiveHardGate &&
            ActiveObjective.targetPanel != TutorialTargetPanel.None &&
            ActiveObjective.targetPanel == ToTutorialTargetPanel(panelKind);
    }

    public bool IsTargetAction(TutorialTargetAction action)
    {
        return HasActiveHardGate && ActiveObjective.targetAction == action;
    }

    public bool CanPlaceBuilding(BuildingData buildingData, Vector2Int startCell, int width, int height)
    {
        if (!CanSelectBuilding(buildingData))
            return false;

        if (!HasActiveHardGate || ActiveObjective.type != ObjectiveType.BuildStructures)
            return true;

        List<Vector2Int> allowedCells = ActiveObjective.allowedPlacementCells;
        if (allowedCells == null || allowedCells.Count == 0)
            return true;

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

    public bool CanAssignWorker(Building building, WorkerData workerData)
    {
        if (!HasActiveHardGate || ActiveObjective.type != ObjectiveType.AssignWorkers)
            return true;

        if (ActiveObjective.requiredWorker != null && ActiveObjective.requiredWorker != workerData)
            return false;

        if (ActiveObjective.requiredAssignmentBuilding != null)
            return building != null && building.BuildingData == ActiveObjective.requiredAssignmentBuilding;

        return true;
    }

    public bool CanAssembleWorker(WorkerData workerData)
    {
        if (!HasActiveHardGate || ActiveObjective.type != ObjectiveType.AssignWorkers)
            return true;

        if (ActiveObjective.requiredAssignmentBuilding != null)
            return true;

        return ActiveObjective.requiredWorker == null || ActiveObjective.requiredWorker == workerData;
    }

    public bool CanResearchTechnology(TechnologyData technologyData)
    {
        if (!HasActiveHardGate || ActiveObjective.type != ObjectiveType.ResearchTechnology)
            return true;

        if (ActiveObjective.requiredTechnologies != null && ActiveObjective.requiredTechnologies.Count > 0)
            return technologyData != null && ActiveObjective.requiredTechnologies.Contains(technologyData);

        return ActiveObjective.requiredTechnology == null || ActiveObjective.requiredTechnology == technologyData;
    }

    public bool TryGetCameraFocusWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (!HasCameraLock)
            return false;

        MissionObjective objective = ActiveObjective;

        if (objective.hasFocusWorldCell && GridManager.Instance != null)
        {
            worldPosition = GridManager.Instance.GridToWorldPosition(objective.focusWorldCell);
            return true;
        }

        if (objective.type == ObjectiveType.BuildStructures &&
            objective.allowedPlacementCells != null &&
            objective.allowedPlacementCells.Count > 0 &&
            GridManager.Instance != null)
        {
            worldPosition = GridManager.Instance.GridToWorldPosition(objective.allowedPlacementCells[0]);
            return true;
        }

        if (objective.type == ObjectiveType.AssignWorkers &&
            objective.requiredAssignmentBuilding != null &&
            BuildingManager.Instance != null)
        {
            List<Building> buildings = BuildingManager.Instance.GetBuildingsByType(objective.requiredAssignmentBuilding);
            foreach (Building building in buildings)
            {
                if (building != null)
                {
                    worldPosition = building.transform.position;
                    return true;
                }
            }
        }

        return false;
    }

    public static TutorialTargetPanel ToTutorialTargetPanel(UIManager.PanelKind panelKind)
    {
        return panelKind switch
        {
            UIManager.PanelKind.BuildingSelection => TutorialTargetPanel.BuildingList,
            UIManager.PanelKind.WorkerAssembly => TutorialTargetPanel.WorkerAssembly,
            UIManager.PanelKind.WorkerAssign => TutorialTargetPanel.WorkerAssignment,
            UIManager.PanelKind.Research => TutorialTargetPanel.Research,
            UIManager.PanelKind.Mission => TutorialTargetPanel.Mission,
            _ => TutorialTargetPanel.None
        };
    }
}
