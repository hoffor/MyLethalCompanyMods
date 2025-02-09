/*
NOTES AND TODO:

for some reason bozoros isn't being overriden for enemy weight vs All. no idea, seems like everything's how it should be?

IMPERIUM BUG!!!!!!: sometimes you'll boot the game up and the quota will be set to 0. this will create divide by 0 errors and cause all sorts of wacky stuff to happen especially with enemy spawns

for modifying LLL per-level enemy/scrap rarities:

(info is in Patches.cs, ItemManager.cs, EnemyManager.cs)

for enemy (pseudocode):
`StartOfRound.Awake()` postfix
	foreach (ExtendedLevel globalLevel in PatchedContent.ExtendedLevels)
		foreach (ExtendedItem globalItem in PatchedContent.CustomExtendedItems)
			if (globalItem == PatchedContent.CustomExtendedItems.member.Item.isScrap)
			foreach (SpawnableItemWithRarity spawnableItemInLevel in globalLevel.SelectableLevel.spawnableScrap)
				if (spawnableItemInLevel.spawnableItem == globalItem)
				{
					var newRarity = (int)Math.Floor(spawnableItemInLevel.rarity * (rarityPercent / 100.0));
					spawnableItemInLevel.rarity = newRarity;
				}

for scrap:
StartOfRound.Awake() postfix
	for every level in existence
		for every custom enemy in existence
			for every spawnable enemy in the level (gotta check each: Enemies, OutsideEnemies, DaytimeEnemies)
				if the custom item is already in the spawnable items list
					simply adjust the 'rarity' field in the existing item entry in the spawnable enemies list for the level

btw i want to be able to add ooblterra's eyesecurity enemy to global pool lol

so uhh... i can't figure out if it's possible to get mod dll names from LLL, so the scrap items will have to go under an 'LLL' config section instead of per dll name, unless we wanna get super clever in order to find it. bleh

eventually i want to be able to use reflection on lethallib and LLL to avoid requiring them as a dependency. not quite there yet skill-wise, the process is pretty intimidating

*/


using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;
using LethalLib.Modules;

namespace SpawnWeightOverrides;

public static class PluginMetadata
{
	public const string PLUGIN_VERSION = "1.0.0";
	public const string PLUGIN_NAME = "SpawnWeightOverrides";
	public const string PLUGIN_AUTHOR = "hoffr";
	public const string PLUGIN_GUID = PLUGIN_AUTHOR + "." + PLUGIN_NAME;
}

