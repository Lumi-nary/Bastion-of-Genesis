using System;
using UnityEngine;
using UnityEngine.UI;

public enum MainMenuCharacter
{
    Kyra,
    Nexus
}

/// <summary>
/// Adds character-specific dialogue interactions to the main menu without owning menu canvas navigation.
/// </summary>
public class MainMenuCharacterDialogueController : MonoBehaviour
{
    private const int QuitSensitiveTouchCount = 3;

    [Header("Character Display")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Sprite kyraCharacterSprite;
    [SerializeField] private Sprite nexusCharacterSprite;

    [Header("Touch Areas")]
    [SerializeField] private Button characterTouchArea;
    [SerializeField] private Button sensitiveTouchArea;

    [Header("Selection")]
    [Range(0f, 1f)]
    [SerializeField] private float nexusReturningPlayerChance = 0.4f;

    [Header("Kyra Dialogue")]
    [SerializeField] private DialogueData kyraNewPlayerIntro;
    [SerializeField] private DialogueData kyraReturningWelcome;
    [SerializeField] private DialogueData kyraTouch;
    [SerializeField] private DialogueData kyraSensitiveWarning1;
    [SerializeField] private DialogueData kyraSensitiveWarning2;
    [SerializeField] private DialogueData kyraSensitiveFinalQuit;

    [Header("Nexus Dialogue")]
    [SerializeField] private DialogueData nexusReturningWelcome;
    [SerializeField] private DialogueData nexusTouch;
    [SerializeField] private DialogueData nexusSensitiveWarning1;
    [SerializeField] private DialogueData nexusSensitiveWarning2;
    [SerializeField] private DialogueData nexusSensitiveFinalQuit;

    private MainMenuCharacter selectedCharacter;
    private int sensitiveTouchCount;
    private DialogueData pendingQuitDialogue;
    private Action quitHandlerOverride;
    private bool dialogueEventsSubscribed;
    private bool characterAnimatorDefaultEnabled;

    public MainMenuCharacter SelectedCharacter => selectedCharacter;
    public int SensitiveTouchCount => sensitiveTouchCount;
    public bool HasPendingQuit => pendingQuitDialogue != null;

    private void Awake()
    {
        CacheCharacterAnimator();
        WireTouchAreas();
    }

    private void Start()
    {
        EnsureDialogueEventSubscriptions();

        bool hasSaves = SaveManager.Instance != null && SaveManager.Instance.GetAllSaves().Count > 0;
        SelectCharacterForMenu(hasSaves, UnityEngine.Random.value);
    }

    private void OnDestroy()
    {
        if (dialogueEventsSubscribed && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
            DialogueManager.Instance.OnEntryDisplayed -= OnEntryDisplayed;
        }
    }

    public static MainMenuCharacter SelectCharacterForLoad(bool hasAnySaves, float nexusRoll, float nexusChance = 0.4f)
    {
        if (!hasAnySaves)
            return MainMenuCharacter.Kyra;

        float clampedChance = Mathf.Clamp01(nexusChance);
        return nexusRoll < clampedChance ? MainMenuCharacter.Nexus : MainMenuCharacter.Kyra;
    }

    public static bool ShouldQuitAfterSensitiveTouch(int sensitiveTouchCount)
    {
        return sensitiveTouchCount >= QuitSensitiveTouchCount;
    }

    public void SelectCharacterForMenu(bool hasSaves, float nexusRoll)
    {
        selectedCharacter = SelectCharacterForLoad(hasSaves, nexusRoll, nexusReturningPlayerChance);
        sensitiveTouchCount = 0;
        pendingQuitDialogue = null;

        ApplyCharacterSprite();
        PlayDialogue(GetInitialDialogue(hasSaves));
    }

    public void OnCharacterTouch()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            return;

        PlayDialogue(selectedCharacter == MainMenuCharacter.Nexus ? nexusTouch : kyraTouch);
    }

    public void OnSensitiveTouch()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            return;

        sensitiveTouchCount++;

        DialogueData dialogue = GetSensitiveDialogue(sensitiveTouchCount);
        if (ShouldQuitAfterSensitiveTouch(sensitiveTouchCount))
        {
            PlayFinalQuitDialogue(dialogue);
            return;
        }

