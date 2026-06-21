using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("Flow")]
    [Tooltip("Skip the main menu and start the game immediately on play.")]
    [SerializeField] private bool _skipMenu;
    [Tooltip("Master switch for enemy encounters and recurring spawns. Off = no enemies at all.")]
    [SerializeField] private bool _enableSpawning = true;

    [Header("Player")]
    [Tooltip("Sprint never depletes.")]
    [SerializeField] private bool _unlimitedSprint;
    [Tooltip("Multiplies walk and sprint speed by 3.")]
    [SerializeField] private bool _fasterMovement;
    [Tooltip("Sets the player's health to a million.")]
    [SerializeField] private bool _unlimitedHealth;
    [Tooltip("Start with all 3 ember balls already in the inventory.")]
    [SerializeField] private bool _startWithEmbers;
    [Tooltip("Press F to instantly kill the player.")]
    [SerializeField] private bool _enableKillKey;

    public const float FasterMovementMultiplier = 3f;
    public const int UnlimitedHealthAmount = 1000000;
    public const int StartingEmberCount = 3;

    public bool SkipMenu => _skipMenu;
    public bool EnableSpawning => _enableSpawning;
    public bool UnlimitedSprint => _unlimitedSprint;
    public bool FasterMovement => _fasterMovement;
    public bool UnlimitedHealth => _unlimitedHealth;
    public bool StartWithEmbers => _startWithEmbers;
    public bool EnableKillKey => _enableKillKey;

    public static bool SpawningEnabled => Instance == null || Instance._enableSpawning;
    public static bool IsUnlimitedSprint => Instance != null && Instance._unlimitedSprint;
    public static bool IsFasterMovement => Instance != null && Instance._fasterMovement;
    public static bool ShouldSkipMenu => Instance != null && Instance._skipMenu;
    public static bool IsUnlimitedHealth => Instance != null && Instance._unlimitedHealth;
    public static bool ShouldStartWithEmbers => Instance != null && Instance._startWithEmbers;
    public static bool IsKillKeyEnabled => Instance != null && Instance._enableKillKey;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
