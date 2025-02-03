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
using System.Collections;
using System.Collections.Generic;

namespace AutoMessage;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance;
	
	private static readonly BepInEx.Logging.ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("AutoMessage");
	
	// config
	public bool randomize;
	public string title;
	public List<string> messages = new List<string>();
	
	static System.Random random = new System.Random();
	
	private static int messageIndex = 0;
	
	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		
		MakeConfig();
		
		new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll(Assembly.GetExecutingAssembly());

		Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
	}
	
	public void MakeConfig()
	{
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
	private static readonly BepInEx.Logging.ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("AutoMessage");
	
	private static RectTransform foundObjectRectTransform;
	private static GameObject foundObject;
	
	private static Vector3 startingPosition;
	private static int count;
	
	[HarmonyPrefix] // prefix so it runs before the original method, otherwise will show after moon is finished loading
	[HarmonyPatch("SceneManager_OnLoad")]
	public static void OnStartGame()
	{
		// adjust tips panel size and positioning so it's more visible behind the overlays that appear and so more text can fit
		// this is inefficient ugly code but also i am very tired
		foundObject = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/SpecialHUDGraphics/HintPanelContainer/Image");
		if (foundObject != null)
		{
			foundObjectRectTransform = foundObject.GetComponent<RectTransform>();
			if (foundObjectRectTransform != null)
			{
				foundObjectRectTransform.anchorMin = new Vector2(0.6f, 1.1f); // high enough to not be covered by a scanner text box
				foundObjectRectTransform.anchorMax = new Vector2(0.6f, 1.1f);
				foundObjectRectTransform.sizeDelta = new Vector2(345.8f, 200f);
			}
		}
		foundObject = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/SpecialHUDGraphics/HintPanelContainer/Image/Header");
		if (foundObject != null)
		{
			foundObjectRectTransform = foundObject.GetComponent<RectTransform>();
			if (foundObjectRectTransform != null)
			{
				foundObjectRectTransform.anchorMin = new Vector2(0.5f, 0.72f);
				foundObjectRectTransform.anchorMax = new Vector2(0.5f, 0.72f);
			}
		}
		foundObject = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/SpecialHUDGraphics/HintPanelContainer/Image/BodyText");
		if (foundObject != null)
		{
			foundObjectRectTransform = foundObject.GetComponent<RectTransform>();
			if (foundObjectRectTransform != null)
			{
				foundObjectRectTransform.anchorMin = new Vector2(0.5f, 0.7f);
				foundObjectRectTransform.anchorMax = new Vector2(0.5f, 0.7f);
			}
		}
		
		string message = Plugin.Instance.GetNextMessage();
		string title = Plugin.Instance.title;
		Plugin.Instance.DebugLog(title, message);
		
		GameObject tempGameObject = new GameObject("TempMonoBehaviour");
		MonoBehaviour tempMonoBehaviour = tempGameObject.AddComponent<TempCoroutineRunner>();
		tempMonoBehaviour.StartCoroutine(CheckLocalPositionCoroutine()); // wait for the panel to disappear, then reset its values
	}
	
	private static IEnumerator CheckLocalPositionCoroutine()
	{
		yield return new WaitForSeconds(3f); // wait long enough so that the panel animation stops twitching so we can get a concrete position
		
		foundObject = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/SpecialHUDGraphics/HintPanelContainer/Image");
		if (foundObject != null)
		{
			startingPosition = foundObject.transform.localPosition;
			
			count = 0;
			while (foundObject.transform.localPosition == startingPosition) // once the panel's position has changed that means it's done with its animation aka invisible
			{
				yield return new WaitForSeconds(0.2f);
				count++;
				if (count >= 50)
				{
					break; // timeout
				}
			}
			
			foundObject = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/SpecialHUDGraphics/HintPanelContainer/Image");
			if (foundObject != null)
			{
				foundObjectRectTransform = foundObject.GetComponent<RectTransform>();
				if (foundObjectRectTransform != null)
				{
					foundObjectRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
					foundObjectRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
					foundObjectRectTransform.sizeDelta = new Vector2(345.8f, 100f);
				}
			}
			foundObject = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/SpecialHUDGraphics/HintPanelContainer/Image/Header");
			if (foundObject != null)
			{
				foundObjectRectTransform = foundObject.GetComponent<RectTransform>();
				if (foundObjectRectTransform != null)
				{
					foundObjectRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
					foundObjectRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
				}
			}
			foundObject = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/SpecialHUDGraphics/HintPanelContainer/Image/BodyText");
			if (foundObject != null)
			{
				foundObjectRectTransform = foundObject.GetComponent<RectTransform>();
				if (foundObjectRectTransform != null)
				{
					foundObjectRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
					foundObjectRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
				}
			}
		}
	}
	
	private class TempCoroutineRunner : MonoBehaviour { } // temporary class to run coroutines also i barely know what i'm doing also i hope you're having a great day
}
