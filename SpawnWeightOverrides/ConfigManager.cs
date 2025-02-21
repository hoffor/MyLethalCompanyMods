using BepInEx;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;

namespace SpawnWeightOverrides;

public class ConfigManager
{
	public static ConfigFile configFile;
	public static List<string> activeConfigs = new List<string>();
	
	public static void InitConfig()
	{
		string configFilePath = Path.Combine(Paths.ConfigPath, $"{PluginMetadata.PLUGIN_NAME}.cfg"); // keeping our config file name simple rather than using the plugin's GUID
		configFile = new ConfigFile(configFilePath, true);
	}
	
	
	
	
	public static int MakeScrapPercentageConfig(string modName)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> scrapConfig = configFile.Bind($"(2. Scrap) {modName}: Spawn weights", 
			"Percentage of original spawn weights",
			100,  // Default value
			$"Percentage of the original spawn weights for all scrap items in mod: {modName}"); // Description
		if (scrapConfig.Value >= 0)
		{
			return scrapConfig.Value;
		}
		return -1;
	}
	
	
	public static bool MakeAddEnemyToAllLevelsConfig(string enemyName)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<bool> addToAllLevelsConfig = configFile.Bind($"(1. Enemy) {enemyName}", 
			"Forcibly add enemy to all levels",
			false,  // Default value
			$"If true, this enemy will be added to all level spawn pools"); // Description
		return addToAllLevelsConfig.Value;
	}
	
	
	public static int MakeEnemyRarityConfig(string enemyName)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> rarityConfig = configFile.Bind($"(1. Enemy) {enemyName}", // Category
			"Spawn weight for this enemy for all moons", // Name
			-1, // Default
			"Leave at -1 for default, any positive (or 0) integer otherwise"); // Description
		if (rarityConfig.Value >= 0)
		{
			return rarityConfig.Value;
		}
		return -1;
	}
	
	
	public static float MakePowerLevelConfig(string enemyName) // using int on the user-end for ease of use
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> powerLevelConfig = configFile.Bind($"(1. Enemy) {enemyName}", // Category - numbering at the start is weird but helps to order the categories
			"Power level (number that affects how often all enemies spawn)", // Name
			-1, // Default
			"Leave blank for default, any positive nonzero integer otherwise.\n3 is standard for powerful enemies. Higher generally means you'll see less of this enemy, and less enemies in general"); // Description
		if (powerLevelConfig.Value > 0)
		{
			return (float)powerLevelConfig.Value;
		}
		return (float)-1;
	}
	
	
	public static int MakeGroupSizeConfig(string enemyName)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> groupSizeConfig = configFile.Bind($"(1. Enemy) {enemyName}", // Category
			"Group size (how many in each spawn)", // Name
			-1, // Default
			"Leave blank for default, any positive nonzero integer otherwise.\nNumber of this enemy that will spawn in each group\nOnly affects outdoor enemies"); // Description
		if (groupSizeConfig.Value > 0)
		{
			return groupSizeConfig.Value;
		}
		return -1;
	}
	
	
	public static int MakeMaxCountConfig(string enemyName)
	{
		// Cannot use any of the following characters in section and key names: = \n \t \ " ' [ ]
		ConfigEntry<int> maxCountConfig = configFile.Bind($"(1. Enemy) {enemyName}", // Category
			"Max count (how many groups to spawn)", // Name
			-1, // Default
			"Leave blank for default, any positive nonzero integer otherwise\nMaximum number of groups of this enemy that can spawn in a day"); // Description
		if (maxCountConfig.Value > 0)
		{
			return maxCountConfig.Value;
		}
		return -1;
	}
}