using System;
using GreenDemonChallenge.Behaviour;
using GreenDemonChallenge.Data;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace GreenDemonChallenge.Patchers;

public static class GreenDemonPatcher
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.Awake))]
    [HarmonyPostfix]
    public static void RunManagerAwakePostfix(RunManager __instance)
    {
        var gdcManager = __instance.gameObject.GetOrAddComponent<GreenDemonHandler>();

        if (gdcManager == null)
        {
            GreenDemonChallenge.Log.LogError($"{nameof(GreenDemonHandler)} was not found on {__instance.gameObject}");
        }
    }

    [HarmonyPatch(typeof(GUIManager), nameof(GUIManager.Awake))]
    [HarmonyPostfix]
    public static void GUIManagerAwakePostfix(GUIManager __instance)
    {
        var gduiManager = __instance.gameObject.GetOrAddComponent<GreenDemonGUIManager>();

        if (gduiManager == null)
        {
            GreenDemonChallenge.Log.LogError($"{nameof(GreenDemonGUIManager)} was not found on {__instance.gameObject}");
        }
    }
    
    [HarmonyPatch(typeof(PeakHandler), nameof(PeakHandler.EndScreenComplete))]
    [HarmonyPostfix]
    public static void EndCutsceneCreditAwakePostfix(PeakHandler __instance)
    {
        // Congrats y'all won!
        GreenDemonHandler.Instance.ShrinkAllDemons();
    }
    
    [HarmonyPatch(typeof(MapHandler), nameof(MapHandler.JumpToSegment))]
    [HarmonyPostfix]
    public static void JumpToPostfix(MapHandler __instance, Segment segment)
    {
        switch (GreenDemonChallenge.RoomGreenDemonMode)
        {
            case GreenDemonModes.NORMAL:
            {
                GreenDemonHandler.Instance.ShrinkAllDemons();
                break;
            }
            case GreenDemonModes.VERY_HARD:
            case GreenDemonModes.HARD:
            {
                GreenDemonHandler.Instance.StopAllDemon((MapHandler.PreviousCampfire?.burnsFor ?? 0f) + 0.25f);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Resume the spawning after the campfire burns out.
        GreenDemonHandler.Instance.ResumeSpawning((MapHandler.PreviousCampfire?.burnsFor ?? 0f) + 0.25f);
    }

    [HarmonyPatch(typeof(TriggerEvent), nameof(TriggerEvent.OnTriggerEnter))]
    [HarmonyPrefix]
    public static void TriggerEventPrefix(TriggerEvent __instance, Collider other, ref bool __runOriginal)
    {
        if (__instance is GreenDemonTombTrigger tombTrigger)
        {
            __runOriginal = false;
            tombTrigger.OnGDCTriggerEnter(other);
        }
    }
    
    [HarmonyPatch(typeof(CharacterInput), nameof(CharacterInput.GetMovementInput))]
    [HarmonyPostfix]
    public static void TriggerEventPrefix(CharacterInput __instance, ref Vector2 __result)
    {
        if (GreenDemonHandler.Instance != null && GreenDemonHandler.IsWKeyStuck)
        {
            __result.y = 1;
        }
    }

    [HarmonyPatch(typeof(Campfire), nameof(Campfire.Light_Rpc))]
    [HarmonyPostfix]
    public static void LightPostfix(Campfire __instance, bool updateSegment)
    {
        if (updateSegment)
        {

            switch (GreenDemonChallenge.RoomGreenDemonMode)
            {
                case GreenDemonModes.NORMAL:
                {
                    GreenDemonHandler.Instance.ShrinkAllDemons();
                    break;
                }
                case GreenDemonModes.VERY_HARD:
                case GreenDemonModes.HARD:
                {
                    GreenDemonHandler.Instance.StopAllDemon(__instance.burnsFor + 1f);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Resume the spawning after the campfire burns out.
            GreenDemonHandler.Instance.ResumeSpawning(__instance.burnsFor + 1f);

        }
    }
    
}