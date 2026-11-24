using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class ExitZone : NetworkBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(1.6f, 2f);
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);
    [SerializeField] private int requiredPlayers = 2;
    [SerializeField] private string nextScene = "";
    [SerializeField] private float loadDelay = 2f;
    [SerializeField] private GameObject completeIndicator;

    private readonly SyncVar<bool> completed = new SyncVar<bool>();
    private readonly Collider2D[] hits = new Collider2D[16];
    private ContactFilter2D filter;
    private float completedTime;
    private bool loadRequested;

    private void Awake()
    {
        filter = new ContactFilter2D { useTriggers = false };
        completed.OnChange += OnCompletedChanged;
    }

    private void OnDestroy()
    {
        completed.OnChange -= OnCompletedChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyIndicator(completed.Value);
    }

    private void FixedUpdate()
    {
        if (!IsServerInitialized)
            return;
        if (completed.Value)
        {
            if (!loadRequested && !string.IsNullOrEmpty(nextScene) && Time.time - completedTime >= loadDelay)
            {
                loadRequested = true;
                SceneLoadData data = new SceneLoadData(nextScene);
                data.ReplaceScenes = ReplaceOption.All;
                NetworkManager.SceneManager.LoadGlobalScenes(data);
            }
            return;
        }

        int count = Physics2D.OverlapBox((Vector2)transform.position + offset, size, 0f, filter, hits);
        int inside = 0;
        PlayerController[] seen = new PlayerController[count];
        for (int i = 0; i < count; i++)
        {
            PlayerController p = hits[i].GetComponentInParent<PlayerController>();
            if (p == null || System.Array.IndexOf(seen, p) >= 0)
                continue;
            seen[inside] = p;
            inside++;
        }
        if (inside >= requiredPlayers)
        {
            completed.Value = true;
            completedTime = Time.time;
        }
    }

    private void OnCompletedChanged(bool previous, bool next, bool asServer)
    {
        ApplyIndicator(next);
    }

    private void ApplyIndicator(bool on)
    {
        if (completeIndicator != null)
            completeIndicator.SetActive(on);
    }

    private void OnGUI()
    {
        if (!completed.Value || !IsClientInitialized)
            return;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 48;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(0.4f, 0.95f, 0.6f);
        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 80), "LEVEL COMPLETE", style);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
        Gizmos.DrawWireCube((Vector2)transform.position + offset, size);
    }
}
