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
        float tileSize = 32 * previewScale;
        Color outerColor = new Color(0.76f, 0.70f, 0.50f, 1f);  // Tan/sand
        Color innerColor = new Color(0.3f, 0.6f, 0.3f, 1f);     // Green/grass

        // === SQUARE PREVIEW (Outer corners + Edges) ===
        EditorGUILayout.LabelField("Square Preview (Outer Corners 0,2,6,8 + Edges 1,3,5,7 + Center 4)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Shows a square island of inner terrain. Outer corners point outward.", MessageType.None);

        int[,] squareMap = new int[3, 3]
        {
            { 0, 1, 2 },
            { 3, 4, 5 },
            { 6, 7, 8 }
        };

        for (int y = 0; y < 3; y++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int x = 0; x < 3; x++)
            {
                int spriteIndex = squareMap[y, x];
                Rect rect = GUILayoutUtility.GetRect(tileSize, tileSize);
                EditorGUI.DrawRect(rect, innerColor);

                if (tilesetSprites[spriteIndex] != null)
                {
                    Texture2D tex = AssetPreview.GetAssetPreview(tilesetSprites[spriteIndex]);
                    if (tex != null)
                    {
                        GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                    }
                }

                if (showIndices)
                {
                    GUI.Label(rect, spriteIndex.ToString(), EditorStyles.whiteBoldLabel);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(20);

        // === INNER CORNERS PREVIEW ===
        EditorGUILayout.LabelField("Inner Corners Preview (9,10,11,12)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Shows concave corners where outer terrain cuts into inner terrain diagonally.", MessageType.None);

        // 3x3 grid showing inner corners at their positions
        // Inner corners have outer terrain diagonally but NOT on cardinals
        //   O = outer terrain, I = inner with inner corner sprite
        //   Pattern for each corner shown separately:
        //
        //   IC-TL (9):  O  O      IC-TR (10): O  O
        //               O  9                  10 O
        //
        //   IC-BL (11): O 11      IC-BR (12): 12 O
        //               O  O                  O  O

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // Draw 2x2 for each inner corner
        string[] cornerLabels = { "IC-TL (9)", "IC-TR (10)", "IC-BL (11)", "IC-BR (12)" };
        int[] cornerIndices = { 9, 10, 11, 12 };
        // Position of the inner corner tile in each 2x2: TL=BR, TR=BL, BL=TR, BR=TL
        int[,] cornerPositions = {
            { 1, 1 },  // IC-TL: corner at bottom-right of 2x2
            { 1, 0 },  // IC-TR: corner at bottom-left of 2x2
            { 0, 1 },  // IC-BL: corner at top-right of 2x2
            { 0, 0 }   // IC-BR: corner at top-left of 2x2
        };

        for (int c = 0; c < 4; c++)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(cornerLabels[c], EditorStyles.miniLabel);

            for (int y = 0; y < 2; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < 2; x++)
                {
                    Rect rect = GUILayoutUtility.GetRect(tileSize * 0.75f, tileSize * 0.75f);

                    bool isCornerTile = (y == cornerPositions[c, 0] && x == cornerPositions[c, 1]);

                    if (isCornerTile)
                    {
                        EditorGUI.DrawRect(rect, innerColor);
                        if (tilesetSprites[cornerIndices[c]] != null)
                        {
                            Texture2D tex = AssetPreview.GetAssetPreview(tilesetSprites[cornerIndices[c]]);
                            if (tex != null)
                            {
                                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                            }
                        }
                        if (showIndices)
                        {
                            GUI.Label(rect, cornerIndices[c].ToString(), EditorStyles.whiteBoldLabel);
                        }
                    }
                    else
                    {
                        EditorGUI.DrawRect(rect, outerColor);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(15);
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
