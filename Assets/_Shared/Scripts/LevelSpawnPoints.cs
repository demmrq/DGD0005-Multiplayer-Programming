using UnityEngine;

public class LevelSpawnPoints : MonoBehaviour
{
    [SerializeField] private Transform[] points = new Transform[0];

    public Transform[] Points => points;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
        foreach (Transform p in points)
            if (p != null)
                Gizmos.DrawWireCube(p.position + Vector3.up * 0.5f, new Vector3(0.8f, 1f, 0f));
    }
}
