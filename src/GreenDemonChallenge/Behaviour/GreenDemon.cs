using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GreenDemonChallenge.Compatibility;
using GreenDemonChallenge.Data;
using Peak.Afflictions;
using Photon.Pun;
using pworld.Scripts;
using pworld.Scripts.Extensions;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Diagnostics;
using Zorro.Core;
using Zorro.Core.Serizalization;

namespace GreenDemonChallenge.Behaviour;

public class GreenDemon : MonoBehaviourPunCallbacks
{
    public static List<GreenDemon> AllDemons = [];
    public SphereCollider collider = null!;

    public Vector3 centerOfMass;

    public float mass = 5f;
    public Rigidbody rig = null!;
    public AudioSource source = null!;

    public Transform m_demonTransform;

    public Renderer mainRenderer = null!;
    public Animator animator = null!;

    public float m_catchRadius = 1f;
    private float m_chaseTimeout;
    private float m_chaseUpdateCooldown = 3f;
    private LayerMask m_characterLayerMask = 1024;

    private Character m_chasingCharacter = null!;
    private float m_destroyTick;
    private bool m_inActiveChase;
    private bool m_isConsumed;
    private bool m_isShrinking;
    private const float MovementForce = 250f;
    public float m_stopTick;

    public Action? OnPlayerCaught;
    public Action? OnShrink;
    private PositionSyncer m_posSyncer = null!;

    private float m_shrinkDuration = 1.25f;

    private double m_timeSinceTick;
    protected PhotonView view = null!;

    private bool m_isSpawning = true;

