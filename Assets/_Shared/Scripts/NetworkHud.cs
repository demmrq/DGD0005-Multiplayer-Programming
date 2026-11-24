using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

public class NetworkHud : MonoBehaviour
{
    [SerializeField] private string address = "127.0.0.1";

    private NetworkManager manager;

    private void Start()
    {
        manager = InstanceFinder.NetworkManager;
    }

    private void OnGUI()
    {
        if (manager == null)
        {
            manager = InstanceFinder.NetworkManager;
            if (manager == null)
                return;
        }

        GUILayout.BeginArea(new Rect(12, 12, 240, 220), GUI.skin.box);
        bool server = manager.ServerManager.Started;
        bool client = manager.ClientManager.Started;

        if (!server && !client)
        {
            GUILayout.Label("Address");
            address = GUILayout.TextField(address);
            if (GUILayout.Button("Host", GUILayout.Height(32)))
            {
                manager.ServerManager.StartConnection();
                manager.ClientManager.StartConnection();
            }
            if (GUILayout.Button("Client", GUILayout.Height(32)))
                manager.ClientManager.StartConnection(address);
            if (GUILayout.Button("Server", GUILayout.Height(32)))
                manager.ServerManager.StartConnection();
        }
        else
        {
            GUILayout.Label(server && client ? "Host" : server ? "Server" : "Client");
            if (GUILayout.Button("Stop", GUILayout.Height(32)))
            {
                if (client)
                    manager.ClientManager.StopConnection();
                if (server)
                    manager.ServerManager.StopConnection(true);
            }
        }
        GUILayout.EndArea();
    }
}
