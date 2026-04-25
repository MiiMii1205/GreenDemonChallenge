using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GreenDemonChallenge.Compatibility;
using GreenDemonChallenge.Data;
using Peak.Network;
using PEAKLib.Core;
using Photon.Pun;
using pworld.Scripts.Extensions;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace GreenDemonChallenge.Behaviour;

public class GreenDemonHandler : MonoBehaviourPunCallbacks
{
    public static GreenDemonHandler Instance = null!;

    private Transform m_peakEndTransform = null!;
    private Vector3 m_peakCrowStartPos;
    private Transform m_kilnBridgeTransform = null!;

    private Bounds m_tombBounds;

    private Coroutine? m_demonSpawnCoroutine;
    private Coroutine? m_peakCheckCoroutine;

    public Vector3 GroupPosition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    public bool RunHasStarted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    private IEnumerator Start()
    {
        RunHasStarted = false;
        while (!PhotonNetwork.InRoom || !Character.localCharacter || LoadingScreenHandler.loading ||
               MapHandler.Instance == null)
        {
            yield return new WaitUntil(() => MapHandler.Instance != null);
        }

        GreenDemonChallenge.Log.LogInfo("RUN REALLY STARTED!");

        StartRun();

        yield return new WaitForSeconds(2f);
        m_peakEndTransform = PeakHandler.Instance.transform.Find("Box");

        m_kilnBridgeTransform = MapHandler.Instance.segments[4].segmentParent.transform.Find("Bridge");

        m_peakCrowStartPos = new Vector3(m_kilnBridgeTransform.position.x, MountainProgressHandler.Instance.progressPoints[^1].transform.position.y,
            m_kilnBridgeTransform.position.z);

        // AddTombTriggers();
        
        ResumeSpawning();
    }

    public void ResumeSpawning(float delay = 0f)
    {
        if (m_demonSpawnCoroutine != null)
        {
            StopCoroutine(m_demonSpawnCoroutine);
        }

        m_demonSpawnCoroutine = StartCoroutine(CheckForProgression(delay));
    }

