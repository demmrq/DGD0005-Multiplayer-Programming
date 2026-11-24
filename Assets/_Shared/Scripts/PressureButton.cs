using UnityEngine;

public class PressureButton : PowerSource
{
    [SerializeField] private Vector2 detectSize = new Vector2(0.8f, 0.5f);
    [SerializeField] private Vector2 detectOffset = new Vector2(0f, 0.3f);
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;

    private readonly Collider2D[] hits = new Collider2D[16];
    private ContactFilter2D filter;

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
        if (!IsServerInitialized)
            return;
        Vector2 center = (Vector2)transform.position + detectOffset;
        int count = Physics2D.OverlapBox(center, detectSize, 0f, filter, hits);
        bool pressed = false;
        for (int i = 0; i < count; i++)
        {
            Collider2D c = hits[i];
            if (c.GetComponentInParent<PlayerController>() != null || c.GetComponentInParent<Crate>() != null)
            {
                pressed = true;
                break;
            }
        }
        SetOn(pressed);
    }

    protected override void OnPowerStateChanged(bool on)
    {
        ApplyVisual(on);
    }

    private void ApplyVisual(bool on)
    {
        if (spriteRenderer != null && upSprite != null && downSprite != null)
            spriteRenderer.sprite = on ? downSprite : upSprite;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.6f);
        Gizmos.DrawWireCube((Vector2)transform.position + detectOffset, detectSize);
    }
}
