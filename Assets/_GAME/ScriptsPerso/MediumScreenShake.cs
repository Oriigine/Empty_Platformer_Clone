using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediumScreenShake : MonoBehaviour
{
    [SerializeField]
    private float m_Duration = .4f;
    [SerializeField]
    private float m_Magnitude = 1.0f;

    public void MeduimShake()
    {
        Vector3 l_OriginalPos = transform.localPosition;

        float elapsed = 0.0f;

        while (elapsed < m_Duration)
        {
            float x = Random.Range(-1f, 1f) * m_Magnitude;
            float y = Random.Range(-1f, 1f) * m_Magnitude;

            transform.localPosition = new Vector3(x, y, l_OriginalPos.z);

            elapsed += Time.deltaTime;
        }

        transform.localPosition = l_OriginalPos;
        Debug.LogWarning("MeduimShake!");
    }
}
