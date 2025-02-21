using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace SpawnWeightOverrides;

public static class PluginMetadata
{
	public const string PLUGIN_VERSION = "2.0.0";
	public const string PLUGIN_NAME = "SpawnWeightOverrides";
	public const string PLUGIN_AUTHOR = "hoffr";
	public const string PLUGIN_GUID = PLUGIN_AUTHOR + "." + PLUGIN_NAME;
}


[BepInPlugin(PluginMetadata.PLUGIN_GUID, PluginMetadata.PLUGIN_NAME, PluginMetadata.PLUGIN_VERSION)]
[BepInDependency("evaisa.lethallib", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance;
	public static bool LLExists = false;
	public static bool LLLExists = false;
	
	private void Awake()
	{	
		if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("evaisa.lethallib"))
			LLExists = true;
		if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("imabatby.lethallevelloader"))
			LLLExists = true;
		
		if (!LLExists && !LLLExists)
			Logs.Log.LogInfo($"Neither LethalLib nor LethalLevelLoader are present, nothing to do this session.");
			
		new Harmony(PluginMetadata.PLUGIN_GUID).PatchAll(Assembly.GetExecutingAssembly());
		Logs.Log.LogInfo($"{PluginMetadata.PLUGIN_NAME} v{PluginMetadata.PLUGIN_VERSION} loaded.");
		
		ConfigManager.InitConfig();
	}
}
