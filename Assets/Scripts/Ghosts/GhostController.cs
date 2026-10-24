using UnityEngine;

/// <summary>Which of the four behaviours a seal is currently showing.</summary>
public enum GhostState
{
    Normal,
    Scared,
    Recovering,
    Dead
}

/// <summary>
/// A seal's identity and its current state.
///
/// This band only deals with how a seal looks and whether PacStudent can eat
/// it; the chasing behaviour arrives with the ghost AI. Recovering reuses the
/// scared animation and flashes the sprite white, which is the usual warning
/// that the power pellet is about to run out.
/// </summary>
public class GhostController : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("1 to 4, matching the label above the seal.")]
    [SerializeField] private int ghostNumber = 1;

    [Tooltip("Spawn cell, as column then row, with row 0 at the top.")]
    [SerializeField] private int startColumn;
    [SerializeField] private int startRow;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private RuntimeAnimatorController normalAnimator;
    [SerializeField] private RuntimeAnimatorController scaredAnimator;
    [SerializeField] private RuntimeAnimatorController deadAnimator;

    [Tooltip("Flashes per second while recovering.")]
    [SerializeField] private float flashRate = 6f;

    private GhostState _state = GhostState.Normal;

    public int Number
    {
        get { return ghostNumber; }
    }

    public GhostState State
    {
        get { return _state; }
    }

    /// <summary>True while PacStudent can eat this seal rather than the reverse.</summary>
    public bool IsEdible
    {
        get { return _state == GhostState.Scared || _state == GhostState.Recovering; }
    }

    public Vector2Int StartCell
    {
        get { return new Vector2Int(startColumn, startRow); }
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        SetState(GhostState.Normal);
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (_state == GhostState.Recovering)
        {
            // Alternate between the scared colours and white as a warning.
            bool bright = Mathf.PingPong(Time.time * flashRate, 1f) > 0.5f;
            spriteRenderer.color = bright ? Color.white : new Color(0.65f, 0.8f, 1f, 1f);
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void SetState(GhostState state)
    {
        _state = state;

        if (animator == null)
        {
            return;
        }

        RuntimeAnimatorController wanted = normalAnimator;

        if (state == GhostState.Scared || state == GhostState.Recovering)
        {
            wanted = scaredAnimator != null ? scaredAnimator : normalAnimator;
        }
        else if (state == GhostState.Dead)
        {
            wanted = deadAnimator != null ? deadAnimator : normalAnimator;
        }

        if (wanted != null && animator.runtimeAnimatorController != wanted)
        {
            animator.runtimeAnimatorController = wanted;
        }
    }

    /// <summary>Back to the spawn cell in the normal state.</summary>
    public void ResetToStart()
    {
        transform.position = LevelGrid.CellToWorld(startRow, startColumn);
        SetState(GhostState.Normal);
    }
}
