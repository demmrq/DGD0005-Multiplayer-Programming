using UnityEngine;

public class Lift : PowerTarget
{
    [SerializeField] private Rigidbody2D platform;
    [SerializeField] private float travelHeight = 3f;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private bool cycleWhilePowered = true;
    [SerializeField] private float pauseTime = 2.5f;
    [SerializeField] private float carryCheckHeight = 0.2f;

    private readonly Collider2D[] riders = new Collider2D[16];
    private ContactFilter2D filter;
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
        Vector2 goal = bottomPosition + (WantsTop() ? Vector2.up * travelHeight : Vector2.zero);
        Vector2 next = Vector2.MoveTowards(platform.position, goal, speed * Time.fixedDeltaTime);
        if (next == platform.position)
            return;
        Vector2 delta = next - platform.position;
        if (delta.y < 0f)
            CarryRiders(delta);
        platform.MovePosition(next);
    }

    private void CarryRiders(Vector2 delta)
    {
        Collider2D own = platform.GetComponent<Collider2D>();
        if (own == null)
            return;
        Bounds b = own.bounds;
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
