using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A puff of snow kicked up behind PacStudent while it is moving.
///
/// Written as a pool of small sprites rather than a Unity ParticleSystem so the
/// whole effect is plain, readable code with nothing to tune outside the
/// Inspector fields below. Each puff fades out and shrinks over its lifetime.
/// </summary>
public class DustTrail : MonoBehaviour
{
    private class Puff
    {
        public GameObject Root;
        public SpriteRenderer Renderer;
        public float Age;
        public float Drift;
    }

    [Header("Look")]
    [SerializeField] private Sprite puffSprite;
    [SerializeField] private Color tint = new Color(0.86f, 0.95f, 1f, 0.75f);
    [SerializeField] private int sortingOrder = 15;

    [Header("Timing")]
    [Tooltip("Seconds between puffs while emitting.")]
    [SerializeField] private float interval = 0.07f;

    [Tooltip("How long each puff lasts.")]
    [SerializeField] private float lifetime = 0.45f;

    [Header("Size")]
    [SerializeField] private float startScale = 0.34f;
    [SerializeField] private float endScale = 0.06f;
    [SerializeField] private float rise = 0.35f;

    private readonly List<Puff> _live = new List<Puff>();
    private float _sinceLast;

    /// <summary>Turned on while PacStudent is travelling.</summary>
    public bool Emitting { get; set; }

    private void Update()
    {
        if (Emitting && puffSprite != null)
        {
            _sinceLast += Time.deltaTime;

            if (_sinceLast >= interval)
            {
                _sinceLast = 0f;
                Spawn();
            }
        }
        else
        {
            _sinceLast = interval;
        }

        Age();
    }

    private void Spawn()
    {
        GameObject go = new GameObject("Puff");
        go.transform.SetParent(transform, false);
        go.transform.position = transform.position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = puffSprite;
        sr.color = tint;
        sr.sortingOrder = sortingOrder;

        Puff puff = new Puff();
        puff.Root = go;
        puff.Renderer = sr;
        puff.Age = 0f;
        puff.Drift = Random.Range(-0.25f, 0.25f);
        _live.Add(puff);
    }

    /// <summary>Fades, shrinks and drifts every live puff, retiring the old ones.</summary>
    private void Age()
    {
        float span = Mathf.Max(0.01f, lifetime);

        for (int i = _live.Count - 1; i >= 0; i--)
        {
            Puff puff = _live[i];

            if (puff.Root == null)
            {
                _live.RemoveAt(i);
                continue;
            }

            puff.Age += Time.deltaTime;
            float t = Mathf.Clamp01(puff.Age / span);

            if (t >= 1f)
            {
                Destroy(puff.Root);
                _live.RemoveAt(i);
                continue;
            }

            float scale = Mathf.Lerp(startScale, endScale, t);
            puff.Root.transform.localScale = new Vector3(scale, scale, 1f);

            Vector3 p = puff.Root.transform.position;
            puff.Root.transform.position = new Vector3(
                p.x + puff.Drift * Time.deltaTime,
                p.y + rise * Time.deltaTime,
                p.z);

            Color c = tint;
            c.a = tint.a * (1f - t);
            puff.Renderer.color = c;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < _live.Count; i++)
        {
            if (_live[i].Root != null)
            {
                Destroy(_live[i].Root);
            }
        }

        _live.Clear();
    }
}
