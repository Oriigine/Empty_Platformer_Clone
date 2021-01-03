﻿using UnityEngine;
using UnityEngine.Events;

///<summary>
/// Represents the capacity of an object to take damages by touching another entity.
///</summary>
[HelpURL("https://github.com/DaCookie/empty-platformer/blob/master/Docs/contact-damages.md")]
public class ContactDamages : MonoBehaviour
{

    #region Subclasses

    [System.Serializable]
    private class ContactDamagesEvents
    {
        // Called when the character takes damages
        public HitInfosEvent OnContact = new HitInfosEvent();

        // Called after the character took damages, and enter in invincible state
        // Sends the invincibility duration
        public FloatEvent OnBeginInvincibility = new FloatEvent();

        // Called when the invincible state updates
        // Sends the invincibility timer ratio over the total duration
        public FloatEvent OnUpdateInvincibility = new FloatEvent();

        // Called when the character lose its invincible state
        public UnityEvent OnStopInvincibility = new UnityEvent();
    }

    #endregion


    #region Properties

    [Header("References")]

    [SerializeField, Tooltip("By default, gets the Health component on this GameObject")]
    private Health m_Health = null;

    [SerializeField, Tooltip("By default, gets the BoxCollider component on this GameObject")]
    private BoxCollider m_Collider = null;

    [Header("Settings")]

    [SerializeField, Tooltip("Defines the layer of the objects that can damage this entity")]
    private LayerMask m_DamagingLayer = 0;

    [SerializeField, Tooltip("Defines the number of lives to lose when touching a damaging object")]
    private int m_DamagesOnContact = 1;

    [SerializeField, Tooltip("Sets the duration of invincibility after touching a damaging object")]
    private float m_InvincibilityDuration = .4f;

    [Header("Events")]

    [SerializeField]
    private ContactDamagesEvents m_ContactDamagesEvents = new ContactDamagesEvents();

    private float m_InvincibilityTimer = 0f;

    #endregion


    #region Lifecycle

    /// <summary>
    /// Called when this component is loaded.
    /// </summary>
    private void Awake()
    {
        if (m_Health == null) { m_Health = GetComponent<Health>(); }
        if (m_Collider == null) { m_Collider = GetComponent<BoxCollider>(); }
        m_InvincibilityTimer = m_InvincibilityDuration + 1f;
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    private void Update()
    {
        // If the character is already dead, do nothing
        if(m_Health.IsDead) { return; }

        // Update the invincibility timer if needed
        if (m_InvincibilityTimer <= m_InvincibilityDuration)
        {
            m_InvincibilityTimer += Time.deltaTime;
            // If the invincibility state is finished
            if (m_InvincibilityTimer > m_InvincibilityDuration)
            {
                m_ContactDamagesEvents.OnStopInvincibility.Invoke();
            }
            else
            {
                m_ContactDamagesEvents.OnUpdateInvincibility.Invoke(InvincibilityRatio);
            }
        }

        // If the character is still invincible, don't check for contact damages
        if (IsInvincible) { return; }

        Collider[] contacts = Physics.OverlapBox(transform.position, Extents, Quaternion.identity, m_DamagingLayer);

        // If the character touches a damaging object
        if (contacts.Length != 0)
        {
            m_ContactDamagesEvents.OnContact.Invoke(new HitInfos()
            {
                shooter = contacts[0].gameObject,
                target = gameObject,
                distance = 0f,
                damages = m_DamagesOnContact,
                impact = transform.position,
                origin = contacts[0].transform.position
            });

            // Apply damages
            m_Health.RemoveLives(m_DamagesOnContact);

            // ScreenShake
            ScreenShake.instance.StartShake(.4f, .2f);

            // If not dead, begins invincibility state
            if (!m_Health.IsDead)
            {
                m_InvincibilityTimer = 0f;
                m_ContactDamagesEvents.OnBeginInvincibility.Invoke(m_InvincibilityDuration);
            }
        }
    }

    #endregion


    #region Public API

    /// <summary>
    /// Checks if the character is currently invincible.
    /// </summary>
    public bool IsInvincible
    {
        get { return m_InvincibilityTimer <= m_InvincibilityDuration; }
    }

    /// <summary>
    /// Gets the invincibility timer ratio over invincibility duration.
    /// </summary>
    public float InvincibilityRatio
    {
        get { return (m_InvincibilityDuration > 0f) ? Mathf.Clamp01(m_InvincibilityTimer / m_InvincibilityDuration) : 0f; }
    }

    /// <summary>
    /// Called when the character takes damages.
    /// </summary>
    public HitInfosEvent OnContact
    {
        get { return m_ContactDamagesEvents.OnContact; }
    }

    /// <summary>
    /// Called after the character took damages, and enter in invincible state. Sends the invincibility duration.
    /// </summary>
    public FloatEvent OnBeginInvincibility
    {
        get { return m_ContactDamagesEvents.OnBeginInvincibility; }
    }

    /// <summary>
    /// Called when the invincible state updates. Sends the invincibility timer ratio over the total duration.
    /// </summary>
    public FloatEvent OnUpdateInvincibility
    {
        get { return m_ContactDamagesEvents.OnUpdateInvincibility; }
    }

    /// <summary>
    /// Called when the character lose its invincible state.
    /// </summary>
    public UnityEvent OnStopInvincibility
    {
        get { return m_ContactDamagesEvents.OnStopInvincibility; }
    }

    #endregion


    #region Private Methods

    /// <summary>
    /// Gets the collider extents.
    /// If no collider set, returns Vector3.one.
    /// </summary>
    private Vector3 Extents
    {
        get { return (m_Collider != null) ? m_Collider.bounds.extents : Vector3.one; }
    }

    #endregion

}