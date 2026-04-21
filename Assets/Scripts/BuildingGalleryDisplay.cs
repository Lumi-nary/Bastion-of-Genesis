using UnityEngine;

/// <summary>
/// Spawns every building from a BuildingDatabase in a grid for visual inspection.
/// Attach to an empty GameObject in a dedicated gallery scene.
/// </summary>
public class BuildingGalleryDisplay : MonoBehaviour
{
    [Tooltip("If null, loads 'Data/Buildings/BuildingDatabase' from Resources.")]
    public BuildingDatabase database;

    [Tooltip("Horizontal spacing between buildings (world units).")]
    public float spacingX = 5f;

    [Tooltip("Vertical spacing between rows (world units).")]
    public float spacingY = 5f;

    [Tooltip("Number of buildings per row.")]
    public int columns = 5;

    [Tooltip("If true, adds a 3D TextMesh label under each building.")]
    public bool showLabels = true;

    [Tooltip("Font size for name labels.")]
    public int labelFontSize = 32;

    void Start()
    {
        if (database == null)
        {
            database = Resources.Load<BuildingDatabase>("Data/Buildings/BuildingDatabase");
        }

        if (database == null)
        {
            Debug.LogError("[BuildingGalleryDisplay] No BuildingDatabase assigned and none found in Resources/Data/Buildings/BuildingDatabase.");
            return;
        }

        SpawnAll();
    }

    void SpawnAll()
    {
        int index = 0;
        foreach (var data in database.availableBuildings)
        {
            if (data == null) continue;

            int row = index / columns;
            int col = index % columns;
            Vector3 pos = new Vector3(col * spacingX, -row * spacingY, 0f);

            GameObject parent = new GameObject($"Slot_{index}_{data.buildingName}");
            parent.transform.SetParent(transform);
            parent.transform.position = pos;

            if (data.prefab != null)
            {
                GameObject instance = Instantiate(data.prefab, pos, Quaternion.identity, parent.transform);
                instance.name = data.buildingName;

                // Disable Building component so it doesn't try to register with managers that don't exist in this scene.
                var buildingComponent = instance.GetComponent<Building>();
                if (buildingComponent != null) buildingComponent.enabled = false;

                // Disable any Turret scripts that might try to find targets.
                var turret = instance.GetComponent<Turret>();
                if (turret != null) turret.enabled = false;

                // Disable colliders/rigidbodies so they don't interact.
                foreach (var col2d in instance.GetComponentsInChildren<Collider2D>()) col2d.enabled = false;
                foreach (var rb in instance.GetComponentsInChildren<Rigidbody2D>()) rb.simulated = false;
            }
            else
            {
                // Fallback placeholder so missing prefabs are still visible.
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Quad);
                placeholder.transform.SetParent(parent.transform);
                placeholder.transform.localPosition = Vector3.zero;
                placeholder.name = "(missing prefab)";
                Destroy(placeholder.GetComponent<Collider>());
            }

            if (showLabels)
            {
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(parent.transform);
                labelObj.transform.localPosition = new Vector3(0f, -spacingY * 0.4f, 0f);

                TextMesh text = labelObj.AddComponent<TextMesh>();
                text.text = string.IsNullOrEmpty(data.buildingName) ? data.name : data.buildingName;
                text.anchor = TextAnchor.UpperCenter;
                text.alignment = TextAlignment.Center;
                text.fontSize = labelFontSize;
                text.characterSize = 0.08f;
                text.color = Color.white;

                // Put label on default sorting layer, high order so it draws above sprites.
                MeshRenderer mr = labelObj.GetComponent<MeshRenderer>();
                if (mr != null) mr.sortingOrder = 1000;
            }

            index++;
        }

        // Re-center camera on the grid.
        if (Camera.main != null && index > 0)
        {
            int rows = Mathf.CeilToInt(index / (float)columns);
            float cx = (columns - 1) * spacingX * 0.5f;
            float cy = -(rows - 1) * spacingY * 0.5f;
            Camera.main.transform.position = new Vector3(cx, cy, -10f);
            Camera.main.orthographic = true;
            Camera.main.orthographicSize = Mathf.Max(rows * spacingY, columns * spacingX) * 0.6f;
        }
    }
}
