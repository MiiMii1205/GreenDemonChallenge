using System.Collections;
using GreenDemonChallenge.Behaviour.GUI;
using Photon.Voice.Unity.Demos;
using pworld.Scripts.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace GreenDemonChallenge.Behaviour;

public class GreenDemonGUIManager : MonoBehaviour
{
    public static GreenDemonGUIManager Instance = null!;
    public GameObject demonIsHere = null!;
    public GameObject trackerScreen = null!;
    public RectTransform trackerScreenTransform = null!;
    public Vector2 m_scaleReferences;

    private void Awake()
    {
        Instance = this;
        ResetDemonIsHere();
        trackerScreen = Instantiate(GUIManager.instance.heroObject, GUIManager.instance.heroObject.transform.parent,
            true);
        trackerScreen.name = "GreenDemonTrackers";

        trackerScreenTransform =  trackerScreen.GetComponent<RectTransform>();
        
        trackerScreenTransform.anchoredPosition = new Vector2(0f, 100f);
        trackerScreenTransform.sizeDelta = new Vector2(0f, -200f);
        trackerScreenTransform.localPosition = new Vector3(0f, 100f, 0f);

        trackerScreen.transform.DestroyChildren();

        trackerScreen.SetActive(true);

        var canvasScaler = trackerScreen.GetComponentInParent<CanvasScaler>();

        if (canvasScaler)
        {
            m_scaleReferences = canvasScaler.referenceResolution / new Vector2(Screen.width, Screen.height);
        }
    }

    public void TheDemonIsHere()
    {
        demonIsHere.SetActive(true);
        StartCoroutine(DemonSpawnRoutine());
    }

    private IEnumerator DemonSpawnRoutine()
    {
        yield return new WaitForSeconds(4f);
        demonIsHere.SetActive(false);
        ResetDemonIsHere();
    }

    private void ResetDemonIsHere()
    {
        if (demonIsHere)
        {
            Destroy(demonIsHere);    
        }
        
        demonIsHere = Instantiate(GreenDemonChallenge.TheDemonIsHerePrefab,
            GUIManager.instance.lavaRises.transform.parent, false);
    }

    public void AddDemonTracker(GreenDemon greenDemon)
    {
        var track = Instantiate(GreenDemonChallenge.TrackerPrefab, trackerScreen.transform, false);

        var t = track.GetOrAddComponent<GreenDemonTracker>();
        
        t.Demon = greenDemon;
    }
}