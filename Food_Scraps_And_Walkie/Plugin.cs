using System.IO; // For Path class
using System.Reflection; // For Assembly class
using HarmonyLib;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace FoodScrapsAndWalkie;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID)]

public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
	
	public static AssetBundle MyCustomAssets;
        
    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
		
		string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		MyCustomAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "food_scraps_and_walkie_bundle"));
		
		if (MyCustomAssets == null)
		{
			Logger.LogError("Failed to load custom assets.");
			return;
		}
		else
		{
			Logger.LogInfo("Custom assets are loaded!");
			Logger.LogInfo("Registering items");
			
			int iRarity = 55;
			string[] itemNames = { "Coffee", "Burger", "Cake", "Cookie", "Bread", "CursedWalkie" };
			Item[] items = new Item[itemNames.Length];

			for (int i = 0; i < itemNames.Length; i++)
			{
				items[i] = MyCustomAssets.LoadAsset<Item>($"Assets/ScrapMod/Items/{itemNames[i]}.asset");
				LethalLib.Modules.Utilities.FixMixerGroups(items[i].spawnPrefab);
				LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(items[i].spawnPrefab);
				LethalLib.Modules.Items.RegisterScrap(items[i], iRarity, LethalLib.Modules.Levels.LevelTypes.All);
			}
		}
    }
}
