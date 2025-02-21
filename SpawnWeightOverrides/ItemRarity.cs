using System;
using System.Collections.Generic;

namespace SpawnWeightOverrides;

public class ItemRarity
{	
	public static Dictionary<string, List<string>> LLScrapItems = new();
	public static Dictionary<string, List<string>> LLLScrapItems = new();
	
	public static void GetModdedScrapInfo()
	{
		if (Plugin.LLExists)
			GetScrapInfoLL();
		if (Plugin.LLLExists)
			GetScrapInfoLLL();
	}
	
	
	public static void ModifyAllLevelScrapRarity(string LLorLLL)
	{
		Dictionary<string, List<string>> moddedScrapItems = (LLorLLL == "LL") ? LLScrapItems : LLLScrapItems;
		string moddingAPI = (LLorLLL == "LL") ? "LethalLib" : "LethalLevelLoader";
			
		foreach (var kvp in moddedScrapItems)
		{
			string modName = kvp.Key;
			int percentage = ConfigManager.MakeScrapPercentageConfig(modName);
			if (percentage == 100) // no change
			{
				Logs.Log.LogDebug($"Default or otherwise invalid scrap rarity config entry used for '{modName}' ({moddingAPI}), using this item's defaults");
				continue;
			}
			
			foreach (SelectableLevel selectableLevel in StartOfRound.Instance.levels)
			{
				foreach (SpawnableItemWithRarity spawnableItemWithRarity in selectableLevel.spawnableScrap)
				{
					string levelScrapID = GenerateItemID(spawnableItemWithRarity.spawnableItem);
					
					foreach (string moddedScrapID in kvp.Value)
					{
						if (moddedScrapID == levelScrapID)
						{
							int oldRarity = spawnableItemWithRarity.rarity;
							int newRarity = (int)Math.Floor(oldRarity * (percentage / 100.0));
							spawnableItemWithRarity.rarity = newRarity;
							Logs.Log.LogDebug($"Scrap item in mod {modName} ({moddingAPI}) for {selectableLevel.PlanetName}: {spawnableItemWithRarity.spawnableItem.itemName} (rarity: {oldRarity} -> {newRarity})");
						}
					}
				}
			}
		}
	}
	
	
	public static void GetScrapInfoLL()
	{
		foreach (var item in LethalLib.Modules.Items.scrapItems)
		{
			string modName = item.modName;
			string itemID = GenerateItemID(item.item);
			if (!LLScrapItems.ContainsKey(modName))
			{
				LLScrapItems[modName] = new List<string>();
				ConfigManager.MakeScrapPercentageConfig(modName); // make entry with defaults if doesn't exist already
			}
			LLScrapItems[modName].Add(itemID);
		}
	}
	
	
	public static void GetScrapInfoLLL()
	{
		foreach (var mod in LethalLevelLoader.PatchedContent.ExtendedMods)
		{
			string modName = mod.ModName;
			if (!LLLScrapItems.ContainsKey(modName))
			{
				LLLScrapItems[modName] = new List<string>();
				ConfigManager.MakeScrapPercentageConfig(modName);
			}
			foreach (var extendedItem in mod.ExtendedItems)
			{
				LLLScrapItems[modName].Add(GenerateItemID(extendedItem.Item));
			}
		}
	}
	
	
	public static void ModifyScrapRarity()
	{
		if (Plugin.LLExists)
			ModifyAllLevelScrapRarity("LL");
		if (Plugin.LLLExists)
			ModifyAllLevelScrapRarity("LLL");
	}
	
	
	private static string GenerateItemID(Item item)
	{
		return item.itemName + item.weight.ToString() + item.maxValue.ToString() + item.minValue.ToString();
	}
}