    public Vector3 Center
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !mainRenderer.UnityObjectExists() ? transform.position : mainRenderer.bounds.center;
    }

    public bool IsChasingLocalPlayer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    private bool ShouldSearchForAnotherTarget => !HasTarget || !m_chasingCharacter.IsLocal;

    private void Awake()
    {
        view ??= GetComponent<PhotonView>();
        AddPhysics();
        m_posSyncer ??= GetComponent<PositionSyncer>();
        animator ??= GetComponent<Animator>();
        source ??= GetComponent<AudioSource>();
        mainRenderer ??= GetComponent<Renderer>();
        m_demonTransform ??= mainRenderer.transform;
    }

    private void Start()
    {
        GreenDemonChallenge.Log.LogInfo("SPAWN A GREEN DEMON!");
        GreenDemonGUIManager.Instance.TheDemonIsHere();

        m_inActiveChase = true;
        AllDemons.Add(this);
        GreenDemonGUIManager.Instance.AddDemonTracker(this);

        m_minSpeedSqrDistance = Mathf.Pow(source.minDistance, 2);
        m_maxSpeedSqrDistance = Mathf.Pow(source.maxDistance, 2);

        // Y speeds are half 
        m_baseSpeed = new Vector3(1f, 0.5f, 1f);

        m_roomSpeedMultiplier = GreenDemonChallenge.RoomGreenDemonSpeed switch
        {
            GreenDemonSpeeds.SLOW => 0.5f,
            GreenDemonSpeeds.MEDIUM => 1f,
            GreenDemonSpeeds.FAST => 2f,
            _ => throw new ArgumentOutOfRangeException()
        };

        StartCoroutine(WaitForSpawnAnimationToFinish());
    }

    private IEnumerator WaitForSpawnAnimationToFinish()
    {
        m_isSpawning = true;

        // Wait for the animation to finish.
        yield return new WaitUntil(() => !animator.IsPlaying("GreenDemonSpawn"));

        // Wait another delay before starting the chase
        yield return new WaitForSecondsRealtime(GreenDemonChallenge.RoomGreenDemonDelay);

        m_isSpawning = false;

        source.Play();
    }

    public float vel;
    public SFX_Instance[] m_impact;
    public float velMult = 10f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PlayImpactSound(Vector3 pos)
    {
        for (int i = 0, length = m_impact.Length; i < length; ++i)
        {
            m_impact[i].Play(pos);
        }
    }

    private void Update()
    {
        if (!m_isSpawning && !m_isConsumed)
        {
            UpdateChase();
        }

        if (rig && !rig.isKinematic)
        {
            vel = Mathf.Lerp(vel, Vector3.SqrMagnitude(rig.linearVelocity) * velMult, 10f * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        // Move closer to your target

        if (!m_isSpawning && PhotonNetwork.InRoom && !m_isConsumed)
        {
            m_chaseTimeout += Time.fixedDeltaTime;

            if (m_stopTick > 0f)
            {
                m_stopTick -= Time.fixedDeltaTime;
            }
            else if (view.IsMine && HasTarget)
            {
                var movementForce = Vector3.Scale((TargetPosition - Center).normalized, CalcMoveSpeed());

                rig.AddForce(
                    (movementForce *
                     Time.fixedDeltaTime),
                    ForceMode.Acceleration);

                m_movement = movementForce.normalized;

                CheckForCaughtPlayers();
            }
        }
    }

    public void RefreshVolume()
    {
        source.volume = GreenDemonChallenge.GreenDemonVolume.Value;
    }

    public Vector3 TargetPosition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_chasingCharacter?.Center ?? GreenDemonHandler.Instance.GroupPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 CalcMoveSpeed()
    {
        return (m_baseSpeed + 0.ToVec()) * (Util.RangeLerp(
            0.5f,
            1f,
            m_minSpeedSqrDistance,
            m_maxSpeedSqrDistance,
            (TargetPosition - Center).sqrMagnitude
        ) * (MovementForce * m_roomSpeedMultiplier));
    }

    public SFX_Instance[] consumedSfx = [];
    public SFX_Instance[] shrunkSfx = [];

    private void OnDestroy()
    {
        AllDemons.Remove(this);
    }

    private void PlayImpactSounds(Collision collision)
    {
        if (rig)
        {
            if (vel > 4.0f && !collision.collider.gameObject.IsInLayer(m_characterLayerMask))
            {
                view.RPC(nameof(RPC_PlayImpactSFX), RpcTarget.All, collision.contacts[0].point);
            }

            vel = 0;
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (m_inActiveChase && col.gameObject.IsInLayer(m_characterLayerMask) &&
            col.gameObject.GetComponentInParent<Character>() is { } character && character &&
            character.IsLocal && TargetIsValid(character))
        {
            CatchPlayer(Character.localCharacter);
        }
        else
        {
            rig.AddForce(
                (Vector3.Scale(col.impulse.normalized, CalcMoveSpeed()) + (Vector3.Scale(
                    (TargetPosition - Center).normalized, -CalcMoveSpeed() * 0.5f)) * Time.fixedDeltaTime),
                ForceMode.Acceleration);

            if (view.IsMine)
            {
                PlayImpactSounds(col);
            }
        }
    }

    private void OnCollisionStay(Collision col)
    {
        // No need to check for collisions here since it's expensive and the Update will eventually catch them.
        if (!col.gameObject.IsInLayer(m_characterLayerMask))
        {
            rig.AddForce(
                (Vector3.Scale(col.impulse.normalized, CalcMoveSpeed()) + (Vector3.Scale(
                    (TargetPosition - Center).normalized, -CalcMoveSpeed())) * Time.fixedDeltaTime),
                ForceMode.Acceleration);

            if (view.IsMine)
            {
                PlayImpactSounds(col);
            }
        }
    }

    private void AddPhysics()
    {
        rig ??= gameObject.GetOrAddComponent<Rigidbody>();
        rig.mass = mass;
        centerOfMass = rig.centerOfMass;
        rig.interpolation = RigidbodyInterpolation.Interpolate;
        collider ??= GetComponent<SphereCollider>();
        m_catchRadius = collider.radius * 1.25f;
    }

    private void ForceSyncForFrames(int frames = 10)
    {
        if (m_posSyncer)
        {
            m_posSyncer.forceSyncFrames = frames;
        }
    }

    public bool HasTarget
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_chasingCharacter;
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        if (view.IsMine)
        {
            ForceSyncForFrames();
            view.RPC(nameof(UpdateNewChasingCharacter), newPlayer, m_chasingCharacter.view.ViewID);
        }
    }

    [PunRPC]
    public void RequestUpdatePlayerChase(int viewId, float distSqr)
    {
        var devilCenter = Center;

        if (!HasTarget || (
                Character.GetCharacterWithPhotonID(viewId, out var character) && character &&
                character != m_chasingCharacter &&
                TargetIsValid(character) &&
                distSqr <
                Vector3.SqrMagnitude(m_chasingCharacter.Center - devilCenter)))
        {
            GreenDemonChallenge.Log.LogInfo("Sending update for chase change");
            view.RPC(nameof(UpdateNewChasingCharacter), RpcTarget.All, viewId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TargetIsValid(Character target)
    {
        return !target.isBot && !target.data.dead && !target.data.fullyPassedOut &&
               GreenDemonHandler.Instance.ShouldChaseCharacter(target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHoldingSomethingTemporary(Character c)
    {
        return !c.player.tempFullSlot.IsEmpty() && c.refs.items.currentSelectedSlot.IsSome &&
               c.refs.items.currentSelectedSlot.Value == c.player.tempFullSlot.itemSlotID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHoldingBackpack(Character c)
    {
        return !c.player.backpackSlot.IsEmpty() && c.refs.items.currentSelectedSlot.IsSome &&
               c.refs.items.currentSelectedSlot.Value == c.player.backpackSlot.itemSlotID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHoldingOutSlot(Character c, ItemSlot s)
    {
        return !s.IsEmpty() && c.refs.items.currentSelectedSlot.IsSome &&
               c.refs.items.currentSelectedSlot.Value == s.itemSlotID;
    }

    [PunRPC]
    public void UpdateNewChasingCharacter(int viewId)
    {
        if (Character.GetCharacterWithPhotonID(viewId, out var character) && m_chasingCharacter != character)
        {
            m_chasingCharacter = character;
            GreenDemonChallenge.Log.LogInfo($"New target: {character}!");

            IsChasingLocalPlayer = m_chasingCharacter.IsLocal;

            m_chaseTimeout = 0f;

            if (view.IsMine && !Equals(character.view.Owner, view.Owner))
            {
                GreenDemonChallenge.Log.LogInfo($"Transferring ownership to {character.view.Owner}...");
                view.TransferOwnership(character.view.Owner);
            }
        }
    }

    private void CheckForCaughtPlayers()
    {
        if (m_inActiveChase && Character.localCharacter && TargetIsValid(Character.localCharacter) &&
            Vector3.SqrMagnitude(Character.localCharacter.Center - Center) <= (m_catchRadius * m_catchRadius))
        {
            CatchPlayer(Character.localCharacter);
        }
    }

    private static int RemoveFlairFromBackpack(BackpackData backpackData, BackpackVisuals backpackVisuals)
    {
        var flareRemoved = 0;

        for (var i = 0; i < backpackData.itemSlots.Length; i++)
        {
            if (backpackData.itemSlots[i].IsEmpty())
            {
                continue;
            }

            var it = backpackVisuals.spawnedVisualItems[(byte) i];

            if (UnnamedCompatibilityHandler.Enabled &&
                UnnamedCompatibilityHandler.IsGarbageBag(backpackData.itemSlots[i].prefab))
            {
                flareRemoved += UnnamedCompatibilityHandler.RemoveFlairsFormGarbageBags(
                    backpackData.itemSlots[i].prefab,
                    backpackData.itemSlots[i].data);
            }
            else
            {
                if (it.TryGetComponent<Flare>(out _))
                {
                    PhotonNetwork.Destroy(it.gameObject);
                    flareRemoved++;

                    backpackData.itemSlots[(byte) i].EmptyOut();
                    backpackVisuals.RefreshVisuals();
                }
            }
        }

        return flareRemoved;
    }

    private static int CookItemFromBackpack(BackpackData backpackData, BackpackVisuals backpackVisuals)
    {
        var cookedItems = 0;
        for (var i = 0; i < backpackData.itemSlots.Length; i++)
        {
            if (backpackData.itemSlots[i].IsEmpty())
            {
                continue;
            }

            var it = backpackVisuals.spawnedVisualItems[(byte) i];

            if (UnnamedCompatibilityHandler.Enabled &&
                UnnamedCompatibilityHandler.IsGarbageBag(backpackData.itemSlots[i].prefab))
            {
                cookedItems += UnnamedCompatibilityHandler.CookGarbageBags(backpackData.itemSlots[i].prefab,
                    backpackData.itemSlots[i].data, it);
            }
            else
            {
                it.cooking.FinishCooking();
                cookedItems++;
            }
        }

        return cookedItems;
    }

    private void ApplyEffect(Character character, GreenDemonCaughtEffects effect)
    {
        GreenDemonChallenge.Log.LogInfo($"Applying effect: {effect} to {character}...");

        switch (effect)
        {
            case GreenDemonCaughtEffects.RANDOM:
            {
                GreenDemonCaughtEffects eff;

                var vals =
                    (GreenDemonCaughtEffects[]) Enum.GetValues(typeof(GreenDemonCaughtEffects));

                do
                {
                    eff = vals.GetRandom();
                } while (!IsEffectRandoValid(character, eff));

                ApplyEffect(character, eff);
            }
                break;
            case GreenDemonCaughtEffects.KILL:
                // Congratulations!
                character.DieInstantly();
                break;

            case GreenDemonCaughtEffects.ZOMBIFY:
                // A new friend!
                if (!character.TryCheckpoint())
                {
                    character.view.RPC(nameof(Character.RPCA_Zombify), RpcTarget.All, character.Center);
                }

                break;

            case GreenDemonCaughtEffects.FULL_INJURY:
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, 1f);
                break;

            case GreenDemonCaughtEffects.HALF_INJURY:
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, 0.5f);
                break;

            case GreenDemonCaughtEffects.FULL_POISON:
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Poison, 1f);
                break;

            case GreenDemonCaughtEffects.HALF_POISON:
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Poison, 0.5f);
                break;

            case GreenDemonCaughtEffects.FULL_SPORES:
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Spores, 1f);
                break;

            case GreenDemonCaughtEffects.HALF_SPORES:
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Spores, 0.5f);
                break;

            case GreenDemonCaughtEffects.CURSE:
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, 0.25f);
                break;

            case GreenDemonCaughtEffects.FLING:
                // Bye!
                character.AddForce(((Center - character.Center).normalized * 125f) / Time.fixedDeltaTime);
                break;

            case GreenDemonCaughtEffects.FALL:
                character.Fall(30f);
                break;

            case GreenDemonCaughtEffects.SCOUTMASTER:
            {
                if (Scoutmaster.GetPrimaryScoutmaster(out var sc))
                {
                    sc.SetCurrentTarget(character, 60f);
                }
                else
                {
                    GreenDemonChallenge.Log.LogWarning("Can't call the SCOUTMASTER right now. Flinging you instead...");
                    ApplyEffect(character, GreenDemonCaughtEffects.FLING);
                }

                break;
            }

            case GreenDemonCaughtEffects.POOR_BOY:
            {
                var itemRemoved = 0;
                // Poor boys gets no items. 
                var currentItem = character.data.currentItem;

                if (IsHoldingSomethingTemporary(character) && currentItem)
                {
                    character.player.EmptySlot(character.refs.items.currentSelectedSlot);
                    PhotonNetwork.Destroy(character.data.currentItem.gameObject);
                    itemRemoved++;
                }

                // Backpack slot
                if (!character.player.backpackSlot.IsEmpty())
                {
                    if (IsHoldingBackpack(character) && currentItem)
                    {
                        // Currently holding the backpack
                        if (currentItem is Backpack b &&
                            b.data.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData bd))
                        {
                            itemRemoved += bd.FilledSlotCount();
                            itemRemoved++;
                            character.player.EmptySlot(Optionable<byte>.Some(character.player.backpackSlot.itemSlotID));
                            PhotonNetwork.Destroy(character.data.currentItem.gameObject);
                        }
                    }
                    else if (character.player.backpackSlot.hasBackpack)
                    {
                        // Currenty wearking the backpack
                        if (character.player.backpackSlot.data.TryGetDataEntry(DataEntryKey.BackpackData,
                                out BackpackData bd))
                        {
                            itemRemoved += bd.FilledSlotCount();
                            character.player.EmptySlot(Optionable<byte>.Some(character.player.backpackSlot.itemSlotID));
                            itemRemoved++;
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

                    if (IsHoldingOutSlot(character, s) && currentItem)
                    {
                        character.player.EmptySlot(character.refs.items.currentSelectedSlot);
                        PhotonNetwork.Destroy(character.data.currentItem.gameObject);
                        itemRemoved++;
                    }
                    else
                    {
                        character.player.EmptySlot(Optionable<byte>.Some((byte) index));
                        itemRemoved++;
                    }
                }

                character.view.RPC(nameof(Player.SyncInventoryRPC), RpcTarget.Others,
                    IBinarySerializable.ToManagedArray(
                        new InventorySyncData(character.player.itemSlots, character.player.backpackSlot,
                            character.player.tempFullSlot)), false);

                GreenDemonChallenge.Log.LogInfo($"Removed {itemRemoved} item(s).");

                break;
            }

            case GreenDemonCaughtEffects.NO_FLARE:
            {
                var flareRemoved = 0;
                // You forgor the flare at shore 
                var currentItem = character.data.currentItem;

                if (IsHoldingSomethingTemporary(character) && currentItem)
                {
                    if (currentItem.TryGetComponent<Flare>(out _))
                    {
                        character.player.EmptySlot(character.refs.items.currentSelectedSlot);
                        flareRemoved++;
                    }
                    else
                    {
                        if (UnnamedCompatibilityHandler.Enabled)
                        {
                            flareRemoved += UnnamedCompatibilityHandler.RemoveFlairsFormGarbageBags(
                                character.player.tempFullSlot.prefab, currentItem.data);
                        }
                    }
                }


                // Backpack slot
                if (!character.player.backpackSlot.IsEmpty())
                {
                    if (IsHoldingBackpack(character) && currentItem)
                    {
                        // Currently holding the backpack
                        if (currentItem is Backpack b && b.data.TryGetDataEntry(DataEntryKey.BackpackData,
                                out BackpackData bd) && b.TryGetComponent(out BackpackVisuals v))
                        {
                            flareRemoved += RemoveFlairFromBackpack(bd, v);
                        }
                    }
                    else if (character.player.backpackSlot.hasBackpack)
                    {
                        // Currenty wearking the backpack
                        if (character.refs.backpackTransform.GetComponentInChildren<BackpackVisuals>(true) is var bv &&
                            character.player.backpackSlot.data.TryGetDataEntry(DataEntryKey.BackpackData,
                                out BackpackData bd))
                        {
                            flareRemoved += RemoveFlairFromBackpack(bd, bv);
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

                    if (UnnamedCompatibilityHandler.Enabled && UnnamedCompatibilityHandler.IsGarbageBag(s.prefab))
                    {
                        flareRemoved += UnnamedCompatibilityHandler.RemoveFlairsFormGarbageBags(s.prefab, s.data);
                    }
                    else
                    {
                        if (IsHoldingOutSlot(character, s) && currentItem)
                        {
                            if (currentItem.TryGetComponent<Flare>(out _))
                            {
                                character.player.EmptySlot(character.refs.items.currentSelectedSlot);
                                flareRemoved++;
                            }
                        }
                        else
                        {
                            if (s.prefab.TryGetComponent<Flare>(out _))
                            {
                                character.player.EmptySlot(Optionable<byte>.Some((byte) index));

                                flareRemoved++;
                            }
                        }
                    }
                }

                character.view.RPC(nameof(Player.SyncInventoryRPC), RpcTarget.Others,
                    IBinarySerializable.ToManagedArray(
                        new InventorySyncData(character.player.itemSlots, character.player.backpackSlot,
                            character.player.tempFullSlot)), false);

                GreenDemonChallenge.Log.LogInfo($"Removed {flareRemoved} flare(s).");

                if (flareRemoved > 0 && MapHandler.CurrentMapSegment.biome == Biome.BiomeType.Shore)
                {
                    // Replace the flares where they should be

                    var itemSpawners = MapHandler.CurrentMapSegment.segmentParent
                        .GetComponentsInChildren<SingleItemSpawner>(true);

                    foreach (var spawner in itemSpawners)
                    {
                        if (spawner.prefab.TryGetComponent<Flare>(out _))
                        {
                            GreenDemonHandler.Instance.photonView.RPC(nameof(GreenDemonHandler.RPC_RespawnFlares),
                                RpcTarget.All,
                                spawner.transform.position, flareRemoved);
                        }
                    }
                }

                break;
            }


            case GreenDemonCaughtEffects.COOKED:
                // Poor boys gets no items.
            {
                var cookedItems = 0;

                var currentItem = character.data.currentItem;

                if (IsHoldingSomethingTemporary(character))
                {
                    if (UnnamedCompatibilityHandler.Enabled && UnnamedCompatibilityHandler.IsGarbageBag(character.player
                            .tempFullSlot.prefab))
                    {
                        cookedItems += UnnamedCompatibilityHandler.CookGarbageBags(character.player.tempFullSlot.prefab,
                            character.player.tempFullSlot.data);
                    }
                    else
                    {
                        currentItem.cooking.FinishCooking();
                        cookedItems++;
                    }
                }

                // Backpack slot
                if (!character.player.backpackSlot.IsEmpty())
                {
                    if (IsHoldingBackpack(character) && currentItem)
                    {
                        // Currently holding the backpack
                        if (currentItem is Backpack b && b.data.TryGetDataEntry(DataEntryKey.BackpackData,
                                out BackpackData bd) && b.TryGetComponent(out BackpackVisuals v))
                        {
                            cookedItems += CookItemFromBackpack(bd, v);
                        }

                        currentItem.cooking.FinishCooking();
                    }
                    else if (character.player.backpackSlot.hasBackpack)
                    {
                        // Currenty wearking the backpack
                        if (character.refs.backpackTransform.GetComponentInChildren<BackpackVisuals>(true) is var bv &&
                            character.player.backpackSlot.data.TryGetDataEntry(DataEntryKey.BackpackData,
                                out BackpackData bd))
                        {
                            cookedItems += CookItemFromBackpack(bd, bv);
                        }

                        if (character.player.backpackSlot.data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount,
                                out var cooked))
                        {
                            ++cooked.Value;
                            cookedItems++;

                            if (bv is BackpackOnBackVisuals onBack)
                            {
                                onBack.RefreshCooking();
                            }
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

                    if (UnnamedCompatibilityHandler.Enabled && UnnamedCompatibilityHandler.IsGarbageBag(s.prefab))
                    {
                        cookedItems += UnnamedCompatibilityHandler.CookGarbageBags(s.prefab, s.data);
                    }
                    else
                    {
                        if (IsHoldingOutSlot(character, s) && currentItem)
                        {
                            currentItem.cooking.FinishCooking();
                            cookedItems++;
                        }
                        else
                        {
                            if (s.data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var cookData))
                            {
                                ++cookData.Value;
                            }

                            cookedItems++;
                        }
                    }

                    if (s.prefab.cooking &&
                        (s.prefab.cooking.hasExplosion &&
                         s.prefab.cooking.additionalCookingBehaviors.Any(cb => cb is CookingBehavior_Explode)) || (
                            (UnnamedCompatibilityHandler.Enabled && UnnamedCompatibilityHandler.IsGarbageBag(s.prefab))
                        )
                       )
                    {
                        GreenDemonChallenge.Log.LogInfo(
                            $"{s.prefab.gameObject} has special interaction when being cooked! Automatically droping cooked {s.prefab.gameObject}...");
                        character.photonView.RPC(nameof(CharacterItems.DropItemFromSlotRPC), RpcTarget.All,
                            s.itemSlotID,
                            character.Center + character.data.lookDirection);
                    }
                }

                character.view.RPC(nameof(Player.SyncInventoryRPC), RpcTarget.Others,
                    IBinarySerializable.ToManagedArray(
                        new InventorySyncData(character.player.itemSlots, character.player.backpackSlot,
                            character.player.tempFullSlot)), false);

                GreenDemonChallenge.Log.LogInfo($"Cooked {cookedItems} item(s).");

                break;
            }

            case GreenDemonCaughtEffects.EPPY:
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 1f);
                break;

            case GreenDemonCaughtEffects.POISON_CLOUD:
                GreenDemonHandler.Instance.photonView.RPC(nameof(GreenDemonHandler.RPC_SpawnPoisonCloud), RpcTarget.All,
                    character.Center);
                break;

            case GreenDemonCaughtEffects.ICE_CLOUD:
                GreenDemonHandler.Instance.photonView.RPC(nameof(GreenDemonHandler.RPC_SpawnIceCloud), RpcTarget.All,
                    character.Center);
                break;

            case GreenDemonCaughtEffects.FIRE_CLOUD:
                GreenDemonHandler.Instance.photonView.RPC(nameof(GreenDemonHandler.RPC_SpawnFireCloud), RpcTarget.All,
                    character.Center);
                break;

            case GreenDemonCaughtEffects.SPORE_CLOUD:
                GreenDemonHandler.Instance.photonView.RPC(nameof(GreenDemonHandler.RPC_SpawnSporeCloud), RpcTarget.All,
                    character.Center);
                break;

            case GreenDemonCaughtEffects.BLINDS:
            {
                character.refs.afflictions.AddAffliction(new Affliction_Blind()
                {
                    totalTime = 60f
                });
            }
                break;
            case GreenDemonCaughtEffects.NUMBS:
            {
                character.refs.afflictions.AddAffliction(new Affliction_Numb()
                {
                    totalTime = 60f
                });
            }
                break;

            case GreenDemonCaughtEffects.BIOME_CLOUD:
                SpawnBiomeCloud(character);
                break;

            case GreenDemonCaughtEffects.DYNA_BRUH:
                ForceGiveItem(character, 106, GreenDemonCaughtEffects.HALF_INJURY);
                break;

            case GreenDemonCaughtEffects.MANDRAKE:
                ForceGiveItem(character, 155, GreenDemonCaughtEffects.EPPY);
                break;

            case GreenDemonCaughtEffects.SCORPION:
                ForceGiveItem(character, 111, GreenDemonCaughtEffects.HALF_POISON);
                break;

            case GreenDemonCaughtEffects.BAD_SHROOMBERRY:
            {
                character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Spores, 0.05f);
                character.StartCoroutine(DoRandomShroomberryEffect(character,
                    Action_RandomMushroomEffect.BadEffects.RandomElement()));
                break;
            }

            case GreenDemonCaughtEffects.EXPLODE:
            {
                var d =
                    PhotonNetwork.Instantiate("0_Items/Dynamite", character.Center, Quaternion.identity);
                var dyn = d.GetComponent<Dynamite>();
                dyn.LightFlare();
                dyn.startingFuseTime = 0f;
                // BOOM!
                break;
            }

            case GreenDemonCaughtEffects.TORNADO:
            {
                // Time to fly away! 
                var t = PhotonNetwork.Instantiate("Tornado", character.Center, Quaternion.identity);
                var tor = t.GetComponent<Tornado>();

                tor.tornadoLifetimeMax = 12f;
                tor.tornadoLifetimeMin = 20f;
                tor.force = 75f;

                tor.selectNewTargetInSeconds = 35f;

                // tornado will target the player no matter what.
                tor.targetParent = character.refs.hip.transform;
                tor.target = character.transform;

                break;
            }

            case GreenDemonCaughtEffects.GO_BACK:
            {
                // See ya!
                if (!character.TryCheckpoint())
                {
                    character.view.RPC(nameof(Character.WarpPlayerRPC), RpcTarget.All,
                        MapHandler.CurrentBaseCampSpawnPoint.position, true);
                }

                break;
            }

            case GreenDemonCaughtEffects.SPAWN_LUCKY_BLOCK:
            {
                if (LuckyBlocksCompatibilityHandler.Enabled)
                {
                    var block = PhotonNetwork.Instantiate("0_Items/legocool.LuckyBlocks:LuckyBlock",
                        character.refs.head.transform.position + (Vector3.up * 1.85f),
                        Quaternion.identity).GetComponent<Item>();

                    block.lastThrownCharacter = character;

                    var vv = ((character.refs.head.transform.position - block.transform.position).normalized * 25f) /
                             Time.deltaTime;

                    block.rig.AddForce(vv, ForceMode.Acceleration);
                }
                else
                {
                    GreenDemonChallenge.Log.LogWarning(
                        $"Can't find Lucky Blocks... Giving you a RANDOM effect instead.");
                    ApplyEffect(character, GreenDemonCaughtEffects.RANDOM);
                }

                break;
            }

            case GreenDemonCaughtEffects.UNNAMIFY:
            {
                if (UnnamedCompatibilityHandler.Enabled)
                {
                    UnnamedCompatibilityHandler.UnnamifyInventory(character);
                }
                else
                {
                    GreenDemonChallenge.Log.LogWarning(
                        $"Can't find Unnamed Products... Giving you a RANDOM effect instead.");
                    ApplyEffect(character, GreenDemonCaughtEffects.RANDOM);
                }

                break;
            }

            case GreenDemonCaughtEffects.SET_FIRE:
            {
                if (UnnamedCompatibilityHandler.Enabled)
                {
                    UnnamedCompatibilityHandler.SetCharacterOnFire(character);
                }
                else
                {
                    GreenDemonChallenge.Log.LogWarning(
                        $"Can't find Unnamed Products... Giving you a FIRE CLOUD effect instead.");
                    ApplyEffect(character, GreenDemonCaughtEffects.FIRE_CLOUD);
                }

                break;
            }

            case GreenDemonCaughtEffects.FIREBALL:
            {
                if (UnnamedCompatibilityHandler.Enabled)
                {
                    GreenDemonHandler.Instance.photonView.RPC(nameof(GreenDemonHandler.RPC_ThrowFireball), RpcTarget.MasterClient, character.view.ViewID);
                }
                else
                {
                    GreenDemonChallenge.Log.LogWarning(
                        $"Can't find Unnamed Products... Giving you a FIRE CLOUD effect instead.");
                    ApplyEffect(character, GreenDemonCaughtEffects.FIRE_CLOUD);
                }

                break;
            }

            case GreenDemonCaughtEffects.BEES:
            {
                var bees = PhotonNetwork.Instantiate("BeeSwarm", character.Head, Quaternion.identity).GetComponent<
                    BeeSwarm>();

                bees.photonView.RPC(nameof(BeeSwarm.SetBeesAngryRPC), RpcTarget.All, true);
                break;
            }

            case GreenDemonCaughtEffects.SLIP:
            {
                var peel = FindAnyObjectByType<BananaPeel>()?.gameObject ?? PhotonNetwork.InstantiateItem("Berrynana Peel Pink Variant", character.Head,
                    Quaternion.identity);

                peel.GetComponent<PhotonView>().RPC(nameof(BananaPeel.RPCA_TriggerBanana), RpcTarget.All,
                    character.view.ViewID);

                break;
            }

            case GreenDemonCaughtEffects.NO_STAM:
            {
                character.UseStamina(1.0f, false);
                GreenDemonHandler.Instance.StartCoroutine(GreenDemonHandler.KeepStamEmpty(character, 60f));
                break;
            }
            case GreenDemonCaughtEffects.W_KEY_STUCK:
            {
                GreenDemonHandler.Instance.StartCoroutine(GreenDemonHandler.StickWFor(60f));
                break;
            }
            case GreenDemonCaughtEffects.ASTORNAUT:
            {
                character.refs.afflictions.AddAffliction(new Affliction_LowGravity()
                {
                    lowGravAmount = 10,
                    warning = false,
                    totalTime = 15f
                });

                character.view.RPC(nameof(CharacterMovement.JumpRpc), RpcTarget.All, false);

                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(GreenDemonChallenge.RoomGreenDemonCaughtEffect));
        }
    }

    private void ForceGiveItem(Character character, ushort itemId, GreenDemonCaughtEffects fallback)
    {
        if (ItemDatabase.TryGetItem(itemId, out var it))
        {
            if (UnnamedCompatibilityHandler.Enabled)
            {
                UnnamedCompatibilityHandler.TryTurnIntoUnnamed(ref it);
            }

            GameUtils.instance.InstantiateAndGrab(it, character);
        }
        else
        {
            GreenDemonChallenge.Log.LogWarning(
                $"Can't find item... Falling back to {fallback}");
            ApplyEffect(character, fallback);
        }
    }

    private void SpawnBiomeCloud(Character character)
    {
        switch (MapHandler.CurrentMapSegment._biome)
        {
            case Biome.BiomeType.Shore:
                if (Ascents.isNightCold && !(Mathf.Approximately(DayNightManager.instance.isDay, 1f)))
                {
                    ApplyEffect(character, GreenDemonCaughtEffects.ICE_CLOUD);
                }
                else
                {
                    ApplyEffect(character, GreenDemonCaughtEffects.POISON_CLOUD);
                }

                break;
            case Biome.BiomeType.Tropics:
                ApplyEffect(character, GreenDemonCaughtEffects.POISON_CLOUD);
                break;
            case Biome.BiomeType.Alpine:
                ApplyEffect(character, GreenDemonCaughtEffects.ICE_CLOUD);
                break;
            case Biome.BiomeType.Volcano:
                ApplyEffect(character, GreenDemonCaughtEffects.FIRE_CLOUD);
                break;
            case Biome.BiomeType.Peak:
                ApplyEffect(character, GreenDemonCaughtEffects.FIRE_CLOUD);
                break;
            case Biome.BiomeType.Mesa:
                ApplyEffect(character, GreenDemonCaughtEffects.FIRE_CLOUD);
                break;
            case Biome.BiomeType.Roots:
                ApplyEffect(character, GreenDemonCaughtEffects.SPORE_CLOUD);
                break;
            default:
                GreenDemonChallenge.Log.LogWarning(
                    $"Unknown biome type {MapHandler.CurrentMapSegment.biome}. Defaulting to poison clouds...");
                ApplyEffect(character, GreenDemonCaughtEffects.POISON_CLOUD);
                break;
        }
    }

    private static bool IsEffectRandoValid(Character c, GreenDemonCaughtEffects eff)
    {
        return eff switch
        {
            GreenDemonCaughtEffects.RANDOM => false,
            GreenDemonCaughtEffects.BAD_SHROOMBERRY or GreenDemonCaughtEffects.FULL_POISON
                or GreenDemonCaughtEffects.HALF_POISON or GreenDemonCaughtEffects.FULL_SPORES
                or GreenDemonCaughtEffects.HALF_SPORES or GreenDemonCaughtEffects.EPPY => !c.data.isSkeleton,
            GreenDemonCaughtEffects.BEES => !c.data.isSkeleton && (!RunSettings.IsCustomRun ||
                                                                   RunSettings.GetValue(RunSettings.SETTINGTYPE
                                                                       .Hazard_Bees) >= 1),
            GreenDemonCaughtEffects.SCORPION => !c.data.isSkeleton && (!RunSettings.IsCustomRun ||
                                                                       RunSettings.GetValue(RunSettings.SETTINGTYPE
                                                                           .Hazard_Scorpions) >= 1),
            GreenDemonCaughtEffects.SET_FIRE => UnnamedCompatibilityHandler.Enabled && !c.data.isSkeleton,
            GreenDemonCaughtEffects.FIREBALL or GreenDemonCaughtEffects.UNNAMIFY => UnnamedCompatibilityHandler.Enabled,
            GreenDemonCaughtEffects.SPAWN_LUCKY_BLOCK => LuckyBlocksCompatibilityHandler.Enabled,
            GreenDemonCaughtEffects.NO_FLARE => !Ascents.shouldSpawnFlare && IsAFlareBearer(c),
            GreenDemonCaughtEffects.SCOUTMASTER => RunSettings.IsCustomRun
                ? RunSettings.GetValue(RunSettings.SETTINGTYPE.Hazard_Scoutmaster) >= 1
                : Ascents.currentAscent < 0,
            GreenDemonCaughtEffects.ZOMBIFY => Ascents.shouldSpawnZombie,
            GreenDemonCaughtEffects.TORNADO => !RunSettings.IsCustomRun ||
                                               RunSettings.GetValue(RunSettings.SETTINGTYPE.Hazard_Tornado) >= 1,
            GreenDemonCaughtEffects.MANDRAKE => !RunSettings.IsCustomRun ||
                                                RunSettings.GetValue(RunSettings.SETTINGTYPE.Hazard_Mandrake) >= 1,
            GreenDemonCaughtEffects.DYNA_BRUH => !RunSettings.IsCustomRun ||
                                                 RunSettings.GetValue(RunSettings.SETTINGTYPE.Hazard_Dynamite) >= 1,
            _ => true
        };
    }

    private static bool IsAFlareBearer(Character character)
    {
        if (!UnnamedCompatibilityHandler.Enabled)
        {
            const ushort flareItemId = 32;

            if (!character.player.backpackSlot.IsEmpty())
            {
                if (IsHoldingBackpack(character) && character.data.currentItem)
                {
                    if (character.data.currentItem is Backpack b)
                    {
                        foreach (var itemSlot in b.backpackReference.Value.Item2.GetData().itemSlots)
                        {
                            if (!itemSlot.IsEmpty() && itemSlot.prefab.itemID == flareItemId)
                                return true;
                        }
                    }
                }
                else if (character.player.backpackSlot.hasBackpack)
                {
                    if (character.refs.backpackTransform.GetComponentInChildren<BackpackVisuals>(true) is { } bbv)
                    {
                        foreach (var itemSlot in bbv.GetBackpackData().itemSlots)
                        {
                            if (!itemSlot.IsEmpty() && itemSlot.prefab.itemID == flareItemId)
                                return true;
                        }
                    }
                }
            }

            return character.player.HasInAnySlot(flareItemId);
        }
        else
        {
            return UnnamedCompatibilityHandler.HasAnyFlares(character);
        }
    }

    private IEnumerator DoRandomShroomberryEffect(Character character, int effect)
    {
        switch (effect)
        {
            case 5:
                yield return new WaitForSeconds(5f);
                GameUtils.instance.SpawnResourceAtPositionNetworked(Action_RandomMushroomEffect.RESOURCE_PATH_EXPLOSION,
                    character.Center,
                    RpcTarget.Others);
                GameUtils.instance.RPC_SpawnResourceAtPosition(
                    Action_RandomMushroomEffect.RESOURCE_PATH_EXPLOSION_NO_KNOCKBACK,
                    character.Center);
                character.AddForceToBodyPart(character.GetBodypartRig(BodypartType.Hip), Vector3.zero,
                    Vector3.up * 100f);
                break;
            case 6:
                yield return new WaitForSeconds(3f);
                character.refs.afflictions.AddAffliction(new Affliction_Blind
                {
                    totalTime = 60f
                });
                break;

            case 7:
                yield return new WaitForSeconds(3f);
                character.Fall(8f);
                break;

            case 8:
                character.refs.afflictions.AdjustStatus(CharacterAfflictions.STATUSTYPE.Spores, 0.25f);
                break;
            case 9:
                yield return new WaitForSeconds(3f);
                character.refs.afflictions.AddAffliction(new Affliction_Numb
                {
                    totalTime = 60f
                });
                break;
            default:
                character.refs.afflictions.AdjustStatus(CharacterAfflictions.STATUSTYPE.Spores, 0.25f);
                break;
        }
    }

    private void CatchPlayer(Character character)
    {
        if (!m_isConsumed)
        {
            GreenDemonChallenge.Log.LogInfo($"Catching {character}!");

            ApplyEffect(character, GreenDemonChallenge.RoomGreenDemonCaughtEffect);

            m_isConsumed = true;

            // In case there wasn't enough time to switch ownership, we'll delete it through RPCs.
            view.RPC(nameof(RPC_ConsumeDemon), RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPC_ConsumeDemon()
    {
        m_isConsumed = true;

        OnPlayerCaught?.Invoke();

        foreach (var sfx in consumedSfx)
        {
            sfx.Play(transform.position);
        }

        if (m_vfxPrefab)
        {
            Instantiate(m_vfxPrefab, transform.position, Quaternion.identity);
        }

        if (view.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    private void RPC_PlayImpactSFX(Vector3 position)
    {
        PlayImpactSound(position);
    }

    private void UpdateChase()
    {
        if (m_chaseTimeout > m_chaseUpdateCooldown)
        {
            if ((!HasTarget || !m_chasingCharacter.IsLocal) && TargetIsValid(Character.localCharacter))
            {
                var sqrMagnitude = Vector3.SqrMagnitude(Character.localCharacter.Center - Center);

                GreenDemonChallenge.Log.LogInfo("Sending request for chase target change");

                view.RPC(nameof(RequestUpdatePlayerChase), view.Owner,
                    Character.localCharacter.view.ViewID,
                    sqrMagnitude);
            }

            m_chaseTimeout = 0f;
        }

        // Rotate towards movement
        m_demonTransform.rotation = Quaternion.RotateTowards(m_demonTransform.rotation,
            Quaternion.LookRotation(m_movement), 30f * Time.deltaTime);
    }

    [PunRPC]
    public void RPC_StartShrinking()
    {
        if (!m_isShrinking)
        {
            GameUtils.instance.StartCoroutine(Shrink());
        }
    }

    [PunRPC]
    public void RPC_StopDemon(float time)
    {
        m_stopTick = time;
    }
    
    public GameObject? m_vfxPrefab;
    public GameObject? m_poofVfxPrefab;
    private Vector3 m_movement = Vector3.forward;
    private float m_minSpeedSqrDistance;
    private float m_maxSpeedSqrDistance;
    private Vector3 m_baseSpeed;
    private float m_roomSpeedMultiplier;

    private IEnumerator Shrink()
    {
        m_inActiveChase = false;
        m_isShrinking = true;

        OnShrink?.Invoke();

        // TODO: Play sound

        var currentTime = 0f;
        var originalScale = transform.localScale;
        var originalVolume = source.volume;
        var originalPitch = source.pitch;

        while (currentTime < m_shrinkDuration)
        {
            currentTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, currentTime / m_shrinkDuration);
            source.volume = Mathf.Lerp(originalVolume, 0f, currentTime / m_shrinkDuration);
            source.pitch = Mathf.Lerp(originalPitch, 0f, currentTime / m_shrinkDuration);
            yield return null;
        }

        foreach (var sfx in shrunkSfx)
        {
            sfx.Play(transform.position);
        }

        // TODO: Spawn VFX

        if (m_poofVfxPrefab)
        {
            Instantiate(m_poofVfxPrefab, transform.position, Quaternion.identity);
        }

        PhotonNetwork.Destroy(gameObject);
    }
}