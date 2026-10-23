using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns the bonus squid and walks it across the level.
///
/// Every few seconds one appears at a random point on a circle that sits
/// outside whatever the camera can see, travels in a straight line through the
/// centre of the level, and is destroyed once it has left the view on the far
/// side. The circle is measured from the camera at spawn time, so it stays off
/// screen at 16:9 and 4:3 alike.
///
/// Travel goes through <see cref="Tweener"/> like everything else, so it is
/// interpolated from accumulated Time.deltaTime with no physics involved.
/// </summary>
public class CherryController : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Seconds between one bonus item and the next.")]
    [SerializeField] private float spawnInterval = 5f;

    [SerializeField] private GameObject cherryPrefab;

    [Tooltip("Parent for spawned items. Defaults to this transform.")]
    [SerializeField] private Transform spawnParent;

    [Header("Travel")]
    [Tooltip("Centre of the level, which every bonus item passes through.")]
    [SerializeField] private Vector2 levelCentre = new Vector2(13.5f, -14f);

    [Tooltip("World units per second.")]
    [SerializeField] private float speed = 10f;

    [Tooltip("Extra clearance beyond the camera corner, in world units.")]
    [SerializeField] private float margin = 2f;

    private readonly List<GameObject> _active = new List<GameObject>();
    private float _sinceLast;

    private void Update()
    {
        _sinceLast += Time.deltaTime;

        if (_sinceLast >= spawnInterval)
        {
            _sinceLast = 0f;
            Spawn();
        }

        Sweep();
    }

    private void Spawn()
    {
        if (cherryPrefab == null)
        {
            return;
        }

        float radius = OffScreenRadius();
        float angle = Random.Range(0f, Mathf.PI * 2f);

        Vector3 from = new Vector3(
            levelCentre.x + Mathf.Cos(angle) * radius,
            levelCentre.y + Mathf.Sin(angle) * radius,
            0f);

        // Straight through the centre and out the opposite side.
        Vector3 to = new Vector3(
            levelCentre.x - Mathf.Cos(angle) * radius,
            levelCentre.y - Mathf.Sin(angle) * radius,
            0f);

        GameObject cherry = Instantiate(cherryPrefab);
        cherry.name = "BonusCherry";
        cherry.transform.SetParent(spawnParent != null ? spawnParent : transform, false);
        cherry.transform.position = from;

        Tweener tweener = cherry.GetComponent<Tweener>();

        if (tweener == null)
        {
            tweener = cherry.AddComponent<Tweener>();
        }

        float distance = (to - from).magnitude;
        tweener.AddTween(cherry.transform, from, to, distance / Mathf.Max(0.01f, speed));

        _active.Add(cherry);
    }

    /// <summary>Radius of a circle that clears the camera's corner at any aspect.</summary>
    private float OffScreenRadius()
    {
        Camera cam = Camera.main;
        float halfHeight = cam != null ? cam.orthographicSize : 15f;
        float aspect = cam != null ? cam.aspect : 2f;
        float halfWidth = halfHeight * Mathf.Max(1f, aspect);

        return Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + margin;
    }

    /// <summary>Retires anything that has finished its trip or been eaten.</summary>
    private void Sweep()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            GameObject cherry = _active[i];

            if (cherry == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            Tweener tweener = cherry.GetComponent<Tweener>();

            if (tweener == null || !tweener.TweenExists)
            {
                Destroy(cherry);
                _active.RemoveAt(i);
            }
        }
    }
}
