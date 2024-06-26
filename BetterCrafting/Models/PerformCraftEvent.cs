#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Leclair.Stardew.BetterCrafting.Menus;
using Leclair.Stardew.Common.Crafting;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Menus;

namespace Leclair.Stardew.BetterCrafting.Models;

public class PerformCraftEvent : IGlobalPerformCraftEvent {

	public IRecipe Recipe { get; }
	public Farmer Player { get; }
	public Item? Item { get; set; }
	public IClickableMenu Menu { get; }

	public bool IsDone { get; private set; }
	public bool Success { get; private set; }

	public Action? OnDone { get; internal set; }

	public PerformCraftEvent(IRecipe recipe, Farmer who, Item? item, IClickableMenu menu) {
		Recipe = recipe;
		Player = who;
		Item = item;
		Menu = menu;
	}

	public void Cancel() {
		if (!IsDone) {
			IsDone = true;
			Success = false;
			OnDone?.Invoke();
		}
	}

	public void Complete() {
		if (!IsDone) {
			IsDone = true;
			Success = true;
			OnDone?.Invoke();
		}
	}
}


public class ChainedPerformCraftHandler {

	public readonly Action<IGlobalPerformCraftEvent>[] Handlers;

	public readonly IRecipe Recipe;
	public readonly Farmer Player;
	public readonly BetterCraftingPage Menu;

	public Item? Item;

	public readonly Action<ChainedPerformCraftHandler> OnDone;

	private bool finished = false;
	private bool success = true;
	private int current = 0;
	private PerformCraftEvent? currentEvent;

	public ChainedPerformCraftHandler(ModEntry mod, IRecipe recipe, Farmer who, Item? item, BetterCraftingPage menu, Action<ChainedPerformCraftHandler> onDone) {

		List<Action<IGlobalPerformCraftEvent>> handlers = new();

		foreach (var api in mod.APIInstances.Values)
			handlers.AddRange(api.GetPerformCraftHooks());

		handlers.Add(recipe.PerformCraft);
		Handlers = handlers.ToArray();

		Recipe = recipe;
		Player = who;
		Item = item;
		Menu = menu;
		OnDone = onDone;

		Process();
	}

	// We're done when we've had a non-success, or we've run out of handlers.
	public bool IsDone => ! success || current >= Handlers.Length;
	public bool Success => success;

	private void Finish() {
		current++;
		if (currentEvent is not null) {
			Item = currentEvent.Item;
			success = currentEvent.Success;
		}

		Process();
	}

	private void Process() {

		if (IsDone) {
			if (!finished)
				OnDone(this);
			finished = true;
			return;
		}

		currentEvent = new PerformCraftEvent(Recipe, Player, Item, Menu);

		Handlers[current].Invoke(currentEvent);

		if (currentEvent.IsDone)
			Finish();

		// We don't set this immediately to avoid weird double events.
		currentEvent.OnDone = Finish;

	}


}
