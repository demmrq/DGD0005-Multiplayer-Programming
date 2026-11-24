using UnityEngine;

public class Bridge : PowerTarget
{
    [SerializeField] private BoxCollider2D deck;
    [SerializeField] private SpriteRenderer deckSprite;
    [SerializeField] private float fullLength = 7f;
    [SerializeField] private float retractedLength = 0f;
    [SerializeField] private float thickness = 0.5f;
    [SerializeField] private float speed = 8f;

    private float length;

    private void Awake()
    {
        length = retractedLength;
        Apply();
    }

    protected override void OnPoweredChanged(bool isPowered)
    {
    }

    private void FixedUpdate()
    {
        float goal = Powered ? fullLength : retractedLength;
        float next = Mathf.MoveTowards(length, goal, speed * Time.fixedDeltaTime);
        if (Mathf.Approximately(next, length))
            return;
        length = next;
        Apply();
    }

    private void Apply()
    {
        bool visible = length > 0.05f;
        if (deck != null)
        {
            deck.enabled = visible;
            deck.size = new Vector2(Mathf.Max(length, 0.01f), thickness);
            deck.offset = new Vector2(length * 0.5f, -thickness * 0.5f);
        }
        if (deckSprite != null)
        {
            deckSprite.enabled = visible;
            deckSprite.drawMode = SpriteDrawMode.Tiled;
            deckSprite.size = new Vector2(Mathf.Max(length, 0.01f), thickness);
            deckSprite.transform.localPosition = new Vector3(length * 0.5f, -thickness * 0.5f, 0f);
        }
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.6f);
        Gizmos.DrawWireCube(transform.position + new Vector3(fullLength * 0.5f, -thickness * 0.5f, 0f), new Vector3(fullLength, thickness, 0f));
    }
}
