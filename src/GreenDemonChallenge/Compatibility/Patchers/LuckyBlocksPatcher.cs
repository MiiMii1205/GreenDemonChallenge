using System.Runtime.CompilerServices;
using GreenDemonChallenge.Behaviour;
using HarmonyLib;
using UnityEngine;

namespace GreenDemonChallenge.Compatibility.Patchers;

public static class LuckyBlocksPatcher
{
    [HarmonyPatch(typeof(Outcomes), nameof(Outcomes.TriggerRandom))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void TriggerRandomPostfix(LuckyBreakable lb, Collision coll, ref bool __runOriginal)
    {
        if (Random.value < 0.01f)
        {
            __runOriginal = false;
            // Congratulations
            GreenDemonHandler.Instance.SpawnGreenDemon(lb.transform.position);
        } 
    }
}