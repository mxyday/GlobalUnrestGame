using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListSingleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyNameText;

    private Lobby lobby;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (lobby != null)
            {
                GameLobby.Instance.JoinWithId(lobby.Id);
            }
            else
            {
                Debug.LogWarning("Lobby is null");
            }
        });
    }

    public void SetLobby(Lobby lobby)
    {
        this.lobby = lobby;
        lobbyNameText.text = lobby.Name;
    }
}
