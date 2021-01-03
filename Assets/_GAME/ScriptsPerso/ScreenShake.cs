using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake instance;

    [SerializeField]
    private float m_ShakeTimeRemaining, m_ShakePower, m_ShakeFadeTime, m_ShakeRotation;

    //public float m_RotationMultiplier = 15f;


    public void Start()
    {
        instance = this;
    }

    private void Update()
    {
        
    }
    private void LateUpdate()
    {
        /*Vector3 OriginalPos = transform.localPosition;
        Quaternion OriginalRot = transform.localRotation;*/
        

        if (m_ShakeTimeRemaining > 0)
        {
            m_ShakeTimeRemaining -= Time.deltaTime;

            float xAmount = Random.Range(-1f, 1f) * m_ShakePower;
            float yAmount = Random.Range(-1f, 1f) * m_ShakePower;

            transform.position += new Vector3(xAmount, yAmount, 0f);

            m_ShakePower = Mathf.MoveTowards(m_ShakePower, 0f, m_ShakeFadeTime * Time.deltaTime);

            //m_ShakeRotation = Mathf.MoveTowards(m_ShakeRotation, 0f, m_ShakeFadeTime * m_RotationMultiplier * Time.deltaTime);
        }
        /*else
        {
            transform.localPosition = new Vector3(OriginalPos.x, OriginalPos.y, OriginalPos.z);
            transform.localRotation = new Quaternion(OriginalRot.x, OriginalRot.y, OriginalRot.z, OriginalRot.w);
        }*/

        //transform.rotation = Quaternion.Euler(0f, 0f, m_ShakeRotation * Random.Range(-1f, 1f));

    }

    public void StartShake(float length, float power)
    {
        m_ShakeTimeRemaining = length;
        m_ShakePower = power;

        m_ShakeFadeTime = power / length;

        //m_ShakeRotation = power * m_RotationMultiplier;
    }
}
