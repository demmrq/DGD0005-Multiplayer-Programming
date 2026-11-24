using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Spikes : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryFail(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryFail(other);
    }

    private void TryFail(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !player.IsOwner)
            return;
        if (LevelRestarter.Instance != null)
            LevelRestarter.Instance.RequestRestart();
        else
            player.Kill();
    }
}
