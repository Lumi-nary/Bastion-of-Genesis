using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuCharacterDialogueControllerTests
{
    private GameObject controllerObject;
    private GameObject dialogueManagerObject;
    private MainMenuCharacterDialogueController controller;
    private DialogueData finalDialogue;
    private Texture2D expressionTexture;
    private Sprite expressionSprite;

    [SetUp]
    public void SetUp()
    {
        controllerObject = new GameObject("MainMenuCharacterDialogueControllerTest");
        controller = controllerObject.AddComponent<MainMenuCharacterDialogueController>();

        dialogueManagerObject = new GameObject("DialogueManagerTest");
        DialogueManager dialogueManager = dialogueManagerObject.AddComponent<DialogueManager>();
        SetDialogueManagerInstance(dialogueManager);

        finalDialogue = ScriptableObject.CreateInstance<DialogueData>();
        finalDialogue.dialogueName = "FinalQuitTest";
        finalDialogue.entries.Add(new DialogueEntry
        {
            speakerName = "Kyra-Dominia",
            dialogueText = "Final warning."
        });

        expressionTexture = new Texture2D(1, 1);
        expressionSprite = Sprite.Create(expressionTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(dialogueManagerObject);
        Object.DestroyImmediate(finalDialogue);
        Object.DestroyImmediate(expressionSprite);
        Object.DestroyImmediate(expressionTexture);
        SetDialogueManagerInstance(null);
    }

    [Test]
    public void SelectCharacterForLoad_NewPlayer_NeverRollsNexus()
    {
        Assert.AreEqual(MainMenuCharacter.Kyra, MainMenuCharacterDialogueController.SelectCharacterForLoad(false, 0f));
        Assert.AreEqual(MainMenuCharacter.Kyra, MainMenuCharacterDialogueController.SelectCharacterForLoad(false, 0.19f));
        Assert.AreEqual(MainMenuCharacter.Kyra, MainMenuCharacterDialogueController.SelectCharacterForLoad(false, 1f));
    }

    [Test]
    public void SelectCharacterForLoad_ReturningPlayer_UsesConfiguredChance()
    {
        Assert.AreEqual(MainMenuCharacter.Nexus, MainMenuCharacterDialogueController.SelectCharacterForLoad(true, 0.19f, 0.2f));
        Assert.AreEqual(MainMenuCharacter.Kyra, MainMenuCharacterDialogueController.SelectCharacterForLoad(true, 0.2f, 0.2f));
        Assert.AreEqual(MainMenuCharacter.Kyra, MainMenuCharacterDialogueController.SelectCharacterForLoad(true, 0.99f, 0.2f));
    }

    [Test]
    public void SelectCharacterForLoad_ReturningPlayer_DefaultChanceIsFortyPercent()
    {
        Assert.AreEqual(MainMenuCharacter.Nexus, MainMenuCharacterDialogueController.SelectCharacterForLoad(true, 0.39f));
        Assert.AreEqual(MainMenuCharacter.Kyra, MainMenuCharacterDialogueController.SelectCharacterForLoad(true, 0.4f));
    }

    [Test]
    public void SensitiveTouchCounter_TriggersQuitOnlyOnThirdInteraction()
    {
        Assert.IsFalse(MainMenuCharacterDialogueController.ShouldQuitAfterSensitiveTouch(1));
        Assert.IsFalse(MainMenuCharacterDialogueController.ShouldQuitAfterSensitiveTouch(2));
        Assert.IsTrue(MainMenuCharacterDialogueController.ShouldQuitAfterSensitiveTouch(3));
    }

    [Test]
    public void SensitiveTouchCounter_ResetsWhenCharacterSelectionRefreshes()
    {
        controller.OnSensitiveTouch();
        controller.OnSensitiveTouch();

        controller.SelectCharacterForMenu(hasSaves: true, nexusRoll: 0f);

        Assert.AreEqual(0, controller.SensitiveTouchCount);
        Assert.AreEqual(MainMenuCharacter.Nexus, controller.SelectedCharacter);
    }

    [Test]
    public void PendingFinalQuit_WaitsUntilFinalDialogueCompletes()
    {
        bool quitCalled = false;
        controller.SetQuitHandlerForTests(() => quitCalled = true);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("kyraSensitiveFinalQuit").objectReferenceValue = finalDialogue;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        controller.OnSensitiveTouch();
        controller.OnSensitiveTouch();
        controller.OnSensitiveTouch();

        Assert.IsTrue(controller.HasPendingQuit);
        Assert.IsFalse(quitCalled);

        DialogueManager.Instance.EndDialogue();

        Assert.IsTrue(quitCalled);
        Assert.IsFalse(controller.HasPendingQuit);
    }

    [Test]
    public void DialogueEntryExpression_ReplacesCharacterImageSprite()
    {
        Image characterImage = controllerObject.AddComponent<Image>();
        Animator characterAnimator = controllerObject.AddComponent<Animator>();
        DialogueData touchDialogue = ScriptableObject.CreateInstance<DialogueData>();
        touchDialogue.entries.Add(new DialogueEntry
        {
            speakerName = "Kyra-Dominia",
            dialogueText = "Expression test.",
            expression = expressionSprite
        });

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("characterImage").objectReferenceValue = characterImage;
        serializedController.FindProperty("characterAnimator").objectReferenceValue = characterAnimator;
        serializedController.FindProperty("kyraTouch").objectReferenceValue = touchDialogue;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        controller.OnCharacterTouch();

        Assert.AreSame(expressionSprite, characterImage.sprite);
        Assert.IsFalse(characterAnimator.enabled);

        Object.DestroyImmediate(touchDialogue);
    }

    [Test]
    public void SelectCharacterForMenu_Nexus_DisablesKyraAnimatorAndUsesNexusSprite()
    {
        Image characterImage = controllerObject.AddComponent<Image>();
        Animator characterAnimator = controllerObject.AddComponent<Animator>();
        Sprite nexusSprite = Sprite.Create(expressionTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("characterImage").objectReferenceValue = characterImage;
        serializedController.FindProperty("characterAnimator").objectReferenceValue = characterAnimator;
        serializedController.FindProperty("nexusCharacterSprite").objectReferenceValue = nexusSprite;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        controller.SelectCharacterForMenu(hasSaves: true, nexusRoll: 0f);

        Assert.AreSame(nexusSprite, characterImage.sprite);
        Assert.IsFalse(characterAnimator.enabled);

        Object.DestroyImmediate(nexusSprite);
    }

    [Test]
    public void CharacterTouch_WhenDialogueActive_DoesNotStartAnotherDialogue()
    {
        DialogueData activeDialogue = ScriptableObject.CreateInstance<DialogueData>();
        activeDialogue.entries.Add(new DialogueEntry
        {
            speakerName = "Kyra-Dominia",
            dialogueText = "Already active."
        });

        DialogueData touchDialogue = ScriptableObject.CreateInstance<DialogueData>();
        touchDialogue.entries.Add(new DialogueEntry
        {
            speakerName = "Kyra-Dominia",
            dialogueText = "Touch dialogue."
        });

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("kyraTouch").objectReferenceValue = touchDialogue;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        DialogueManager.Instance.StartDialogue(activeDialogue);
        controller.OnCharacterTouch();

        Assert.AreSame(activeDialogue, DialogueManager.Instance.CurrentDialogue);

        Object.DestroyImmediate(activeDialogue);
        Object.DestroyImmediate(touchDialogue);
    }

    private static void SetDialogueManagerInstance(DialogueManager dialogueManager)
    {
        typeof(DialogueManager)
            .GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static)
            ?.SetValue(null, dialogueManager);
    }
}
