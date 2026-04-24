using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GreenDemonChallenge.Behaviour;
using GreenDemonChallenge.Behaviour.GUI;
using GreenDemonChallenge.Compatibility;
using GreenDemonChallenge.Data;
using GreenDemonChallenge.Patchers;
using HarmonyLib;
using PEAKLib.Core;
using PEAKLib.Core.Extensions;
using Photon.Pun;
using PhotonCustomPropsUtils;
using TMPro;
using UnityEngine;

using Zorro.Core.CLI;
using pworld.Scripts.Extensions;



namespace GreenDemonChallenge;

[BepInAutoPlugin]
[BepInDependency(CorePlugin.Id)]
[BepInDependency("com.snosz.photoncustompropsutils")]
[BepInDependency("legocool.LuckyBlocks", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.github.MiiMii1205.UnnamedProducts", BepInDependency.DependencyFlags.SoftDependency)]
public partial class GreenDemonChallenge : BaseUnityPlugin
{
    public static GameObject GreenDemonPrefab { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;
    public static PhotonScopedManager? Manager { get; private set; }
    public static ConfigEntry<GreenDemonCaughtEffects> GreenDemonCaughtEffect { get; private set; } = null!;
    public static ConfigEntry<GreenDemonTrackerSettings> GreenDemonTrackerSetting { get; private set; } = null!;
    
    public static ConfigEntry<float> GreenDemonVolume { get; private set; } = null!;

    public static GreenDemonCaughtEffects RoomGreenDemonCaughtEffect = GreenDemonCaughtEffects.KILL;
    public static Dictionary<Biome.BiomeType, ConfigEntry<bool>> BiomeConfig { get; private set; } = null!;
    
    public static Dictionary<Biome.BiomeType, bool> RoomBiomeConfig = null!;
    public static GameObject TriggerEntryPrefab { get; set; } = null!;
    public static GameObject TriggerExitPrefab { get; set; } = null!;
    public static GameObject TheDemonIsHerePrefab { get; set; } = null!;
    public static GameObject TrackerPrefab { get; set; } = null!;
    public static GameObject PoisonCloudPrefab { get; set; } = null!;
    public static GameObject SporeCloudPrefab { get; set; } = null!;
    public static GameObject IceCloudPrefab { get; set; } = null!;
    public static GameObject FireCloudPrefab { get; set; } = null!;
    
    private static TMP_FontAsset? _darumaFontAsset;

    public static TMP_FontAsset DarumaDropOne
    {
        get
        {
            if (_darumaFontAsset == null)
            {
                var assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                _darumaFontAsset = assets.FirstOrDefault(fontAsset =>
                    fontAsset.faceInfo.familyName == "Daruma Drop One"
                );

                Log.LogInfo("Daruma Drop One font found!");
            }

            return _darumaFontAsset!;
        }
    }
    
    private static Material? _darumaShadowMaterial;

    public static Material DarumaDropOneFogShadowMaterial
    {
        get
        {
            if (_darumaShadowMaterial == null)
            {
                _darumaShadowMaterial = ThrowHelper.ThrowIfArgumentNull(Instantiate(DarumaDropOne.material));

                _darumaShadowMaterial.EnableKeyword("UNDERLAY_ON");
                _darumaShadowMaterial.SetFloat(UnderlayDilate, 1f);
                _darumaShadowMaterial.SetFloat(UnderlayOffsetY, -0.7f);
                _darumaShadowMaterial.SetFloat(UnderlaySoftness, 1f);
                _darumaShadowMaterial.SetColor(UnderlayColor, new Color(0f, 0f, 0f, 0.1960784f));

                Log.LogInfo("Shadow material for Green Demon Challenge was successfully generated!");
            }

            return _darumaShadowMaterial!;
        }
    }


    private void Awake()
    {
        Log = Logger;
        Manager = PhotonCustomPropsUtilsPlugin.GetManager(Id);

        var genConfigDescr = new ConfigDescription("What happens to players caught by Green Demons",
            new AcceptableEnumList<GreenDemonCaughtEffects>(GetValidEffects()));
        
        GreenDemonCaughtEffect = Config.Bind("General", "Green Demon Caught Effect", GreenDemonCaughtEffects.KILL,
            genConfigDescr);
        
        GreenDemonVolume = Config.Bind("Gameplay", "Green Demon Volume", 1f, new ConfigDescription("The volume of the Green Demon Music", new AcceptableValueRange<float>(0f, 2f)));

        GreenDemonTrackerSetting = Config.Bind("Gameplay", "Green Demon Tracker Mode",
            GreenDemonTrackerSettings.OFFSCREEN, "When on-screen trackers should appear.");
            
        GreenDemonAmount = Config.Bind("General", "Green Demon amount", 1,
            new ConfigDescription("The amount of Green Demons that spawns in.", new AcceptableValueRange<int>(1, 10)));

        GreenDemonSpeed = Config.Bind("General", "Green Demon Speed", GreenDemonSpeeds.MEDIUM,
            "How fast green demons goes.");

        GreenDemonDelay = Config.Bind("General", "Green Demon Delay", 1f,
            "How long the demon has to wait before starting to chase players after spawning.");

        GreenDemonMode = Config.Bind("General", "Green Demon Mode", GreenDemonModes.NORMAL,
            "The current Green Demon Challenge mode. 'NORMAL' despawns all active demons after starting a campfire, 'HARD' stops them momentarily but max out demon count to \"Green Demon amount\", 'VERY_HARD' is 'HARD' but demon keeps spawning .");

        GreenDemonCaughtEffect.SettingChanged += UpdateGreenDemonConfig;
        GreenDemonAmount.SettingChanged += UpdateGreenDemonConfig;
        GreenDemonSpeed.SettingChanged += UpdateGreenDemonConfig;
        GreenDemonMode.SettingChanged += UpdateGreenDemonConfig;

        GreenDemonVolume.SettingChanged += UpdateDemonVolumes;
        
        GreenDemonTrackerSetting.SettingChanged += UpdateTrackersSettings;
        
        BiomeConfig = new Dictionary<Biome.BiomeType, ConfigEntry<bool>>();
        RoomBiomeConfig = new Dictionary<Biome.BiomeType, bool>();

        
        foreach (var enumValue in typeof(Biome.BiomeType).GetEnumValues())
        {
            if ((int) enumValue > 7)
            {
                continue;
            }

            var biomeName = typeof(Biome.BiomeType).GetEnumName((Biome.BiomeType) enumValue)?.Replace("Volcano", "Caldera");

            var configEntry = Config.Bind(
                "Biomes",
                $"Enable {biomeName}",
                true,
                $"Enables/disables Green Demons for {biomeName}"
            );


            configEntry.SettingChanged += UpdateGreenDemonConfig;
            
            BiomeConfig.Add((Biome.BiomeType) enumValue,  configEntry );
            
            RoomBiomeConfig.Add((Biome.BiomeType) enumValue, configEntry.Value);
        }

        EnableGreenDemonInTheKiln = Config.Bind("Biomes", "Enable The Kiln", true,
            "Enables/Disables the Green Demon for The Kiln");
        
        // StopGreenDemonInTomb =
        //     Config.Bind("Biomes", "Stop in the Tomb", true, "Stops the Green Demon while in the Tomb");

        EnableGreenDemonInTheKiln.SettingChanged += UpdateGreenDemonConfig;
        // StopGreenDemonInTomb.SettingChanged += UpdateGreenDemonConfig;
        
        
        GreenDemonDelay.SettingChanged += UpdateGreenDemonConfig;

        RoomGreenDemonAmount = GreenDemonAmount.Value;
        RoomGreenDemonSpeed = GreenDemonSpeed.Value;
        RoomGreenDemonCaughtEffect = GreenDemonCaughtEffect.Value;
        RoomEnableGreenDemonInTheKiln = EnableGreenDemonInTheKiln.Value;
        // RoomStopGreenDemonInTomb = StopGreenDemonInTomb.Value;
        RoomGreenDemonDelay = GreenDemonDelay.Value;
        
        RoomGreenDemonMode = GreenDemonMode.Value;
        
        this.LoadBundleAndContentsWithName("greendemon.peakbundle", bundle =>
        {
            GreenDemonPrefab = bundle.LoadAsset<GameObject>("GreenDemon.prefab");
            TriggerEntryPrefab = bundle.LoadAsset<GameObject>("GreenDemonEntry.prefab");
            TriggerExitPrefab = bundle.LoadAsset<GameObject>("GreenDemonExit.prefab");

            TheDemonIsHerePrefab = bundle.LoadAsset<GameObject>("TheDemonIsHere.prefab");
            TrackerPrefab = bundle.LoadAsset<GameObject>("GreenDemonTracker.prefab");

            TrackerPrefab.GetComponentInChildren<CanvasGroup>(true).alpha = 0;
            
            TriggerEntryPrefab.GetOrAddComponent<GreenDemonTombTrigger>();
            TriggerExitPrefab.GetOrAddComponent<GreenDemonTombTrigger>();

            PoisonCloudPrefab = bundle.LoadAsset<GameObject>("PoisonCloud.prefab");
            SporeCloudPrefab = bundle.LoadAsset<GameObject>("SporeCloud.prefab");
            IceCloudPrefab = bundle.LoadAsset<GameObject>("IceCloud.prefab");
            FireCloudPrefab = bundle.LoadAsset<GameObject>("FireCloud.prefab");

            var consumeVFX = bundle.LoadAsset<GameObject>("GreenDemonConsumeVFX.prefab");
            var poofVFX = bundle.LoadAsset<GameObject>("GreenDemonPoofVFX.prefab");
            
            ShaderExtensions.ReplaceShaders(consumeVFX);
            ShaderExtensions.ReplaceShaders(poofVFX);
            
            ReplaceShaders([
                "W/Peak_Standard",
                "W/Character",
                "W/Peak_Transparent",
                "W/Peak_Glass",
                "W/Peak_Clip",
                "W/Peak_glass_liquid",
                "W/Peak_GroundTransition",
                "W/Peak_Guidebook",
                "W/Peak_Honey",
                "W/Peak_Ice",
                "W/Peak_Rock",
                "W/Peak_Rope",
                "W/Peak_Splash",
                "W/Peak_Waterfall",
                "GD/FireParticle",
                "SmokeParticle"
            ], consumeVFX);
            
            ReplaceShaders([
                "W/Peak_Standard",
                "W/Character",
                "W/Peak_Transparent",
                "W/Peak_Glass",
                "W/Peak_Clip",
                "W/Peak_glass_liquid",
                "W/Peak_GroundTransition",
                "W/Peak_Guidebook",
                "W/Peak_Honey",
                "W/Peak_Ice",
                "W/Peak_Rock",
                "W/Peak_Rope",
                "W/Peak_Splash",
                "W/Peak_Waterfall",
                "GD/FireParticle",
                "SmokeParticle"
            ], poofVFX);
            
            var cdae = consumeVFX.GetOrAddComponent<DestroyAfterEmit>();
            cdae.m_particle = consumeVFX.GetComponent<ParticleSystem>();
            
            var pdae = poofVFX.GetOrAddComponent<DestroyAfterEmit>();
            pdae.m_particle = poofVFX.GetComponent<ParticleSystem>();
            
            var gdc = GreenDemonPrefab.GetOrAddComponent<GreenDemon>();
            gdc.mainRenderer = GreenDemonPrefab.GetComponentInChildren<Renderer>();

            gdc.m_demonTransform = gdc.mainRenderer.transform.parent;
            
            if (ItemDatabase.TryGetItem(158, out var shrb) && ItemDatabase.TryGetItem(13, out var bingb))
            {
                var shroomImpt = shrb.GetComponent<ItemImpactSFX>();
                var bingbImpt = bingb.GetComponent<ItemImpactSFX>();

                gdc.m_impact = gdc.m_impact.AddRangeToArray(shroomImpt.impact);

                foreach (var sfxInstance in bingbImpt.impact)
                {
                    if (sfxInstance.name == "SFXI BingBong 2")
                    {
                        gdc.m_impact = gdc.m_impact.AddToArray(sfxInstance);
                    }
                }
            }
            

            var sfxPlayOneShot = GreenDemonPrefab.GetComponent<SFX_PlayOneShot>();
            var sfxiLists = sfxPlayOneShot.sfxs;
            
            foreach (var sfx in sfxiLists)
            {
                if (sfx.name == "SFXI Heal Hunger Stamina")
                {
                    gdc.consumedSfx = gdc.consumedSfx.AddToArray(sfx);
                }
            }

            gdc.shrunkSfx = gdc.shrunkSfx.AddToArray(bundle.LoadAsset<SFX_Instance>("SFXI Demon_Despawn.asset"));
            gdc.consumedSfx = gdc.consumedSfx.AddToArray(bundle.LoadAsset<SFX_Instance>("SFXI Demon_Consume.asset"))
                .AddToArray(bundle.LoadAsset<SFX_Instance>("SFXI Demon_Consume_Jingle.asset"));
            
            gdc.m_vfxPrefab = consumeVFX;
            gdc.m_poofVfxPrefab = poofVFX;
            
            Destroy(sfxPlayOneShot);
            
            gdc.source = GreenDemonPrefab.GetComponent<AudioSource>();
            
            ReplaceShaders([
                "W/Peak_Standard",
                "W/Character",
                "W/Peak_Transparent",
                "W/Peak_Glass",
                "W/Peak_Clip",
                "W/Peak_glass_liquid",
                "W/Peak_GroundTransition",
                "W/Peak_Guidebook",
                "W/Peak_Honey",
                "W/Peak_Ice",
                "W/Peak_Rock",
                "W/Peak_Rope",
                "W/Peak_Splash",
                "W/Peak_Waterfall",
                "GD/FireParticle",
                "W/Vine"
            ], GreenDemonPrefab);
            
            
            ReplaceShaders([
                "Scouts/UI",
                "TextMeshPro/Distance Field"
            ], TheDemonIsHerePrefab);

            ReplaceShaders([
                "Scouts/UI",
                "TextMeshPro/Distance Field"
            ], TrackerPrefab);

            
            ShaderExtensions.ReplaceShaders(TheDemonIsHerePrefab);
            ShaderExtensions.ReplaceShaders(TrackerPrefab);
            
            ShaderExtensions.ReplaceShaders(GreenDemonPrefab);
            ShaderExtensions.ReplaceShaders(PoisonCloudPrefab);
            
            ShaderExtensions.ReplaceShaders(SporeCloudPrefab);
            ShaderExtensions.ReplaceShaders(IceCloudPrefab);
            ShaderExtensions.ReplaceShaders(FireCloudPrefab);
            
            foreach (var tmpText in TheDemonIsHerePrefab.GetComponentsInChildren<TMP_Text>())
            {
                tmpText.font = DarumaDropOne;
                tmpText.fontMaterial = DarumaDropOneFogShadowMaterial;
            }
            
            NetworkPrefabManager.TryRegisterNetworkPrefab(GreenDemonPrefab.name,
                GreenDemonPrefab);
            NetworkPrefabManager.TryRegisterNetworkPrefab(PoisonCloudPrefab.name,
                PoisonCloudPrefab);
            NetworkPrefabManager.TryRegisterNetworkPrefab(SporeCloudPrefab.name,
                SporeCloudPrefab);
            NetworkPrefabManager.TryRegisterNetworkPrefab(IceCloudPrefab.name,
                IceCloudPrefab);
            NetworkPrefabManager.TryRegisterNetworkPrefab(FireCloudPrefab.name,
                FireCloudPrefab);
            
            bundle.Mod.RegisterContent();
            
            Log.LogInfo("Green Demon bundle is loaded!");
        });


        Manager.RegisterRoomProperty<int>(nameof(RoomGreenDemonSpeed), RoomEventType.All, v =>
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Log.LogInfo($"Received {(GreenDemonSpeeds) v} as new {nameof(RoomGreenDemonSpeed)}!");
                RoomGreenDemonSpeed = (GreenDemonSpeeds) v;
            }
        });

        Manager.RegisterRoomProperty<int>(nameof(RoomGreenDemonAmount), RoomEventType.All, v =>
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Log.LogInfo($"Received {v} as new {nameof(RoomGreenDemonAmount)}!");
                RoomGreenDemonAmount = v;
            }
        });
        
        Manager.RegisterRoomProperty<bool>(nameof(RoomEnableGreenDemonInTheKiln), RoomEventType.All, v =>
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Log.LogInfo($"Received {v} as new {nameof(RoomEnableGreenDemonInTheKiln)}!");
                RoomEnableGreenDemonInTheKiln = v;
            }
        });
        //
        // Manager.RegisterRoomProperty<bool>(nameof(RoomStopGreenDemonInTomb), RoomEventType.All, v =>
        // {
        //     if (!PhotonNetwork.IsMasterClient)
        //     {
        //         Log.LogInfo($"Received {v} as new {nameof(RoomStopGreenDemonInTomb)}!");
        //         RoomStopGreenDemonInTomb = v;
        //     }
        // });

        Manager.RegisterRoomProperty<int>(nameof(RoomGreenDemonCaughtEffect), RoomEventType.All, v =>
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Log.LogInfo($"Received {(GreenDemonCaughtEffects) v} as new {nameof(RoomGreenDemonCaughtEffect)}!");
                RoomGreenDemonCaughtEffect = (GreenDemonCaughtEffects) v;
            }
        });
        Manager.RegisterRoomProperty<float>(nameof(RoomGreenDemonDelay), RoomEventType.All, v =>
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Log.LogInfo($"Received {v} as new {nameof(RoomGreenDemonDelay)}!");
                RoomGreenDemonDelay = v;
            }
        });
        
        foreach (var keyValuePair in RoomBiomeConfig)
        {
            var biomeSettingName = $"{keyValuePair.Key}_{nameof(RoomBiomeConfig)}";
            
            Manager.RegisterRoomProperty<bool>(biomeSettingName,
                RoomEventType.All,
                v =>
                {
                    if (!PhotonNetwork.IsMasterClient)
                    {
                        Log.LogInfo(
                            $"Received {v} as new {nameof(RoomBiomeConfig)}[{keyValuePair.Key}]!");
                        RoomBiomeConfig[keyValuePair.Key] = v;
                    }
                });
        }
        
        Manager.RegisterOnJoinedRoom(SetupGreenDemonSettings);
        
        AddLocalizedTextCsv();

        var harmony = new Harmony(Id);
        
        harmony.PatchAll(typeof(GreenDemonPatcher));

        if (LuckyBlocksCompatibilityHandler.Enabled)
        {
            LuckyBlocksCompatibilityHandler.PatchLuckyBlocks(harmony);
        }
        
        Log.LogInfo($"Plugin {Name} is loaded!");
        
    }

    private static void UpdateDemonVolumes(object sender, EventArgs e)
    {
        foreach (var greenDemon in GreenDemon.AllDemons)
        {
            greenDemon.RefreshVolume();
        }
    }
    private static void UpdateTrackersSettings(object sender, EventArgs e)
    {
        foreach (var tracker in GreenDemonTracker.AllTrackers)
        {
            tracker.RefreshTrackerVisibility();
        }
    }

    private GreenDemonCaughtEffects[] GetValidEffects()
    {
        List<GreenDemonCaughtEffects> effs = [];
        
        foreach (GreenDemonCaughtEffects o in Enum.GetValues(typeof(GreenDemonCaughtEffects)))
        {
            switch (o)
            {
                case GreenDemonCaughtEffects.SET_FIRE:
                case GreenDemonCaughtEffects.FIREBALL:
                case GreenDemonCaughtEffects.UNNAMIFY:
                {
                    if (UnnamedCompatibilityHandler.Enabled)
                    {
                        effs.Add(o);
                    }

                    break;
                }
                case GreenDemonCaughtEffects.SPAWN_LUCKY_BLOCK:
                {
                    if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("legocool.LuckyBlocks"))
                    {
                        effs.Add(o);
                    }

                    break;
                }
                default:
                    effs.Add(o);
                    break;
            }
        }
        
        return effs.ToArray();
    }

    private static bool IsRunStarted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => RunManager.Instance?.runStarted ?? false;
    }

    private void UpdateGreenDemonConfig(object sender, EventArgs e)
    {
        if (!IsRunStarted)
        {
            SetupGreenDemonSettings(PhotonNetwork.LocalPlayer);
        }
        else
        {
            Log.LogWarning("Run has already started. Can't change the settings mid-run...");
        }
    }
    
    private static void SetupGreenDemonSettings(Photon.Realtime.Player player)
    {
        if (!player.IsMasterClient)
        {
            Log.LogWarning($"{player} is NOT the host. Can't send our Settings.");
        }
        
        if (PhotonNetwork.InRoom && player.IsMasterClient && Manager != null)
        {
            Log.LogInfo($"Sending our current {nameof(RoomGreenDemonSpeed)} of {GreenDemonSpeed.Value}...");
            RoomGreenDemonSpeed = GreenDemonSpeed.Value;
            Manager.SetRoomProperty(nameof(RoomGreenDemonSpeed), (int) GreenDemonSpeed.Value);

            Log.LogInfo($"Sending our current {nameof(RoomGreenDemonAmount)} of {GreenDemonAmount.Value}...");
            RoomGreenDemonAmount = GreenDemonAmount.Value;
            Manager.SetRoomProperty(nameof(RoomGreenDemonAmount), GreenDemonAmount.Value);
            
            Log.LogInfo($"Sending our current {nameof(RoomEnableGreenDemonInTheKiln)} in the kiln of {EnableGreenDemonInTheKiln.Value}...");
            RoomEnableGreenDemonInTheKiln = EnableGreenDemonInTheKiln.Value;
            Manager.SetRoomProperty(nameof(RoomEnableGreenDemonInTheKiln), EnableGreenDemonInTheKiln.Value);
            
            // Log.LogInfo($"Sending our current {nameof(RoomStopGreenDemonInTomb)} of {StopGreenDemonInTomb.Value}...");
            // RoomStopGreenDemonInTomb = StopGreenDemonInTomb.Value;
            // Manager.SetRoomProperty(nameof(RoomStopGreenDemonInTomb), StopGreenDemonInTomb.Value);
            
            Log.LogInfo($"Sending our current {nameof(RoomGreenDemonCaughtEffect)} of {GreenDemonCaughtEffect.Value}...");
            RoomGreenDemonCaughtEffect = GreenDemonCaughtEffect.Value;
            Manager.SetRoomProperty(nameof(RoomGreenDemonCaughtEffect), (int) GreenDemonCaughtEffect.Value);
            
            Log.LogInfo($"Sending our current {nameof(RoomGreenDemonMode)} of {GreenDemonMode.Value}...");
            RoomGreenDemonMode = GreenDemonMode.Value;
            Manager.SetRoomProperty(nameof(RoomGreenDemonMode), (int) GreenDemonMode.Value);

            Log.LogInfo($"Sending our current {nameof(RoomGreenDemonDelay)} of {GreenDemonDelay.Value}...");
            RoomGreenDemonDelay = GreenDemonDelay.Value;
            Manager.SetRoomProperty(nameof(RoomGreenDemonDelay), GreenDemonDelay.Value);

            foreach (var conf in BiomeConfig)
            {
                var biomeSettingName = $"{conf.Key}_{nameof(RoomBiomeConfig)}";
                Log.LogInfo($"Sending our current ${nameof(RoomBiomeConfig)}[{conf.Key}] of {conf.Value.Value}...");
                RoomBiomeConfig[conf.Key] = conf.Value.Value;
                Manager.SetRoomProperty(biomeSettingName, conf.Value.Value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReplaceShaders(List<string> shaderNames, GameObject go)
    {
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
        {
            ReplaceAllShaderInRenderer(shaderNames, renderer);
        }
    }

    private void ReplaceAllShaderInRenderer(List<string> shaderNames, Renderer ren)
    {
        foreach (var shaderName in shaderNames)
        {

            var shader = Shader.Find(shaderName);

            if (shader == null)
            {
                Log.LogWarning(
                    $": Shader {shaderName} was not found."
                );
                continue;
            }

            foreach (var mat in ren.sharedMaterials)
            {

                ReplaceShader(shader, mat);
            }

            foreach (var mat in ren.materials)
            {
                ReplaceShader(shader, mat);
            }

        }

    }

    private void AddLocalizedTextCsv()
    {
        using var reader = new StreamReader(Path.Join(Path.GetDirectoryName(Info.Location),
            "GreenDemonChallengeLocalizedText.csv"));

        var currentLine = 0;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            if (line == null)
            {
                break;
            }

            currentLine++;

            List<string> valList = new List<string>(CSVReader.SplitCsvLine(line));

            var locName = valList.Deque();

            var endline = valList.Pop();

            if (endline != "ENDLINE")
            {
                Log.LogError($"Invalid localization at line {currentLine}");
            }

            if (locName != "CURRENT_LANGUAGE")
            {
                LocalizedText.mainTable[locName.ToUpper()] = valList;
                Log.LogDebug($"Added localization of {locName.ToUpper()}");
            }
        }

        Log.LogDebug($"Added {currentLine - 1} localizations");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReplaceShader(Shader shader, Material mat)
    {
        if (mat.shader.name != shader.name)
        {
            return;
        }

        mat.shader = shader;
    }

    private void OnGUI()
    {
#if DEBUG

        if (GreenDemonHandler.Instance && GreenDemonHandler.Instance.RunHasStarted)
        {
            var height = 128f + (32f) * 4f;
            GUI.Label(new Rect((Screen.width - 400f), (Screen.height - height), 400f, height),
                GreenDemonHandler.Instance.WaitingToSpawn ? $"<color=green>[{Name} v{Version}]</color>\nProgression: {MountainProgressHandler.Instance.progressPoints[GreenDemonHandler.Instance.CurrentProgressPoint].title} \nCrow Completion: <color={(GreenDemonHandler.Instance.MeetsCurrentCrowCompletion() ? "green" : "red")}>{GreenDemonHandler.Instance.CurrentCrowCompletion}</color> \nClimb Completion: <color={(GreenDemonHandler.Instance.MeetsCurrentClimbCompletion() ? "green" : "red")}>{GreenDemonHandler.Instance.CurrentClimbCompletion}</color>"  : $"<color=green>[{Name} v{Version}]</color>\nProgression: ?" );
        }
        
#endif
    }

    public static GreenDemonSpeeds RoomGreenDemonSpeed = GreenDemonSpeeds.MEDIUM;
    public static int RoomGreenDemonAmount = 1;

    public static bool RoomEnableGreenDemonInTheKiln;
    // public static bool RoomStopGreenDemonInTomb;
    public static float RoomGreenDemonDelay;
    public static GreenDemonModes RoomGreenDemonMode;
    
    private static readonly int UnderlayDilate = Shader.PropertyToID("_UnderlayDilate");
    private static readonly int UnderlayOffsetY = Shader.PropertyToID("_UnderlayOffsetY");
    private static readonly int UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");

    private static readonly int UnderlayColor = Shader.PropertyToID("_UnderlayColor");

    public static ConfigEntry<int> GreenDemonAmount { get; set; } = null!;
    public static ConfigEntry<bool> EnableGreenDemonInTheKiln { get; private set; } = null!;
    public static ConfigEntry<bool> StopGreenDemonInTomb { get; private set; } = null!;
    public static ConfigEntry<GreenDemonSpeeds> GreenDemonSpeed { get; set; } = null!;
    public static ConfigEntry<float> GreenDemonDelay { get; set; } = null!;
    public static ConfigEntry<GreenDemonModes> GreenDemonMode { get; set; } = null!;

    [ConsoleCommand]
    public static void SpawnGreenDemon()
    {
        GreenDemonHandler.Instance.SpawnGreenDemon(1);
    }
    [ConsoleCommand]
    public static void DespawnGreenDemons()
    {
        GreenDemonHandler.Instance.ShrinkAllDemons();;
    }
    [ConsoleCommand]
    public static void SpawnIceCloud()
    {
        Instantiate(GreenDemonChallenge.IceCloudPrefab, Character.localCharacter.Center, Quaternion.identity);
    }
    [ConsoleCommand]
    public static void SpawnFireCloud()
    {
        Instantiate(GreenDemonChallenge.FireCloudPrefab, Character.localCharacter.Center, Quaternion.identity);
    }
    [ConsoleCommand]
    public static void SpawnSporeCloud()
    {

        Instantiate(GreenDemonChallenge.SporeCloudPrefab, Character.localCharacter.Center, Quaternion.identity);
    }
    [ConsoleCommand]
    public static void SpawnPoisonCloud()
    {
        Instantiate(GreenDemonChallenge.PoisonCloudPrefab, Character.localCharacter.Center, Quaternion.identity);
    }
}