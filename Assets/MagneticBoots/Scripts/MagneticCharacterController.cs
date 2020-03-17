using UnityEngine;

/// <summary>
/// Controls the character by magnetizing to nearby objects that he can walk on.
/// </summary>
[RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
public class MagneticCharacterController : MonoBehaviour
{
    public const float DEFAULT_MOVEMENT_SPEED = 2f;
    public const float DEFAULT_ROTATION_SPEED = 100f;
    public const float DEFAULT_MAGNETIC_ROTATION_SPEED_GROUNDED = 10f;
    public const float DEFAULT_MAGNETIC_ROTATION_SPEED_FLYING = 0.5f;
    public const float DEFAULT_GRAVITY_SPEED = 500f;
    public const float DEFAULT_JUMP_FORCE = 500f;
    public const float SPHERECAST_DISTANCE = 5f;
    public const float DEFAULT_RIGIDBODY_ANGULAR_DRAG = 100f;
    public const int LAYER_EVERYTHING = 0;

    [SerializeField] private float movementSpeed = DEFAULT_MOVEMENT_SPEED;
    [SerializeField] private float rotationSpeed = DEFAULT_ROTATION_SPEED;
    [SerializeField] private float magneticRotationSpeedGrounded = DEFAULT_MAGNETIC_ROTATION_SPEED_GROUNDED;
    [SerializeField] private float magneticRotationSpeedFly = DEFAULT_MAGNETIC_ROTATION_SPEED_FLYING;
    [SerializeField] private float gravitySpeed = DEFAULT_GRAVITY_SPEED;
    [SerializeField] private float jumpForce = DEFAULT_JUMP_FORCE;

    [Header("Layers on which the character can walk.")]
    [SerializeField] private LayerMask RayCastLayerMask = ~LAYER_EVERYTHING;

    private float currentMagneticRotationSpeed;
    private bool isGrounded;
    private new Rigidbody rigidbody;
    private new CapsuleCollider collider;
    private Animator animator;
    private RaycastHit? closestSphereCastHit;

    /// <summary>
    /// Is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        collider = GetComponent<CapsuleCollider>();
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError($"Missing reference at {nameof(animator)}.");

        rigidbody.useGravity = false;
        rigidbody.angularDrag = DEFAULT_RIGIDBODY_ANGULAR_DRAG;
    }

    /// <summary>
    /// Is called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    private void Start()
    {
        isGrounded = true;
        currentMagneticRotationSpeed = magneticRotationSpeedGrounded;
    }

    /// <summary>
    /// Is called every fixed framerate frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void FixedUpdate()
    {
        Move();
        Rotate();
        MagneticRotate();
        Gravitate();
    }

    /// <summary>
    /// Moves the character using user's input.
    /// </summary>
    private void Move()
    {
        // Forward and backward movement.
        var inputVertical = Input.GetAxis("Vertical");
        animator?.SetFloat("WalkingSpeed", inputVertical);
        var positionOffset = transform.forward * inputVertical * movementSpeed;
        var targetPosition = transform.position + positionOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime);

        // Jumping.
        if (Input.GetKeyDown("space") && isGrounded)
        {
            animator?.SetBool("IsJumping", true);
            rigidbody.AddForce(transform.up * jumpForce);
            isGrounded = false;
            currentMagneticRotationSpeed = magneticRotationSpeedFly;
        }
    }

    /// <summary>
    /// Rotates the character using user's input.
    /// </summary>
    private void Rotate()
    {
        // Left and right turn in place.
        var inputHorizontal = Input.GetAxis("Horizontal");
        animator?.SetFloat("RotatingSpeed", inputHorizontal);
        var rotationOffset = Quaternion.AngleAxis(inputHorizontal * rotationSpeed, Vector3.up);
        var targetRotation = transform.rotation * rotationOffset;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime);
    }

    /// <summary>
    /// Rotates the character to the nearest walking surface.
    /// </summary>
    private void MagneticRotate()
    {
        var stickedRotation = GetMagneticRotation();
        transform.rotation = Quaternion.Slerp(transform.rotation, stickedRotation, Time.fixedDeltaTime * currentMagneticRotationSpeed);
    }

    /// <summary>
    /// Gets the target rotation quaternion to the nearest walking surface.
    /// Shoots three SphereCasts: forward, down and back. Than selects the nearest one from their hits, and returns the created rotation to the hit's normal.
    /// </summary>
    /// <returns>The target rotation quaternion.</returns>
    private Quaternion GetMagneticRotation()
    {
        closestSphereCastHit = null;

        if (Physics.SphereCast(transform.position, collider.radius, -transform.up + transform.forward, out RaycastHit hitForward, SPHERECAST_DISTANCE, RayCastLayerMask))
            closestSphereCastHit = hitForward;

        if (Physics.SphereCast(transform.position, collider.radius, -transform.up, out RaycastHit hitDown, SPHERECAST_DISTANCE, RayCastLayerMask))
        {
            if (!closestSphereCastHit.HasValue || closestSphereCastHit.Value.distance > hitDown.distance)
                closestSphereCastHit = hitDown;
        }

        if (Physics.SphereCast(transform.position, collider.radius, -transform.up - transform.forward, out RaycastHit hitBack, SPHERECAST_DISTANCE, RayCastLayerMask))
        {
            if (!closestSphereCastHit.HasValue || closestSphereCastHit.Value.distance > hitBack.distance)
                closestSphereCastHit = hitBack;
        }

        var normal = closestSphereCastHit?.normal ?? Vector3.zero;

        return Quaternion.LookRotation(Vector3.Cross(transform.right, normal), normal);
    }

    /// <summary>
    /// Moves the character by the gravity force.
    /// </summary>
    private void Gravitate()
    {
        rigidbody.AddForce(-transform.up * Time.deltaTime * gravitySpeed);
    }

    /// <summary>
    /// Is called when this collider/rigidbody has begun touching another rigidbody/collider.
    /// </summary>
    /// <param name="collided">The another collided object.</param>
    private void OnCollisionEnter(Collision collided)
    {
        // Checks if the character is collide with objects on which he can walk.
        if (((1 << collided.gameObject.layer) & RayCastLayerMask) == 0)
            return;

        animator?.SetBool("IsJumping", false);
        isGrounded = true;
        currentMagneticRotationSpeed = magneticRotationSpeedGrounded;

        // Stick to animated platform.
        if (collided.gameObject.tag == "StickyPlatform")
            transform.SetParent(collided.transform);
    }

    /// <summary>
    /// Is called when this collider/rigidbody has stopped touching another rigidbody/collider.
    /// </summary>
    /// <param name="collided">The another collided object.</param>
    private void OnCollisionExit(Collision collided)
    {
        // Unstick to animated platform.
        if (collided.gameObject.tag == "StickyPlatform")
            transform.SetParent(null);
    }
}