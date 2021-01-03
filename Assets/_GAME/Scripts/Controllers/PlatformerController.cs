﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

///<summary>
/// A Platformer Controller for a playable character in a side-scrolling platformer.
///</summary>
[HelpURL("https://github.com/DaCookie/empty-platformer/blob/master/Docs/platformer-controller.md")]
public class PlatformerController : MonoBehaviour
{

    #region Subclasses

    [System.Serializable]
    private class MovementEvents
    {
        // Called when the character starts moving.
        public MovementInfosEvent OnBeginMove = new MovementInfosEvent();

        // Called each frame while the character is moving (even if there's an obstacle in front of it).
        public MovementInfosEvent OnUpdateMove = new MovementInfosEvent();

        // Called when the character stops moving.
        public UnityEvent OnStopMove = new UnityEvent();

        // Called when the character changes its movment direction. Sends the new direction vector.
        public Vector3Event OnChangeOrientation = new Vector3Event();
    }

    [System.Serializable]
    private class JumpEvents
    {
        // Called when the player press the Jump button and the Jump action begins to apply.
        public JumpInfosEvent OnBeginJump = new JumpInfosEvent();

        // Called each frame while the character is ascending after a Jump.
        public JumpUpdateInfosEvent OnUpdateJump = new JumpUpdateInfosEvent();

        // Called when the character stops jumping by releasing the Jump button (if Hold Input Mode enabled), by encountering an obstacle above
        // him, or by completing the Jump curve.
        public UnityEvent OnStopJump = new UnityEvent();

        // Called when the character lands on the floor after falling down.
        public LandingInfosEvent OnLand = new LandingInfosEvent();

        // Called when the character hit something above him. Sends the hit position.
        public Vector3Event OnHitCeiling = new Vector3Event();

        // Called when the character is falling down.
        public FloatEvent OnFall = new FloatEvent();
    }

    #endregion


    #region Properties

    /***** Constants *****/

    // Offset used for Jump obstacles detection (see UpdateJump() method)
    private const float JUMP_OBSTACLES_DETECTION_OFFSET = 1f;

    private const string DEFAULT_JUMP_BINDING = "<Keyboard>/space";

    /***** Settings *****/

    [Header("Controls")]

    [SerializeField]
    [Tooltip("Defines the user inputs (using the new Input System) to make the player move along the X axis")]
    private InputAction m_MoveXAction = new InputAction("Move X", InputActionType.Value, null, null, null, "Axis");

    [SerializeField]
    [Tooltip("Defines the user inputs (using the new Input System) to make the player jump")]
    private InputAction m_JumpAction = new InputAction("Jump", InputActionType.Button, DEFAULT_JUMP_BINDING, null, null, null);

    [Header("Movement Settings")]

    [SerializeField, Tooltip("Maximum speed of the character (in units/second)")]
    private float m_Speed = 8f;

    [SerializeField, Tooltip("Defines the physics layers that can block player movement")]
    private LayerMask m_MovementObstaclesDetectionLayer = ~0;

    [SerializeField, Tooltip("Reference to the character's collider. By default, use the Collider on this GameObject")]
    private BoxCollider m_Collider = null;

    [Header("Jump Settings")]

    [SerializeField, Tooltip("Defines how the jump should behave, where X represents the duration of the jump and Y axis represents the height")]
    private AnimationCurve m_JumpCurve = new AnimationCurve();

    [SerializeField, Tooltip("Defines the falling speed of the character")]
    private float m_GravityScale = 1f;

    [SerializeField, Tooltip("If false, press the Jump button once to run the jump curve completely. If true, the jump curve is read until the Jump button is released or until the curve is complete (Super Mario-like jump)")]
    private bool m_HoldInputMode = true;

    [SerializeField, Tooltip("Used only if \"Hold Input Mode\" is set to true. Defines the minimum jump duration when the jump input is pressed for one frame only")]
    private float m_MinJumpDuration = .2f;

    [SerializeField, Tooltip("Defines the physics layers that can block player jump")]
    private LayerMask m_JumpObstaclesDetectionLayer = ~0;

    [Header("Other Settings")]

