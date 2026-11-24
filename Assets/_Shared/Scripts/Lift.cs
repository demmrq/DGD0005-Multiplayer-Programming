using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class Lift : PowerTarget
{
    [SerializeField] private Rigidbody2D platform;
    [SerializeField] private NetworkBehaviour platformAnchor;
    [SerializeField] private float travelHeight = 3f;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private bool cycleWhilePowered = true;
    [SerializeField] private float pauseTime = 2.5f;
    [SerializeField] private float carryCheckHeight = 0.2f;
    [SerializeField] private float rideCheckHeight = 0.12f;

    private readonly Collider2D[] riders = new Collider2D[16];
    private readonly HashSet<NetworkObject> onTop = new HashSet<NetworkObject>();
    private readonly List<Rigidbody2D> frozenInterpolation = new List<Rigidbody2D>();
    private readonly List<NetworkObject> parentedHere = new List<NetworkObject>();
    private ContactFilter2D filter;
    private Collider2D platformCollider;
    private Vector2 bottomPosition;
    private bool hasBottomPosition;

    private void Awake()
    {
        filter = new ContactFilter2D { useTriggers = false };
        CacheBottom();
    }

    private void CacheBottom()
    {
        if (hasBottomPosition || platform == null)
            return;
        platform.bodyType = RigidbodyType2D.Kinematic;
        platformCollider = platform.GetComponent<Collider2D>();
        bottomPosition = platform.position;
        hasBottomPosition = true;
    }

    protected override void OnPoweredChanged(bool isPowered)
    {
        CacheBottom();
    }

    private bool WantsTop()
    {
        if (!Powered)
            return false;
        if (!cycleWhilePowered || TimeManager == null)
            return true;
        float period = 2f * (travelHeight / Mathf.Max(speed, 0.01f) + pauseTime);
        double time = TimeManager.Tick * TimeManager.TickDelta;
        float phase = (float)(time % period);
        return phase < period * 0.5f;
    }

    private void FixedUpdate()
    {
        if (platform == null || !hasBottomPosition)
            return;
        UpdateRiders();
        Vector2 goal = bottomPosition + (WantsTop() ? Vector2.up * travelHeight : Vector2.zero);
        Vector2 next = Vector2.MoveTowards(platform.position, goal, speed * Time.fixedDeltaTime);
        if (next == platform.position)
            return;
        Vector2 delta = next - platform.position;
        if (delta.y < 0f)
            CarryRiders(delta);
        platform.MovePosition(next);
    }

    private void UpdateRiders()
    {
        if (platformAnchor == null || platformCollider == null || !IsSpawned)
            return;

        onTop.Clear();
        Bounds b = platformCollider.bounds;
        Vector2 center = new Vector2(b.center.x, b.max.y + rideCheckHeight * 0.5f);
        Vector2 size = new Vector2(b.size.x, rideCheckHeight);
        int count = Physics2D.OverlapBox(center, size, 0f, filter, riders);
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D rb = riders[i].attachedRigidbody;
            if (rb == null || rb == platform)
                continue;
            NetworkObject nob = rb.GetComponent<NetworkObject>();
            if (nob != null && nob.IsSpawned)
                onTop.Add(nob);
        }

        foreach (NetworkObject nob in onTop)
        {
            if (!Controls(nob) || nob.transform.parent == platformAnchor.transform)
                continue;
            Rigidbody2D rb = nob.GetComponent<Rigidbody2D>();
            if (rb != null && rb.linearVelocity.y > 0.5f)
                continue;
            if (rb != null)
                nob.transform.position = new Vector3(rb.position.x, rb.position.y, nob.transform.position.z);
            nob.SetParent(platformAnchor);
            if (!parentedHere.Contains(nob))
                parentedHere.Add(nob);
        }

        for (int i = parentedHere.Count - 1; i >= 0; i--)
        {
            NetworkObject nob = parentedHere[i];
            if (nob == null)
            {
                parentedHere.RemoveAt(i);
                continue;
            }
            if (nob.transform.parent != platformAnchor.transform)
            {
                parentedHere.RemoveAt(i);
                continue;
            }
            if (Controls(nob) && !onTop.Contains(nob))
            {
                Rigidbody2D rb = nob.GetComponent<Rigidbody2D>();
                if (rb != null)
                    nob.transform.position = new Vector3(rb.position.x, rb.position.y, nob.transform.position.z);
                nob.UnsetParent();
                parentedHere.RemoveAt(i);
            }
        }

        UpdateRemoteInterpolation();
    }

    private void UpdateRemoteInterpolation()
    {
        Transform anchor = platformAnchor.transform;
        for (int i = frozenInterpolation.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rb = frozenInterpolation[i];
            if (rb != null && rb.transform.parent == anchor && rb.bodyType == RigidbodyType2D.Kinematic)
                continue;
            if (rb != null)
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            frozenInterpolation.RemoveAt(i);
        }
        for (int i = 0; i < anchor.childCount; i++)
        {
            Rigidbody2D rb = anchor.GetChild(i).GetComponent<Rigidbody2D>();
            if (rb == null || rb.bodyType != RigidbodyType2D.Kinematic || frozenInterpolation.Contains(rb))
                continue;
            rb.interpolation = RigidbodyInterpolation2D.None;
            frozenInterpolation.Add(rb);
        }
    }

    private bool Controls(NetworkObject nob)
    {
        if (nob.Owner != null && nob.Owner.IsValid)
            return nob.IsOwner;
        return IsServerInitialized;
    }

    private void CarryRiders(Vector2 delta)
    {
        if (platformCollider == null)
            return;
        Bounds b = platformCollider.bounds;
        Vector2 center = new Vector2(b.center.x, b.max.y + carryCheckHeight * 0.5f);
        Vector2 size = new Vector2(b.size.x, carryCheckHeight);
        int count = Physics2D.OverlapBox(center, size, 0f, filter, riders);
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D rb = riders[i].attachedRigidbody;
            if (rb == null || rb == platform || rb.bodyType != RigidbodyType2D.Dynamic)
                continue;
            if (rb.linearVelocity.y > 0.1f)
                continue;
            rb.position += delta;
        }
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
        Vector3 start = platform != null ? platform.transform.position : transform.position;
        Gizmos.DrawLine(start, start + Vector3.up * travelHeight);
    }
}
