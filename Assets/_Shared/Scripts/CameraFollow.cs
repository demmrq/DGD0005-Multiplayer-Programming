using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);
    [SerializeField] private bool useBounds;
    [SerializeField] private Vector2 boundsMin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 boundsMax = new Vector2(50f, 50f);

    private Transform target;
    private Vector3 velocity;
    private Camera cam;

    public static CameraFollow Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            Vector3 p = Desired();
            transform.position = p;
            velocity = Vector3.zero;
        }
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        useBounds = true;
        boundsMin = min;
        boundsMax = max;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;
        transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
    }

    private Vector3 Desired()
    {
        Vector3 p = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
        if (useBounds && cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            p.x = Mathf.Clamp(p.x, boundsMin.x + halfW, Mathf.Max(boundsMin.x + halfW, boundsMax.x - halfW));
            p.y = Mathf.Clamp(p.y, boundsMin.y + halfH, Mathf.Max(boundsMin.y + halfH, boundsMax.y - halfH));
        }
        return p;
    }
}
