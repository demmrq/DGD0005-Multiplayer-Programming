using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PowerWire : MonoBehaviour
{
    [SerializeField] private PowerSource source;
    [SerializeField] private Vector3[] points = new Vector3[0];
    [SerializeField] private Color offColor = new Color(0.45f, 0.12f, 0.14f);
    [SerializeField] private Color onColor = new Color(1f, 0.3f, 0.32f);
    [SerializeField] private float width = 0.08f;

    private LineRenderer line;
    private bool subscribed;

    private void OnEnable()
    {
        line = GetComponent<LineRenderer>();
        Rebuild();
        if (Application.isPlaying && source != null && !subscribed)
        {
            source.Changed += OnSourceChanged;
            subscribed = true;
        }
        SetColor(false);
    }

    private void OnDisable()
    {
        if (subscribed && source != null)
            source.Changed -= OnSourceChanged;
        subscribed = false;
    }

    private void OnValidate()
    {
        line = GetComponent<LineRenderer>();
        Rebuild();
        SetColor(false);
    }

    private void Rebuild()
    {
        if (line == null)
            return;
        line.useWorldSpace = true;
        line.startWidth = width;
        line.endWidth = width;
        line.positionCount = points.Length;
        line.SetPositions(points);
    }

    private void OnSourceChanged(PowerSource s, bool on)
    {
        SetColor(on);
    }

    private void SetColor(bool on)
    {
        if (line == null)
            return;
        Color c = on ? onColor : offColor;
        line.startColor = c;
        line.endColor = c;
    }
}
