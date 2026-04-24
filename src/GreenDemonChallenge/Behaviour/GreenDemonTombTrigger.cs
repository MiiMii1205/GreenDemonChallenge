using System.Collections;
using Peak.Network;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GreenDemonChallenge.Behaviour;

public class GreenDemonTombTrigger : TriggerEvent
{
    public UnityEvent<string> greenDemonTriggerEvent;
    public void OnGDCTriggerEnter(Collider other)
    {
        if (!other.isTrigger && other.GetComponentInParent<Character>() is { } player)
        {
            TriggerEntered(player);
        }
    }

    public void TriggerEntered(Character player)
    {
        if (view.IsMine)
        {
            hasActivated = true;
            view.RPC(nameof(GreenDemonTriggerRelay.RPCA_GDCTrigger), RpcTarget.All, base.transform.GetSiblingIndex(), player.player.GetUserId());
        }
    }

    public void Trigger(string playerId)
    {
        GameUtils.instance.StartCoroutine(TriggerRoutine(playerId));
    }


    private IEnumerator TriggerRoutine(string playerID)
    {
        if (waitForRenderFrame)
        {
            yield return new WaitForEndOfFrame();
        }

        greenDemonTriggerEvent.Invoke(playerID);
        hasActivated = true;
        hasTriggered = true;
    }
}