using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class RepetedSound : MonoBehaviour
{
    public AudioSource m_SoundSource;
    public AudioClip m_Sound;

    public void PlaySound()
    {
        //m_SoundSource.pitch = Random.Range(.2f, .6f);
        m_SoundSource.PlayOneShot(m_Sound);
    }
}
