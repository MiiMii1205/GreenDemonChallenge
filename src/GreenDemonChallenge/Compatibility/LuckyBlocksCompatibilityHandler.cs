using System.Runtime.CompilerServices;
using GreenDemonChallenge.Behaviour;
using HarmonyLib;
using UnityEngine;

namespace GreenDemonChallenge.Compatibility;

public static class LuckyBlocksCompatibilityHandler
{
    private static bool? _enabled;

    public static bool Enabled
    {
        get
        {
            if (_enabled == null)
            {
                _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(
                    "legocool.LuckyBlocks");
                GreenDemonChallenge.Log.LogInfo(
                    $"LuckyBlocks support is {((bool) _enabled ? "enabled" : "disabled")}");
            }

            return (bool) _enabled;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void PatchLuckyBlocks(Harmony harmony)
    {
        Outcomes.AddOutcome(SpawnDemon, 1, "Green Demon Spawn");
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void SpawnDemon(LuckyBreakable lb, Collision coll)
    {
        GreenDemonHandler.Instance.SpawnGreenDemon(lb.transform.position);
    }
    
}