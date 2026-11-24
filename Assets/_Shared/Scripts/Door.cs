using UnityEngine;

public class Door : PowerTarget
{
    [SerializeField] private Rigidbody2D panel;
    [SerializeField] private float openHeight = 2f;
    [SerializeField] private float speed = 6f;

    private Vector2 closedPosition;
    private bool hasClosedPosition;

    private void Awake()
    {
        CacheClosed();
    }

    private void CacheClosed()
    {
        if (hasClosedPosition || panel == null)
            return;
        panel.bodyType = RigidbodyType2D.Kinematic;
        closedPosition = panel.position;
        hasClosedPosition = true;
    }

    protected override void OnPoweredChanged(bool isPowered)
    {
        CacheClosed();
    }

    private void FixedUpdate()
    {
        if (panel == null || !hasClosedPosition)
            return;
        Vector2 goal = closedPosition + (Powered ? Vector2.up * openHeight : Vector2.zero);
        Vector2 next = Vector2.MoveTowards(panel.position, goal, speed * Time.fixedDeltaTime);
        if (next != panel.position)
            panel.MovePosition(next);
    }
}
