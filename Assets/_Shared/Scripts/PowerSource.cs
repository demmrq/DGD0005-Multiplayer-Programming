using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public abstract class PowerSource : NetworkBehaviour
{
    private readonly SyncVar<bool> isOn = new SyncVar<bool>();

    public bool IsOn => isOn.Value;
    public event Action<PowerSource, bool> Changed;

    protected virtual void Awake()
    {
        isOn.OnChange += HandleChange;
    }

    protected virtual void OnDestroy()
    {
        isOn.OnChange -= HandleChange;
    }

    protected void SetOn(bool value)
    {
        if (!IsServerInitialized)
            return;
        if (isOn.Value != value)
            isOn.Value = value;
    }

    private void HandleChange(bool previous, bool next, bool asServer)
    {
        if (asServer && IsClientInitialized)
            return;
        Changed?.Invoke(this, next);
        OnPowerStateChanged(next);
    }

    protected virtual void OnPowerStateChanged(bool on)
    {
    }
}
