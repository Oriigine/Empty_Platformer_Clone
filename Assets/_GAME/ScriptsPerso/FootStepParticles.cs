using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootStepParticles : MonoBehaviour
{

    [SerializeField]
    private ParticleSystem m_Dust = null;

    public void CreateDust()
    {
        m_Dust.Play();
    }

}
