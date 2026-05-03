using System;
using System.Runtime.CompilerServices;
using GreenDemonChallenge.Behaviour;
using PEAKLib.Core;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnnamedProducts;
using UnnamedProducts.Behaviours;
using UnnamedProducts.Behaviours.Item.GarbageBag;
using Zorro.Core;
using Zorro.Core.Serizalization;
using Object = UnityEngine.Object;

namespace GreenDemonChallenge.Compatibility;

public static class UnnamedCompatibilityHandler
{
    private static bool? _enabled;

    public static bool Enabled
    {
        get
        {
            if (_enabled == null)
            {
                _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(
                    "com.github.MiiMii1205.UnnamedProducts");
                GreenDemonChallenge.Log.LogInfo(
                    $"UnnamedProducts support is {((bool) _enabled ? "enabled" : "disabled")}");
            }

            return (bool) _enabled;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void SetCharacterOnFire(Character character)
    {
        var bd = character.refs.head;
        var fb = PhotonNetwork.Instantiate(StickyFireballController.FireballPrefab.name, bd.rig.transform.position,
            bd.rig.transform.rotation);
        
        var stick = fb.GetComponent<StickyFireballController>();
        stick.StickTo(character.refs.head.gameObject);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void SpawnFireball(Character character)
    {
        if (NetworkPrefabManager.TryGetNetworkPrefab($"{UnnamedPlugin.Id}:AntifreezeExplosion", out var p))
        {
            Object.Instantiate(p, character.Center, Quaternion.identity);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void UnnamifyInventory(Character character)
    {
        var unnamedItem = 0;
        var currentItem = character.data.currentItem;

        if (GreenDemon.IsHoldingSomethingTemporary(character) && currentItem)
        {
            if (IsGarbageBag(currentItem))
            {
                unnamedItem += UnnamifyGarbageBags(character.player.tempFullSlot.prefab, currentItem.data);
            }
            else
            {
                if (UnnamedPlugin.HasUnnamedVariant(currentItem))
                {
                    var d = currentItem.data;
                    var pref = UnnamedPlugin.GetUnnamedVariant(currentItem);

                    character.player.tempFullSlot.EmptyOut();
                    character.player.tempFullSlot.SetItem(pref.GetComponent<Item>(), d);
                    unnamedItem++;
                }
            }
        }

        // Backpack slot
        if (!character.player.backpackSlot.IsEmpty())
        {
            if (GreenDemon.IsHoldingBackpack(character) && currentItem)
            {
                // Currently holding the backpack
                if (currentItem is Backpack b && b.data.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData bd) && b.TryGetComponent(out BackpackVisuals bv))
                {
                    unnamedItem += ChangeBackpackItems(bd, bv);
                }
            }
            else if (character.player.backpackSlot.hasBackpack)
            {
                // Currenty wearking the backpack
                if (character.refs.backpackTransform.GetComponentInChildren<BackpackVisuals>(true) is { } bbv &&
                    character.player.backpackSlot.data.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData bd))
                {
                    unnamedItem += ChangeBackpackItems(bd, bbv);
                }
            }
        }

        for (var index = character.player.itemSlots.Length - 1; index >= 0; --index)
        {
            var s = character.player.GetItemSlot((byte) index);

            if (s.IsEmpty())
            {
                continue;
            }

            if (IsGarbageBag(s.prefab))
            {
                unnamedItem += UnnamifyGarbageBags(s.prefab, s.data);
            }
            else
            {
                if (GreenDemon.IsHoldingOutSlot(character, s) && currentItem)
                {
                    if (UnnamedPlugin.HasUnnamedVariant(currentItem))
                    {
                        var d = currentItem.data;
                        var pref = UnnamedPlugin.GetUnnamedVariant(currentItem);

                        character.player.EmptySlot(Optionable<byte>.Some((byte) index));
                        s.SetItem(pref.GetComponent<Item>(), d);
                        unnamedItem++;
                    }
                }
                else
                {
                    if (UnnamedPlugin.HasUnnamedVariant(s.prefab))
                    {
                        var pref = UnnamedPlugin.GetUnnamedVariant(s.prefab);
                        s.prefab = pref.GetComponent<Item>();
                        unnamedItem++;
                    }
                }
            }
        }

        character.view.RPC(nameof(Player.SyncInventoryRPC), RpcTarget.Others,
            IBinarySerializable.ToManagedArray(
                new InventorySyncData(character.player.itemSlots, character.player.backpackSlot,
                    character.player.tempFullSlot)), false);

        GreenDemonChallenge.Log.LogInfo($"Unnameified {unnamedItem} item(s).");
    }

    private static int ChangeBackpackItems(BackpackData backpackData, BackpackVisuals backpackVisuals)
    {
        var unnamedItem = 0;
        for (var i = 0; i < backpackData.itemSlots.Length; i++)
        {
            if (backpackData.itemSlots[i].IsEmpty())
            {
                continue;
            }

            var it = backpackVisuals.spawnedVisualItems[(byte) i];

            if (IsGarbageBag(it))
            {
                unnamedItem += UnnamifyGarbageBags(backpackData.itemSlots[i].prefab, backpackData.itemSlots[i].data);
            }
            else
            {
                if (UnnamedPlugin.HasUnnamedVariant(it))
                {
                    var d = it.data;

                    var pref = UnnamedPlugin.GetUnnamedVariant(it);

                    backpackData.itemSlots[(byte) i].EmptyOut();
                    backpackVisuals.RefreshVisuals();

                    backpackData.itemSlots[(byte) i].SetItem(pref.GetComponent<Item>(), d);
                    unnamedItem++;
                }
            }
        }

        return unnamedItem;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void RespawnFlares(Vector3 spawnPos, int amountToRespawn)
    {
        if (ItemDatabase.TryGetItem(32, out var it))
        {
            for (var i = 0; i < amountToRespawn; i++)
            {
                var usedPrefab = UnnamedPlugin.ShouldBeUnnamed
                    ? UnnamedPlugin.GetUnnamedVariant(it).GetComponent<Item>()
                    : it;

                PhotonNetwork.InstantiateItemRoom(usedPrefab.gameObject.name, spawnPos + (Vector3.up * 0.09147437f * i),
                    Quaternion.identity);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int RemoveFlairsFormGarbageBags(Item sPrefab, ItemInstanceData sData)
    {
        var flareRemoved = 0;

        if (sPrefab.TryGetComponent(out UnnamedGarbageBagController ugb) &&
            sData.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData bpd))
        {
            for (var i = 0; i < bpd.itemSlots.Length; i++)
            {
                var slot = bpd.itemSlots[i];

                if (slot.IsEmpty())
                {
                    continue;
                }

                if (slot.prefab.TryGetComponent<Flare>(out _))
                {
                    slot.EmptyOut();
                    flareRemoved++;
                }
                else if (slot.prefab.TryGetComponent<UnnamedGarbageBagController>(out _))
                {
                    flareRemoved += RemoveFlairsFormGarbageBags(slot.prefab, slot.data);
                }
            }
        }

        return flareRemoved;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int CookGarbageBags(Item sPrefab, ItemInstanceData sData, Item? bakcpackItem = null)
    {
        var cookedItems = 0;

        if (sPrefab.TryGetComponent(out UnnamedGarbageBagController ugb) &&
            sData.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData bpd))
        {
            for (var i = 0; i < bpd.itemSlots.Length; i++)
            {
                var slot = bpd.itemSlots[i];

                if (slot.IsEmpty())
                {
                    continue;
                }

                if (slot.prefab.TryGetComponent<UnnamedGarbageBagController>(out _))
                {
                    cookedItems += CookGarbageBags(slot.prefab, slot.data);
                }

                if (slot.data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var cook))
                {
                    ++cook.Value;
                    cookedItems++;
                }

                cookedItems++;
            }

            if (bakcpackItem)
            {
                bakcpackItem.cooking.FinishCooking();
                cookedItems++;
            }
            else
            {

                if (sData.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var bgcook))
                {
                    ++bgcook.Value;
                    cookedItems++;
                }
            }

        }

        return cookedItems;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int UnnamifyGarbageBags(Item sPrefab, ItemInstanceData sData)
    {
        var unnamedItem = 0;

        if (sPrefab.TryGetComponent(out UnnamedGarbageBagController ugb) &&
            sData.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData bpd))
        {
            for (var i = 0; i < bpd.itemSlots.Length; i++)
            {
                var slot = bpd.itemSlots[i];

                if (slot.IsEmpty())
                {
                    continue;
                }

                if (slot.prefab.TryGetComponent<UnnamedGarbageBagController>(out _))
                {
                    unnamedItem += UnnamifyGarbageBags(slot.prefab, slot.data);
                }
                else
                {
                    if (UnnamedPlugin.HasUnnamedVariant(slot.prefab))
                    {
                        var d = slot.data;

                        var pref = UnnamedPlugin.GetUnnamedVariant(slot.prefab);

                        bpd.itemSlots[(byte) i].EmptyOut();

                        bpd.itemSlots[(byte) i].SetItem(pref.GetComponent<Item>(), d);
                        unnamedItem++;
                    }
                }
            }
        }

        return unnamedItem;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static bool IsGarbageBag(Item sPrefab)
    {
        return sPrefab.TryGetComponent<UnnamedGarbageBagController>(out _);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static bool HasAnyFlares(Character character)
    {
        if (!character.player.backpackSlot.IsEmpty())
        {
            if (GreenDemon.IsHoldingBackpack(character) && character.data.currentItem)
            {
                if (character.data.currentItem.TryGetComponent(out Backpack b))
                {
                    foreach (var itemSlot in b.backpackReference.Value.Item2.GetData().itemSlots)
                    {
                        if (!itemSlot.IsEmpty() && (itemSlot.prefab.TryGetComponent<Flare>(out _) ||
                                                    CheckForFlareInGarbageBag(itemSlot))) return true;
                    }
                }
            }
            else if (character.player.backpackSlot.hasBackpack)
            {
                if (character.refs.backpackTransform.GetComponentInChildren<BackpackVisuals>(true) is { } bbv)
                {
                    foreach (var itemSlot in bbv.GetBackpackData().itemSlots)
                    {
                        if (!itemSlot.IsEmpty() && (itemSlot.prefab.TryGetComponent<Flare>(out _) ||
                                                    CheckForFlareInGarbageBag(itemSlot)))
                        {
                            return true;
                        }
                    }
                }
            }   
        }

        foreach (var itemSlot in character.player.itemSlots)
        {
            if (!itemSlot.IsEmpty() &&
                (itemSlot.prefab.TryGetComponent<Flare>(out _) || CheckForFlareInGarbageBag(itemSlot)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CheckForFlareInGarbageBag(ItemSlot itemSlot)
    {
        if (itemSlot.prefab.TryGetComponent(out UnnamedGarbageBagController ugb) &&
            itemSlot.data.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData bpd))
        {
            foreach (var its in bpd.itemSlots)
            {
                if (!itemSlot.IsEmpty() && (its.prefab.TryGetComponent<Flare>(out _) || CheckForFlareInGarbageBag(its)))
                {
                    return true;
                }
            }
        }

        return false;
    }


    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void TryTurnIntoUnnamed(ref Item item)
    {
        if (UnnamedPlugin.HasUnnamedVariant(item) &&  UnnamedPlugin.ShouldBeUnnamed)
        {
            item = UnnamedPlugin.GetUnnamedVariant(item).GetComponent<Item>();
        }
    }
}