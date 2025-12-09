using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window to preview tileset sprite mappings for 13-sprite transition tilesets.
/// Shows a visual grid demonstrating which sprite is used for each tile position.
/// </summary>
public class TilesetPreviewWindow : EditorWindow
{
    private Sprite[] tilesetSprites = new Sprite[13];
    private bool showIndices = true;
    private float previewScale = 2f;
    private Vector2 scrollPosition;

    // Sprite names for reference
    private readonly string[] spriteNames = new string[]
    {
        "OC-TL (Outer Corner Top-Left)",      // 0
        "Edge Top",                            // 1
        "OC-TR (Outer Corner Top-Right)",     // 2
        "Edge Left",                           // 3
        "Center",                              // 4
        "Edge Right",                          // 5
        "OC-BL (Outer Corner Bottom-Left)",   // 6
        "Edge Bottom",                         // 7
        "OC-BR (Outer Corner Bottom-Right)",  // 8
        "IC-TL (Inner Corner Top-Left)",      // 9
        "IC-TR (Inner Corner Top-Right)",     // 10
        "IC-BL (Inner Corner Bottom-Left)",   // 11
        "IC-BR (Inner Corner Bottom-Right)"   // 12
    };

    [MenuItem("Tools/Tileset Preview")]
    public static void ShowWindow()
    {
        var window = GetWindow<TilesetPreviewWindow>("Tileset Preview");
        window.minSize = new Vector2(600, 700);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Tileset Sprite Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag sprites from your tileset into the slots below.\n" +
            "This tool helps visualize which sprite maps to which position.\n\n" +
            "Layout (13 sprites):\n" +
            "0-2: Top row (OC-TL, Top, OC-TR)\n" +
            "3-5: Middle row (Left, Center, Right)\n" +
            "6-8: Bottom row (OC-BL, Bottom, OC-BR)\n" +
            "9-12: Inner corners (IC-TL, IC-TR, IC-BL, IC-BR)",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // Settings
        showIndices = EditorGUILayout.Toggle("Show Indices", showIndices);
        previewScale = EditorGUILayout.Slider("Preview Scale", previewScale, 1f, 4f);

        EditorGUILayout.Space(10);

        // Sprite slots in grid layout
        EditorGUILayout.LabelField("Sprite Assignments", EditorStyles.boldLabel);

        // Draw sprite grid (3 columns for main tiles, then inner corners)
        DrawSpriteGrid();

        EditorGUILayout.Space(20);

        // Preview section
        EditorGUILayout.LabelField("Preview Grid", EditorStyles.boldLabel);
        DrawPreviewGrid();

        EditorGUILayout.Space(10);

        // Quick assign from texture
        DrawQuickAssign();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSpriteGrid()
    {
        float spriteSize = 64;

        // Outer corners and edges (3x3 grid)
        EditorGUILayout.LabelField("Main Tiles (3x3 Layout):");

        // Top row
        EditorGUILayout.BeginHorizontal();
        DrawSpriteSlot(0, spriteSize);
        DrawSpriteSlot(1, spriteSize);
        DrawSpriteSlot(2, spriteSize);
        EditorGUILayout.EndHorizontal();

        // Middle row
        EditorGUILayout.BeginHorizontal();
        DrawSpriteSlot(3, spriteSize);
        DrawSpriteSlot(4, spriteSize);
        DrawSpriteSlot(5, spriteSize);
        EditorGUILayout.EndHorizontal();

        // Bottom row
        EditorGUILayout.BeginHorizontal();
        DrawSpriteSlot(6, spriteSize);
        DrawSpriteSlot(7, spriteSize);
        DrawSpriteSlot(8, spriteSize);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Inner corners (2x2 grid)
        EditorGUILayout.LabelField("Inner Corners (2x2 Layout):");

        EditorGUILayout.BeginHorizontal();
        DrawSpriteSlot(9, spriteSize);
        DrawSpriteSlot(10, spriteSize);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawSpriteSlot(11, spriteSize);
        DrawSpriteSlot(12, spriteSize);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSpriteSlot(int index, float size)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(size + 10));

        if (showIndices)
        {
            EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(size));
        }

        tilesetSprites[index] = (Sprite)EditorGUILayout.ObjectField(
            tilesetSprites[index],
            typeof(Sprite),
            false,
            GUILayout.Width(size),
            GUILayout.Height(size)
        );

