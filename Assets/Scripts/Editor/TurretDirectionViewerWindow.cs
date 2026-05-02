using UnityEditor;
using UnityEngine;

public class TurretDirectionViewerWindow : EditorWindow
{
    private const string DefaultTurretPath = "Assets/Prefabs/Buildings/Gun Turret.prefab";

    private GameObject turretPrefab;
    private Turret turret;
    private SerializedObject serializedTurret;
    private SerializedProperty directionalSpritesProperty;
    private SerializedProperty firstSpriteAngleProperty;
    private SerializedProperty spritesAreClockwiseProperty;

    private Vector2 scrollPosition;
    private float previewAngle;
    private float previewSize = 96f;

    [MenuItem("Tools/Turret Direction Viewer")]
    private static void Open()
    {
        GetWindow<TurretDirectionViewerWindow>("Turret Direction Viewer");
    }

    private void OnEnable()
    {
        LoadDefaultTurret();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Turret Direction Viewer", EditorStyles.boldLabel);

        DrawTurretSelector();

        if (turret == null)
        {
            EditorGUILayout.HelpBox("Select a prefab or scene object with a Turret component.", MessageType.Info);
            return;
        }

        serializedTurret.Update();

        DrawSettings();
        EditorGUILayout.Space(8);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawActivePreview();
        EditorGUILayout.Space(8);
        DrawDirectionGrid();
        EditorGUILayout.EndScrollView();

        if (serializedTurret.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(turret);
        }
    }

    private void DrawTurretSelector()
    {
        EditorGUI.BeginChangeCheck();
        turretPrefab = (GameObject)EditorGUILayout.ObjectField("Turret Prefab", turretPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            SetTurretSource(turretPrefab);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Gun Turret"))
        {
            LoadDefaultTurret();
        }

        GameObject selectedObject = Selection.activeGameObject;
        GUI.enabled = selectedObject != null && selectedObject.GetComponent<Turret>() != null;
        if (GUILayout.Button("Use Selection"))
        {
            SetTurretSource(selectedObject);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("Direction Mapping", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(firstSpriteAngleProperty);
        EditorGUILayout.PropertyField(spritesAreClockwiseProperty);
        previewAngle = EditorGUILayout.Slider("Preview Angle", previewAngle, 0f, 360f);
        previewSize = EditorGUILayout.Slider("Sprite Size", previewSize, 48f, 160f);
    }

    private void DrawActivePreview()
    {
        int directionIndex = GetDirectionIndex(previewAngle);
        Sprite sprite = GetSprite(directionIndex);

        EditorGUILayout.LabelField("Angle Preview", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        DrawSprite(sprite, previewSize);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField($"Angle: {previewAngle:0} degrees");
        EditorGUILayout.LabelField($"Direction: {GetDirectionName(previewAngle)}");
        EditorGUILayout.LabelField($"Sprite Index: {directionIndex}");
        EditorGUILayout.LabelField(sprite != null ? $"Sprite: {sprite.name}" : "Sprite: Missing");
        float spriteAngle = GetSpriteAngle(directionIndex);
        EditorGUILayout.LabelField($"Sprite Direction: {GetDirectionName(spriteAngle)} ({spriteAngle:0} degrees)");
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawDirectionGrid()
    {
        EditorGUILayout.LabelField("Eight Direction Sprites", EditorStyles.boldLabel);

        for (int row = 0; row < 2; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < 4; col++)
            {
                int index = row * 4 + col;
                DrawDirectionSlot(index);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawDirectionSlot(int index)
    {
        Sprite sprite = GetSprite(index);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(previewSize + 28f));
        EditorGUILayout.LabelField($"Index {index}", EditorStyles.boldLabel, GUILayout.Width(previewSize + 16f));
        DrawSprite(sprite, previewSize);
        float spriteAngle = GetSpriteAngle(index);
        EditorGUILayout.LabelField($"{GetDirectionName(spriteAngle)}", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField($"{spriteAngle:0} degrees", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.PropertyField(directionalSpritesProperty.GetArrayElementAtIndex(index), GUIContent.none);
        EditorGUILayout.EndVertical();
    }

    private void DrawSprite(Sprite sprite, float size)
    {
        Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

        if (sprite == null)
        {
            GUI.Box(rect, "Missing");
            return;
        }

        GUI.Box(rect, GUIContent.none);
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, GetSpriteUVs(sprite), true);
    }

    private int GetDirectionIndex(float angle)
    {
        float firstSpriteAngle = firstSpriteAngleProperty.floatValue;
        bool spritesAreClockwise = spritesAreClockwiseProperty.boolValue;
        float normalizedAngle = Mathf.Repeat(angle - firstSpriteAngle, 360f);
        int directionIndex = Mathf.RoundToInt(normalizedAngle / 45f) % 8;

        if (spritesAreClockwise && directionIndex != 0)
        {
            directionIndex = 8 - directionIndex;
        }

        return directionIndex;
    }

    private float GetSpriteAngle(int index)
    {
        float step = spritesAreClockwiseProperty.boolValue ? -45f : 45f;
        return Mathf.Repeat(firstSpriteAngleProperty.floatValue + index * step, 360f);
    }

    private static string GetDirectionName(float angle)
    {
        string[] directionNames =
        {
            "East",
            "North East",
            "North",
            "North West",
            "West",
            "South West",
            "South",
            "South East"
        };

        int directionIndex = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % directionNames.Length;
        return directionNames[directionIndex];
    }

    private Sprite GetSprite(int index)
    {
        if (directionalSpritesProperty == null || directionalSpritesProperty.arraySize <= index)
        {
            return null;
        }

        return directionalSpritesProperty.GetArrayElementAtIndex(index).objectReferenceValue as Sprite;
    }

    private static Rect GetSpriteUVs(Sprite sprite)
    {
        Rect textureRect = sprite.textureRect;
        return new Rect(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height
        );
    }

    private void LoadDefaultTurret()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTurretPath);
        SetTurretSource(prefab);
    }

    private void SetTurretSource(GameObject source)
    {
        turretPrefab = source;
        turret = source != null ? source.GetComponent<Turret>() : null;

        if (turret == null)
        {
            serializedTurret = null;
            directionalSpritesProperty = null;
            firstSpriteAngleProperty = null;
            spritesAreClockwiseProperty = null;
            return;
        }

        serializedTurret = new SerializedObject(turret);
        directionalSpritesProperty = serializedTurret.FindProperty("directionalSprites");
        firstSpriteAngleProperty = serializedTurret.FindProperty("firstSpriteAngle");
        spritesAreClockwiseProperty = serializedTurret.FindProperty("spritesAreClockwise");

        if (directionalSpritesProperty != null && directionalSpritesProperty.arraySize != 8)
        {
            directionalSpritesProperty.arraySize = 8;
            serializedTurret.ApplyModifiedProperties();
        }
    }
}
