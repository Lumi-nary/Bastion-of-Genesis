using UnityEngine;

[CreateAssetMenu(fileName = "GameplayAudioLibrary", menuName = "Planetfall/Audio/Gameplay Audio Library")]
public class GameplayAudioLibrary : ScriptableObject
{
    [Header("UI")]
    public AudioClip uiButtonClick;
    public AudioClip uiPanelOpen;
    public AudioClip uiPanelClose;
    public AudioClip uiDeny;

    [Header("Building")]
    public AudioClip buildPlacementPreview;
    public AudioClip buildPlaced;
    public AudioClip buildCancel;
    public AudioClip buildConstructionComplete;
    public AudioClip buildDamaged;
    public AudioClip buildDestroyed;

    [Header("Resources And Research")]
    public AudioClip researchStart;
    public AudioClip researchComplete;
    public AudioClip workerAssign;

    [Header("Combat")]
    public AudioClip turretFire;
    public AudioClip turretHit;
    public AudioClip enemyHit;
    public AudioClip enemyDeath;
    public AudioClip waveIncoming;
    public AudioClip baseUnderAttack;

    [Header("World")]
    public AudioClip pollutionSpread;
    public AudioClip tileIntegrate;
}
