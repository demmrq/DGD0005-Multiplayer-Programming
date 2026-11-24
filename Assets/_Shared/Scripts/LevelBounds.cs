using UnityEngine;

public class LevelBounds : MonoBehaviour
{
    [SerializeField] private Vector2 min = new Vector2(-1f, -1f);
    [SerializeField] private Vector2 max = new Vector2(60f, 9f);

    private void Start()
    {
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.SetBounds(min, max);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.5f);
        Vector2 center = (min + max) * 0.5f;
        Gizmos.DrawWireCube(center, max - min);
    }
}
