using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    public static GameMenu Instance { get; private set; }

    [SerializeField] private Button leftTeamButton;
    [SerializeField] private Button rightTeamButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject gameMenu;

    private PlayerSettings localPlayerSettings;

    PlayerSettings playerSettings;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            playerSettings = players[0].GetComponent<PlayerSettings>();
        }
        else
        {
            Debug.LogWarning("No player objects found with tag 'Player'");
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

        leftTeamButton.onClick.AddListener(() => {
            playerSettings.SetTeamColorServerRpc(0);
            GameMenuDeactivate();
        });

        rightTeamButton.onClick.AddListener(() => {
            playerSettings.SetTeamColorServerRpc(1);
            GameMenuDeactivate();
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
}
