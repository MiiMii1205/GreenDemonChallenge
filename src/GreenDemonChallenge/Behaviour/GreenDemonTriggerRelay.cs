using Photon.Pun;
using UnityEngine;

namespace GreenDemonChallenge.Behaviour;

public class GreenDemonTriggerRelay : MonoBehaviour
{
    public PhotonView view = null!;

    private void Awake()
    {
        view ??= GetComponent<PhotonView>();
    }

    [PunRPC]
    public void RPCA_GDCTrigger(int childID, string characterId)
    {
        if (transform.GetChild(childID) is { } child  &&
            child.TryGetComponent(out GreenDemonTombTrigger component))
        {
            component.Trigger(characterId);
        }
    }
}