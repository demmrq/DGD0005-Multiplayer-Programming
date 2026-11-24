using FishNet;
using FishNet.Managing;
using UnityEngine;

[RequireComponent(typeof(RelayConnector))]
public class RelayHud : MonoBehaviour
{
    [SerializeField] private string localAddress = "127.0.0.1";

    private RelayConnector connector;
    private NetworkManager manager;
    private string codeInput = "";
    private bool showLocal;

    private void Awake()
    {
        connector = GetComponent<RelayConnector>();
    }

    private void OnGUI()
    {
        if (manager == null)
        {
            manager = InstanceFinder.NetworkManager;
            if (manager == null)
                return;
        }

        GUILayout.BeginArea(new Rect(12, 12, 260, Screen.height - 24));
        GUILayout.BeginVertical(GUI.skin.box);
        bool server = manager.ServerManager.Started;
        bool client = manager.ClientManager.Started;

        if (!server && !client)
            DrawMenu();
        else
            DrawConnected(server, client);

        if (!string.IsNullOrEmpty(connector.Status))
            GUILayout.Label(connector.Status);
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawMenu()
    {
        GUI.enabled = !connector.Busy;
        if (GUILayout.Button("Host Online", GUILayout.Height(32)))
            connector.HostOnline();

        GUILayout.Space(6);
        GUILayout.Label("Join code");
        codeInput = GUILayout.TextField(codeInput, 12).ToUpperInvariant();
        if (GUILayout.Button("Join", GUILayout.Height(32)))
            connector.JoinOnline(codeInput);

        GUILayout.Space(6);
        showLocal = GUILayout.Toggle(showLocal, "Local network");
        if (showLocal)
        {
            localAddress = GUILayout.TextField(localAddress);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Host Local"))
                connector.HostLocal();
            if (GUILayout.Button("Join Local"))
                connector.JoinLocal(localAddress);
            GUILayout.EndHorizontal();
        }
        GUI.enabled = true;
    }

    private void DrawConnected(bool server, bool client)
    {
        GUILayout.Label(server && client ? "Host" : server ? "Server" : "Client");
        if (server && !string.IsNullOrEmpty(connector.JoinCode))
        {
            GUIStyle codeStyle = new GUIStyle(GUI.skin.label);
            codeStyle.fontSize = 28;
            codeStyle.fontStyle = FontStyle.Bold;
            codeStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(connector.JoinCode, codeStyle, GUILayout.Height(40));
            if (GUILayout.Button("Copy Code"))
                GUIUtility.systemCopyBuffer = connector.JoinCode;
        }
        if (GUILayout.Button("Stop", GUILayout.Height(32)))
            connector.Stop();
    }
}
