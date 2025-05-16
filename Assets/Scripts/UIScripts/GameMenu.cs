using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    public static GameMenu Instance { get; private set; }

    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button changeTeamButton;
    [SerializeField] private Button changeClassButton;
    [SerializeField] private Button respawnButton;

    [SerializeField] private Button aTeamButton;
    [SerializeField] private Button bTeamButton;

    [SerializeField] private Button RiflemanButton;
    [SerializeField] private Button ScoutButton;
    [SerializeField] private Button MachinegunnerButton;
    [SerializeField] private Button BreacherButton;

    [SerializeField] private GameObject gameMenu;
    [SerializeField] private GameObject teamSelectMenu;
    [SerializeField] private GameObject classSelectMenu;

    private PlayerSettings localPlayerSettings;
    private PlayerController playerController;

    PlayerSettings playerSettings;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            var settings = player.GetComponent<PlayerSettings>();
            if (settings != null && settings.IsOwner)
            {
                playerSettings = settings;
                playerController = player.GetComponent<PlayerController>();
                break;
            }
        }

        if (playerSettings == null)
        {
            Debug.LogWarning("Local player not found for GameMenu.");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        changeTeamButton.onClick.AddListener(() =>
        {
            gameMenu.SetActive(false);
            teamSelectMenu.SetActive(true);
        });

        changeClassButton.onClick.AddListener(() =>
        {
            gameMenu.SetActive(false);
            classSelectMenu.SetActive(true);
        });

        respawnButton.onClick.AddListener(() =>
        {
            playerSettings.Respawn();
            GameMenuDeactivate();
        });

        aTeamButton.onClick.AddListener(() => {
            playerSettings.RequestChangeTeam(0);
            TeamSelectMenuDeactivate();
        });

        bTeamButton.onClick.AddListener(() => {
            playerSettings.RequestChangeTeam(1);
            TeamSelectMenuDeactivate();
        });

        resumeButton.onClick.AddListener(() =>
        {
            GameMenuDeactivate();
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.LobbyScene);
        });

        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        RiflemanButton.onClick.AddListener(() =>
        {
            playerController.SetClass(0);
            playerSettings.Respawn();
            ClassSelectMenuDeactivate();
        });

        ScoutButton.onClick.AddListener(() =>
        {
            playerController.SetClass(3);
            playerSettings.Respawn();
            ClassSelectMenuDeactivate();
        });

        MachinegunnerButton.onClick.AddListener(() =>
        {
            playerController.SetClass(2);
            playerSettings.Respawn();
            ClassSelectMenuDeactivate();
        });

        BreacherButton.onClick.AddListener(() =>
        {
            playerController.SetClass(1);
            playerSettings.Respawn();
            ClassSelectMenuDeactivate();
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = gameMenu.activeSelf;
            gameMenu.SetActive(!isActive);

            Cursor.lockState = isActive ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isActive;
        }
    }

    public void RegisterPlayer(PlayerSettings playerSettings)
    {
        localPlayerSettings = playerSettings;
    }

    private void GameMenuDeactivate()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameMenu.SetActive(false);
    }

    private void TeamSelectMenuDeactivate()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        teamSelectMenu.SetActive(false);
    }

    private void ClassSelectMenuDeactivate()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        classSelectMenu.SetActive(false);
    }
}