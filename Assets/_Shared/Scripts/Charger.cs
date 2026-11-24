using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class Charger : PowerSource
{
    [SerializeField] private Vector2 detectSize = new Vector2(1.6f, 1.5f);
    [SerializeField] private Vector2 detectOffset = new Vector2(0f, 0.75f);
    [SerializeField] private Vector2 checkpointOffset = new Vector2(0f, 0.05f);
    [SerializeField] private float activeDuration = 10f;
    [SerializeField] private float serverRangeMargin = 0.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;

    private readonly SyncVar<uint> activeUntilTick = new SyncVar<uint>();
    private PlayerController lastCheckpointed;
    private bool localInRange;

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyVisual(IsOn);
    }

    private void Update()
    {
        localInRange = false;
        if (!IsClientInitialized)
            return;
        PlayerController local = PlayerController.Local;
        if (local == null || !InRange(local, 0f))
            return;
        localInRange = true;
        if (lastCheckpointed != local)
        {
            local.SetCheckpoint((Vector2)transform.position + checkpointOffset);
            lastCheckpointed = local;
        }
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            RequestActivate();
    }

    private void FixedUpdate()
    {
        if (!IsServerInitialized || !IsOn)
            return;
        if (TimeManager.Tick >= activeUntilTick.Value)
            SetOn(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestActivate(NetworkConnection caller = null)
    {
        if (caller == null || !CallerInRange(caller))
            return;
        activeUntilTick.Value = TimeManager.Tick + TimeManager.TimeToTicks(activeDuration);
        SetOn(true);
    }

    private bool CallerInRange(NetworkConnection caller)
    {
        foreach (NetworkObject nob in caller.Objects)
        {
            if (nob == null)
                continue;
            PlayerController p = nob.GetComponent<PlayerController>();
            if (p != null && InRange(p, serverRangeMargin))
                return true;
        }
        return false;
    }

    private bool InRange(PlayerController player, float margin)
    {
        Collider2D col = player.GetComponent<Collider2D>();
        if (col == null)
            return false;
        Bounds area = new Bounds((Vector2)transform.position + detectOffset, detectSize + Vector2.one * margin * 2f);
        Bounds b = col.bounds;
        b.center = new Vector3(b.center.x, b.center.y, area.center.z);
        return area.Intersects(b);
    }

    protected override void OnPowerStateChanged(bool on)
    {
        ApplyVisual(on);
    }

    private void ApplyVisual(bool on)
    {
        if (spriteRenderer != null && offSprite != null && onSprite != null)
            spriteRenderer.sprite = on ? onSprite : offSprite;
    }

    private float RemainingSeconds()
    {
        if (!IsOn || TimeManager == null)
            return 0f;
        long ticks = (long)activeUntilTick.Value - TimeManager.Tick;
        return Mathf.Max(0f, (float)(ticks * TimeManager.TickDelta));
    }

    private void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null || !IsClientInitialized)
            return;

        if (IsOn)
        {
            Vector3 top = cam.WorldToScreenPoint(transform.position + new Vector3(0f, 1.9f, 0f));
            if (top.z > 0f)
            {
                GUIStyle timer = new GUIStyle(GUI.skin.label);
                timer.fontSize = 22;
                timer.fontStyle = FontStyle.Bold;
                timer.alignment = TextAnchor.MiddleCenter;
                timer.normal.textColor = new Color(0.45f, 1f, 0.55f);
                GUI.Label(new Rect(top.x - 40f, Screen.height - top.y - 15f, 80f, 30f), Mathf.CeilToInt(RemainingSeconds()).ToString(), timer);
            }
        }

        PlayerController local = PlayerController.Local;
        if (!localInRange || local == null)
            return;
        Vector3 p = cam.WorldToScreenPoint(local.transform.position + new Vector3(0.75f, 0.75f, 0f));
        if (p.z <= 0f)
            return;
        GUIStyle key = new GUIStyle(GUI.skin.box);
        key.fontSize = 20;
        key.fontStyle = FontStyle.Bold;
        key.alignment = TextAnchor.MiddleCenter;
        key.normal.textColor = Color.white;
        GUI.Box(new Rect(p.x - 16f, Screen.height - p.y - 16f, 32f, 32f), "E", key);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireCube((Vector2)transform.position + detectOffset, detectSize);
    }
}
