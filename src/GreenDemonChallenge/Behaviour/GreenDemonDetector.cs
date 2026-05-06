using System;
using System.Collections.Generic;
using UnityEngine;

namespace GreenDemonChallenge.Behaviour;

public class GreenDemonDetector: MonoBehaviour
{
    public Dictionary<int, GreenDemon> m_demons = new Dictionary<int, GreenDemon>();
    public int selfID;
    public float m_detectorRadius;

    private void Start()
    {
        selfID = GetInstanceID();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger && selfID != other.GetInstanceID() && other.gameObject.TryGetComponent(out GreenDemon gd))
        {
            m_demons[other.GetInstanceID()] = gd;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger)
        {
            m_demons.Remove(other.GetInstanceID());
        }
    }
}