        // Show short name
        string shortName = spriteNames[index].Split('(')[0].Trim();
        EditorGUILayout.LabelField(shortName, EditorStyles.miniLabel, GUILayout.Width(size));

        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewGrid()
    {
        EditorGUILayout.HelpBox(
            "This shows how tiles would appear in a 5x5 sample with a center integrated zone.\n" +
            "Green = Integrated, Yellow = Wither/Polluted, Gray = Alive/Grass",
            MessageType.None);

        float tileSize = 32 * previewScale;

        // 5x5 preview grid showing transition zones
        // Pattern: GGGGG
        //          GWWWG
        //          GWIWG
        //          GWWWG
        //          GGGGG
        // G = Grass (outer), W = Wither (border), I = Integrated (center)

        // Map of what sprite index to use at each position
        int[,] previewMap = new int[5, 5]
        {
            // Row 0 (top) - All grass
            { -1, -1, -1, -1, -1 },
            // Row 1 - Grass, Wither top-left outer, Wither top, Wither top-right outer, Grass
            { -1, 0, 1, 2, -1 },
            // Row 2 - Grass, Wither left, Integrated center, Wither right, Grass
            { -1, 3, 4, 5, -1 },
            // Row 3 - Grass, Wither bottom-left outer, Wither bottom, Wither bottom-right outer, Grass
            { -1, 6, 7, 8, -1 },
            // Row 4 (bottom) - All grass
            { -1, -1, -1, -1, -1 }
        };

        Color grassColor = new Color(0.3f, 0.5f, 0.3f, 0.5f);
        Color witherColor = new Color(0.6f, 0.5f, 0.2f, 0.5f);
        Color integratedColor = new Color(0.2f, 0.6f, 0.2f, 0.5f);

        for (int y = 0; y < 5; y++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int x = 0; x < 5; x++)
            {
                int spriteIndex = previewMap[y, x];
                Rect rect = GUILayoutUtility.GetRect(tileSize, tileSize);

                // Draw background color
                Color bgColor = grassColor;
                if (spriteIndex >= 0)
                {
                    bgColor = (spriteIndex == 4) ? integratedColor : witherColor;
                }
                EditorGUI.DrawRect(rect, bgColor);

                // Draw sprite if assigned
                if (spriteIndex >= 0 && tilesetSprites[spriteIndex] != null)
                {
                    Texture2D tex = AssetPreview.GetAssetPreview(tilesetSprites[spriteIndex]);
                    if (tex != null)
                    {
                        GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                    }
                }

                // Draw index
                if (showIndices && spriteIndex >= 0)
                {
                    GUI.Label(rect, spriteIndex.ToString(), EditorStyles.whiteBoldLabel);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        // Inner corner preview
        EditorGUILayout.LabelField("Inner Corners Preview (for concave edges):");
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        for (int i = 9; i <= 12; i++)
        {
            Rect rect = GUILayoutUtility.GetRect(tileSize, tileSize);
            EditorGUI.DrawRect(rect, witherColor);

            if (tilesetSprites[i] != null)
            {
                Texture2D tex = AssetPreview.GetAssetPreview(tilesetSprites[i]);
                if (tex != null)
                {
                    GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                }
            }

            if (showIndices)
            {
                GUI.Label(rect, i.ToString(), EditorStyles.whiteBoldLabel);
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawQuickAssign()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Assign from Texture", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drop a sliced texture here to auto-assign sprites.\n" +
            "Sprites should be numbered 0-12 in the texture.",
            MessageType.Info);

        Texture2D sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Source Texture",
            null,
            typeof(Texture2D),
            false
        );

        if (sourceTexture != null)
        {
            string path = AssetDatabase.GetAssetPath(sourceTexture);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            int assignedCount = 0;
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    // Try to parse index from sprite name (e.g., "tileset_0", "tileset_1")
                    string name = sprite.name;
                    int lastUnderscore = name.LastIndexOf('_');
                    if (lastUnderscore >= 0)
                    {
                        string indexStr = name.Substring(lastUnderscore + 1);
                        if (int.TryParse(indexStr, out int index) && index >= 0 && index < 13)
                        {
                            tilesetSprites[index] = sprite;
                            assignedCount++;
                        }
                    }
                }
            }

            if (assignedCount > 0)
            {
                Debug.Log($"Auto-assigned {assignedCount} sprites from {sourceTexture.name}");
                Repaint();
            }
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Copy Sprite Array to Console"))
        {
            Debug.Log("=== TILESET SPRITE MAPPING ===");
            for (int i = 0; i < 13; i++)
            {
                if (tilesetSprites[i] != null)
                {
                    string path = AssetDatabase.GetAssetPath(tilesetSprites[i]);
                    Debug.Log($"[{i}] {spriteNames[i]}: {tilesetSprites[i].name} at {path}");
                }
                else
                {
                    Debug.Log($"[{i}] {spriteNames[i]}: NOT ASSIGNED");
                }
            }
            Debug.Log("=== END MAPPING ===");
        }

        if (GUILayout.Button("Clear All"))
        {
            for (int i = 0; i < 13; i++)
            {
                tilesetSprites[i] = null;
            }
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Rename Sprites in Texture"))
        {
            RenameSpritesInTexture();
        }
    }

    private void RenameSpritesInTexture()
    {
        // Find the texture path from the first assigned sprite
        string texturePath = null;
        for (int i = 0; i < 13; i++)
        {
            if (tilesetSprites[i] != null)
            {
                texturePath = AssetDatabase.GetAssetPath(tilesetSprites[i]);
                break;
            }
        }

        if (string.IsNullOrEmpty(texturePath))
        {
            Debug.LogError("No sprites assigned. Assign sprites first before renaming.");
            return;
        }

        // Get the texture importer
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Could not get TextureImporter for {texturePath}");
            return;
        }

        // Expected names for each index
        string[] expectedNames = new string[]
        {
            "outerTL",      // 0
            "top",          // 1
            "outerTR",      // 2
            "left",         // 3
            "center",       // 4
            "right",        // 5
            "outerBL",      // 6
            "bottom",       // 7
            "outerBR",      // 8
            "innerTL",      // 9
            "innerTR",      // 10
            "innerBL",      // 11
            "innerBR"       // 12
        };

        // Get texture name prefix
        string textureName = System.IO.Path.GetFileNameWithoutExtension(texturePath);

        // Get current sprite sheet data
        var spriteSheet = importer.spritesheet;
        if (spriteSheet == null || spriteSheet.Length == 0)
        {
            Debug.LogError("No sprite sheet data found. Make sure texture is set to Multiple sprite mode.");
            return;
        }

        // Create a mapping from current sprite to new name
        var newSpriteSheet = new SpriteMetaData[spriteSheet.Length];
        System.Array.Copy(spriteSheet, newSpriteSheet, spriteSheet.Length);

        int renamedCount = 0;

        // For each assigned sprite, find it in the sprite sheet and rename
        for (int i = 0; i < 13; i++)
        {
            if (tilesetSprites[i] == null) continue;

            string currentSpriteName = tilesetSprites[i].name;
            string newSpriteName = $"{textureName}_{expectedNames[i]}";

            // Find this sprite in the sheet
            for (int j = 0; j < newSpriteSheet.Length; j++)
            {
                if (newSpriteSheet[j].name == currentSpriteName)
                {
                    newSpriteSheet[j].name = newSpriteName;
                    renamedCount++;
                    Debug.Log($"Renamed: {currentSpriteName} -> {newSpriteName}");
                    break;
                }
            }
        }

        if (renamedCount > 0)
        {
            // Apply the changes
            importer.spritesheet = newSpriteSheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Debug.Log($"Renamed {renamedCount} sprites in {texturePath}. Texture reimported.");

            // Refresh the sprite references
            RefreshSpriteReferences(texturePath, expectedNames, textureName);
        }
        else
        {
            Debug.LogWarning("No sprites were renamed. Check if sprites are properly assigned.");
        }
    }

    private void RefreshSpriteReferences(string texturePath, string[] expectedNames, string textureName)
    {
        // Reload sprites with new names
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);

        for (int i = 0; i < 13; i++)
        {
            string targetName = $"{textureName}_{expectedNames[i]}";

            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == targetName)
                {
                    tilesetSprites[i] = sprite;
                    break;
                }
            }
        }

        Repaint();
    }
}
