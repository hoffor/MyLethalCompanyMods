using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using Unity.Netcode;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;

namespace AutoMessage;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance;
	
	internal static new ManualLogSource Logger;
	
	// config
	public bool randomize;
	public string title;
	public List<string> messages = new List<string>();
	
	static System.Random random = new System.Random();
	
	private static int messageIndex = 0;
	
	// FOR TESTING:
	//public static string lastMoon = "";
	//public static bool isFirstRun = true;
	
	private void Awake()
	{
		Logger = base.Logger;
		if (Instance == null)
		{
			Instance = this;
		}
		
		MakeConfig();
		
		new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll(Assembly.GetExecutingAssembly());

		Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
	}
	
	public void MakeConfig()
	{
		Logger = base.Logger;
		
		ConfigEntry<string> randomizeConfig = Config.Bind("1. Randomize", // Category
			"Randomize", // Name
			"false", // Default
			"true / false - Will scramble the order of messages displayed.\nNote that once it starts looping, that same order will persist.\nThus it's not a true on-the-fly randomization."); // Description
		bool.TryParse(randomizeConfig.Value, out randomize); // convert string to bool
		
		ConfigEntry<string> titleConfig = Config.Bind("2. Title", // Category
			"Title", // Name
			"AutoMessage", // Default
			"The title text that will show above all the messages.\nSome symbols will automatically be preceded by a backslash after the game runs.\nThis won't show in game and it's okay to omit them when typing"); // Description
		title = titleConfig.Value;
		
		ConfigEntry<string> messagesConfig1 = Config.Bind("3. Messages", // Category
			"Message1", // Name
			"Message 1", // Default
			"Messages will be displayed in sequential order (unless 'randomize' is true) every time the lever is pulled to land, until they loop.\nBlank message entries will be skipped.\nSome symbols will automatically be preceded by a backslash after the game runs.\nThis won't show in game and it's okay to omit them when typing"); // Description
		messages.Add(messagesConfig1.Value);
		
		for (int i = 2; i <= 100; i++)
		{
			ConfigEntry<string> messageConfig2 = Config.Bind("3. Messages", $"Message{i}", "");
			if (messageConfig2.Value != "")
				messages.Add(messageConfig2.Value);
		}
		
		if (randomize)
		{
			messages = messages.OrderBy(x => random.Next()).ToList(); // scramble message order
		}
	}
	
	public void DebugLog(string title, string message)
	{
		if (HUDManager.Instance != null)
		{
			HUDManager.Instance.DisplayTip(title, message);
		}
	}
	
	// Function to get the next message
	public string GetNextMessage()
	{
		string message = messages[messageIndex];
		messageIndex = (messageIndex + 1) % messages.Count; // cycle through messages
		return message;
	}
}



[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
	[HarmonyPrefix] // prefix so it runs before the original method, otherwise will show after moon is finished loading
	[HarmonyPatch("StartGame")]
	public static void OnStartGame()
	{
		string message = Plugin.Instance.GetNextMessage();
		string title = Plugin.Instance.title;
		Plugin.Instance.DebugLog(title, message);

	}
}

/*
// FOR TESTING:
[HarmonyPatch(typeof(StartOfRound))]
public class MoonSelectionPatch
{
	[HarmonyPostfix]
	[HarmonyPatch("ChangeLevel")]
	public static void OnMoonSelection(StartOfRound __instance)
	{
		string currentMoon = __instance.currentLevel.PlanetName;
		
		if (Plugin.lastMoon != currentMoon)
		{
			if (!Plugin.isFirstRun)
			{
				// Get the next message from the list
				string message = Plugin.Instance.GetNextMessage();
				string title = Plugin.Instance.title;
				Plugin.Instance.DebugLog(title, message);
			}
			else
			{
				Plugin.isFirstRun = false;
			}

			Plugin.lastMoon = currentMoon;
		}
	}
}
*/
