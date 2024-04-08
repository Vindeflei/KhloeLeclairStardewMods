#nullable enable

using System.Collections.Generic;

using Leclair.Stardew.Common.Inventory;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;

namespace Leclair.Stardew.Common.Crafting;

// Remember to update IBetterCrafting whenever this changes!

/// <summary>
/// An <c>IIngredient</c> represents a single ingredient used when crafting a
/// recipe. An ingredient can be an item, a currency, or anything else.
///
/// The API provides methods for getting basic item and currency ingredients,
/// so you need not use this unless you're doing something fancy.
/// </summary>
public interface IIngredient {
	/// <summary>
	/// Whether or not this <c>IIngredient</c> supports quality control
	/// options, including using low quality first and limiting the maximum
	/// quality to use.
	/// </summary>
	bool SupportsQuality { get; }

	// Display
	/// <summary>
	/// The name of this ingredient to be displayed in the menu.
	/// </summary>
	string DisplayName { get; }

	/// <summary>
	/// The texture to use when drawing this ingredient in the menu.
	/// </summary>
	Texture2D Texture { get; }

	/// <summary>
	/// The source rectangle to use when drawing this ingredient in the menu.
	/// </summary>
	Rectangle SourceRectangle { get; }

	#region Quantity

	/// <summary>
	/// The amount of this ingredient required to perform a craft.
	/// </summary>
	int Quantity { get; }

	/// <summary>
	/// Determine how much of this ingredient is available for crafting both
	/// in the player's inventory and in the other inventories.
	/// </summary>
	/// <param name="who">The farmer performing the craft</param>
	/// <param name="items">A list of all available <see cref="Item"/>s across
	/// all available <see cref="IBCInventory"/> instances. If you only support
	/// consuming ingredients from certain <c>IBCInventory</c> types, you should
	/// not use this value and instead iterate over the inventories. Please
	/// note that this does <b>not</b> include the player's inventory.</param>
	/// <param name="inventories">All the available inventories.</param>
	/// <param name="maxQuality">The maximum item quality we are allowed to
	/// count. This cannot be ignored unless <see cref="SupportsQuality"/>
	/// returns <c>false</c>.</param>
	int GetAvailableQuantity(Farmer who, IList<Item?>? items, IList<IBCInventory>? inventories, int maxQuality);

	#endregion

	#region Consumption

	/// <summary>
	/// Consume this ingredient out of the player's inventory and the other
	/// available inventories.
	/// </summary>
	/// <param name="who">The farmer performing the craft</param>
	/// <param name="inventories">All the available inventories.</param>
	/// <param name="maxQuality">The maximum item quality we are allowed to
	/// count. This cannot be ignored unless <see cref="SupportsQuality"/>
	/// returns <c>false</c>.</param>
	/// <param name="lowQualityFirst">Whether or not we should make an effort
	/// to consume lower quality ingredients before consuming higher quality
	/// ingredients.</param>
	void Consume(Farmer who, IList<IBCInventory>? inventories, int maxQuality, bool lowQualityFirst);

	#endregion
}


public interface IConditionalIngredient {

	/// <summary>
	/// A Game State Query that needs to evaluate to true for this
	/// ingredient to be required by the recipe.
	/// </summary>
	string? Condition { get; }

}


public interface IOptimizedIngredient : IIngredient {

	/// <summary>
	/// Determine if at least a given amount of this ingredient is available.
	/// This should return immediately once the amount has been found for
	/// best performance.
	/// </summary>
	/// <param name="quantity">The required quantity.</param>
	/// <param name="who">The farmer performing the craft</param>
	/// <param name="items">A list of all available <see cref="Item"/>s across
	/// all available <see cref="IBCInventory"/> instances. If you only support
	/// consuming ingredients from certain <c>IBCInventory</c> types, you should
	/// not use this value and instead iterate over the inventories. Please
	/// note that this does <b>not</b> include the player's inventory.</param>
	/// <param name="inventories">All the available inventories.</param>
	/// <param name="maxQuality">The maximum item quality we are allowed to
	/// count. This cannot be ignored unless <see cref="SupportsQuality"/>
	/// returns <c>false</c>.</param>
	bool HasAvailableQuantity(int quantity, Farmer who, IList<Item?>? items, IList<IBCInventory>? inventories, int maxQuality) {
		return GetAvailableQuantity(who, items, inventories, maxQuality) >= quantity;
	}

}
