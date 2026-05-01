using System.Runtime.CompilerServices;
using DG.Tweening;
using GreenDemonChallenge.Behaviour.GUI;
using UnityEngine;

namespace GreenDemonChallenge.Compatibility;

public class TimeThemeCompatibilityHandler
{
    private static bool? _enabled;
    private static Color TrackerArrowColorDay = new (0.8742138f, 0.8567384f, 0.7615007f, 1f);
    private static Color TrackerArrowColorNight = new (0.6509804f, 0.3764706f, 0.7529412f, 1f);

    public static bool Enabled
    {
        get
        {
            if (_enabled == null)
            {
                _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(
                    TimeTheme.TimeThemePlugin.Id);
                GreenDemonChallenge.Log.LogInfo(
                    $"Time Theme support is {((bool) _enabled ? "enabled" : "disabled")}");
            }

            return (bool) _enabled;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init()
    {
        TimeTheme.TimeThemePlugin.OnThemeChanged += UpdateAllTrackers;
    }

    private static void UpdateAllTrackers(bool isDay)
    {
        var arrowColor = isDay ? TrackerArrowColorDay : TrackerArrowColorNight;
        
        for (int i = 0, l = GreenDemonTracker.AllTrackers.Count; i < l; i++)
        {
            var track = GreenDemonTracker.AllTrackers[i];
            
            if (track.m_group.alpha > 0)
            {
                track.m_arrowImgae.DOColor(arrowColor, 0.25f);
            }
            else
            {
                track.m_arrowImgae.color = arrowColor;
            }
            
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void InitTracker(GreenDemonTracker tracker)
    {
        var arrowColor = TimeTheme.TimeThemePlugin.IsDarkTheme ? TrackerArrowColorNight : TrackerArrowColorDay;
        
        if (tracker.m_group.alpha > 0)
        {
            tracker.m_arrowImgae.DOColor(arrowColor, 0.25f);
        }
        else
        {
            tracker.m_arrowImgae.color = arrowColor;
        }
        
        
    }
    
}