using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutscenePreviewWindow : EditorWindow
{
    private const string CutsceneScenePath = "Assets/Scenes/CutsceneScene.unity";
    private const string DefaultCutscenePath = "Assets/Resources/Data/Cutscenes/Chapter1_Intro.asset";

    private CutsceneData cutsceneData;
    private int beatIndex;
    private bool loopBeat;
    private bool previewTypewriter = true;
    private bool isPreviewing;
    private double beatStartTime;

    [MenuItem("Tools/Cutscene Preview")]
    public static void Open()
    {
        GetWindow<CutscenePreviewWindow>("Cutscene Preview");
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;

        if (cutsceneData == null)
        {
            cutsceneData = AssetDatabase.LoadAssetAtPath<CutsceneData>(DefaultCutscenePath);
        }
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        cutsceneData = (CutsceneData)EditorGUILayout.ObjectField("Cutscene Data", cutsceneData, typeof(CutsceneData), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open CutsceneScene"))
            {
                OpenCutsceneScene();
            }

            if (GUILayout.Button("Load Chapter 1"))
            {
                cutsceneData = AssetDatabase.LoadAssetAtPath<CutsceneData>(DefaultCutscenePath);
                beatIndex = 0;
                PreviewCurrentBeat();
            }
        }

        EditorGUILayout.Space();

        if (cutsceneData == null)
        {
            EditorGUILayout.HelpBox("Assign a CutsceneData asset to preview.", MessageType.Info);
            return;
        }

        int beatCount = cutsceneData.BeatCount;

        if (beatCount == 0)
        {
            EditorGUILayout.HelpBox("This cutscene has no beats.", MessageType.Warning);
            return;
        }

        beatIndex = Mathf.Clamp(beatIndex, 0, beatCount - 1);
        int newBeatIndex = EditorGUILayout.IntSlider("Beat", beatIndex, 0, beatCount - 1);

        if (newBeatIndex != beatIndex)
        {
            beatIndex = newBeatIndex;
            PreviewCurrentBeat();
        }

        CutsceneBeat beat = cutsceneData.beats[beatIndex];
        EditorGUILayout.LabelField("Speaker", string.IsNullOrWhiteSpace(beat.speaker) ? "(none)" : beat.speaker);
        EditorGUILayout.LabelField("Visual Effect", beat.visualEffect.ToString());

        using (new EditorGUILayout.HorizontalScope())
        {
            previewTypewriter = EditorGUILayout.ToggleLeft("Typewriter", previewTypewriter, GUILayout.Width(110));
            loopBeat = EditorGUILayout.ToggleLeft("Loop Beat", loopBeat, GUILayout.Width(110));
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Previous Beat"))
            {
                PreviousBeat();
            }

            if (GUILayout.Button(isPreviewing ? "Restart Beat" : "Preview Beat"))
            {
                StartPreview();
            }

            if (GUILayout.Button("Next Beat"))
            {
                NextBeat();
            }

            if (GUILayout.Button("Stop"))
            {
                StopPreview();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialogue");
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextArea(beat.dialogue, GUILayout.MinHeight(70));
        }
    }

    private void OnEditorUpdate()
    {
        if (!isPreviewing || cutsceneData == null || beatIndex < 0 || beatIndex >= cutsceneData.BeatCount)
        {
            return;
        }

        StillImageCutscenePlayer player = FindPreviewPlayer();

        if (player == null)
        {
            return;
        }

        double elapsed = EditorApplication.timeSinceStartup - beatStartTime;
        float duration = Mathf.Max(0.1f, player.GetPreviewBeatDuration(cutsceneData, beatIndex));

        if (elapsed >= duration)
        {
            if (loopBeat)
            {
                beatStartTime = EditorApplication.timeSinceStartup;
                PlayPreviewAudio(cutsceneData.beats[beatIndex]);
                elapsed = 0d;
            }
            else
            {
                isPreviewing = false;
                player.PreviewBeat(cutsceneData, beatIndex, cutsceneData.beats[beatIndex].dialogue);
                Repaint();
                return;
            }
        }

        string previewText = previewTypewriter
            ? player.GetPreviewTypewriterText(cutsceneData, beatIndex, (float)elapsed)
            : cutsceneData.beats[beatIndex].dialogue;

        player.PreviewBeat(cutsceneData, beatIndex, previewText);
        player.PreviewVisualEffect(cutsceneData, beatIndex, (float)elapsed);
        Repaint();
    }

    private void StartPreview()
    {
        if (!EnsureReadyForPreview())
        {
            return;
        }

        isPreviewing = true;
        beatStartTime = EditorApplication.timeSinceStartup;
        PlayPreviewAudio(cutsceneData.beats[beatIndex]);
        PreviewCurrentBeat();
    }

    private void StopPreview()
    {
        isPreviewing = false;
        StopPreviewAudio();

        StillImageCutscenePlayer player = FindPreviewPlayer();

        if (player != null)
        {
            player.Stop();
        }
    }

    private void PreviewCurrentBeat()
    {
        if (!EnsureReadyForPreview())
        {
            return;
        }

        string previewText = previewTypewriter ? string.Empty : cutsceneData.beats[beatIndex].dialogue;
        FindPreviewPlayer()?.PreviewBeat(cutsceneData, beatIndex, previewText);
        SceneView.RepaintAll();
    }

    private void PreviousBeat()
    {
        beatIndex = beatIndex <= 0 ? cutsceneData.BeatCount - 1 : beatIndex - 1;
        StartPreview();
    }

    private void NextBeat()
    {
        beatIndex = beatIndex >= cutsceneData.BeatCount - 1 ? 0 : beatIndex + 1;
        StartPreview();
    }

    private bool EnsureReadyForPreview()
    {
        if (cutsceneData == null || cutsceneData.BeatCount == 0)
        {
            return false;
        }

        if (!IsCutsceneSceneOpen() && !OpenCutsceneScene())
        {
            return false;
        }

        if (FindPreviewPlayer() == null)
        {
            Debug.LogError("[CutscenePreviewWindow] CutsceneScene does not contain a StillImageCutscenePlayer.");
            return false;
        }

        beatIndex = Mathf.Clamp(beatIndex, 0, cutsceneData.BeatCount - 1);
        return true;
    }

    private static bool OpenCutsceneScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return false;
        }

        EditorSceneManager.OpenScene(CutsceneScenePath, OpenSceneMode.Single);
        return true;
    }

    private static bool IsCutsceneSceneOpen()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.path == CutsceneScenePath;
    }

    private static StillImageCutscenePlayer FindPreviewPlayer()
    {
        return UnityEngine.Object.FindFirstObjectByType<StillImageCutscenePlayer>();
    }

    private static void PlayPreviewAudio(CutsceneBeat beat)
    {
        StopPreviewAudio();

        if (beat.soundEffect != null)
        {
            PlayEditorAudioClip(beat.soundEffect);
        }

        if (beat.loopAmbience && beat.ambienceLoop != null)
        {
            PlayEditorAudioClip(beat.ambienceLoop, true);
        }

        if (beat.voiceline != null)
        {
            PlayEditorAudioClip(beat.voiceline);
        }
    }

    private static void PlayEditorAudioClip(AudioClip clip, bool loop = false)
    {
        Type audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo playMethod = audioUtilType?.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null)
            ?? audioUtilType?.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null);

        playMethod?.Invoke(null, new object[] { clip, 0, loop });
    }

    private static void StopPreviewAudio()
    {
        Type audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo stopMethod = audioUtilType?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
        stopMethod?.Invoke(null, null);
    }
}
