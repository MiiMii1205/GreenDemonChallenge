using System;
using System.Runtime.CompilerServices;
using pworld.Scripts.Extensions;
using UnityEngine;

namespace GreenDemonChallenge.Behaviour;

public class GreenDemonImpactSFX : MonoBehaviour
{
    private static readonly int CharacterLayerMask = LayerMask.GetMask("Character");
    public float vel;
    public Rigidbody rig;

    public float velMult = 1f;
    public SFX_Instance[] m_impact;

    private void Start()
    {
        this.rig ??= GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (rig && !rig.isKinematic)
        {
            vel = Mathf.Lerp(vel, Vector3.SqrMagnitude(rig.linearVelocity) * velMult, 10f * Time.deltaTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PlayImpactSound(Vector3 pos)
    {
        for (var i = 0; i < m_impact.Length; i++)
        {
            m_impact[i].Play(pos);
        }
    }

    private void onCollisionEnter(Collision collision)
    {
        if (rig)
        {
            if (vel > 4.0f && !collision.collider.gameObject.IsInLayer( CharacterLayerMask))
            {
                PlayImpactSound(collision.contacts[0].point);
            }

            vel = 0;
        }
    }
}