using UnityEngine;

/// <summary>
/// The snow PacStudent kicks up while he is moving.
///
/// This drives a real <see cref="ParticleSystem"/>. Drop a Particle System onto
/// PacStudent and assign it below to tune it in the Inspector; leave it empty
/// and one is built and configured here at startup instead, so the effect works
/// out of the box either way.
///
/// The particle material is copied from the sprite material already in the
/// scene rather than looked up by shader name, so there is nothing to go
/// missing in a build.
/// </summary>
public class DustTrail : MonoBehaviour
{
    [Header("System")]
    [Tooltip("Optional. Leave empty and one is created and configured at start.")]
    [SerializeField] private ParticleSystem particles;

    [Header("Look")]
    [SerializeField] private Sprite puffSprite;
    [SerializeField] private Color tint = new Color(0.88f, 0.96f, 1f, 0.9f);
    [SerializeField] private int sortingOrder = 15;

    [Tooltip("Where the puffs leave from, relative to PacStudent. Down a little "
             + "so they look kicked up from his feet.")]
    [SerializeField] private Vector2 offset = new Vector2(0f, -0.28f);

    [Header("Behaviour")]
    [Tooltip("Puffs emitted per second while moving.")]
    [SerializeField] private float rate = 20f;

    [SerializeField] private float lifetime = 0.6f;
    [SerializeField] private float size = 0.55f;

    [Tooltip("How fast a puff drifts away from his feet.")]
    [SerializeField] private float speed = 0.5f;

    [Tooltip("Spread of the puffs around the emission point.")]
    [SerializeField] private float spread = 0.12f;

    private bool _emitting;

    /// <summary>Turned on by the controller while PacStudent is travelling.</summary>
    public bool Emitting
    {
        get { return _emitting; }
        set
        {
            if (_emitting == value)
            {
                return;
            }

            _emitting = value;
            Apply();
        }
    }

    private void Awake()
    {
        if (particles == null)
        {
            particles = Build();
        }

        Configure();
        Apply();
    }

    /// <summary>Makes the particle system when none was assigned in the editor.</summary>
    private ParticleSystem Build()
    {
        GameObject go = new GameObject("DustParticles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        return go.AddComponent<ParticleSystem>();
    }

    private void Configure()
    {
        if (particles == null)
        {
            return;
        }

        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = tint;
        main.gravityModifier = 0f;
        main.maxParticles = 120;
        main.loop = true;
        main.playOnAwake = false;

        // World space, so a puff stays where it was dropped while he keeps going.
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.01f, spread);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;
        emission.enabled = false;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            Material material = BuildMaterial();

            if (material != null)
            {
                renderer.material = material;
            }
        }

        particles.Play();
    }

    /// <summary>
    /// Copies the sprite material already used in this scene and puts the puff
    /// texture on it, rather than hunting for a shader by name.
    /// </summary>
    private Material BuildMaterial()
    {
        if (puffSprite == null)
        {
            return null;
        }

        SpriteRenderer source = GetComponent<SpriteRenderer>();
        Material template = source != null ? source.sharedMaterial : null;
        Material material = template != null
            ? new Material(template)
            : new Material(Shader.Find("Sprites/Default"));

        material.mainTexture = puffSprite.texture;
        return material;
    }

    private void Apply()
    {
        if (particles == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = _emitting;
    }

    private void OnDisable()
    {
        Emitting = false;
    }
}
