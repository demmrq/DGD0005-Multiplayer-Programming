using System.Collections;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;

public struct LevelFailedRequest : IBroadcast
{
}

public struct LevelFailedNotice : IBroadcast
{
}

public class LevelRestarter : MonoBehaviour
{
    [SerializeField] private string transitionScene = "Restart";
    [SerializeField] private float failDelay = 1.2f;
    [SerializeField] private float requestCooldown = 2f;

    private NetworkManager manager;
    private bool restarting;
    private string levelToReload = "";
    private float showFailedUntil = -1f;
    private float nextRequestTime;

    public static LevelRestarter Instance { get; private set; }

    private void Start()
    {
        manager = GetComponent<NetworkManager>();
        if (manager == null)
            return;
        Instance = this;
        manager.ServerManager.RegisterBroadcast<LevelFailedRequest>(OnFailedRequest);
        manager.ClientManager.RegisterBroadcast<LevelFailedNotice>(OnFailedNotice);
        manager.SceneManager.OnLoadEnd += OnLoadEnd;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (manager == null)
            return;
        if (manager.ServerManager != null)
            manager.ServerManager.UnregisterBroadcast<LevelFailedRequest>(OnFailedRequest);
        if (manager.ClientManager != null)
            manager.ClientManager.UnregisterBroadcast<LevelFailedNotice>(OnFailedNotice);
        if (manager.SceneManager != null)
            manager.SceneManager.OnLoadEnd -= OnLoadEnd;
    }

    public void RequestRestart()
    {
        if (manager == null || !manager.ClientManager.Started || Time.unscaledTime < nextRequestTime)
            return;
        nextRequestTime = Time.unscaledTime + requestCooldown;
        manager.ClientManager.Broadcast(new LevelFailedRequest());
    }

    private void OnFailedRequest(NetworkConnection conn, LevelFailedRequest message, Channel channel)
    {
        if (restarting || !manager.ServerManager.Started)
            return;
        restarting = true;
        levelToReload = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        manager.ServerManager.Broadcast(new LevelFailedNotice());
        StartCoroutine(BeginReload());
    }

    private IEnumerator BeginReload()
    {
        yield return new WaitForSecondsRealtime(failDelay);
        LoadScene(transitionScene);
    }

    private void OnLoadEnd(SceneLoadEndEventArgs args)
    {
        if (!restarting || !args.QueueData.AsServer || args.LoadedScenes == null)
            return;
        foreach (UnityScene scene in args.LoadedScenes)
        {
            if (scene.name == transitionScene)
            {
                LoadScene(levelToReload);
                return;
            }
            if (scene.name == levelToReload)
            {
                restarting = false;
                return;
            }
        }
    }

    private void LoadScene(string sceneName)
    {
        SceneLoadData data = new SceneLoadData(sceneName);
        data.ReplaceScenes = ReplaceOption.All;
        manager.SceneManager.LoadGlobalScenes(data);
    }

    private void OnFailedNotice(LevelFailedNotice message, Channel channel)
    {
        showFailedUntil = Time.unscaledTime + failDelay + 1.5f;
    }

    private void OnGUI()
    {
        if (Time.unscaledTime >= showFailedUntil)
            return;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 48;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(1f, 0.35f, 0.35f);
        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 80), "LEVEL FAILED", style);
    }
}
