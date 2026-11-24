using System.Collections.Generic;
using FishNet.Component.Spawning;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;

public class LevelPlayerRespawner : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private float waitForAllTimeout = 10f;

    private readonly Dictionary<int, int> rememberedSlots = new Dictionary<int, int>();
    private NetworkManager manager;
    private PlayerSpawner spawner;
    private UnityScene levelScene;
    private Transform[] points = new Transform[0];
    private bool pending;
    private float pendingSince;

    private void Start()
    {
        manager = GetComponent<NetworkManager>();
        spawner = GetComponent<PlayerSpawner>();
        if (manager == null)
            return;
        manager.SceneManager.OnLoadStart += OnLoadStart;
        manager.SceneManager.OnLoadEnd += OnLoadEnd;
    }

    private void OnDestroy()
    {
        if (manager == null || manager.SceneManager == null)
            return;
        manager.SceneManager.OnLoadStart -= OnLoadStart;
        manager.SceneManager.OnLoadEnd -= OnLoadEnd;
    }

    private void OnLoadStart(SceneLoadStartEventArgs args)
    {
        if (!args.QueueData.AsServer)
            return;
        PlayerVisuals[] players = FindObjectsByType<PlayerVisuals>(FindObjectsSortMode.None);
        if (players.Length == 0)
            return;
        rememberedSlots.Clear();
        foreach (PlayerVisuals p in players)
            if (p.Owner != null && p.Owner.IsValid && p.Slot >= 0)
                rememberedSlots[p.OwnerId] = p.Slot;
    }

    private void OnLoadEnd(SceneLoadEndEventArgs args)
    {
        if (!args.QueueData.AsServer || args.LoadedScenes == null)
            return;
        foreach (UnityScene scene in args.LoadedScenes)
        {
            LevelSpawnPoints found = FindSpawnPoints(scene);
            if (found == null)
                continue;
            levelScene = scene;
            points = found.Points;
            if (spawner != null)
                spawner.Spawns = points;
            pending = true;
            pendingSince = Time.unscaledTime;
            return;
        }
    }

    private void Update()
    {
        if (!pending)
            return;
        if (manager == null || !manager.IsServerStarted || !levelScene.IsValid())
        {
            pending = false;
            return;
        }

        List<NetworkConnection> waiting = new List<NetworkConnection>();
        foreach (NetworkConnection conn in manager.ServerManager.Clients.Values)
            if (conn.IsActive && conn.IsAuthenticated && !HasPlayer(conn))
                waiting.Add(conn);
        if (waiting.Count == 0)
        {
            pending = false;
            return;
        }

        HashSet<NetworkConnection> present;
        manager.SceneManager.SceneConnections.TryGetValue(levelScene, out present);
        List<NetworkConnection> ready = new List<NetworkConnection>();
        foreach (NetworkConnection conn in waiting)
            if (present != null && present.Contains(conn))
                ready.Add(conn);

        bool timedOut = Time.unscaledTime - pendingSince >= waitForAllTimeout;
        if (ready.Count == 0 || (ready.Count < waiting.Count && !timedOut))
            return;

        ready.Sort((a, b) => SortKey(a).CompareTo(SortKey(b)));
        foreach (NetworkConnection conn in ready)
            SpawnPlayer(conn);
        if (ready.Count == waiting.Count)
            pending = false;
    }

    private int SortKey(NetworkConnection conn)
    {
        int slot;
        if (rememberedSlots.TryGetValue(conn.ClientId, out slot))
            return slot * 100000 + conn.ClientId;
        return 99 * 100000 + conn.ClientId;
    }

    private void SpawnPlayer(NetworkConnection conn)
    {
        int slot = NextFreeSlot();
        Transform point = points.Length > 0 ? points[slot % points.Length] : null;
        Vector3 position = point != null ? point.position : Vector3.zero;
        Quaternion rotation = point != null ? point.rotation : Quaternion.identity;
        NetworkObject nob = manager.GetPooledInstantiated(playerPrefab, position, rotation, true);
        manager.ServerManager.Spawn(nob, conn);
        manager.SceneManager.AddOwnerToDefaultScene(nob);
    }

    private static LevelSpawnPoints FindSpawnPoints(UnityScene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            LevelSpawnPoints found = root.GetComponentInChildren<LevelSpawnPoints>(true);
            if (found != null)
                return found;
        }
        return null;
    }

    private static int NextFreeSlot()
    {
        PlayerVisuals[] players = FindObjectsByType<PlayerVisuals>(FindObjectsSortMode.None);
        for (int candidate = 0; candidate < 8; candidate++)
        {
            bool used = false;
            foreach (PlayerVisuals p in players)
            {
                if (p.Slot == candidate)
                {
                    used = true;
                    break;
                }
            }
            if (!used)
                return candidate;
        }
        return 0;
    }

    private static bool HasPlayer(NetworkConnection conn)
    {
        foreach (NetworkObject nob in conn.Objects)
            if (nob != null && nob.GetComponent<PlayerController>() != null)
                return true;
        return false;
    }
}
