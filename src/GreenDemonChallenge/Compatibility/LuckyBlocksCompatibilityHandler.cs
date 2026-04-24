using System;
using System.Runtime.CompilerServices;
using GreenDemonChallenge.Behaviour;
using GreenDemonChallenge.Compatibility.Patchers;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnnamedProducts;
using UnnamedProducts.Behaviours;
using UnnamedProducts.Behaviours.Item.GarbageBag;
using Zorro.Core;
using Zorro.Core.Serizalization;

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
                    LuckBlocks.Plugin.Id);
                GreenDemonChallenge.Log.LogInfo(
                    $"Lucky Blocks support is {((bool) _enabled ? "enabled" : "disabled")}");
            }

            return (bool) _enabled;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void PatchLuckyBlocks(Harmony harmony)
    {
        harmony.PatchAll(typeof(LuckyBlocksPatcher));
    }
    
}