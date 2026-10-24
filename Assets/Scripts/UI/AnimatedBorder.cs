using UnityEngine;

/// <summary>
/// Runs a ring of dots around the edge of its RectTransform.
///
/// Positions are recomputed from the rect every frame, so the border follows the
/// canvas when the aspect ratio changes rather than being baked to one size.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AnimatedBorder : MonoBehaviour
{
    [Tooltip("Laps around the border per second.")]
    [SerializeField] private float speed = 0.06f;

    [Tooltip("Distance in from the edge of the rect, in canvas units.")]
    [SerializeField] private float inset = 34f;

    [Tooltip("How much the dots swell as they travel.")]
    [SerializeField] private float pulse = 0.35f;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_rect == null || transform.childCount == 0)
        {
            return;
        }

        Rect r = _rect.rect;
        float width = Mathf.Max(1f, r.width - inset * 2f);
        float height = Mathf.Max(1f, r.height - inset * 2f);
        float perimeter = (width + height) * 2f;

        int count = transform.childCount;
        float offset = Time.time * speed;

        for (int i = 0; i < count; i++)
        {
            float t = Mathf.Repeat((float)i / count + offset, 1f);
            Vector2 point = PointOnPerimeter(t * perimeter, width, height);

            RectTransform dot = transform.GetChild(i) as RectTransform;
            if (dot == null)
            {
                continue;
            }

            dot.anchoredPosition = point;
            float swell = 1f + pulse * Mathf.Sin((t * 4f + Time.time) * Mathf.PI * 2f);
            dot.localScale = new Vector3(swell, swell, 1f);
        }
    }

    /// <summary>Walks a distance clockwise around the rect, starting bottom-left.</summary>
    private static Vector2 PointOnPerimeter(float distance, float width, float height)
    {
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        if (distance < width)
        {
            return new Vector2(-halfW + distance, -halfH);
        }
        distance -= width;

        if (distance < height)
        {
            return new Vector2(halfW, -halfH + distance);
        }
        distance -= height;

        if (distance < width)
        {
            return new Vector2(halfW - distance, halfH);
        }
        distance -= width;

        return new Vector2(-halfW, halfH - distance);
    }
}
