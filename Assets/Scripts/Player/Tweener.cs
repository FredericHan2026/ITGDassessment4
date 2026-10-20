using UnityEngine;

/// <summary>One in-progress linear move from a start position to an end position.</summary>
public class Tween
{
    public Transform Target { get; private set; }
    public Vector3 StartPosition { get; private set; }
    public Vector3 EndPosition { get; private set; }
    public float Duration { get; private set; }
    public float Elapsed { get; set; }

    public Tween(Transform target, Vector3 startPosition, Vector3 endPosition, float duration)
    {
        Target = target;
        StartPosition = startPosition;
        EndPosition = endPosition;
        Duration = Mathf.Max(0.0001f, duration);
        Elapsed = 0f;
    }
}

/// <summary>
/// Programmatic tweening used for every bit of motion in the game.
///
/// Movement is worked out entirely from accumulated Time.deltaTime, so it is
/// frame-rate independent: the interpolation factor is elapsed/duration and the
/// position is start + (end - start) * factor.  No Rigidbody, no
/// CharacterController, and no built-in movement helper such as
/// Vector3.MoveTowards is involved.
/// </summary>
public class Tweener : MonoBehaviour
{
    private Tween _activeTween;

    /// <summary>True while a move is still running.</summary>
    public bool TweenExists
    {
        get { return _activeTween != null; }
    }

    /// <summary>Queue a move.  Returns false if one is already in progress.</summary>
    public bool AddTween(Transform target, Vector3 startPosition, Vector3 endPosition, float duration)
    {
        if (_activeTween != null || target == null)
        {
            return false;
        }

        _activeTween = new Tween(target, startPosition, endPosition, duration);
        target.position = startPosition;
        return true;
    }

    public void Clear()
    {
        _activeTween = null;
    }

    private void Update()
    {
        if (_activeTween == null)
        {
            return;
        }

        if (_activeTween.Target == null)
        {
            _activeTween = null;
            return;
        }

        _activeTween.Elapsed += Time.deltaTime;

        // Linear interpolation, computed by hand so the speed is identical over
        // every leg of the trip and independent of the frame rate.
        float factor = Mathf.Clamp01(_activeTween.Elapsed / _activeTween.Duration);
        Vector3 delta = _activeTween.EndPosition - _activeTween.StartPosition;
        _activeTween.Target.position = _activeTween.StartPosition + delta * factor;

        if (factor >= 1f)
        {
            _activeTween.Target.position = _activeTween.EndPosition;
            _activeTween = null;
        }
    }
}
