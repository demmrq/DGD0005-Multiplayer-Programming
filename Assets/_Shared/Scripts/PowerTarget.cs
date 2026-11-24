using FishNet.Object;
using UnityEngine;

public abstract class PowerTarget : NetworkBehaviour
{
    [SerializeField] protected PowerSource[] sources = new PowerSource[0];
    [SerializeField] protected bool requireAll;
    [SerializeField] protected bool invert;

    private bool powered;
    private bool initialized;

    public bool Powered => powered;
    public PowerSource[] Sources => sources;

    protected virtual void OnEnable()
    {
        foreach (PowerSource s in sources)
            if (s != null)
                s.Changed += OnSourceChanged;
    }

    protected virtual void OnDisable()
    {
        foreach (PowerSource s in sources)
            if (s != null)
                s.Changed -= OnSourceChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Evaluate(true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Evaluate(true);
    }

    private void OnSourceChanged(PowerSource source, bool on)
    {
        Evaluate(false);
    }

    private void Evaluate(bool force)
    {
        bool result = ComputePowered();
        if (!force && initialized && result == powered)
            return;
        powered = result;
        initialized = true;
        OnPoweredChanged(powered);
    }

    private bool ComputePowered()
    {
        bool any = false;
        bool all = true;
        int count = 0;
        foreach (PowerSource s in sources)
        {
            if (s == null)
                continue;
            count++;
            if (s.IsOn)
                any = true;
            else
                all = false;
        }
        bool value = count > 0 && (requireAll ? all : any);
        return invert ? !value : value;
    }

    protected abstract void OnPoweredChanged(bool isPowered);

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        foreach (PowerSource s in sources)
            if (s != null)
                Gizmos.DrawLine(transform.position, s.transform.position);
    }
}
