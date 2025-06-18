using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class GameLobby : MonoBehaviour
{
    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    public static GameLobby Instance { get; private set; }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float listLobbiesTimer;

    private bool isCreatingLobby = false;
    private bool isJoiningLobby = false;

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());
            await UnityServices.InitializeAsync();

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void Update()
    {
        HandleHeartbeat();
        HandlePeriodicListLobbies();
    }

    private void HandlePeriodicListLobbies()
    {
        if (joinedLobby == null && AuthenticationService.Instance.IsSignedIn)
        {
            listLobbiesTimer -= Time.deltaTime;
            if (listLobbiesTimer <= 0)
            {
                float listLobbiesTimerMax = 3f;
                listLobbiesTimer = listLobbiesTimerMax;
                ListLobbies();
            }
        }
    }

    private void HandleHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void ListLobbies()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs
            {
                lobbyList = queryResponse.Results
            });
        } catch (LobbyServiceException e) 
        {
            Debug.LogException(e);
        }
    }
    
    private async Task<Allocation> AllocateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(GameMultiplayer.MAX_PLAYER_AMOUNT);

            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);

            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            return relayJoinCode;
        } catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            return await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay join failed: {e}");
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        if (isCreatingLobby) return;
        isCreatingLobby = true;

        try
        {
            Debug.Log("Creating lobby...");

            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GameMultiplayer.MAX_PLAYER_AMOUNT, new CreateLobbyOptions
            {
                IsPrivate = isPrivate
            });
            Debug.Log("Created lobby: " + joinedLobby.Name);

            Allocation allocation = await AllocateRelay();
            if (allocation == null)
            {
                Debug.LogError("Allocation failed");
                return;
            }

            string relayJoinCode = await GetRelayJoinCode(allocation);
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.LogError("Failed to get relay join code");
                return;
            }
            Debug.Log("Got relay join code: " + relayJoinCode);

            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                {KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
            }
            });

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            Debug.Log("Host relay data set");

            GameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobby creation failed: {e}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error: {e}");
        }

        finally
        {
            isCreatingLobby = false;
        }
    }

    public async Task<bool> JoinWithId(string lobbyId)
    {
        if (isJoiningLobby) return false;
        isJoiningLobby = true;

        try
        {
            Debug.Log("Joining lobby with ID: " + lobbyId);

            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log("Joined lobby: " + joinedLobby.Name);

            if (!joinedLobby.Data.ContainsKey(KEY_RELAY_JOIN_CODE))
            {
                Debug.LogError("No Relay join code in lobby data");
                return false;
            }

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            if (joinAllocation == null)
            {
                Debug.LogError("Failed to join Relay");
                return false;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            GameMultiplayer.Instance.StartClient();

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e}");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to join Relay: {e}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error: {e}");
        }
        finally
        {
            isJoiningLobby = false;
        }

        return false; // додано, щоб задовольнити всі шляхи виконання
    }

    public async void QuickJoin()
    {
        if (isJoiningLobby) return;
        isJoiningLobby = true;

        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            GameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Quick join failed: {e}");
        }
        finally
        {
            isJoiningLobby = false;
        }
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }
}