    [SerializeField, Tooltip("If true, disables movement")]
    private bool m_FreezeMovement = false;

    [SerializeField, Tooltip("If true, disables jump")]
    private bool m_FreezeJump = false;

    /***** Events *****/

    [Header("Movement Events")]

    [SerializeField]
    private MovementEvents m_MovementEvents = new MovementEvents();

    [Header("Jump Events")]

    [SerializeField]
    private JumpEvents m_JumpEvents = new JumpEvents();

    /***** Movement properties *****/

    // Stores the last orientation of the character to eventually trigger the OnChangeOrientation event if it changes
    private Vector3 m_LastOrientation = Vector3.zero;

    // The last movement input axis, helpful for checking if the player moved or not
    private float m_LastMovementAxis = 0f;

    // Stores the expected movement direction from user inputs.
    private float m_MovementDirectionX = 0f;

    /***** Jump properties *****/

    // Defines if the character is currently on the floor. Useful for triggering OnLand event the first frame that character touches the ground
    private bool m_IsOnFloor = false;

    // The current time (x position) on the Jump curve to read
    private float m_JumpTime = 0f;

    // The Y position of the character when it begins to jump
    private Vector3 m_JumpInitialPosition = Vector3.zero;

    // Defines the current velocity of the character on the Y axis
    private float m_YVelocity = 0f;

    // Defines if the character is currently jumping
    private bool m_IsJumping = false;

    // Defines from how much time the character is falling
    private float m_FallingTime = 0f;

    // Stores the state of the jump input button.
    private bool m_IsJumpInputPressed = false;

    /***** Debug properties *****/

    #if UNITY_EDITOR

    // Store the last "cast distance" of the movement obstacles detection (on X axis), for drawing gizmos
    private float m_LastMovementObstaclesDetectionCastDistance = 0f;

    // Store the last "cast distance" of the jump obstacles detection (on Y axis), for drawing gizmos
    private float m_LastJumpObstaclesDetectionCastDistance = 0f;

#endif

    #endregion


    #region Lifecycle

