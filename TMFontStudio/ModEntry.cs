using System;

using Leclair.Stardew.Common.Events;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;

using Leclair.Stardew.ThemeManager;

using Leclair.Stardew.Common;

using Leclair.Stardew.ThemeManagerFontStudio.Menus;
using Leclair.Stardew.ThemeManagerFontStudio.Models;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Leclair.Stardew.ThemeManagerFontStudio;

public class ModEntry : ModSubscriber {

	public static ModEntry Instance { get; set; } = null!;

	public ModConfig Config = null!;

	public Theme Theme = new();

	internal IThemeManagerApi? TMApi;
	internal IThemeManager<Theme>? ThemeManager;

	internal Managers.SourceManager SourceManager = null!;

	#region Life Cycle

	public override void Entry(IModHelper helper) {
		base.Entry(helper);
		SpriteHelper.SetHelper(Helper);
		RenderHelper.SetHelper(Helper);

		Instance = this;

		SourceManager = new(this);

		// Read Configuration
		Config = Helper.ReadConfig<ModConfig>();

		// I18n
		I18n.Init(Helper.Translation);
	}

	[Subscriber]
	private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
		try {
			TMApi = Helper.ModRegistry.GetApi<IThemeManagerApi>("leclair.thememanager");
		} catch(Exception ex) {
			Log($"Unable to get Theme Manager API. Certain features will not work.", LogLevel.Warn, ex);
		}

		if (TMApi is not null) {
			VariableSetConverter.SetConverter(TMApi.VariableSetConverter);

			// Recreate the default theme now that we have Theme Manager's API, and thus
			// the ability to create variable sets.
			Theme = new();

			ThemeManager = TMApi.GetOrCreateManager<Theme>(defaultTheme: Theme, onThemeChanged: OnThemeChanged);
			Theme = ThemeManager.ActiveTheme;
		}
	}

	#endregion

	#region Events

	private void OnThemeChanged(object? sender, IThemeChangedEvent<Theme> e) {
		Theme = e.NewData;

		Log($"Theme: {Theme}", LogLevel.Error);
		Log($"--  Manager: {Theme.Manager}", LogLevel.Info);
		Log($"-- Manifest: {Theme.Manifest}", LogLevel.Info);
		Log($"   -- UniqueId: {Theme.Manifest?.UniqueID}", LogLevel.Info);
		Log($"-- Colors: {Theme.ColorVariables}", LogLevel.Info);
		if (Theme.ColorVariables is null)
			Log($"   -- none", LogLevel.Debug);
		else
			foreach(var entry in Theme.ColorVariables)
				Log($"   -- {entry.Key}: {entry.Value}", LogLevel.Debug);

		Log($"-- Textures: {Theme.TextureVariables}", LogLevel.Info);
		if (Theme.TextureVariables is null)
			Log($"   -- none", LogLevel.Debug);
		else
			foreach (string entry in Theme.TextureVariables.Keys)
				Log($"   -- {entry}: {Theme.TextureVariables[entry]}", LogLevel.Debug);
	}

	[Subscriber]
	private void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
		if (e.Button == SButton.F10 && Game1.activeClickableMenu is null)
			Game1.activeClickableMenu = new FontEditorMenu(this);
	}

	#endregion

}
