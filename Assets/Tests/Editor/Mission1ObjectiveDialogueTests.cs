using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class Mission1ObjectiveDialogueTests
{
    [Test]
    public void Mission1_ObjectiveDialogues_AreNexusOnly()
    {
        MissionData mission = AssetDatabase.LoadAssetAtPath<MissionData>(
            "Assets/Resources/Data/Campaign/Missions/Mission1.asset");

        Assert.IsNotNull(mission);
        Assert.GreaterOrEqual(mission.objectives.Count, 3);

        AssertObjectiveDialogueIsNexusOnly(mission.objectives[0], 6);
        AssertObjectiveDialogueIsNexusOnly(mission.objectives[1], 4);
        AssertObjectiveDialogueIsNexusOnly(mission.objectives[2], 2);
    }

    [Test]
    public void ObjectiveDialogueBlockingState_RaisesEventsAndClears()
    {
        GameObject managerObject = new GameObject("MissionChapterManager_ObjectiveDialogue_Test");
        MissionChapterManager manager = managerObject.AddComponent<MissionChapterManager>();
        DialogueData dialogue = ScriptableObject.CreateInstance<DialogueData>();

        try
        {
            int changes = 0;
            manager.OnObjectiveDialogueStateChanged += () => changes++;

            InvokePrivate(manager, "SetObjectiveDialogueBlock", dialogue, true);

            Assert.IsTrue(manager.IsObjectiveDialogueBlockingTutorial);
            Assert.AreEqual(1, changes);

            InvokePrivate(manager, "ClearObjectiveDialogueBlock");

            Assert.IsFalse(manager.IsObjectiveDialogueBlockingTutorial);
            Assert.AreEqual(2, changes);
        }
        finally
        {
            Object.DestroyImmediate(dialogue);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void TutorialInstructionPanelPrefab_IsEditableAndBottomLeftAnchored()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Prefabs/UI/TutorialInstructionPanel.prefab");

        Assert.IsNotNull(prefab);

        RectTransform rectTransform = prefab.GetComponent<RectTransform>();
        Assert.IsNotNull(rectTransform);
        Assert.AreEqual(Vector2.zero, rectTransform.anchorMin);
        Assert.AreEqual(Vector2.zero, rectTransform.anchorMax);
        Assert.AreEqual(Vector2.zero, rectTransform.pivot);
        Assert.AreEqual(new Vector2(50f, 50f), rectTransform.anchoredPosition);
        Assert.IsNotNull(prefab.GetComponentInChildren<TMPro.TextMeshProUGUI>(true));
    }

    private static void AssertObjectiveDialogueIsNexusOnly(MissionObjective objective, int expectedEntries)
    {
        Assert.IsNotNull(objective.objectiveDialogue);
        Assert.AreEqual(expectedEntries, objective.objectiveDialogue.EntryCount);

        foreach (DialogueEntry entry in objective.objectiveDialogue.entries)
        {
            Assert.AreEqual("Nexus", entry.speakerName);
            Assert.IsFalse(entry.speakerName.Contains("Kyra"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.dialogueText));
        }
    }

    private static void InvokePrivate(MissionChapterManager manager, string methodName, params object[] arguments)
    {
        typeof(MissionChapterManager)
            .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(manager, arguments);
    }
}
