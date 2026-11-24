using UnityEngine;

public class CrushGuard : MonoBehaviour
{
    [SerializeField] private Collider2D crusher;
    [SerializeField] private float checkDepth = 0.3f;
    [SerializeField] private float clearance = 0.03f;

    private readonly Collider2D[] hits = new Collider2D[16];
    private readonly Collider2D[] blockers = new Collider2D[16];
    private ContactFilter2D filter;
    private float lastBottom;
    private bool hasLast;

    private void Awake()
    {
        filter = new ContactFilter2D { useTriggers = false };
    }

    private void FixedUpdate()
    {
        if (crusher == null || !crusher.enabled)
            return;
        Bounds c = crusher.bounds;
        bool movingDown = hasLast && c.min.y < lastBottom - 0.0001f;
        lastBottom = c.min.y;
        hasLast = true;
        if (!movingDown)
            return;

        Vector2 center = new Vector2(c.center.x, c.min.y - checkDepth * 0.5f);
        Vector2 size = new Vector2(c.size.x, checkDepth);
        int count = Physics2D.OverlapBox(center, size, 0f, filter, hits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];
            Rigidbody2D body = hit.attachedRigidbody;
            if (hit == crusher || body == null || body == crusher.attachedRigidbody || body.bodyType != RigidbodyType2D.Dynamic)
                continue;
            Bounds b = hit.bounds;
            if (b.center.y >= c.min.y || b.max.x <= c.min.x || b.min.x >= c.max.x)
                continue;
            PushOut(body, hit, b, c);
        }
    }

    private void PushOut(Rigidbody2D body, Collider2D own, Bounds b, Bounds c)
    {
        float left = c.min.x - clearance - b.max.x;
        float right = c.max.x + clearance - b.min.x;
        float first = Mathf.Abs(left) <= Mathf.Abs(right) ? left : right;
        float second = first == left ? right : left;
        if (IsFree(own, b, first))
            body.position += new Vector2(first, 0f);
        else if (IsFree(own, b, second))
            body.position += new Vector2(second, 0f);
    }

    private bool IsFree(Collider2D own, Bounds b, float dx)
    {
        Vector2 center = new Vector2(b.center.x + dx, b.center.y + 0.02f);
        Vector2 size = new Vector2(b.size.x * 0.95f, b.size.y * 0.9f);
        int count = Physics2D.OverlapBox(center, size, 0f, filter, blockers);
        for (int i = 0; i < count; i++)
        {
            Collider2D other = blockers[i];
            if (other == own || other == crusher)
                continue;
            if (other.attachedRigidbody != null && other.attachedRigidbody == own.attachedRigidbody)
                continue;
            return false;
        }
        return true;
    }
}
