#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window for testing DialogueData assets.
/// Allows previewing and playing dialogues in Play Mode.
/// Open via: Window > Planetfall > Dialogue Tester
/// </summary>
public class DialogueTester : EditorWindow
{
    private DialogueData selectedDialogue;
    private Vector2 scrollPosition;
    private int previewEntryIndex = 0;

    [MenuItem("Tools/Dialogue Tester")]
    public static void ShowWindow()
    {
        DialogueTester window = GetWindow<DialogueTester>("Dialogue Tester");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Dialogue Tester", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Dialogue selection
        EditorGUI.BeginChangeCheck();
        selectedDialogue = (DialogueData)EditorGUILayout.ObjectField(
            "Dialogue Data",
            selectedDialogue,
            typeof(DialogueData),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            previewEntryIndex = 0;
        }

        EditorGUILayout.Space(10);

        if (selectedDialogue == null)
        {
            EditorGUILayout.HelpBox("Select a DialogueData asset to preview and test.", MessageType.Info);
            return;
        }

        // Dialogue info
        EditorGUILayout.LabelField("Dialogue Info", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Name:", selectedDialogue.dialogueName);
        EditorGUILayout.LabelField("Entries:", selectedDialogue.EntryCount.ToString());
        EditorGUILayout.LabelField("Pause Game:", selectedDialogue.pauseGame.ToString());
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(10);

        // Entry preview
        DrawEntryPreview();

        EditorGUILayout.Space(10);

        // Play mode test button
        DrawTestButtons();

        EditorGUILayout.Space(10);

        // Entry list
        DrawEntryList();
    }

    private void DrawEntryPreview()
    {
        if (selectedDialogue.EntryCount == 0)
        {
            EditorGUILayout.HelpBox("No entries in this dialogue.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Entry Preview", EditorStyles.boldLabel);

        // Navigation
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = previewEntryIndex > 0;
        if (GUILayout.Button("< Previous", GUILayout.Width(100)))
        {
            previewEntryIndex--;
        }
        GUI.enabled = true;

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{previewEntryIndex + 1} / {selectedDialogue.EntryCount}",
            EditorStyles.centeredGreyMiniLabel, GUILayout.Width(60));
        GUILayout.FlexibleSpace();

        GUI.enabled = previewEntryIndex < selectedDialogue.EntryCount - 1;
        if (GUILayout.Button("Next >", GUILayout.Width(100)))
        {
            previewEntryIndex++;
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Preview box
        DialogueEntry entry = selectedDialogue.GetEntry(previewEntryIndex);
        if (entry != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Portrait and name row
            EditorGUILayout.BeginHorizontal();

            // Portrait preview
            Sprite displayPortrait = entry.GetDisplayPortrait();
            if (displayPortrait != null)
            {
                Rect spriteRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64));
                GUI.DrawTextureWithTexCoords(spriteRect, displayPortrait.texture, GetSpriteUVs(displayPortrait));
            }
            else
            {
                GUILayout.Box("No Portrait", GUILayout.Width(64), GUILayout.Height(64));
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(entry.speakerName, EditorStyles.boldLabel);

            // Voice clip indicator
            if (entry.voiceClip != null)
            {
                EditorGUILayout.LabelField($"Voice: {entry.voiceClip.name}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Dialogue text
            EditorGUILayout.LabelField("Dialogue:", EditorStyles.miniBoldLabel);
            EditorGUILayout.TextArea(entry.dialogueText, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawTestButtons()
    {
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test dialogue playback.", MessageType.Info);

            if (GUILayout.Button("Enter Play Mode & Test", GUILayout.Height(30)))
            {
                // Store the dialogue to test
                EditorPrefs.SetString("DialogueTester_PendingDialogue", AssetDatabase.GetAssetPath(selectedDialogue));
                EditorApplication.isPlaying = true;
            }
        }
        else
        {
            // In play mode
            if (DialogueManager.Instance == null)
            {
                EditorGUILayout.HelpBox("DialogueManager not found in scene!", MessageType.Error);
                return;
            }

            if (DialogueManager.Instance.IsDialogueActive)
            {
                EditorGUILayout.LabelField("Dialogue is playing...", EditorStyles.centeredGreyMiniLabel);

                if (GUILayout.Button("Skip Dialogue", GUILayout.Height(25)))
                {
                    DialogueManager.Instance.SkipDialogue();
                }
            }
            else
            {
                if (GUILayout.Button("Play Dialogue", GUILayout.Height(30)))
                {
                    DialogueManager.Instance.StartDialogue(selectedDialogue);
                }
            }
        }
    }

    private void DrawEntryList()
    {
        EditorGUILayout.LabelField("All Entries", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        for (int i = 0; i < selectedDialogue.EntryCount; i++)
        {
            DialogueEntry entry = selectedDialogue.GetEntry(i);
            if (entry == null) continue;

            EditorGUILayout.BeginHorizontal(i == previewEntryIndex ? EditorStyles.selectionRect : EditorStyles.helpBox);

            // Index
            EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(25));

            // Speaker
            EditorGUILayout.LabelField(entry.speakerName, EditorStyles.boldLabel, GUILayout.Width(100));

            // Text preview (truncated)
            string preview = entry.dialogueText;
            if (preview.Length > 50)
                preview = preview.Substring(0, 47) + "...";
            EditorGUILayout.LabelField(preview, EditorStyles.wordWrappedMiniLabel);

            // Select button
            if (GUILayout.Button("View", GUILayout.Width(50)))
            {
                previewEntryIndex = i;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Get UV coordinates for sprite rendering in editor
    /// </summary>
    private Rect GetSpriteUVs(Sprite sprite)
    {
        Rect rect = sprite.textureRect;
        return new Rect(
            rect.x / sprite.texture.width,
            rect.y / sprite.texture.height,
            rect.width / sprite.texture.width,
            rect.height / sprite.texture.height
        );
    }

    /// <summary>
    /// Check for pending dialogue test on play mode start
    /// </summary>
    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            string pendingPath = EditorPrefs.GetString("DialogueTester_PendingDialogue", "");
            if (!string.IsNullOrEmpty(pendingPath))
            {
                EditorPrefs.DeleteKey("DialogueTester_PendingDialogue");

                // Delay to let managers initialize
                EditorApplication.delayCall += () =>
                {
                    DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(pendingPath);
                    if (dialogue != null && DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.StartDialogue(dialogue);
                        Debug.Log($"[DialogueTester] Auto-started dialogue: {dialogue.dialogueName}");
                    }
                };
            }
        }
    }
}
#endif