    private void AddTombTriggers()
    {
        if (MapHandler.Instance.biomes.Contains(Biome.BiomeType.Mesa) &&
            GreenDemonChallenge.RoomBiomeConfig[Biome.BiomeType.Mesa])
        {
            var timple = MapHandler.Instance.gameObject.GetComponentInChildren<DesertRockSpawner>(true);

            var o = timple.transform.GetChild(0).gameObject;

            var rockEntrance = o.GetComponentInChildren<MultipleGroundPoints>().gameObject;

            if (rockEntrance.name.Contains("3_E"))
            {
                GreenDemonChallenge.Log.LogInfo("MESA was found and Tomb is open! Setting up Tomb Triggers...");
                // Tomb is opened.
                m_isTombOpened = true;

                // Adding the Green Demon triggers

                var tombEntry = timple.transform.Find(@"Inside/Entry");
                var tombExit = timple.transform.Find(@"Inside/Exit");

                var entryTrigger = Instantiate(GreenDemonChallenge.TriggerEntryPrefab, tombEntry, false);

                var backFromEntryTrigger =
                    Instantiate(GreenDemonChallenge.TriggerEntryPrefab, rockEntrance.transform, false);

                var exitTrigger = Instantiate(GreenDemonChallenge.TriggerExitPrefab, tombExit, false);

                var backFromExitTrigger = Instantiate(GreenDemonChallenge.TriggerExitPrefab,
                    tombExit, false);

                entryTrigger.transform.SetLocalPositionAndRotation(new Vector3(0.0989999995f, 1.25f, -0.25f),
                    Quaternion.identity);
                backFromEntryTrigger.transform.SetLocalPositionAndRotation(new Vector3(0.0989999995f, 1.25f, -0.25f),
                    Quaternion.identity);
                backFromExitTrigger.transform.SetLocalPositionAndRotation(
                    new Vector3(-46.4861755f, 478.75f, 680.27002f), Quaternion.identity);
                exitTrigger.transform.SetLocalPositionAndRotation(new Vector3(-46.4861755f, 504.75f, 696.109985f),
                    Quaternion.identity);

                var entryT = ExtensionMethods.GetOrAddComponent<GreenDemonTombTrigger>(entryTrigger);
                var exitT = exitTrigger.GetComponent<GreenDemonTombTrigger>();
                var bfEntryT = backFromEntryTrigger.GetComponent<GreenDemonTombTrigger>();
                var bfExitT = backFromExitTrigger.GetComponent<GreenDemonTombTrigger>();

                entryT.greenDemonTriggerEvent.AddListener(ManageTombEnrty);
                exitT.greenDemonTriggerEvent.AddListener(ManageTombExits);
                bfEntryT.greenDemonTriggerEvent.AddListener(ManageTombExits);
                bfExitT.greenDemonTriggerEvent.AddListener(ManageTombEnrty);

                GlobalEvents.OnCharacterDied += OnCharacterDied;
                GlobalEvents.OnCharacterSpawned += OnCharacterSpawned;

                m_tombBounds.Encapsulate(timple.transform.Find(@"Inside/LightBlocker").GetComponent<Renderer>().bounds);

                var bfetRelay = backFromExitTrigger.gameObject.GetComponentInParent<TriggerRelay>();

                var rockEntrancePhotonView = rockEntrance.GetComponentInParent<PhotonView>();

                ThrowHelper.ThrowIfArgumentNull(rockEntrancePhotonView, "Rock entrance Photon View");

                var entryPhotonView = tombEntry.GetComponentInParent<PhotonView>();

                ThrowHelper.ThrowIfArgumentNull(entryPhotonView, "Tomb entrance Photon View");

                ExtensionMethods.GetOrAddComponent<GreenDemonTriggerRelay>(rockEntrancePhotonView.gameObject);
                ExtensionMethods.GetOrAddComponent<GreenDemonTriggerRelay>(entryPhotonView.gameObject);
                ExtensionMethods.GetOrAddComponent<GreenDemonTriggerRelay>(bfetRelay.gameObject);
                ExtensionMethods.GetOrAddComponent<GreenDemonTriggerRelay>(rockEntrancePhotonView.gameObject);
            }
            else
            {
                GreenDemonChallenge.Log.LogInfo("MESA was found but Tomb is CLOSED.");
            }
        }
        else
        {
            if (GreenDemonChallenge.RoomBiomeConfig[Biome.BiomeType.Mesa])
            {
                GreenDemonChallenge.Log.LogInfo("MESA green demon is disabled.");
            }
            else
            {
                GreenDemonChallenge.Log.LogInfo("No MESA were found.");
            }

            m_isTombOpened = false;
        }
    }

    private void OnCharacterSpawned(Character obj)
    {
        if (m_isTombOpened && !obj.data.dead && obj is {isBot: false, isScoutmaster: false} &&
            m_tombBounds.Contains(obj.Center))
        {
            photonView.RPC(nameof(RPC_AddPlayerToTombEntry), RpcTarget.All, obj.player.GetUserId());
        }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (m_isTombOpened && MapHandler.Instance.currentSegment == 2)
        {
            photonView.RPC(nameof(RPC_AddPlayerToTombExit), RpcTarget.All, otherPlayer.UserId);
        }
    }

    public override void OnPlayerEnteredRoom
        (Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerEnteredRoom(otherPlayer);

        if (m_isTombOpened && MapHandler.Instance.currentSegment == 2)
        {
            var c = PlayerHandler.GetPlayerCharacter(otherPlayer);

            if (c != null && !c.data.dead && c is {isBot: false, isScoutmaster: false} &&
                m_tombBounds.Contains(c.Center))
            {
                photonView.RPC(nameof(RPC_AddPlayerToTombEntry), RpcTarget.All, otherPlayer.UserId);
            }
        }
    }

    private void OnCharacterDied(Character character)
    {
        photonView.RPC(nameof(RPC_AddPlayerToTombExit), RpcTarget.All, character.player.GetUserId());
    }

    private void ManageTombExits(string userId)
    {
        photonView.RPC(nameof(RPC_AddPlayerToTombExit), RpcTarget.All, userId);
    }

    private void ManageTombEnrty(string userId)
    {
        photonView.RPC(nameof(RPC_AddPlayerToTombEntry), RpcTarget.All, userId);
    }

    [PunRPC]
    private void RPC_AddPlayerToTombExit(string userId)
    {
        if (m_playersEnteredTomb.Contains(userId))
        {
            m_playersEnteredTomb.Remove(userId);
        }
    }

