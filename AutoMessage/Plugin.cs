using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AutoMessage;

public static class PluginMetadata
{
	public const string PLUGIN_VERSION = "1.2.0";
	public const string PLUGIN_NAME = "AutoMessage";
	public const string PLUGIN_AUTHOR = "hoffr";
	public const string PLUGIN_GUID = PLUGIN_AUTHOR + "." + PLUGIN_NAME;
}

[BepInPlugin(PluginMetadata.PLUGIN_GUID, PluginMetadata.PLUGIN_NAME, PluginMetadata.PLUGIN_VERSION)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{	
	public static Plugin Instance;
	public BepInEx.Logging.ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(PluginMetadata.PLUGIN_NAME);
	
	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		
		MakeConfig();
		new Harmony(PluginMetadata.PLUGIN_GUID).PatchAll(Assembly.GetExecutingAssembly());
		Log.LogInfo($"{PluginMetadata.PLUGIN_NAME} v{PluginMetadata.PLUGIN_VERSION} loaded.");
	}
	

	public bool randomize;
	public string title;
	public List<string> messages = new List<string>();
	private int messageIndex = 0;
	
	public void MakeConfig()
	{
		var configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{PluginMetadata.PLUGIN_NAME}.cfg"), true); // keeping our config file name simple rather than using the plugin's GUID
		System.Random random = new System.Random();
		
		ConfigEntry<string> randomizeConfig = configFile.Bind("1. Randomize", // Category
			"Randomize", // Name
			"false", // Default
			"true / false - Will scramble the order of messages displayed.\nNote that once it starts looping, that same order will persist.\nThus it's not a true on-the-fly randomization."); // Description
		bool.TryParse(randomizeConfig.Value, out randomize); // convert string to bool
		
		ConfigEntry<string> titleConfig = configFile.Bind("2. Title", // Category
			"Title", // Name
			"AutoMessage", // Default
			"The title text that will show next to all the messages (eg. AutoMessage: Hello world).\nIf left blank, only the message will show.\nSome symbols typed here will automatically be preceded by a backslash after the game runs.\nThis won't show in game and it's okay to omit them when typing"); // Description
		title = titleConfig.Value;
		
		ConfigEntry<string> messageConfig1 = configFile.Bind("3. Messages", // Category
			"Message1", // Name
			"Message 1", // Default
			"Messages will be displayed in sequential order (unless 'randomize' is true) every time the lever is pulled to land, until they loop.\nBlank message entries will be skipped.\nSome symbols will automatically be preceded by a backslash after the game runs.\nThis won't show in game and it's okay to omit them when typing"); // Description
		if (messageConfig1.Value != "")
		{
			messages.Add(messageConfig1.Value);
		}
		
		for (int i = 2; i <= 100; i++)
		{
			ConfigEntry<string> messageConfig2 = configFile.Bind("3. Messages", $"Message{i}", "");
			if (messageConfig2.Value != "")
			{
				messages.Add(messageConfig2.Value);
			}
		}
		
		if (randomize)
		{
			messages = messages.OrderBy(x => random.Next()).ToList(); // scramble message order
		}
	}
	
	
	public string GetNextMessage()
	{
		string message = messages[messageIndex];
		messageIndex = (messageIndex + 1) % messages.Count; // cycle through messages
		return message;
	}
	
	
	public GameObject textObject;
	public TextMeshProUGUI textComponent;
	
	public void SetMessage(string message)
	{
		if (title != "")
		{
			textComponent.text = $"{title}: {message}";
		}
		else
		{
			textComponent.text = $"{message}";
		}
	}
	
	
	public void DisplayText()
	{
		if (textObject == null)
		{
			ConstructGameObject();
		}
		SetMessage(GetNextMessage());
		StartCoroutine(MonitorDarkenScreen());
	}
	
	
	public void ConstructGameObject()
	{
		GameObject canvasUI = GameObject.Find("Systems/UI/Canvas");
		
		if (canvasUI == null)
		{
			Log.LogError("Failed to find Canvas GameObject");
			return;
		}
		
		textObject = new GameObject("AutoMessageText");
		textObject.SetActive(false); // disable while we construct it
		
		RectTransform rectTransform = textObject.AddComponent<RectTransform>();
		rectTransform.SetParent(canvasUI.transform);
		rectTransform.SetAsLastSibling(); // ensure it's the bottom-most child in the parent
		textObject.layer = LayerMask.NameToLayer("UI");
		
		rectTransform.anchoredPosition3D = new Vector3(0f, -200f, 0f);
		rectTransform.localScale = new Vector3(1f, 1f, 1f);
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(500f, 100f);
		
		textComponent = textObject.AddComponent<TextMeshProUGUI>();
		
		// attempt to find and assign the "b" font
		TMP_FontAsset fontAsset = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "b");
		if (fontAsset == null)
		{
			Log.LogError("Font 'b' not found");
			return;
		}
		textComponent.font = fontAsset;

		// apply the first material preset if it exists
		Material fontMaterial = fontAsset.material;
		if (fontMaterial == null)
		{
			Log.LogError("FontMaterial not found");
			return;
		}
		textComponent.fontMaterial = fontMaterial;
		
		textComponent.color = new Color(0.6462264f, 0.95584375f, 1f, 0.5529412f);  // same as lethal's loading text
		textComponent.fontSize = 18;
		textComponent.horizontalAlignment = HorizontalAlignmentOptions.Center;
		textComponent.enableWordWrapping = true;
	}
	
	
	private IEnumerator MonitorDarkenScreen() // wait for darken overlay to appear to show our text, then wait for it to leave to disable our text
	{
		while (true)
		{
			if (HUDManager.Instance.loadingDarkenScreen.enabled == true)
			{
				textObject.SetActive(true);
				break;
			}
			yield return new WaitForSeconds(0.2f);
		}
		
		while (true)
		{
			if (HUDManager.Instance.loadingDarkenScreen.enabled == false)
			{
				textObject.SetActive(false);
				break;
			}
			yield return new WaitForSeconds(0.2f);
		}
	}
}


[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
	[HarmonyPrefix]
	[HarmonyPatch("SceneManager_OnLoad")]
	public static void OnLoad() // harmony patch methods need to be static
	{
		Plugin.Instance.DisplayText();
	}
}
