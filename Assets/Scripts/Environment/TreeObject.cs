using UnityEngine;

/// <summary>
/// Tree visual object with pollution-responsive state management
/// Inherits Y-sorting from VisualObject base class
/// </summary>
public class Tree : VisualObject
{
    [Header("Tree State")]
    [SerializeField] private TreeState currentState = TreeState.Healthy;

    [Header("Tree Sprites")]
    [Tooltip("Sprite for healthy tree")]
    public Sprite healthySprite;

    [Tooltip("Sprite for withered tree")]
    public Sprite witherSprite;

    [Tooltip("Sprite for dead tree")]
    public Sprite deadSprite;

    [Header("References")]
    private TreeProperty treeProperty;

    public enum TreeState
    {
        Healthy,
        Withering,
        Dead
    }

    public TreeState CurrentState => currentState;

    protected override void Awake()
    {
        // Set layer to Environment for trees
        visualLayer = VisualLayer.Environment;

        base.Awake();

        // Initialize with healthy sprite
        if (healthySprite != null)
        {
            spriteRenderer.sprite = healthySprite;
        }
    }

    /// <summary>
    /// Initialize tree with grid position and property reference
    /// </summary>
    public void Initialize(Vector2Int gridPos, TreeProperty property, Sprite healthy, Sprite wither, Sprite dead)
    {
        gridPosition = gridPos;
        treeProperty = property;

        // Set sprites
        healthySprite = healthy;
        witherSprite = wither;
        deadSprite = dead;

        // Set initial state
        SetState(TreeState.Healthy);
    }

    /// <summary>
    /// Update tree visual state (called by TreeProperty when pollution changes)
    /// </summary>
    public void SetState(TreeState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        UpdateSprite();
    }

    /// <summary>
    /// Update sprite based on current state
    /// </summary>
    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case TreeState.Healthy:
                if (healthySprite != null)
                {
                    spriteRenderer.sprite = healthySprite;
                }
                break;

            case TreeState.Withering:
                if (witherSprite != null)
                {
                    spriteRenderer.sprite = witherSprite;
                }
                break;

            case TreeState.Dead:
                if (deadSprite != null)
                {
                    spriteRenderer.sprite = deadSprite;
                }
                break;
        }
    }

    /// <summary>
    /// Destroy this tree GameObject
    /// </summary>
    public void DestroyTree()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Get tree health (0-100)
    /// </summary>
    public float GetHealth()
    {
        if (treeProperty != null)
        {
            return treeProperty.treeHealth;
        }
        return 100f;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw state indicator
        Gizmos.color = currentState == TreeState.Healthy ? Color.green :
                       currentState == TreeState.Withering ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Show tree info
        UnityEditor.Handles.Label(transform.position + Vector3.down * 0.5f,
            $"Tree: {currentState}\nHealth: {GetHealth():F0}%");
    }
#endif
}