    /// <summary>
    /// Called when this component is loaded.
    /// </summary>
    private void Awake()
    {
        if (m_Collider == null) { m_Collider = GetComponent<BoxCollider>(); }
        m_JumpTime = m_JumpCurve.ComputeDuration();
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    private void Update()
    {
        UpdateMovement(Time.deltaTime);
        UpdateJump(Time.deltaTime);
    }

    /// <summary>
    /// Called when this object is enabled.
    /// </summary>
    private void OnEnable()
    {
        BindControls(true);
    }

    /// <summary>
    /// Called when this object is disabled.
    /// </summary>
    private void OnDisable()
    {
        BindControls(false);
    }

    #endregion


    #region Movement

    /// <summary>
    /// Checks for the movement inputs, and apply movement if required.
    /// </summary>
    private void UpdateMovement(float _DeltaTime)
    {
        if(m_FreezeMovement)
            return;

        // Get the movement input
        Vector3 movement = Vector3.right * m_MovementDirectionX;

        // Apply movement
        Move(movement, _DeltaTime);
    }

    /// <summary>
    /// Moves the character, based on the given direction.
    /// </summary>
    /// <param name="_Direction">The movement direciton (clamped to 1).</param>
    /// <returns>Returns true if the movement can be resolved, otherwise false. Note that the movement is resolved event if an obstacle is
    /// on the way.</returns>
    private bool Move(Vector3 _Direction, float _DeltaTime)
    {
        // Clamps the direction, so the character can move faster than its maximum speed, but it can move slower.
        _Direction = Vector3.ClampMagnitude(_Direction, 1f);

        // If the input direction is null
        if (_Direction == Vector3.zero)
        {
            // If the player moved the last frame
            if (m_LastMovementAxis != 0f)
            {
                // Call onStopMove event
                m_LastMovementAxis = 0f;
                m_MovementEvents.OnStopMove.Invoke();
            }

            // The movement hasn't been applied: return false
            return false;
        }

        Vector3 lastPosition = transform.position;
        Vector3 targetPosition = lastPosition;

        // If player didn't move last frame
        if (m_LastMovementAxis == 0f)
        {
            // Call OnBeginMove event
            m_MovementEvents.OnBeginMove.Invoke(new MovementInfos { speed = m_Speed, lastPosition = lastPosition, currentPosition = targetPosition, orientation = Orientation, entity = gameObject });
        }

        // Process obstacles detection

        float castDistance = m_Speed * Mathf.Abs(_Direction.x) * Time.deltaTime;
        #if UNITY_EDITOR
        m_LastMovementObstaclesDetectionCastDistance = castDistance;
        #endif
        // If no obstacles hit
        if (!Physics.BoxCast(transform.position, Extents, transform.right, Quaternion.identity, castDistance, m_MovementObstaclesDetectionLayer))
        {
            // Change target position
            targetPosition = transform.position + _Direction * m_Speed * _DeltaTime;
        }

        // Set the new player position and orientation
        transform.position = targetPosition;
        Orientation = _Direction;
        // Call OnUpdateMove event
        m_MovementEvents.OnUpdateMove.Invoke(new MovementInfos { speed = m_Speed, lastPosition = lastPosition, currentPosition = targetPosition, orientation = Orientation, entity = gameObject });

        m_LastMovementAxis = _Direction.x;

        // The movement has been applied: return true
        return true;
    }

    /// <summary>
    /// Sets the X movement direction.
    /// </summary>
    /// <param name="_Context">The current input context, from the InputAction delegate binding.</param>
    private void SetMoveDirectionX(InputAction.CallbackContext _Context)
    {
        m_MovementDirectionX = Mathf.Clamp(m_MovementDirectionX + _Context.ReadValue<float>(), -1f, 1f);
    }

    /// <summary>
    /// Reset the X movement direction to 0.
    /// </summary>
    /// <param name="_Context">The current input context, from the InputAction delegate binding.</param>
    private void ResetMoveDirectionX(InputAction.CallbackContext _Context)
    {
        m_MovementDirectionX = 0f;
    }

    /// <summary>
    /// Checks if the character is moving.
    /// </summary>
    public bool IsMoving
    {
        get { return m_LastMovementAxis != 0f; }
    }

    /// <summary>
    /// Gets the current orientation of the character.
    /// While the game is a Platformer 2D, its forward vector (its orientation) is transform.right.
    /// 
    /// Sets the orientation of the character. If the orientation is different, call OnChangeOrientation event.
    /// </summary>
    private Vector3 Orientation
    {
        get { return transform.right; }
        set
        {
            // Normalize the given direction
            Vector3 orientation = value;
            orientation.Normalize();

            // If the new orientation is equal to the current one, stop
            if (orientation == m_LastOrientation) { return; }

            // Apply orientation and call OnChangeOrientation event
            transform.right = orientation;
            m_MovementEvents.OnChangeOrientation.Invoke(orientation);
            m_LastOrientation = orientation;
        }
    }

    /// <summary>
    /// Gets the current movement direction vector of the character.
    /// </summary>
    public Vector3 MovementDirection
    {
        get
        {
            Vector3 movement = Vector3.zero;
            movement.y = m_YVelocity == 0 ? 0 : Mathf.Sign(m_YVelocity);
            movement.x = m_LastMovementAxis;
            return movement == Vector3.zero ? Vector3.zero : movement.normalized;
        }
    }

    /// <summary>
    /// Called when the character starts moving.
    /// </summary>
    public MovementInfosEvent OnBeginMove
    {
        get { return m_MovementEvents.OnBeginMove; }
    }

    /// <summary>
    /// Called each frame the character is moving (even if there's an obstacle in front of it).
    /// </summary>
    public MovementInfosEvent OnUpdateMove
    {
        get { return m_MovementEvents.OnUpdateMove; }
    }

    /// <summary>
    /// Called when the character stops moving.
    /// </summary>
    public UnityEvent OnStopMove
    {
        get { return m_MovementEvents.OnStopMove; }
    }

    /// <summary>
    /// Called when the character changes its movment direction.
    /// </summary>
    public Vector3Event OnChangeOrientation
    {
        get { return m_MovementEvents.OnChangeOrientation; }
    }

    #endregion


    #region Jump

    /// <summary>
    /// Checks for the Jump input, and updates the Jump state and the character Y position.
    /// </summary>
    private void UpdateJump(float _DeltaTime)
    {
        if(m_FreezeJump)
            return;

        // If the character is currently jumping
        if (m_IsJumping)
        {
            // If Hold Input Mode option is enabled, but the Jump button is released
            if(m_HoldInputMode && !m_IsJumpInputPressed)
            {
                // Ensures the minimum Jump duration is less than the Jump curve duration
                float minDuration = Mathf.Min(m_MinJumpDuration, m_JumpCurve.ComputeDuration());
                // If the minimum jump duration has been reached, cancel jump now
                if (m_JumpTime >= m_MinJumpDuration)
                {
                    StopJump();
                    return;
                }
            }

            // Update jump
            float lastJumpTime = m_JumpTime;
            m_JumpTime += _DeltaTime;

            float lastHeight = m_JumpCurve.Evaluate(lastJumpTime);
            float targetHeight = m_JumpCurve.Evaluate(m_JumpTime);
            float castDistance = (targetHeight - lastHeight);

            #if UNITY_EDITOR
            m_LastJumpObstaclesDetectionCastDistance = castDistance;
            #endif

            // If there's an obstacle above
            if (Physics.BoxCast(transform.position, Extents, Vector3.up, out RaycastHit rayHit, Quaternion.identity, castDistance, m_JumpObstaclesDetectionLayer))
            {
                // Place player at the maximum height possible, and stop jump
                Vector3 targetPosition = transform.position;
                targetPosition.y = m_JumpInitialPosition.y + lastHeight + rayHit.distance;
                transform.position = targetPosition;
                m_JumpEvents.OnHitCeiling.Invoke(rayHit.point);
                StopJump();
            }
            // Else, if there's no obstacle above
            else
            {
                // If the Jump action is finished
                if(m_JumpTime >= m_JumpCurve.ComputeDuration())
                {
                    // Place character at the maximum jump height, and call OnUpdateJump and OnStopJump() events
                    Vector3 targetPosition = transform.position;
                    targetPosition.y = m_JumpInitialPosition.y + m_JumpCurve.Evaluate(m_JumpCurve.ComputeDuration());
                    transform.position = targetPosition;
                    StopJump();
                }
                // Else, if the Jump action is not finished, place character to next Y position
                else
                {
                    Vector3 targetPosition = transform.position;
                    targetPosition.y = m_JumpInitialPosition.y + m_JumpCurve.Evaluate(m_JumpTime);
                    transform.position = targetPosition;
                    m_JumpEvents.OnUpdateJump.Invoke(new JumpUpdateInfos { jumpOrigin = m_JumpInitialPosition, jumpTime = m_JumpTime, jumpRatio = JumpRatio });
                }
            }
        }
        // Else, if the character is not jumping
        else
        {
            float castDistance = Mathf.Abs(m_YVelocity) * _DeltaTime + JUMP_OBSTACLES_DETECTION_OFFSET;

            #if UNITY_EDITOR
            m_LastJumpObstaclesDetectionCastDistance = -(castDistance - JUMP_OBSTACLES_DETECTION_OFFSET);
            #endif

            /**
             * NOTE: An offset is added to start the obstacles detection cast from above the character's real position.
             * It allow us to deal with float variables accuracy, which can cause the character to pass through the floor because the cast
             * origin is computed from inside the platform where the character stands.
             * 
             * You can try to remove the JUMP_OBSTACLES_DETECTION_OFFSET constant where it's used, the reason for doing it will be obvious!
             */

            // If character touches something below
            if (Physics.BoxCast(transform.position + Vector3.up * JUMP_OBSTACLES_DETECTION_OFFSET, Extents, Vector3.down, out RaycastHit rayHit, Quaternion.identity, castDistance, m_JumpObstaclesDetectionLayer))
            {
                // If the character wasn't on the floor
                if(!m_IsOnFloor)
                {
                    // Place character on the floor
                    Vector3 targetPosition = transform.position;
                    targetPosition.y = rayHit.point.y + Extents.y;
                    transform.position = targetPosition;

                    // Reset velocity and falling state
                    m_YVelocity = 0f;
                    m_IsOnFloor = true;

                    // Call OnLand event
                    m_JumpEvents.OnLand.Invoke(new LandingInfos { fallingTime = m_FallingTime, landingPosition = targetPosition });
                    // Screen Shake
                    ScreenShake.instance.StartShake(.2f, .1f);


                    m_FallingTime = 0f;
                }
            }
            // Else, if there's nothing below the character
            else
            {
                // Updates the character position
                Vector3 targetPosition = transform.position;
                targetPosition.y += m_YVelocity * _DeltaTime;
                transform.position = targetPosition;

                if(m_IsOnFloor)
                {
                    m_IsOnFloor = false;
                    m_FallingTime = 0f;
                }
                else
                {
                    m_FallingTime += _DeltaTime;
                }

                // Call OnFall event
                m_JumpEvents.OnFall.Invoke(m_FallingTime);

                // Update velocity (apply gravity)
                m_YVelocity += Physics.gravity.y * m_GravityScale * _DeltaTime;
            }

            // If the character is on the floor (and so it can jump) and player is pressing Jump button
            if (m_IsOnFloor && m_IsJumpInputPressed)
            {
                // Begin Jump action
                m_IsOnFloor = false;
                m_IsJumping = true;
                m_JumpTime = 0f;
                m_JumpInitialPosition = transform.position;
                m_YVelocity = 1f;
                m_JumpEvents.OnBeginJump.Invoke(new JumpInfos { jumpOrigin = m_JumpInitialPosition, movement = m_LastMovementAxis });
            }
        }
    }

    /// <summary>
    /// Stops a Jump action by resetting the jump state.
    /// </summary>
    private void StopJump()
    {
        if(m_IsJumping)
        {
            m_IsJumping = false;
            m_YVelocity = 0f;
            m_JumpEvents.OnUpdateJump.Invoke(new JumpUpdateInfos { jumpOrigin = m_JumpInitialPosition, jumpRatio = JumpRatio, jumpTime = m_JumpTime });
            m_JumpEvents.OnStopJump.Invoke();
        }
    }

    /// <summary>
    /// Makes the character starts jumping.
    /// </summary>
    /// <param name="_Context">The current input context, from the InputAction delegate binding.</param>
    private void BeginJump(InputAction.CallbackContext _Context)
    {
        m_IsJumpInputPressed = true;
    }

    /// <summary>
    /// Makes the character ends jumping.
    /// </summary>
    /// <param name="_Context">The current input context, from the InputAction delegate binding.</param>
    private void EndJump(InputAction.CallbackContext _Context)
    {
        m_IsJumpInputPressed = false;
    }

    /// <summary>
    /// Checks if the character is juming.
    /// </summary>
    public bool IsJumping
    {
        get { return m_IsJumping; }
    }

    /// <summary>
    /// Checks if the character is on the floor.
    /// </summary>
    public bool IsOnFloor
    {
        get { return m_IsOnFloor; }
    }

    /// <summary>
    /// Checks if the character is falling (not jumping and not on the floor).
    /// </summary>
    public bool IsFalling
    {
        get { return !IsJumping && !IsOnFloor; }
    }

    /// <summary>
    /// Gets the jump timer ratio, over the jump total duration.
    /// </summary>
    public float JumpRatio
    {
        get { return (IsJumping) ? m_JumpTime / m_JumpCurve.ComputeDuration() : 1f; }
    }

    /// <summary>
    /// Called when the player press the Jump button and the Jump action begins to apply.
    /// </summary>
    public JumpInfosEvent OnBeginJump
    {
        get { return m_JumpEvents.OnBeginJump; }
    }

    /// <summary>
    /// Called each frame the character is ascending after a Jump.
    /// </summary>
    public JumpUpdateInfosEvent OnUpdateJump
    {
        get { return m_JumpEvents.OnUpdateJump; }
    }

    /// <summary>
    /// Called when the character stops jumping by releasing the Jump button (if Hold Input Mode enabled), by encountering an obstacle
    /// above him, or by completing the Jump curve.
    /// </summary>
    public UnityEvent OnStopJump
    {
        get { return m_JumpEvents.OnStopJump; }
    }

    /// <summary>
    /// Called when the character lands on the floor after falling down.
    /// </summary>
    public LandingInfosEvent OnLand
    {
        get { return m_JumpEvents.OnLand; }
    }

    /// <summary>
    /// Called when the character hit something above him. Sends the hit position.
    /// </summary>
    public Vector3Event OnHitCeiling
    {
        get { return m_JumpEvents.OnHitCeiling; }
    }

    /// <summary>
    /// Called when the character is falling down.
    /// </summary>
    public FloatEvent OnFall
    {
        get { return m_JumpEvents.OnFall; }
    }

    #endregion


    #region Common

    /// <summary>
    /// Resets the controller state (cancels jump and unfreeze actions).
    /// </summary>
    public void ResetController()
    {
        StopJump();
        FreezeController = false;
    }

    /// <summary>
    /// Freezes/unfreezes both jump and movement.
    /// </summary>
    public bool FreezeController
    {
        get { return m_FreezeMovement && m_FreezeJump; }
        set
        {
            m_FreezeMovement = value;
            m_FreezeJump = value;
        }
    }

    /// <summary>
    /// Freezes/unfreezes movement.
    /// </summary>
    public bool FreezeMovement
    {
        get { return m_FreezeMovement; }
        set { m_FreezeMovement = value; }
    }

    /// <summary>
    /// Freezes/unfreezes jump.
    /// </summary>
    public bool FreezeJump
    {
        get { return m_FreezeJump; }
        set { m_FreezeJump = value; }
    }

    /// <summary>
    /// Returns the size of the object, based on its collider's bounds.
    /// If no collider set, returns Vector3.one.
    /// </summary>
    private Vector3 Size
    {
        get
        {
            if (m_Collider != null)
            {
                return m_Collider.bounds.size;
            }
            return Vector3.one;
        }
    }

    /// <summary>
    /// Returns the extents (half the size) of the object, based on it's collider bounds.
    /// If no collider set, returns Vector3.one / 2.
    /// </summary>
    private Vector3 Extents
    {
        get
        {
            if (m_Collider != null)
            {
                return m_Collider.bounds.extents;
            }
            return Vector3.one / 2;
        }
    }

    /// <summary>
    /// Gets the Y velocity of the character.
    /// </summary>
    public float YVelocity
    {
        get { return m_YVelocity; }
    }

    /// <summary>
    /// Gets the last X movement input axis.
    /// </summary>
    public float LastMovementAxis
    {
        get { return m_LastMovementAxis; }
    }

    #endregion


    #region Private Methods

    /// <summary>
    /// Binds actions to the user inputs.
    /// </summary>
    /// <param name="_Bind">If false, unbind actions.</param>
    private void BindControls(bool _Bind = true)
    {
        if (_Bind)
        {
            m_MoveXAction.performed += SetMoveDirectionX;
            m_MoveXAction.canceled += ResetMoveDirectionX;
            m_JumpAction.started += BeginJump;
            m_JumpAction.canceled += EndJump;

            m_MoveXAction.Enable();
            m_JumpAction.Enable();
        }
        else
        {
            m_MoveXAction.performed -= SetMoveDirectionX;
            m_MoveXAction.canceled -= ResetMoveDirectionX;
            m_JumpAction.started -= BeginJump;
            m_JumpAction.canceled -= EndJump;

            m_MoveXAction.Disable();
            m_JumpAction.Disable();
        }
    }

    #endregion


    #region Debug & Tests

#if UNITY_EDITOR

    /// <summary>
    /// Draws Gizmos for this component in the Scene View.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw the obstacles detection cast
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + Orientation * m_LastMovementObstaclesDetectionCastDistance, Size);

        // Draw the jump obstacles detection cast
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.up * m_LastJumpObstaclesDetectionCastDistance, Size);

        // Draw the movement direction vector
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + MovementDirection * 3f);
    }

    #endif

    #endregion

}