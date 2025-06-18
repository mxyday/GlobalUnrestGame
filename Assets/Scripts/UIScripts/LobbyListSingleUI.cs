using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListSingleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI errorText;

    private Lobby lobby;

    private async void TryJoinLobby()
    {
        if (lobby == null)
        {
            Debug.LogWarning("Lobby is null");
            return;
        }

        bool success = await GameLobby.Instance.JoinWithId(lobby.Id);
        if (!success)
        {
            errorText.gameObject.SetActive(true);
        }
    }

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => TryJoinLobby());
        errorText.gameObject.SetActive(false);
    }

    public void SetLobby(Lobby lobby)
    {
        this.lobby = lobby;
        lobbyNameText.text = lobby.Name;
    }
}