        PlayDialogue(dialogue);
    }

    public void SetQuitHandlerForTests(Action quitHandler)
    {
        quitHandlerOverride = quitHandler;
    }

    private void WireTouchAreas()
    {
        if (characterTouchArea != null)
        {
            characterTouchArea.onClick.RemoveListener(OnCharacterTouch);
            characterTouchArea.onClick.AddListener(OnCharacterTouch);
        }

        if (sensitiveTouchArea != null)
        {
            sensitiveTouchArea.onClick.RemoveListener(OnSensitiveTouch);
            sensitiveTouchArea.onClick.AddListener(OnSensitiveTouch);
        }
    }

    private DialogueData GetInitialDialogue(bool hasSaves)
    {
        if (selectedCharacter == MainMenuCharacter.Nexus)
            return nexusReturningWelcome;

        return hasSaves ? kyraReturningWelcome : kyraNewPlayerIntro;
    }

    private DialogueData GetSensitiveDialogue(int touchCount)
    {
        bool nexus = selectedCharacter == MainMenuCharacter.Nexus;
        if (touchCount <= 1)
            return nexus ? nexusSensitiveWarning1 : kyraSensitiveWarning1;
        if (touchCount == 2)
            return nexus ? nexusSensitiveWarning2 : kyraSensitiveWarning2;

        return nexus ? nexusSensitiveFinalQuit : kyraSensitiveFinalQuit;
    }

    private void ApplyCharacterSprite()
    {
        if (characterImage == null)
            return;

        bool useAnimatedCharacter = selectedCharacter == MainMenuCharacter.Kyra;
        if (useAnimatedCharacter)
            RestoreCharacterAnimator();
        else
            DisableCharacterAnimatorForExpression();

        Sprite nextSprite = useAnimatedCharacter ? kyraCharacterSprite : nexusCharacterSprite;
        if (nextSprite != null)
            characterImage.sprite = nextSprite;

        characterImage.gameObject.SetActive(characterImage.sprite != null);
    }

    private void PlayDialogue(DialogueData dialogue)
    {
        if (dialogue == null || DialogueManager.Instance == null)
            return;

        EnsureDialogueEventSubscriptions();
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void PlayFinalQuitDialogue(DialogueData dialogue)
    {
        if (dialogue == null || DialogueManager.Instance == null)
        {
            Quit();
            return;
        }

        pendingQuitDialogue = dialogue;
        EnsureDialogueEventSubscriptions();
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void OnDialogueEnded(DialogueData endedDialogue)
    {
        ApplyCharacterSprite();

        if (pendingQuitDialogue == null || endedDialogue != pendingQuitDialogue)
            return;

        pendingQuitDialogue = null;
        Quit();
    }

    private void OnEntryDisplayed(DialogueEntry entry, int index)
    {
        if (characterImage == null || entry == null)
            return;

        if (entry.expression == null)
            return;

        DisableCharacterAnimatorForExpression();
        characterImage.sprite = entry.expression;
        characterImage.gameObject.SetActive(true);
    }

    private void EnsureDialogueEventSubscriptions()
    {
        if (dialogueEventsSubscribed || DialogueManager.Instance == null)
            return;

        DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
        DialogueManager.Instance.OnEntryDisplayed += OnEntryDisplayed;
        dialogueEventsSubscribed = true;
    }

    private void CacheCharacterAnimator()
    {
        if (characterAnimator == null && characterImage != null)
            characterAnimator = characterImage.GetComponent<Animator>();

        characterAnimatorDefaultEnabled = characterAnimator != null && characterAnimator.enabled;
    }

    private void DisableCharacterAnimatorForExpression()
    {
        if (characterAnimator == null)
            CacheCharacterAnimator();

        if (characterAnimator != null)
            characterAnimator.enabled = false;
    }

    private void RestoreCharacterAnimator()
    {
        if (characterAnimator == null)
            CacheCharacterAnimator();

        if (characterAnimator != null)
            characterAnimator.enabled = characterAnimatorDefaultEnabled;
    }

    private void Quit()
    {
        if (quitHandlerOverride != null)
        {
            quitHandlerOverride.Invoke();
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
