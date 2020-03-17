using UnityEngine;

/// <summary>
/// Smoothly follows a given target, rotating in space with it.
/// </summary>
public class MagneticFollower : MonoBehaviour
{
    public const float DEFAULT_DAMP_POSITION = 100f;
    public const float DEFAULT_DAMP_ROTATION = 100f;
    public const float DEFAULT_FOLLOWER_HEIGHT = 1f;
    public const float DEFAULT_FOLLOWER_DISTANCE = 5f;

    [SerializeField] private float dampPosition = DEFAULT_DAMP_POSITION;
    [SerializeField] private float dampRotation = DEFAULT_DAMP_ROTATION;
    [SerializeField] private float height = DEFAULT_FOLLOWER_HEIGHT;
    [SerializeField] private float distance = DEFAULT_FOLLOWER_DISTANCE;

    [Header("The target to follow.")]
    [SerializeField] private Transform target;

    private Vector3 targetPosition;

    /// <summary>
    /// Is called when the object becomes enabled and active.
    /// </summary>
    private void OnEnable() => CheckTarget();

    /// <summary>
    /// Is called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    private void Start() => CheckTarget();

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update() => Follow();

    /// <summary>
    /// Checks if the target has been set to follow.
    /// </summary>
    private void CheckTarget()
    {
        if (!target)
        {
            Debug.LogError($"Missing reference at {nameof(target)}.");
            this.enabled = false;
        }
    }

    /// <summary>
    /// Smoothly follows a given target, rotating in space with it.
    /// </summary>
    private void Follow()
    {
        targetPosition = target.position - target.forward * distance + target.up * height;
        transform.position = Vector3.Lerp(transform.position, targetPosition, dampPosition * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, dampRotation * Time.deltaTime);
    }
}