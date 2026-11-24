using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Crate : NetworkBehaviour
{
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float killY = -30f;
    [SerializeField] private float requestInterval = 0.25f;

    private Rigidbody2D body;
    private Vector2 startPosition;
    private float lastRequestTime = -1f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = gravityScale;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.bodyType = RigidbodyType2D.Kinematic;
        startPosition = transform.position;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        RefreshBody();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RefreshBody();
    }

    public override void OnOwnershipServer(NetworkConnection prevOwner)
    {
        base.OnOwnershipServer(prevOwner);
        RefreshBody();
    }

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        RefreshBody();
    }

    private bool SimulatesHere()
    {
        if (Owner != null && Owner.IsValid)
            return IsOwner;
        return IsServerInitialized;
    }

    private void RefreshBody()
    {
        Vector2 velocity = body.linearVelocity;
        bool simulate = SimulatesHere();
        body.bodyType = simulate ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        body.gravityScale = gravityScale;
        body.linearVelocity = simulate ? velocity : Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!SimulatesHere())
            return;
        body.linearVelocity = new Vector2(body.linearVelocity.x * 0.85f, body.linearVelocity.y);
        if (body.position.y < killY)
        {
            body.linearVelocity = Vector2.zero;
            body.position = startPosition;
            transform.position = startPosition;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsClientInitialized || IsOwner)
            return;
        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();
        if (player == null || !player.IsOwner)
            return;
        if (Time.time - lastRequestTime < requestInterval)
            return;
        lastRequestTime = Time.time;
        RequestOwnership();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnership(NetworkConnection caller = null)
    {
        if (caller == null || Owner == caller)
            return;
        GiveOwnership(caller);
    }
}
