using System;
using System.Collections;
using UnityEngine;

namespace GreenDemonChallenge.Behaviour;

public class DestroyAfterEmit: MonoBehaviour
{
    public ParticleSystem m_particle = null!;

    private void Awake()
    {
        m_particle ??= GetComponent<ParticleSystem>();
    }

    private IEnumerator Start()
    {
        m_particle.Play();
        yield return new WaitUntil(() => !m_particle.IsAlive());
        Destroy(gameObject);
    }
}