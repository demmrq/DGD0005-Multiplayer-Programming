using System;
using System.Threading.Tasks;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayConnector : MonoBehaviour
{
    [SerializeField] private int maxClients = 1;
    [SerializeField] private string connectionType = "dtls";
    [SerializeField] private ushort localPort = 7770;

    private NetworkManager manager;
    private UnityTransport transport;
    private Task signInTask;

    public string JoinCode { get; private set; } = "";
    public string Status { get; private set; } = "";
    public bool Busy { get; private set; }

    private void Awake()
    {
        manager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();
    }

    public async void HostOnline()
    {
        if (!BeginOperation("Creating game..."))
            return;
        try
        {
            await EnsureSignedIn();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxClients);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            transport.SetRelayServerData(allocation.ToRelayServerData(connectionType));
            manager.ServerManager.StartConnection();
            manager.ClientManager.StartConnection();
            JoinCode = code;
            Status = "Share this code with your friend";
        }
        catch (Exception e)
        {
            Fail(e);
        }
        Busy = false;
    }

    public async void JoinOnline(string code)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            Status = "Enter a join code";
            return;
        }
        if (!BeginOperation("Joining " + code + "..."))
            return;
        try
        {
            await EnsureSignedIn();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code);
            transport.SetRelayServerData(allocation.ToRelayServerData(connectionType));
            manager.ClientManager.StartConnection();
            JoinCode = code;
            Status = "Code " + code;
        }
        catch (Exception e)
        {
            Fail(e);
        }
        Busy = false;
    }

    public void HostLocal()
    {
        if (!BeginOperation("Hosting on local network"))
            return;
        transport.SetConnectionData("127.0.0.1", localPort, "0.0.0.0");
        manager.ServerManager.StartConnection();
        manager.ClientManager.StartConnection();
        JoinCode = "";
        Busy = false;
    }

    public void JoinLocal(string address)
    {
        address = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
        if (!BeginOperation("Joining " + address))
            return;
        transport.SetConnectionData(address, localPort);
        manager.ClientManager.StartConnection();
        JoinCode = "";
        Busy = false;
    }

    public void Stop()
    {
        if (manager.ClientManager.Started)
            manager.ClientManager.StopConnection();
        if (manager.ServerManager.Started)
            manager.ServerManager.StopConnection(true);
        JoinCode = "";
        Status = "";
    }

    private bool BeginOperation(string status)
    {
        if (Busy || manager == null || transport == null)
            return false;
        Busy = true;
        Status = status;
        return true;
    }

    private void Fail(Exception e)
    {
        Debug.LogException(e);
        Status = "Failed: " + e.Message;
        JoinCode = "";
    }

    private Task EnsureSignedIn()
    {
        if (signInTask == null || signInTask.IsFaulted || signInTask.IsCanceled)
            signInTask = SignIn();
        return signInTask;
    }

    private static async Task SignIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions options = new InitializationOptions();
            options.SetProfile("p" + Guid.NewGuid().ToString("N").Substring(0, 12));
            await UnityServices.InitializeAsync(options);
        }
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}
