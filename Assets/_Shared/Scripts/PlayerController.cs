using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float jumpSpeed = 12f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float killY = -30f;

    private Rigidbody2D body;
    private BoxCollider2D box;
    private readonly Collider2D[] groundHits = new Collider2D[8];
    private ContactFilter2D groundFilter;
    private float lastGroundedTime = -1f;
    private float lastJumpPressedTime = -1f;
    private Vector2 checkpoint;

    public static PlayerController Local { get; private set; }
    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = gravityScale;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        groundFilter = new ContactFilter2D { useTriggers = false };
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
            return;
        Local = this;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = gravityScale;
        checkpoint = body.position;
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.SetTarget(transform);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (Local == this)
            Local = null;
    }

    private void Update()
    {
        if (!IsOwner)
            return;
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;
        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            lastJumpPressedTime = Time.time;
        if (kb.rKey.wasPressedThisFrame)
            Respawn();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        UpdateGrounded();

        float input = 0f;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input += 1f;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.x = input * moveSpeed;

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        if (canJump && wantsJump)
        {
            velocity.y = jumpSpeed;
            lastJumpPressedTime = -1f;
            lastGroundedTime = -1f;
        }

        body.linearVelocity = velocity;

        if (body.position.y < killY)
            Kill();
    }

    private void UpdateGrounded()
    {
        Bounds b = box.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - 0.03f);
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.06f);
        int count = Physics2D.OverlapBox(center, size, 0f, groundFilter, groundHits);
        IsGrounded = false;
        for (int i = 0; i < count; i++)
        {
            if (groundHits[i].attachedRigidbody == body)
                continue;
            IsGrounded = true;
            break;
        }
        if (IsGrounded && body.linearVelocity.y <= 0.01f)
            lastGroundedTime = Time.time;
    }

    public void SetCheckpoint(Vector2 position)
    {
        checkpoint = position;
    }

    public void Kill()
    {
        if (!IsOwner)
            return;
        Respawn();
    }

    private void Respawn()
    {
        body.linearVelocity = Vector2.zero;
        body.position = checkpoint;
        transform.position = checkpoint;
    }
}
