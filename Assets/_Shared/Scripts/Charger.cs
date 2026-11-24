using UnityEngine;

public class Charger : PowerSource
{
    [SerializeField] private Vector2 detectSize = new Vector2(1f, 1.2f);
    [SerializeField] private Vector2 detectOffset = new Vector2(0f, 0.6f);
    [SerializeField] private Vector2 checkpointOffset = new Vector2(0f, 0.05f);
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;

    private readonly Collider2D[] hits = new Collider2D[16];
    private ContactFilter2D filter;
    private PlayerController lastCheckpointed;

    protected override void Awake()
    {
        base.Awake();
        filter = new ContactFilter2D { useTriggers = false };
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyVisual(IsOn);
    }

    private void FixedUpdate()
    {
        bool server = IsServerInitialized;
        PlayerController local = IsClientInitialized ? PlayerController.Local : null;
        if (!server && local == null)
            return;

        Vector2 center = (Vector2)transform.position + detectOffset;
        int count = Physics2D.OverlapBox(center, detectSize, 0f, filter, hits);
        bool anyPlayer = false;
        bool localInside = false;
        for (int i = 0; i < count; i++)
        {
            PlayerController p = hits[i].GetComponentInParent<PlayerController>();
            if (p == null)
                continue;
            anyPlayer = true;
            if (p == local)
                localInside = true;
        }

        if (server && anyPlayer && !IsOn)
            SetOn(true);

        if (localInside && lastCheckpointed != local)
        {
            local.SetCheckpoint((Vector2)transform.position + checkpointOffset);
            lastCheckpointed = local;
        }
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

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireCube((Vector2)transform.position + detectOffset, detectSize);
    }
}
