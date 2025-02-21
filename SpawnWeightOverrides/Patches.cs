using HarmonyLib;

namespace SpawnWeightOverrides;

[HarmonyPatch]
public class Patches
{
	public static bool scrapIsModified = false;
	public static bool infoIsObtained = false;
	
	
	[HarmonyPatch(typeof(StartOfRound), "Awake"), HarmonyPostfix, HarmonyAfter(["imabatby.lethallevelloader", "evaisa.lethallib"])]
	public static void Patch_StartOfRound_Awake()
	{
		if (!infoIsObtained)
		{
			ItemRarity.GetModdedScrapInfo();
			EnemyRarity.GetEnemyInfo();
			infoIsObtained = true;
		}
	}
	
	// lethallib injects its scrap into all levels only after the lever is pulled
	[HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel"), HarmonyPrefix, HarmonyBefore(["imabatby.lethallevelloader"])]
	public static void Patch_RoundManager_SpawnScrapInLevel()
	{
		if (!scrapIsModified) // once is sufficient, subsequent calls would just stack the effect for LL
		{
			ItemRarity.ModifyScrapRarity();
			scrapIsModified = true;
		}
	}
	
	
	[HarmonyPatch(typeof(RoundManager), "BeginEnemySpawning"), HarmonyPrefix]
	public static void Patch_RoundManager_BeginEnemySpawning()
	{
		// run unconditionally every landing, since loading a new save will reset LLL's enemy values and running this again has no ill effect on LL
		EnemyRarity.ModifyEnemyValues();
	}
}