    [PunRPC]
    private void RPC_AddPlayerToTombEntry(string userId)
    {
        m_playersEnteredTomb.Add(userId);
    }

    [PunRPC]
    public void RPC_ThrowFireball(int charView)
    {
        if (Character.GetCharacterWithPhotonID(charView, out var result))
        {
            UnnamedCompatibilityHandler.SpawnFireball(result);
        }
    }

    [PunRPC]
    public void RPC_RespawnFlares(Vector3 spawnPos, int amountToRespawn)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (UnnamedCompatibilityHandler.Enabled)
            {
                UnnamedCompatibilityHandler.RespawnFlares(spawnPos, amountToRespawn);
            }
            else
            {
                if (ItemDatabase.TryGetItem(32, out var it))
                {
                    for (var i = 0; i < amountToRespawn; i++)
                    {
                        PhotonNetwork.InstantiateItemRoom(it.gameObject.name, spawnPos + (Vector3.up * 0.09147437f * i),
                            Quaternion.identity);
                    }
                }
            }
        }
    }

    private HashSet<string> m_playersEnteredTomb = [];

    private bool m_isTombOpened;
    private bool m_reachedPeak = false;
    private int m_currentProgressPointIndex;
    private int m_previousProgressPoint;
    private int m_nextProgressPointIndex;

    public float CurrentCrowCompletion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    public float CurrentClimbCompletion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    public int CurrentProgressPoint
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_currentProgressPointIndex;
    }

    public bool WaitingToSpawn
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_demonSpawnCoroutine != null;
    }

    public bool IsCharacterInTomb(Character character)
    {
        // TODO: Reenable if turns out the demon spawns anyways.
        return false;
        // return !m_isTombOpened && m_playersEnteredTomb.Contains(character.player.GetUserId());
    }

    public bool ShouldChaseCharacter(Character character)
    {
        // TODO: Reenable if turns out the demon spawns anyways.
        // return !GreenDemonChallenge.RoomStopGreenDemonInTomb || !IsCharacterInTomb(character);
        return true;
    }


    [PunRPC]
    public void RPC_SpawnIceCloud(Vector3 pos)
    {
        Instantiate(GreenDemonChallenge.IceCloudPrefab, pos, Quaternion.identity);
    }


    [PunRPC]
    public void RPC_SpawnFireCloud(Vector3 pos)
    {
        Instantiate(GreenDemonChallenge.FireCloudPrefab, pos, Quaternion.identity);
    }

    [PunRPC]
    public void RPC_SpawnSporeCloud(Vector3 pos)
    {
        Instantiate(GreenDemonChallenge.SporeCloudPrefab, pos, Quaternion.identity);
    }

    [PunRPC]
    public void RPC_SpawnPoisonCloud(Vector3 pos)
    {
        Instantiate(GreenDemonChallenge.PoisonCloudPrefab, pos, Quaternion.identity);
    }

    private IEnumerator CheckForProgression(float delay = 0f)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (m_previousProgressPoint != 0)
        {
            yield return new WaitUntil(() => m_previousProgressPoint != GetCurrentProgressPointIndex());
        }

        m_currentProgressPointIndex = GetCurrentProgressPointIndex();
        m_nextProgressPointIndex = GetNextProgressPointIndex(m_currentProgressPointIndex);

        GreenDemonChallenge.Log.LogInfo(
            $"NEW CURRENT PROGRESS INDEX: {m_currentProgressPointIndex}. NEXT PROGRESS INDEX: {m_nextProgressPointIndex}");

        while (!HasProgressedForSpawn())
        {
            yield return new WaitForSecondsRealtime(5f);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            switch (GreenDemonChallenge.RoomGreenDemonMode)
            {
                case GreenDemonModes.NORMAL:
                    SpawnGreenDemon(GreenDemonChallenge.RoomGreenDemonAmount);
                    break;

                case GreenDemonModes.HARD:
                {
                    var currentDemonAmount = GreenDemon.AllDemons.Count;
                    var toSpawn = Mathf.Clamp(GreenDemonChallenge.RoomGreenDemonAmount - currentDemonAmount, 0,
                        GreenDemonChallenge.RoomGreenDemonAmount);

                    if (toSpawn > 0)
                    {
                        SpawnGreenDemon(toSpawn);
                    }

                    break;
                }

                case GreenDemonModes.VERY_HARD:
                    SpawnGreenDemon(GreenDemonChallenge.RoomGreenDemonAmount);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        if (m_currentProgressPointIndex == 4)
        {
            if (m_peakCheckCoroutine != null)
            {
                StopCoroutine(m_peakCheckCoroutine);
            }
            m_peakCheckCoroutine = StartCoroutine(CheckForPeak());
        }

        m_previousProgressPoint = m_currentProgressPointIndex;
    }

    private IEnumerator CheckForPeak()
    {
        while (!m_reachedPeak)
        {
            yield return new WaitForSecondsRealtime(5f);
            
            if (PhotonNetwork.IsMasterClient)
            {
                UpdateGroupAverage();
                
                if (MountainProgressHandler.Instance.IsAtPeak(GroupPosition) && !m_reachedPeak)
                {
                    m_reachedPeak = true;
                    ShrinkAllDemons();

                    ResumeSpawning(1.5f);
                }
            }

        }
    }

    private void UpdateGroupAverage()
    {
        var groupCenterAverage = Vector3.zero;
        var totalPlayers = 0;
        var allPlayerCharacters = PlayerHandler.GetAllPlayerCharacters();

        for (var i = 0; i < allPlayerCharacters.Count; i++)
        {
            var playerCharacter = allPlayerCharacters[i];

            if (playerCharacter && !playerCharacter.data.dead && !playerCharacter.IsGhost)
            {
                groupCenterAverage += playerCharacter.Center;
                totalPlayers++;
            }
        }

        if (totalPlayers != 0)
        {
            groupCenterAverage /= totalPlayers;
            GroupPosition = groupCenterAverage;
        }
        else
        {
            GreenDemonChallenge.Log.LogError($"No alive players were found.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNextProgressPointIndex(int currentProgress)
    {
        var mh = MapHandler.Instance;
        var mph = MountainProgressHandler.Instance;

        return currentProgress switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 4,
            4 => 5,
            5 => 5,
            _ => Mathf.Clamp(currentProgress + 1, 0, mh.biomes.Count - 1)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCurrentProgressPointIndex()
    {
        var mh = MapHandler.Instance;
        var mph = MountainProgressHandler.Instance;

        var currentSegment = mh.currentSegment;

        return currentSegment switch
        {
            0 => 0,
            1 => 1,
            2 => 2,
            3 => 3,
            4 => mph.IsAtPeak(GroupPosition) ? 5 : 4,
            _ => mph.maxProgressPointReached
        };
    }

    private bool HasProgressedForSpawn()
    {
        var mph = MountainProgressHandler.Instance;
        var mh = MapHandler.Instance;

        var currentSegment = mh.currentSegment;
        var currentSegmentS = MapHandler.Instance.segments[MapHandler.Instance.currentSegment];

        if (currentSegment is >= 0 and <= 3)
        {
            if (!GreenDemonChallenge.RoomBiomeConfig[mh.biomes[currentSegment]])
            {
                // No spawning for this biome.
                return false;
            }
        }

        var lastProgressPoint = mph.progressPoints[m_currentProgressPointIndex];

        // find the next progress point
        var nextProgressPoint = mph.progressPoints[m_nextProgressPointIndex];

        var segmentEndPosition = nextProgressPoint.transform.position;

        var segmentStartPosition = lastProgressPoint.transform.position;

        var crowSegmentStartPosition = currentSegmentS.reconnectSpawnPos.position;

        var campfire = currentSegmentS.segmentCampfire;
        // If there's No firecamp, wa are at peak, so we'll have to use something else...
        var crowSegmentEndPosition =
            !campfire ? m_peakEndTransform.transform.position : campfire.transform.position;
        
        switch (mh.GetCurrentSegment())
        {
            case Segment.Caldera:
                // Caldera's campfire transform is at 0 so no bueno.
                crowSegmentEndPosition = campfire!.transform.GetChild(0).position;
                break;
            case Segment.TheKiln:
                // In THE KILN, we need to calculate progression with the PEAK segment 
                crowSegmentEndPosition = m_peakCrowStartPos;
                crowSegmentStartPosition = m_kilnBridgeTransform.position;

                crowSegmentStartPosition.y = currentSegmentS.reconnectSpawnPos.position.y;

                if (m_reachedPeak)
                {
                    crowSegmentEndPosition = m_peakEndTransform.position;
                    crowSegmentStartPosition = m_peakCrowStartPos;

                    segmentStartPosition = m_peakCrowStartPos;
                    segmentEndPosition = m_peakEndTransform.position;
                }
                
                break;
            
            case Segment.Peak:
                // We are at peak. Use the flare box as your end position.
                crowSegmentEndPosition = m_peakEndTransform.position;
                crowSegmentStartPosition = m_peakCrowStartPos;
                
                segmentStartPosition = m_peakCrowStartPos;
                segmentEndPosition = m_peakEndTransform.position;
                
                break;
        }
        
        var distanceFromPlayersToEnd = 0f;
        var distanceFromStartToEnd = Vector3.SqrMagnitude(crowSegmentEndPosition - crowSegmentStartPosition);
        var groupCenterAverage = Vector3.zero;
        var totalPlayers = 0;

        var allPlayerCharacters = PlayerHandler.GetAllPlayerCharacters();

        for (var i = 0; i < allPlayerCharacters.Count; i++)
        {
            var playerCharacter = allPlayerCharacters[i];

            if (playerCharacter && !playerCharacter.data.dead && !playerCharacter.IsGhost)
            {
                distanceFromPlayersToEnd += Vector3.SqrMagnitude(crowSegmentEndPosition - playerCharacter.Center);
                groupCenterAverage += playerCharacter.Center;
                totalPlayers++;
            }
        }

        if (totalPlayers != 0)
        {
            distanceFromPlayersToEnd /= totalPlayers;
            groupCenterAverage /= totalPlayers;

            GroupPosition = groupCenterAverage;
        }
        else
        {
            GreenDemonChallenge.Log.LogError($"No alive players were found.");
            return false;
        }

        // When segment is 4, we're at THE KILN or at PEAK   
        if (currentSegment == 4)
        {
            if (mph.IsAtPeak(groupCenterAverage))
            {
                if (!GreenDemonChallenge.RoomBiomeConfig[Biome.BiomeType.Peak])
                {
                    return false;
                }
            }
            else if (!GreenDemonChallenge.RoomEnableGreenDemonInTheKiln)
            {
                // No Kiln spawning
                return false;
            }
        }

        CurrentCrowCompletion = Mathf.Lerp(1, 0, distanceFromPlayersToEnd / distanceFromStartToEnd);

        CurrentClimbCompletion = Util.RangeLerp(0f,
            1f,
            segmentStartPosition.y,
            segmentEndPosition.y,
            groupCenterAverage.y
        );

        return mh.GetCurrentSegment() switch
        {
            Segment.Beach =>
                CurrentCrowCompletion >= 0.5f && CurrentClimbCompletion >= 0.25f,
            Segment.Tropics =>
                // TROPICS + ROOTS: Shortest ground (except for the kiln)
                CurrentCrowCompletion >= 0.35f && CurrentClimbCompletion >= 0.25f,
            Segment.Alpine =>
                // ALPINE + MESA: longest grounds
                CurrentCrowCompletion >= 0.5f && CurrentClimbCompletion >= 0.25f,
            Segment.Caldera =>
                // Caldera is longer that it's high, so only use the crow progression
                CurrentCrowCompletion >= 0.5f,
            Segment.TheKiln =>
                // The kiln is taller, so only use the climb progression
                CurrentClimbCompletion >= 0.5f,
            Segment.Peak =>
                // we're at peak. It's longer than its height, so let's make it slightly more balanced
                CurrentCrowCompletion >= 0.5f && CurrentClimbCompletion >= 0.5f,
            _ => CurrentCrowCompletion > 0.5f && CurrentClimbCompletion >= 0.25f
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MeetsCurrentCrowCompletion()
    {
        return MapHandler.Instance.GetCurrentSegment() switch
        {
            Segment.Beach =>
                // SHORE: 2nd longest ground.
                CurrentCrowCompletion >= 0.5f,
            Segment.Tropics =>
                // TROPICS + ROOTS: Shortest ground (except for the kiln)
                CurrentCrowCompletion >= 0.35f,
            Segment.Alpine =>
                // ALPINE + MESA: longest grounds
                CurrentCrowCompletion >= 0.5f,
            Segment.Caldera =>
                // Caldera is longer that it's high, so only use the crow progression
                CurrentCrowCompletion >= 0.5f,
            Segment.TheKiln =>
                // The kiln is taller, so only use the climb progression
                true,
            Segment.Peak =>
                // we're at peak. It's longer than its height, so let's make it slightly more balanced
                CurrentCrowCompletion >= 0.5f,
            _ => CurrentCrowCompletion > 0.5f
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MeetsCurrentClimbCompletion()
    {
        return MapHandler.Instance.GetCurrentSegment() switch
        {
            Segment.Beach =>
                // SHORE: 2nd longest ground.
                CurrentClimbCompletion >= 0.25f,
            Segment.Tropics =>
                // TROPICS + ROOTS: Shortest ground (except for the kiln)
                CurrentClimbCompletion >= 0.25f,
            Segment.Alpine =>
                // ALPINE + MESA: longest grounds
                CurrentClimbCompletion >= 0.25f,
            Segment.Caldera =>
                // Caldera is longer that it's high, so only use the crow progression
                true,
            Segment.TheKiln =>
                // The kiln is taller, so only use the climb progression
                CurrentClimbCompletion >= 0.5f,
            Segment.Peak =>
                // we're at peak. It's longer than its height, so let's make it slightly more balanced
                CurrentClimbCompletion >= 0.5f,
            _ => CurrentClimbCompletion >= 0.25f
        };
    }

    public void SpawnGreenDemon(int greenDemonAmount)
    {
        var mh = MapHandler.Instance;

        var currentSegment = mh.segments[mh.currentSegment];

        var spawnPosition = currentSegment.reconnectSpawnPos.position;

        if (mh.GetCurrentSegment() == Segment.Peak)
        {
            spawnPosition = m_peakCrowStartPos;
        }
        else if (mh.GetCurrentSegment() == Segment.TheKiln)
        {
            spawnPosition = m_kilnBridgeTransform.position;
            spawnPosition.y = mh.respawnTheKiln.transform.position.y;

            // GetCurrentSegment can be peak but not 
            if (m_reachedPeak)
            {
                spawnPosition = m_peakCrowStartPos;
            }
        }
        
        for (var i = 0; i < greenDemonAmount; i++)
        {
            NetworkPrefabManager.SpawnNetworkPrefab(GreenDemonChallenge.GreenDemonPrefab.name,
                spawnPosition + (Vector3.up * 10f) + (Vector3.right * i),
                Quaternion.identity);
        }
    }

    [PunRPC]
    public void RPC_SpawnGreenDemonInstance(Vector3 position)
    {
        SpawnGreenDemon(position);
    }

    public void SpawnGreenDemon(Vector3 position)
    {
        NetworkPrefabManager.SpawnNetworkPrefab(GreenDemonChallenge.GreenDemonPrefab.name,
            position + (Vector3.up * 10f), Quaternion.identity);
    }

    public void StartRun()
    {
        RunHasStarted = true;
        Instance = this;
    }

    public void ShrinkAllDemons()
    {
        GreenDemonChallenge.Log.LogInfo($"Shrinking {GreenDemon.AllDemons.Count} Green Demons.");
        foreach (var greenDemon in GreenDemon.AllDemons)
        {
            greenDemon.photonView.RPC(nameof(GreenDemon.RPC_StartShrinking), RpcTarget.All);
        }
    }

    private static bool WKeyIsStuck = false;

    public static bool IsWKeyStuck
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => WKeyIsStuck;
    }

    public void StopAllDemon(float time)
    {
        GreenDemonChallenge.Log.LogInfo($"Stopping {GreenDemon.AllDemons.Count} Green Demons for {time} seconds.");
        foreach (var greenDemon in GreenDemon.AllDemons)
        {
            greenDemon.photonView.RPC(nameof(GreenDemon.RPC_StopDemon), RpcTarget.All, time);
        }
    }

    public static IEnumerator KeepStamEmpty(Character c, float time)
    {
        var t = 0f;
        while (t < time)
        {
            c.UseStamina(1f, false);
            t += Time.deltaTime;
            yield return null;
        }
    }

    public static IEnumerator StickWFor(float f)
    {
        WKeyIsStuck = true;
        yield return new WaitForSeconds(f);
        WKeyIsStuck = false;
    }
}