﻿using UnityEngine;
using UnityEngine.Events;

///<summary>
/// Represents a patrolling entity.
/// This character will go toward its target, then go back, continuously.
/// Note that the transform.right vector of this object is its current direction.
///</summary>
[HelpURL("https://github.com/DaCookie/empty-platformer/blob/master/Docs/patroller-controller.md")]
public class PatrollerController : MonoBehaviour
{

    #region Enums & Subclasses

    /// <summary>
    /// Represents the first direction to go.
    /// </summary>
    public enum EPatrollerPathDirection
    {
        Left,
        Right
    }

    [System.Serializable]
    private class PatrollerControllerEvents
    {
        // Called when the patroller changes its direction (so it arrives at the end of its path).
        // Sends the new direction vector of the entity.
        public Vector3Event OnChangeDirection;

        // Called each frame this patroller moves.
        public MovementInfosEvent OnUpdateMove;
    }

    #endregion


    #region Properties

    [Header("Settings")]

    [SerializeField, Tooltip("Defines the speed of the object (in units/second)")]
    private float m_Speed = 3f;

    [SerializeField, Tooltip("Defines the first direction to go")]
    private EPatrollerPathDirection m_PathDirection = EPatrollerPathDirection.Right;

    [SerializeField, Tooltip("Defines the length of the path to follow")]
    private float m_Distance = 9f;

    [SerializeField, Tooltip("Reference to the characters collider. By default, use this GameObject's collider")]
    private Collider m_Collider = null;

    [SerializeField, Tooltip("If true, the Patroller is not updated")]
    private bool m_FreezePatroller = false;

    [Header("Events")]

    [SerializeField]
    private PatrollerControllerEvents m_MovementEvents = new PatrollerControllerEvents();

    // The initial position of the character
    private Vector3 m_Origin = Vector3.zero;

    // The distance traveled by the object on its path
    private float m_CurrentPathDistance = 0f;

    // Is the object is going forward or backward on, its path?
    private bool m_Forward = true;

    #endregion


    #region Lifecycle

    /// <summary>
    /// Called when this component is loaded.
    /// </summary>
    private void Awake()
    {
        if (m_Collider == null) { m_Collider = GetComponent<BoxCollider>(); }
        m_Origin = transform.position;
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    private void Update()
    {
        UpdatePosition(Time.deltaTime);
    }

    #endregion


    #region Public Methods

    /// <summary>
    /// Resets the Patroller state (place the character at the beginning of its path).
    /// </summary>
    public void ResetPatroller()
    {
        m_CurrentPathDistance = 0f;
        m_Forward = true;
    }

    /// <summary>
    /// Freezes thsi Patroller.
    /// </summary>
    public bool FreezePatroller
    {
        get { return m_FreezePatroller; }
        set { m_FreezePatroller = value; }
    }

    #endregion


    #region Private Methods

    /// <summary>
    /// Updates the position of this object on its path.
    /// </summary>
    private void UpdatePosition(float _DeltaTime)
    {
        if(m_FreezePatroller) { return; }

        float movement = m_Speed * _DeltaTime;
        m_CurrentPathDistance = m_Forward ? Mathf.Min(m_CurrentPathDistance + movement, m_Distance) : Mathf.Max(0f, m_CurrentPathDistance - movement);
        if(m_CurrentPathDistance == m_Distance || m_CurrentPathDistance == 0f)
        {
            m_Forward = !m_Forward;
            m_MovementEvents.OnChangeDirection.Invoke(ForwardVector);
        }

        Vector3 lastPosition = transform.position;
        transform.position = m_Origin + PathVector * m_CurrentPathDistance;
        transform.right = ForwardVector;

        m_MovementEvents.OnUpdateMove.Invoke(new MovementInfos
        {
             entity = gameObject,
             lastPosition = lastPosition,
             currentPosition = transform.position,
             orientation = ForwardVector,
             speed = m_Speed
        });
    }

    /// <summary>
    /// Gets the bounds of this object, using its collider.
    /// </summary>
    private Vector3 Size
    {
        get
        {
            if(m_Collider == null) { m_Collider = GetComponent<Collider>(); }
            return m_Collider != null ? m_Collider.bounds.size : Vector3.one;
        }
    }

    /// <summary>
    /// Gets the current direction of the movement of this object.
    /// </summary>
    private Vector3 ForwardVector
    {
        get { return m_Forward ? PathVector : -PathVector; }
    }

    /// <summary>
    /// Gets the selected direction vector for the path of this object.
    /// </summary>
    private Vector3 PathVector
    {
        get { return (m_PathDirection == EPatrollerPathDirection.Right) ? Vector3.right : Vector3.left; }
    }

    /// <summary>
    /// Called each frame this patroller moves.
    /// </summary>
    public MovementInfosEvent OnUpdateMove
    {
        get { return m_MovementEvents.OnUpdateMove; }
    }

    /// <summary>
    /// Called when the patroller changes its direction (so it arrives at the end of its path).
    /// Sends the new direction vector of the entity.
    /// </summary>
    public Vector3Event OnChangeDirection
    {
        get { return m_MovementEvents.OnChangeDirection; }
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        // Draw patrol path
        Gizmos.color = Color.red;
        Vector3 size = Size;
        size.x += m_Distance;
        Vector3 origin = (UnityEditor.EditorApplication.isPlaying) ? m_Origin : transform.position;
        Vector3 center = origin - PathVector * (Size.x / 2f) + PathVector * (size.x / 2f);
        Gizmos.DrawWireCube(center, size);
    }

    #endif

    #endregion

}