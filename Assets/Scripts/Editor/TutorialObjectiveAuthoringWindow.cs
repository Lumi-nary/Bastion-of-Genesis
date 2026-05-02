using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TutorialObjectiveAuthoringWindow : EditorWindow
{
    private MissionData missionData;
    private int objectiveIndex;
    private WorkerData requiredWorker;
    private TechnologyData requiredTechnology;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Tutorial Objective Authoring")]
    private static void Open()
    {
        GetWindow<TutorialObjectiveAuthoringWindow>("Tutorial Objective Authoring");
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Tutorial Objective Authoring", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select or place a building in the scene, choose a MissionData objective, then capture the current building data and grid footprint into the objective.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        missionData = (MissionData)EditorGUILayout.ObjectField("Mission Data", missionData, typeof(MissionData), false);
        if (EditorGUI.EndChangeCheck())
        {
            objectiveIndex = 0;
        }

        if (missionData == null)
        {
            EditorGUILayout.HelpBox("Assign a MissionData asset from Assets/Resources/Data/Campaign/Missions.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create/Refresh Runtime Managers"))
            {
                TutorialGuideManager.EnsureRuntimeObjects();
                TutorialGuideManager.Instance?.RefreshActiveObjective();
                TutorialHologramManager.Instance?.Refresh();
            }

            EditorGUILayout.LabelField(Application.isPlaying ? "Play Mode" : "Edit Mode", EditorStyles.miniLabel, GUILayout.Width(80f));
        }

        if (missionData.objectives == null || missionData.objectives.Count == 0)
        {
            EditorGUILayout.HelpBox("This mission has no objectives to edit.", MessageType.Warning);
            return;
        }

        objectiveIndex = Mathf.Clamp(objectiveIndex, 0, missionData.objectives.Count - 1);
        objectiveIndex = EditorGUILayout.Popup("Objective", objectiveIndex, GetObjectiveLabels());

        MissionObjective objective = missionData.objectives[objectiveIndex];
        DrawObjectiveSummary(objective);

        EditorGUILayout.Space(8);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawSelectedBuildingCapture(objective);
        EditorGUILayout.Space(8);
        DrawResearchCapture(objective);
        EditorGUILayout.Space(8);
        DrawManualUtility(objective);

        EditorGUILayout.EndScrollView();
    }

    private string[] GetObjectiveLabels()
    {
        string[] labels = new string[missionData.objectives.Count];
        for (int i = 0; i < missionData.objectives.Count; i++)
        {
            MissionObjective objective = missionData.objectives[i];
            string description = string.IsNullOrWhiteSpace(objective.objectiveDescription)
                ? objective.type.ToString()
                : objective.objectiveDescription;
            labels[i] = $"{i}: {description}";
        }

        return labels;
    }

    private void DrawObjectiveSummary(MissionObjective objective)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Current Objective", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Type", objective.type.ToString());
        EditorGUILayout.LabelField("Tutorial Step", objective.isTutorialStep ? "Yes" : "No");
        EditorGUILayout.LabelField("Required Building", objective.requiredBuilding != null ? objective.requiredBuilding.buildingName : "None");
        EditorGUILayout.LabelField("Allowed Cells", objective.allowedPlacementCells != null ? objective.allowedPlacementCells.Count.ToString() : "0");
        EditorGUILayout.LabelField("Assignment Building", objective.requiredAssignmentBuilding != null ? objective.requiredAssignmentBuilding.buildingName : "None");
        EditorGUILayout.LabelField("Required Technology", objective.requiredTechnology != null ? objective.requiredTechnology.techName : "None");
    }

    private void DrawSelectedBuildingCapture(MissionObjective objective)
    {
        EditorGUILayout.LabelField("Capture From Selected Building", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use one objective for the build step and a separate objective for the worker assignment step. Assignment captures change the objective to AssignWorkers, so they will not show build holograms.", MessageType.Info);

        Building selectedBuilding = GetSelectedBuilding();
        if (selectedBuilding == null)
        {
            EditorGUILayout.HelpBox("Select a scene GameObject with a Building component. This works in Edit Mode or Play Mode.", MessageType.Info);
            return;
        }

        BuildingData buildingData = selectedBuilding.BuildingData;
        int width = GetBuildingWidth(selectedBuilding);
        int height = GetBuildingHeight(selectedBuilding);
        Vector2Int startCell = GetBuildingStartCell(selectedBuilding, width, height);

        EditorGUILayout.LabelField("Selected", selectedBuilding.name);
        EditorGUILayout.LabelField("Building Data", buildingData != null ? buildingData.buildingName : "Missing");
        EditorGUILayout.LabelField("Start Cell", startCell.ToString());
        EditorGUILayout.LabelField("Footprint", $"{width} x {height}");

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = buildingData != null;
            if (GUILayout.Button("Capture Build Step"))
            {
                CaptureBuildStep(objective, buildingData, startCell, width, height);
            }

            if (GUILayout.Button("Capture Assignment Target"))
            {
                CaptureAssignmentTarget(objective, buildingData, changeObjectiveType: false);
            }
            GUI.enabled = true;
        }

        requiredWorker = (WorkerData)EditorGUILayout.ObjectField("Required Worker", requiredWorker, typeof(WorkerData), false);
        GUI.enabled = buildingData != null && requiredWorker != null;
        if (GUILayout.Button("Capture Assignment Step With Worker"))
        {
            CaptureAssignmentTarget(objective, buildingData, changeObjectiveType: true);
            objective.requiredWorker = requiredWorker;
            SaveMission();
        }
        GUI.enabled = true;
    }

    private void DrawResearchCapture(MissionObjective objective)
    {
        EditorGUILayout.LabelField("Capture Research Step", EditorStyles.boldLabel);
        requiredTechnology = (TechnologyData)EditorGUILayout.ObjectField("Required Technology", requiredTechnology, typeof(TechnologyData), false);

        GUI.enabled = requiredTechnology != null;
        if (GUILayout.Button("Capture Research Step"))
        {
            Undo.RecordObject(missionData, "Capture Tutorial Research Step");
            objective.isTutorialStep = true;
            objective.gateMode = TutorialGateMode.HardGate;
            objective.type = ObjectiveType.ResearchTechnology;
            objective.requiredTechnology = requiredTechnology;
            if (objective.targetAmount <= 0)
                objective.targetAmount = 1;
            SaveMission();
        }
        GUI.enabled = true;
    }

    private void DrawManualUtility(MissionObjective objective)
    {
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear Allowed Placement Cells"))
        {
            Undo.RecordObject(missionData, "Clear Tutorial Placement Cells");
            objective.allowedPlacementCells?.Clear();
            SaveMission();
        }
    }

    private void CaptureBuildStep(MissionObjective objective, BuildingData buildingData, Vector2Int startCell, int width, int height)
    {
        Undo.RecordObject(missionData, "Capture Tutorial Build Step");
        objective.isTutorialStep = true;
        objective.gateMode = TutorialGateMode.HardGate;
        objective.type = ObjectiveType.BuildStructures;
        objective.requiredBuilding = buildingData;
        if (objective.targetAmount <= 0)
            objective.targetAmount = 1;

        objective.allowedPlacementCells ??= new List<Vector2Int>();
        objective.allowedPlacementCells.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                objective.allowedPlacementCells.Add(new Vector2Int(startCell.x + x, startCell.y + y));
            }
        }

        SaveMission();
    }

    private void CaptureAssignmentTarget(MissionObjective objective, BuildingData buildingData, bool changeObjectiveType)
    {
        Undo.RecordObject(missionData, "Capture Tutorial Assignment Target");
        objective.isTutorialStep = true;
        objective.gateMode = TutorialGateMode.HardGate;
        if (changeObjectiveType)
            objective.type = ObjectiveType.AssignWorkers;
        objective.requiredAssignmentBuilding = buildingData;
        if (objective.targetAmount <= 0)
            objective.targetAmount = 1;
        SaveMission();
    }

    private void SaveMission()
    {
        EditorUtility.SetDirty(missionData);
        AssetDatabase.SaveAssets();
        Repaint();
    }

    private static Building GetSelectedBuilding()
    {
        GameObject selected = Selection.activeGameObject;
        return selected != null ? selected.GetComponentInParent<Building>() : null;
    }

    private static int GetBuildingWidth(Building building)
    {
        if (building.width > 0)
            return building.width;
        return building.BuildingData != null ? Mathf.Max(1, building.BuildingData.width) : 1;
    }

    private static int GetBuildingHeight(Building building)
    {
        if (building.height > 0)
            return building.height;
        return building.BuildingData != null ? Mathf.Max(1, building.BuildingData.height) : 1;
    }

    private static Vector2Int GetBuildingStartCell(Building building, int width, int height)
    {
        if (building.gridPosition != Vector2Int.zero)
            return building.gridPosition;

        if (GridManager.Instance == null)
            return Vector2Int.zero;

        Vector2Int centerCell = GridManager.Instance.WorldToGridPosition(building.transform.position);
        return new Vector2Int(centerCell.x - width / 2, centerCell.y - height / 2);
    }
}
