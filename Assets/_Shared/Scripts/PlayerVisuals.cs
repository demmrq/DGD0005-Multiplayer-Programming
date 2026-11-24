using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerVisuals : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] slotSprites;

    private readonly SyncVar<int> slot = new SyncVar<int>(-1);

    public int Slot => slot.Value;

    private void Awake()
    {
        slot.OnChange += OnSlotChanged;
    }

    private void OnDestroy()
    {
        slot.OnChange -= OnSlotChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        slot.Value = FindFreeSlot();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Apply(slot.Value);
    }

    private int FindFreeSlot()
    {
        PlayerVisuals[] players = FindObjectsByType<PlayerVisuals>(FindObjectsSortMode.None);
        for (int candidate = 0; candidate < 8; candidate++)
        {
            bool used = false;
            foreach (PlayerVisuals p in players)
            {
                if (p != this && p.slot.Value == candidate)
                {
                    used = true;
                    break;
                }
            }
            if (!used)
                return candidate;
        }
        return 0;
    }

    private void OnSlotChanged(int previous, int next, bool asServer)
    {
        Apply(next);
    }

    private void Apply(int value)
    {
        if (spriteRenderer == null || slotSprites == null || slotSprites.Length == 0 || value < 0)
            return;
        spriteRenderer.sprite = slotSprites[value % slotSprites.Length];
        gameObject.name = value == 0 ? "Bolt" : value == 1 ? "Nut" : "Player" + value;
    }
}