[BepInPlugin(PluginMetadata.PLUGIN_GUID, PluginMetadata.PLUGIN_NAME, PluginMetadata.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance;
	public BepInEx.Logging.ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(PluginMetadata.PLUGIN_NAME);
	
	ConfigFile configFile;
	
	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		
		MakeConfig();
		new Harmony(PluginMetadata.PLUGIN_GUID).PatchAll(Assembly.GetExecutingAssembly());
		Log.LogInfo($"{PluginMetadata.PLUGIN_NAME} v{PluginMetadata.PLUGIN_VERSION} loaded.");
	}
	
	
	public void MakeConfig()
	{
		configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{PluginMetadata.PLUGIN_NAME}.cfg"), true); // keeping our config file name simple rather than using the plugin's GUID
	}
	
	
	public void ReregisterScrap()
	{
		int modCount = 0;
		int scrapCount = 0;
		
		var modNames = LethalLib.Modules.Items.scrapItems
			.Select(item => item.modName)
			.Distinct()
			.ToList();
		
		foreach (var modName in modNames)
		{
			var rarityPercent = MakeScrapPercentageConfig(modName);
			if (rarityPercent == -1)
			{
				continue;
			}
			
			foreach (var scrapItem in LethalLib.Modules.Items.scrapItems.Where(item => item.modName == modName))
			{
				var levelRarities2 = new Dictionary<Levels.LevelTypes, int>(scrapItem.levelRarities);
				var customLevelRarities2 = new Dictionary<string, int>(scrapItem.customLevelRarities);
				var rarity = 0;
				
				foreach (var key in levelRarities2.Keys.ToList())
				{
					rarity = (int)Math.Floor(levelRarities2[key] * (rarityPercent / 100.0));
					levelRarities2[key] = rarity;
					//Log.LogDebug($"[LevelType: {key}] SCRAP RARITY: [{scrapItem.modName}] {scrapItem.item.itemName} - {rarity}");
				}
				
				foreach (var key in customLevelRarities2.Keys.ToList())
				{
					rarity = (int)Math.Floor(customLevelRarities2[key] * (rarityPercent / 100.0));
					customLevelRarities2[key] = rarity;
					//Log.LogDebug($"[Custom Level: {key}] SCRAP RARITY: [{scrapItem.modName}] {scrapItem.item.itemName} - {rarity}");
				}
				
				scrapItem.levelRarities.Clear();
				scrapItem.customLevelRarities.Clear();
				
				LethalLib.Modules.Items.RegisterScrap(scrapItem.item, levelRarities2, customLevelRarities2);
				Log.LogInfo($"Re-registered scrap [{scrapItem.modName}] {scrapItem.item.itemName}");
				scrapCount++;
			}
			modCount++;
		}
		
		Log.LogInfo($"Re-registered {scrapCount} scrap items for {modCount} mods");
	}
	
	
	public int MakeScrapPercentageConfig(string modName)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> scrapConfig = configFile.Bind($"(2. Scrap) {modName}: Spawn weights", 
			"Percentage of original spawn weights",
			100,  // Default value
			$"Percentage of the original spawn weights for all scrap items in mod: {modName}"); // Description
		if (scrapConfig.Value == 100)
		{
			Log.LogDebug($"Default scrap rarity config entry used for '{modName}', using this item's defaults");
			return scrapConfig.Value;
		}
		if (scrapConfig.Value >= 0)
		{
			return scrapConfig.Value;
		}
		
		Log.LogWarning($"Invalid scrap rarity config entry for '{modName}', using this item's defaults");
		return -1;
	}
	
	
	public void ReregisterEnemies()
	{
		if (LethalLib.Modules.Enemies.spawnableEnemies != null && LethalLib.Modules.Enemies.spawnableEnemies.Any())
		{
			var spawnableEnemiesCopy = new List<LethalLib.Modules.Enemies.SpawnableEnemy>(LethalLib.Modules.Enemies.spawnableEnemies);
			
			foreach (var spawnableEnemy in spawnableEnemiesCopy) // unregister from all levels, delete from spawnableEnemies list and then re-register
			{
				var rarity = MakeEnemyRarityConfig(spawnableEnemy);
				var powerLevel = MakePowerLevelConfig(spawnableEnemy);
				var maxCount = MakeMaxCountConfig(spawnableEnemy);
				var spawnInGroupsOf = MakeGroupSizeConfig(spawnableEnemy);
				
				LethalLib.Modules.Enemies.RemoveEnemyFromLevels(spawnableEnemy.enemy);
				LethalLib.Modules.Enemies.spawnableEnemies.Remove(spawnableEnemy);
				
				
				int modifiedCount = 0;
				if (powerLevel != -1) // if not using the default for this enemytype
				{
					Log.LogDebug($"Setting power level for {spawnableEnemy.enemy.enemyName} to {powerLevel}, was {spawnableEnemy.enemy.PowerLevel}");
					spawnableEnemy.enemy.PowerLevel = powerLevel;
					modifiedCount++;
				}
				if (maxCount != -1)
				{
					Log.LogDebug($"Setting max count for {spawnableEnemy.enemy.enemyName} to {maxCount}, was {spawnableEnemy.enemy.MaxCount}");
					spawnableEnemy.enemy.MaxCount = maxCount;
					modifiedCount++;
				}
				if (spawnInGroupsOf != -1)
				{
					Log.LogDebug($"Setting group size for {spawnableEnemy.enemy.enemyName} to {spawnInGroupsOf}, was {spawnableEnemy.enemy.spawnInGroupsOf}");
					spawnableEnemy.enemy.spawnInGroupsOf = spawnInGroupsOf;
					modifiedCount++;
				}
				if (rarity != -1)
				{
					Log.LogDebug($"Setting rarity for {spawnableEnemy.enemy.enemyName} to {rarity}");
					modifiedCount++;
					
					if (modifiedCount != 0) // if we haven't modified any of this enemy's values, do nothing
					{
						LethalLib.Modules.Enemies.RegisterEnemy(spawnableEnemy.enemy, rarity, LethalLib.Modules.Levels.LevelTypes.All, spawnableEnemy.spawnType, null, spawnableEnemy.terminalNode, spawnableEnemy.infoKeyword);
						Log.LogInfo($"Re-registered enemy {spawnableEnemy.enemy.enemyName}.");
					}
				}
				else // use the values for levelRarities and customLevelRarities that were already set for this enemy
				{
					if (modifiedCount > 0)
					{
						LethalLib.Modules.Enemies.RegisterEnemy(spawnableEnemy.enemy, spawnableEnemy.spawnType, spawnableEnemy.levelRarities, spawnableEnemy.customLevelRarities, spawnableEnemy.terminalNode, spawnableEnemy.infoKeyword);
						Log.LogInfo($"Re-registered enemy {spawnableEnemy.enemy.enemyName}.");
					}
				}
				
				Log.LogInfo($"Re-registered enemy {spawnableEnemy.enemy.enemyName}");
			}
		}
	}
	
	
	public int MakeEnemyRarityConfig(LethalLib.Modules.Enemies.SpawnableEnemy spawnableEnemy)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> rarityConfig = configFile.Bind($"(1. Enemy) {spawnableEnemy.enemy.enemyName}", // Category
			"Spawn weight for this enemy for all moons", // Name
			-1, // Default
			"Leave at -1 for default, any positive (or 0) integer otherwise"); // Description
		if (rarityConfig.Value == -1)
		{
			Log.LogDebug($"Default rarity config entry used for '{spawnableEnemy.enemy.enemyName}', using this EnemyType's defaults");
			return rarityConfig.Value;
		}
		if (rarityConfig.Value >= 0)
		{
			return rarityConfig.Value;
		}
		
		Log.LogWarning($"Invalid rarity config entry for '{spawnableEnemy.enemy.enemyName}', using this EnemyType's defaults");
		return -1;
	}
	
	
	/*
	// these methods handle levelRarities and customLevelRarities unlike the other version which only handles a single global rarity
	// these methods will work fine as long as you comment out the other variants,
	// except that custom moons for some reason aren't overriding levelRarities values, which is why i'm using the other versions. haven't figured that one out yet
	
	
	public void ReregisterEnemies()
	{
		if (LethalLib.Modules.Enemies.spawnableEnemies != null && LethalLib.Modules.Enemies.spawnableEnemies.Any())
		{
			var spawnableEnemiesCopy = new List<LethalLib.Modules.Enemies.SpawnableEnemy>(LethalLib.Modules.Enemies.spawnableEnemies);
			
			foreach (var spawnableEnemy in spawnableEnemiesCopy) // unregister from all levels, delete from spawnableEnemies list and then re-register
			{
				var levelRarities = new Dictionary<Levels.LevelTypes, int>();
				var levelRaritiesString = MakeLevelRaritiesConfig(spawnableEnemy);
				if (levelRaritiesString != "")
				{
					levelRarities = ParseLevelRarities(levelRaritiesString);
				}
				
				var customLevelRarities = new Dictionary<string, int>();
				var customLevelRaritiesString = MakeCustomLevelRaritiesConfig(spawnableEnemy);
				if (customLevelRaritiesString != "")
				{
					customLevelRarities = ParseCustomLevelRarities(customLevelRaritiesString);
				}
				
				var maxCount = MakeMaxCountConfig(spawnableEnemy);
				var spawnInGroupsOf = MakeGroupSizeConfig(spawnableEnemy);
				var powerLevel = MakePowerLevelConfig(spawnableEnemy);
				
				LethalLib.Modules.Enemies.RemoveEnemyFromLevels(spawnableEnemy.enemy);
				LethalLib.Modules.Enemies.spawnableEnemies.Remove(spawnableEnemy);
				
				
				if (powerLevel != -1) // if not using the default for this enemytype
				{
					Log.LogDebug($"Setting power level for {spawnableEnemy.enemy.enemyName} to {powerLevel}, was {spawnableEnemy.enemy.PowerLevel}");
					spawnableEnemy.enemy.PowerLevel = powerLevel;
				}
				
				if (maxCount != -1)
				{
					Log.LogDebug($"Setting max count for {spawnableEnemy.enemy.enemyName} to {maxCount}, was {spawnableEnemy.enemy.MaxCount}");
					spawnableEnemy.enemy.MaxCount = maxCount;
				}
				if (spawnInGroupsOf != -1)
				{
					Log.LogDebug($"Setting group size for {spawnableEnemy.enemy.enemyName} to {spawnInGroupsOf}, was {spawnableEnemy.enemy.spawnInGroupsOf}");
					spawnableEnemy.enemy.spawnInGroupsOf = spawnInGroupsOf;
				}
				
				//LethalLib.Modules.Enemies.RegisterEnemy(spawnableEnemy.enemy, 9999, LethalLib.Modules.Levels.LevelTypes.All, spawnableEnemy.spawnType, null, spawnableEnemy.terminalNode, spawnableEnemy.infoKeyword);
				LethalLib.Modules.Enemies.RegisterEnemy(spawnableEnemy.enemy, spawnableEnemy.spawnType, levelRarities, customLevelRarities, spawnableEnemy.terminalNode, spawnableEnemy.infoKeyword);
				Log.LogInfo($"Re-registered enemy {spawnableEnemy.enemy.enemyName}.");
			}
		}
	}
	
	
	// btw these configentry types need to change:
	
	public string MakeLevelRaritiesConfig(LethalLib.Modules.Enemies.SpawnableEnemy spawnableEnemy)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<string> levelRaritiesConfig = configFile.Bind($"(1. Enemy) {spawnableEnemy.enemy.enemyName}", // Category
			"Per-moon type and per-vanilla moon name enemy spawn weights", // Name
			"", // Default
			"Leave blank for default\nOnly use moon types (All/Modded/Vanilla) and vanilla moon names in this entry.\nPrecedence order: Moon name overrides moon type (Modded/Vanilla), moon type overrides All\nExample entry: All:20, Modded:50, Vanilla:15, Experimentation:5, Titan:90\nIn the above example, 'Rend' (which is not included) would fall back to 15, and if 'Vanilla' wasn't present, it'd be 20"); // Description
		return levelRaritiesConfig.Value;
	}
	
	
	public string MakeCustomLevelRaritiesConfig(LethalLib.Modules.Enemies.SpawnableEnemy spawnableEnemy)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<string> customLevelRaritiesConfig = configFile.Bind($"(1. Enemy) {spawnableEnemy.enemy.enemyName}", // Category
			"Per-modded moon name enemy spawn weights", // Name
			"", // Default
			"Leave blank for default\nOnly use modded moon names in this entry.\nSee the above explanation for precedence order. It's the same here but Modded/Vanilla/All should not be used in this entry, it's only for modded moon names"); // Description
		return customLevelRaritiesConfig.Value;
	}
	
	
	public Dictionary<Levels.LevelTypes, int> ParseLevelRarities(string input) // eg. All:50, Modded:40, Vanilla:20, Experimentation:25, Titan:50
	{
		var levelRarities = new Dictionary<Levels.LevelTypes, int>();
		var levelType = Levels.LevelTypes.None;
		
		input = input.ToLower();
		var entries = input.Trim().Split(',');

		foreach (var entry in entries)
		{
			var parts = entry.Trim().Split(':');

			if (parts.Length == 2)
			{
				// hi i hate switch statements
				if (parts[0] == "all")
					levelType = Levels.LevelTypes.All;
				else if (parts[0] == "modded")
					levelType = Levels.LevelTypes.Modded;
				else if (parts[0] == "vanilla")
					levelType = Levels.LevelTypes.Vanilla;
				else if (parts[0] == "experimentation" || parts[0] == "experimentationlevel")
					levelType = Levels.LevelTypes.ExperimentationLevel;
				else if (parts[0] == "assurance" || parts[0] == "assurancelevel")
					levelType = Levels.LevelTypes.AssuranceLevel;
				else if (parts[0] == "vow" || parts[0] == "vowlevel")
					levelType = Levels.LevelTypes.VowLevel;
				else if (parts[0] == "offense" || parts[0] == "offenselevel")
					levelType = Levels.LevelTypes.OffenseLevel;
				else if (parts[0] == "march" || parts[0] == "marchlevel")
					levelType = Levels.LevelTypes.MarchLevel;
				else if (parts[0] == "rend" || parts[0] == "rendlevel")
					levelType = Levels.LevelTypes.RendLevel;
				else if (parts[0] == "dine" || parts[0] == "dinelevel")
					levelType = Levels.LevelTypes.DineLevel;
				else if (parts[0] == "titan" || parts[0] == "titanlevel")
					levelType = Levels.LevelTypes.TitanLevel;
				else if (parts[0] == "adamance" || parts[0] == "adamancelevel")
					levelType = Levels.LevelTypes.AdamanceLevel;
				else if (parts[0] == "artifice" || parts[0] == "artificelevel")
					levelType = Levels.LevelTypes.ArtificeLevel;
				else if (parts[0] == "embrion" || parts[0] == "embrionlevel")
					levelType = Levels.LevelTypes.EmbrionLevel;
				else
				{
					Log.LogWarning($"Invalid level type '{parts[0]}' in config");
					continue;
				}
				
				if (int.TryParse(parts[1], out int rarity))
				{
					levelRarities[levelType] = rarity;
				}
				else
				{
					Log.LogWarning($"Invalid rarity '{parts[1]}' for level type '{parts[0]}'");
				}
			}
			else
			{
				Log.LogWarning($"Invalid entry '{entry}' in level rarities config");
			}
		}
		return levelRarities;
	}
	
	
	public Dictionary<string, int> ParseCustomLevelRarities(string input) // eg. Bozoros:50, Seichi:20
	{
		var customLevelRarities = new Dictionary<string, int>();
		
		input = input.ToLower();
		var entries = input.Trim().Split(',');

		foreach (var entry in entries)
		{
			var parts = entry.Trim().Split(':');
			
			if (parts.Length == 2)
			{	
				if (int.TryParse(parts[1], out int rarity))
				{
					customLevelRarities[parts[0]] = rarity;
				}
				else
				{
					Log.LogWarning($"Invalid rarity '{parts[1]}' for level '{parts[0]}'.");
				}
			}
		}
		return customLevelRarities;
	}
	
	*/
	
	
	public int MakePowerLevelConfig(LethalLib.Modules.Enemies.SpawnableEnemy spawnableEnemy)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> powerLevelConfig = configFile.Bind($"(1. Enemy) {spawnableEnemy.enemy.enemyName}", // Category - numbering at the start is weird but helps to order the categories
			"Power level (number that affects how often all enemies spawn)", // Name
			-1, // Default
			"Leave blank for default, any positive nonzero integer otherwise.\n3 is standard for powerful enemies. Higher generally means you'll see less of this enemy, and less enemies in general"); // Description
		if (powerLevelConfig.Value == -1)
		{
			Log.LogDebug($"Default power level config entry used for '{spawnableEnemy.enemy.enemyName}', using this EnemyType's default");
			return powerLevelConfig.Value;
		}
		if (powerLevelConfig.Value > 0)
		{
			return powerLevelConfig.Value;
		}
		
		Log.LogWarning($"Invalid power level config entry for '{spawnableEnemy.enemy.enemyName}', using this EnemyType's default");
		return -1;
	}
	
	
	public int MakeGroupSizeConfig(LethalLib.Modules.Enemies.SpawnableEnemy spawnableEnemy)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> groupSizeConfig = configFile.Bind($"(1. Enemy) {spawnableEnemy.enemy.enemyName}", // Category
			"Group size (how many in each spawn)", // Name
			-1, // Default
			"Leave blank for default, any positive nonzero integer otherwise.\nNumber of this enemy that will spawn in each group\nOnly affects outdoor enemies"); // Description
		if (groupSizeConfig.Value == -1)
		{
			Log.LogDebug($"Default group size config entry used for '{spawnableEnemy.enemy.enemyName}', using this EnemyType's default");
			return groupSizeConfig.Value;
		}
		if (groupSizeConfig.Value > 0)
		{
			return groupSizeConfig.Value;
		}
		
		Log.LogWarning($"Invalid group size config entry for '{spawnableEnemy.enemy.enemyName}', using this EnemyType's default");
		return -1;
	}
	
	
	public int MakeMaxCountConfig(LethalLib.Modules.Enemies.SpawnableEnemy spawnableEnemy)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> maxCountConfig = configFile.Bind($"(1. Enemy) {spawnableEnemy.enemy.enemyName}", // Category
			"Max count (how many groups to spawn)", // Name
			-1, // Default
			"Leave blank for default, any positive nonzero integer otherwise\nMaximum number of groups of this enemy that can spawn in a day"); // Description
		if (maxCountConfig.Value == -1)
		{
			Log.LogDebug($"Default max count config entry used for '{spawnableEnemy.enemy.enemyName}', using this EnemyType's default");
			return maxCountConfig.Value;
		}
		if (maxCountConfig.Value > 0)
		{
			return maxCountConfig.Value;
		}
		
		Log.LogWarning($"Invalid max count config entry for '{spawnableEnemy.enemy.enemyName}', using this EnemyType's default");
		return -1;
	}
	
	

}


[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
	[HarmonyPostfix]
	[HarmonyPatch("Awake")]
	public static void OnStartOfRoundStart()
	{
		Plugin.Instance.ReregisterEnemies();
		Plugin.Instance.ReregisterScrap();
	}
}
