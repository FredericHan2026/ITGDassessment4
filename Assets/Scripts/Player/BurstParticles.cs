using UnityEngine;

/// <summary>
/// A one-shot puff of particles, fired at a point on demand.
///
/// Used for the bump when PacStudent walks into a wall and for the burst around
/// him when he dies. Like the dust trail, it drives a real
/// <see cref="ParticleSystem"/>: assign one in the Inspector to tune it there,
/// or leave the field empty and one is built and configured at startup.
///
/// The material is copied from a sprite material already in the scene rather
/// than looked up by shader name, so there is nothing to go missing.
/// </summary>
public class BurstParticles : MonoBehaviour
{
    [Header("System")]
    [Tooltip("Optional. Leave empty and one is created and configured at start.")]
    [SerializeField] private ParticleSystem particles;

    [Header("Look")]
    [SerializeField] private Sprite puffSprite;
    [SerializeField] private Color tint = new Color(0.9f, 0.96f, 1f, 0.95f);
    [SerializeField] private int sortingOrder = 24;

    [Header("Burst")]
    [Tooltip("Particles released each time it fires.")]
    [SerializeField] private int count = 14;

    [SerializeField] private float lifetime = 0.45f;
    [SerializeField] private float size = 0.3f;
    [SerializeField] private float speed = 3f;

    [Tooltip("How wide the particles start from the point it was fired at.")]
    [SerializeField] private float spread = 0.1f;

    private void Awake()
    {
        if (particles == null)
        {
            GameObject go = new GameObject(name + "Particles");
            go.transform.SetParent(transform, false);
            particles = go.AddComponent<ParticleSystem>();
        }

        Configure();
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
        main.maxParticles = 200;
        main.loop = false;
        main.playOnAwake = false;

        // World space, so the puff stays where it was fired rather than
        // following whatever moved on afterwards.
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.01f, spread);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;        // nothing continuous; it only bursts

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
    }

    private Material BuildMaterial()
    {
        if (puffSprite == null)
        {
            return null;
        }

        SpriteRenderer source = GetComponentInParent<SpriteRenderer>();
        Material template = source != null ? source.sharedMaterial : null;
        Material material = template != null
            ? new Material(template)
            : new Material(Shader.Find("Sprites/Default"));

        material.mainTexture = puffSprite.texture;
        return material;
    }

    /// <summary>Fires the puff at a world position.</summary>
    public void Burst(Vector3 position)
    {
        if (particles == null)
        {
            return;
        }

        particles.transform.position = position;
        particles.Emit(count);
    }
}
