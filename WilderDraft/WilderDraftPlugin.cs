using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using MiraAPI.Modifiers;
using MiraAPI.PluginLoading;
using Reactor;
using Reactor.Utilities;
using UnityEngine;
using WilderDraft.Components;

namespace WilderDraft;

[BepInAutoPlugin("com.missingpixel.wilderdraft",  "Wilder Draft Plugin", "1.0.0")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraAPI.MiraApiPlugin.Id)]
public partial class WilderDraftPlugin : BasePlugin, IMiraPlugin
{
    public Harmony Harmony { get; } = new(Id);
    public static AssetBundle bundle;
    public override void Load()
    {
        bundle = AssetBundleManager.Load("draft");
        ClassInjector.RegisterTypeInIl2Cpp<CardBehaviour>();
        ClassInjector.RegisterTypeInIl2Cpp<CardReleaseZone>();
        ClassInjector.RegisterTypeInIl2Cpp<CardDeck>();
        Harmony.PatchAll();
        Log.LogInfo("Wilder Draft Plugin Loaded Successfully! ￣ω￣");
        ReactorCredits.Register<WilderDraftPlugin>(_ => true);
    }

    public ConfigFile GetConfigFile()
    {
        return Config;
    }

    public string OptionsTitleText => "Wilder Draft";
}
