using UnityEngine;

/// <summary>
/// Controls the character by magnetizing to nearby objects that he can walk on.
/// </summary>
[RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
public class MagneticCharacterController : MonoBehaviour
{
    private const float DEFAULT_MOVEMENT_SPEED = 2f;
    private const float DEFAULT_ROTATION_SPEED = 100f;
    private const float DEFAULT_MAGNETIC_ROTATION_SPEED_GROUNDED = 10f;
    private const float DEFAULT_MAGNETIC_ROTATION_SPEED_FLYING = 0.5f;
    private const float DEFAULT_GRAVITY_SPEED = 500f;
    private const float DEFAULT_JUMP_FORCE = 500f;
    private const float SPHERECAST_DISTANCE = 5f;
    private const float DEFAULT_RIGIDBODY_ANGULAR_DRAG = 100f;
    private const int LAYER_EVERYTHING = 0;

    private static readonly int isJumping = Animator.StringToHash("IsJumping");
    private static readonly int rotatingSpeed = Animator.StringToHash("RotatingSpeed");
    private static readonly int walkingSpeed = Animator.StringToHash("WalkingSpeed");

    [SerializeField] private float movementSpeed = DEFAULT_MOVEMENT_SPEED;
    [SerializeField] private float rotationSpeed = DEFAULT_ROTATION_SPEED;
    [SerializeField] private float magneticRotationSpeedGrounded = DEFAULT_MAGNETIC_ROTATION_SPEED_GROUNDED;
    [SerializeField] private float magneticRotationSpeedFly = DEFAULT_MAGNETIC_ROTATION_SPEED_FLYING;
    [SerializeField] private float gravitySpeed = DEFAULT_GRAVITY_SPEED;
    [SerializeField] private float jumpForce = DEFAULT_JUMP_FORCE;

    [Header("Layers on which the character can walk.")]
    [SerializeField] private LayerMask rayCastLayerMask = ~LAYER_EVERYTHING;

    private float currentMagneticRotationSpeed;
    private bool isGrounded;
    private new Rigidbody rigidbody;
    private new CapsuleCollider collider;
    private Animator animator;
    private RaycastHit? closestSphereCastHit;
    private bool isJumpStarted;

    /// <summary>
    /// Is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        collider = GetComponent<CapsuleCollider>();
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        if (!animator)
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
        Jump();
        Rotate();
        MagneticRotate();
        Gravitate();
    }

    /// <summary>
    /// Is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update() => DetectJump();

    /// <summary>
    ///  Detects jump start.
    /// </summary>
    private void DetectJump()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
            isJumpStarted = true;
    }

    /// <summary>
    /// Moves the character using user's input.
    /// </summary>
    private void Move()
    {
        // Forward and backward movement.
        var inputVertical = Input.GetAxis("Vertical");

        if (animator)
            animator.SetFloat(walkingSpeed, inputVertical);

        var positionOffset = transform.forward * (inputVertical * movementSpeed);
        var targetPosition = transform.position + positionOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime);
    }

    /// <summary>
    /// Makes the character jump if the jump button was pressed.
    /// </summary>
    private void Jump()
    {
        if (!isJumpStarted || !isGrounded)
            return;

        if (animator)
            animator.SetBool(isJumping, true);

        currentMagneticRotationSpeed = magneticRotationSpeedFly;
        rigidbody.AddForce(transform.up * jumpForce);
        isGrounded = false;
        isJumpStarted = false;
    }

    /// <summary>
    /// Rotates the character using user's input.
    /// </summary>
    private void Rotate()
    {
        // Left and right turn in place.
        var inputHorizontal = Input.GetAxis("Horizontal");

        if (animator)
            animator.SetFloat(rotatingSpeed, inputHorizontal);

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

        if (Physics.SphereCast(transform.position, collider.radius, -transform.up + transform.forward, out RaycastHit hitForward, SPHERECAST_DISTANCE, rayCastLayerMask))
            closestSphereCastHit = hitForward;

        if (Physics.SphereCast(transform.position, collider.radius, -transform.up, out RaycastHit hitDown, SPHERECAST_DISTANCE, rayCastLayerMask))
        {
            if (!closestSphereCastHit.HasValue || closestSphereCastHit.Value.distance > hitDown.distance)
                closestSphereCastHit = hitDown;
        }

        if (Physics.SphereCast(transform.position, collider.radius, -transform.up - transform.forward, out RaycastHit hitBack, SPHERECAST_DISTANCE, rayCastLayerMask))
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
    private void Gravitate() =>
        rigidbody.AddForce(-transform.up * (Time.deltaTime * gravitySpeed));

    /// <summary>
    /// Is called when this collider/rigidbody has begun touching another rigidbody/collider.
    /// </summary>
    /// <param name="collided">The another collided object.</param>
    private void OnCollisionEnter(Collision collided)
    {
        // Checks if the character is collide with objects on which he can walk.
        if (((1 << collided.gameObject.layer) & rayCastLayerMask) == 0)
            return;

        if (animator)
            animator.SetBool(isJumping, false);

        isGrounded = true;
        currentMagneticRotationSpeed = magneticRotationSpeedGrounded;

        // Stick to animated platform.
        if (collided.gameObject.CompareTag("StickyPlatform"))
            transform.SetParent(collided.transform);
    }

    /// <summary>
    /// Is called when this collider/rigidbody has stopped touching another rigidbody/collider.
    /// </summary>
    /// <param name="collided">The another collided object.</param>
    private void OnCollisionExit(Collision collided)
    {
        // Unstick to animated platform.
        if (collided.gameObject.CompareTag("StickyPlatform"))
            transform.SetParent(null);
    }
}