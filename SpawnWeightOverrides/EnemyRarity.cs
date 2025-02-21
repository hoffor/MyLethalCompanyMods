using System.Collections.Generic;
using System.Linq;

namespace SpawnWeightOverrides;

public class EnemyRarity
{
	public static List<string> LLEnemies = new();
	public static List<string> LLLEnemies = new();
	
	public static void GetEnemyInfo() // make config entries with defaults if they don't exist already
	{
		if (Plugin.LLExists)
			GetEnemyInfoLL();
		if (Plugin.LLLExists)
			GetEnemyInfoLLL();
	}
	
	
	public static void GetEnemyInfoLL()
	{
		foreach (var spawnableEnemy in LethalLib.Modules.Enemies.spawnableEnemies)
		{
			string enemyName = spawnableEnemy.enemy.enemyName;
			if (!LLEnemies.Contains(enemyName))
			{
				LLEnemies.Add(enemyName);
				ConfigManager.MakeEnemyRarityConfig(enemyName);
				ConfigManager.MakeMaxCountConfig(enemyName);
				ConfigManager.MakeGroupSizeConfig(enemyName);
				ConfigManager.MakePowerLevelConfig(enemyName);
				ConfigManager.MakeAddEnemyToAllLevelsConfig(enemyName);
			}
		}
	}
	
	
	public static void GetEnemyInfoLLL()
	{
		foreach (var extendedMod in LethalLevelLoader.PatchedContent.ExtendedMods)
		{
			foreach (var extendedEnemyType in extendedMod.ExtendedEnemyTypes)
			{
				string enemyName = extendedEnemyType.EnemyType.enemyName;
				if (!LLLEnemies.Contains(enemyName))
				{
					LLLEnemies.Add(enemyName);
					ConfigManager.MakeEnemyRarityConfig(enemyName);
					ConfigManager.MakeMaxCountConfig(enemyName);
					ConfigManager.MakeGroupSizeConfig(enemyName);
					ConfigManager.MakePowerLevelConfig(enemyName);
					ConfigManager.MakeAddEnemyToAllLevelsConfig(enemyName);
				}
			}
		}
	}
	
	
	public static void ModifyEnemyValues()
	{	
		if (Plugin.LLExists)
		{
			AddRequestedEnemiesToAllLevels(LLEnemies);
			MatchEnemiesAndModify(LLEnemies);
		}
		if (Plugin.LLLExists)
		{
			AddRequestedEnemiesToAllLevels(LLLEnemies);
			MatchEnemiesAndModify(LLLEnemies);
		}
	}
	
	
	public static void MatchEnemiesAndModify(List<string> moddedEnemies)
	{
		foreach (SelectableLevel selectableLevel in StartOfRound.Instance.levels)
		{
			List<List<SpawnableEnemyWithRarity>> levelEnemyLists =
			[
				selectableLevel.Enemies,
				selectableLevel.OutsideEnemies,
				selectableLevel.DaytimeEnemies
			];
			
			foreach (var levelEnemyList in levelEnemyLists)
			{
				foreach (var spawnableEnemyWithRarity in levelEnemyList)
				{
					if (moddedEnemies.Contains(spawnableEnemyWithRarity.enemyType.enemyName))
					{
						Logs.Log.LogDebug($"Modifying enemy {spawnableEnemyWithRarity.enemyType.enemyName} (" + ((moddedEnemies == LLEnemies) ? "LethalLib" : "LethalLevelLoader") + $") for {selectableLevel.PlanetName}...");
						UpdateRarityForLevel(spawnableEnemyWithRarity);
						
						// setting these affects the EnemyType instead of the SpawnableEnemyWithRarity,
						// so are redundantly set after the first time,
						// but it doesn't matter since we don't modify the original values with an algorithm, we just set it outright
						UpdateMaxCountForLevel(spawnableEnemyWithRarity);
						UpdateGroupSizeForLevel(spawnableEnemyWithRarity);
						UpdatePowerLevelForLevel(spawnableEnemyWithRarity);
					}
				}
			}
		}
	}
	
	
	public static void UpdateRarityForLevel(SpawnableEnemyWithRarity spawnableEnemyWithRarity)
	{
		string enemyName = spawnableEnemyWithRarity.enemyType.enemyName;
		int newRarity = ConfigManager.MakeEnemyRarityConfig(enemyName);
		if (newRarity == -1)
		{
			Logs.Log.LogDebug($"Default or otherwise invalid rarity config entry used for '{enemyName}', using this EnemyType's defaults");
			return;
		}
		int oldRarity = spawnableEnemyWithRarity.rarity;
		spawnableEnemyWithRarity.rarity = newRarity;
		Logs.Log.LogDebug($"Enemy {enemyName} (rarity: {oldRarity} -> {newRarity})");
	}
	
	
	public static void UpdatePowerLevelForLevel(SpawnableEnemyWithRarity spawnableEnemyWithRarity)
	{
		string enemyName = spawnableEnemyWithRarity.enemyType.enemyName;
		float newPowerLevel = ConfigManager.MakePowerLevelConfig(enemyName);
		if (newPowerLevel < 0.0f) // accounting for float imprecision by using >
		{
			Logs.Log.LogDebug($"Default or otherwise invalid power level config entry used for '{enemyName}', using this EnemyType's default");
			return;
		}
		float oldPowerLevel = spawnableEnemyWithRarity.enemyType.PowerLevel;
		spawnableEnemyWithRarity.enemyType.PowerLevel = newPowerLevel;
		Logs.Log.LogDebug($"Enemy {enemyName} (power level: {oldPowerLevel} -> {newPowerLevel})");
	}
	
	
	public static void UpdateGroupSizeForLevel(SpawnableEnemyWithRarity spawnableEnemyWithRarity)
	{
		string enemyName = spawnableEnemyWithRarity.enemyType.enemyName;
		int newGroupSize = ConfigManager.MakeGroupSizeConfig(enemyName);
		if (newGroupSize == -1)
		{
			Logs.Log.LogDebug($"Default or otherwise invalid group size config entry used for '{enemyName}', using this EnemyType's default");
			return;
		}
		int oldGroupSize = spawnableEnemyWithRarity.enemyType.spawnInGroupsOf;
		spawnableEnemyWithRarity.enemyType.spawnInGroupsOf = newGroupSize;
		Logs.Log.LogDebug($"Enemy {enemyName} (group size: {oldGroupSize} -> {newGroupSize})");
	}
	
	
	public static void UpdateMaxCountForLevel(SpawnableEnemyWithRarity spawnableEnemyWithRarity)
	{
		string enemyName = spawnableEnemyWithRarity.enemyType.enemyName;
		int newMaxCount = ConfigManager.MakeMaxCountConfig(enemyName);
		if (newMaxCount == -1)
		{
			Logs.Log.LogDebug($"Default or otherwise invalid max count config entry used for '{enemyName}', using this EnemyType's default");
			return;
		}
		int oldMaxCount = spawnableEnemyWithRarity.enemyType.MaxCount;
		spawnableEnemyWithRarity.enemyType.MaxCount = newMaxCount;
		Logs.Log.LogDebug($"Enemy {enemyName} (max count: {oldMaxCount} -> {newMaxCount})");
	}
	
	
	public static void AddRequestedEnemiesToAllLevels(List<string> moddedEnemies) // this is a weird one
	{
		Dictionary<string, SpawnableEnemyWithRarity> allEnemies = new();
		
		List<string> enemiesToAdd = new();
		
		// get list of enemies to add to all levels
		foreach (string enemyName in moddedEnemies)
		{
			if (ConfigManager.MakeAddEnemyToAllLevelsConfig(enemyName))
				enemiesToAdd.Add(enemyName);
		}
		
		// build a dictionary of every enemy in the game, vanilla and modded
		// this is so that we can associate enemy names with their respective spawnableenemywithrarity
		foreach (SelectableLevel selectableLevel in StartOfRound.Instance.levels)
		{
			List<List<SpawnableEnemyWithRarity>> levelEnemyLists =
			[
				selectableLevel.Enemies,
				selectableLevel.OutsideEnemies,
				selectableLevel.DaytimeEnemies
			];
			
			foreach (var levelEnemyList in levelEnemyLists)
			{
				foreach (SpawnableEnemyWithRarity spawnableEnemyWithRarity in levelEnemyList)
				{
					if (!allEnemies.ContainsKey(spawnableEnemyWithRarity.enemyType.enemyName))
						allEnemies.Add(spawnableEnemyWithRarity.enemyType.enemyName, spawnableEnemyWithRarity);
				}
			}
		}
		
		
		foreach (SelectableLevel selectableLevel in StartOfRound.Instance.levels)
		{
			List<List<SpawnableEnemyWithRarity>> levelEnemyLists =
			[
				selectableLevel.Enemies,
				selectableLevel.OutsideEnemies,
				selectableLevel.DaytimeEnemies
			];
			
			int listIndex = 0;
			
			foreach (var levelEnemyList in levelEnemyLists)
			{
				listIndex = listIndex + 1; // 1=Enemies, 2=OutsideEnemies, 3=DaytimeEnemies
				
				List<string> existingEnemyNames = new List<string>();
				
				// a list of names in this enemy list (of 3 lists) in this level
				foreach (var spawnableEnemyWithRarity in levelEnemyList)
				{
					existingEnemyNames.Add(spawnableEnemyWithRarity.enemyType.enemyName);
				}
				
				foreach (string enemyToAdd in enemiesToAdd)
				{
					if (!existingEnemyNames.Contains(enemyToAdd))
					{
						// look up this enemy name in allenemies list to get the spawnableenemywithrarity
						// yes this is super backwards and a mess but hey if it works then whatever
						if (allEnemies.TryGetValue(enemyToAdd, out var spawnableEnemyWithRarity))
						{
							// check if it's appropriate for this enemy list (indoor/outside/daytime)
							
							// indoor
							if (listIndex == 1
							&& !spawnableEnemyWithRarity.enemyType.isOutsideEnemy
							&& !spawnableEnemyWithRarity.enemyType.isDaytimeEnemy)
							{
								levelEnemyList.Add(spawnableEnemyWithRarity); // add enemy to this particular enemy list for this level
								continue;
							}
							
							// outside
							if (listIndex == 2
							&& spawnableEnemyWithRarity.enemyType.isOutsideEnemy)
							{
								levelEnemyList.Add(spawnableEnemyWithRarity);
								continue;
							}
							
							// daytime
							if (listIndex == 3
							&& spawnableEnemyWithRarity.enemyType.isDaytimeEnemy)
							{
								levelEnemyList.Add(spawnableEnemyWithRarity);
								continue;
							}
						}
					}
				}
			}
		}
	}
}
