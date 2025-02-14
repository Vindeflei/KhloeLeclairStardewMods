
#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;

using Leclair.Stardew.Common;
using Leclair.Stardew.Common.Types;
using Leclair.Stardew.Common.UI;
using Leclair.Stardew.Common.UI.FlowNode;
using Leclair.Stardew.Common.UI.SimpleLayout;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Utilities;
using StardewValley;

using Leclair.Stardew.Almanac.Menus;

namespace Leclair.Stardew.Almanac.Pages;

public class NoticesPage : BasePage<BaseState>, ICalendarPage {

	private IFlowNode[]? Nodes;
	private List<NPC>?[]? Birthdays;
	private List<SpriteInfo>?[]? Sprites;

	private readonly Dictionary<NPC, SpriteInfo> Portraits = new();
	private readonly Dictionary<NPC, SpriteInfo> Heads = new();

	private Dictionary<string, Models.NPCOverride>? Overrides;

	//private WorldDate HoveredDate;
	//private Cache<ISimpleNode, WorldDate> CalendarTip;

	#region Lifecycle

	public static NoticesPage? GetPage(AlmanacMenu menu, ModEntry mod) {
		if (!mod.Config.ShowNotices)
			return null;

		return new(menu, mod);
	}

	public NoticesPage(AlmanacMenu menu, ModEntry mod) : base(menu, mod) {

		LoadOverrides();

	}

	public void LoadOverrides() {
		Overrides = Game1.content.Load<Dictionary<string, Models.NPCOverride>>(AssetManager.NPCOverridesPath);
	}

	#endregion

	#region Logic

	private SpriteInfo? GetPortrait(NPC? npc) {
		if (npc is null)
			return null;

		if (Portraits.TryGetValue(npc, out SpriteInfo? sprite))
			return sprite;

		Texture2D texture;
		try {
			texture = Game1.content.Load<Texture2D>(@"Characters\" + npc.getTextureName());
		} catch (Exception) {
			texture = npc.Sprite.Texture;
		}

		// Spenny integration. Very important.
		if (npc.Name == "Penny" && (Mod.Helper.ModRegistry.IsLoaded("spacechase0.SpennyLite") || Mod.Helper.ModRegistry.IsLoaded("spacechase0.Spenny")))
			sprite = new SpriteInfo(
				texture,
				new Rectangle(0, 0, 16, 128),
				baseFrames: 4,
				framesPerRow: 1,
				framePadY: 8,
				frameTime: 133
			);
		else
			sprite = new SpriteInfo(
				texture,
				new Rectangle(0, 0, 16, 24)
			);

		Portraits[npc] = sprite;
		return sprite;
	}

	private SpriteInfo? GetHead(NPC? npc) {
		if (npc is null)
			return null;

		if (Heads.TryGetValue(npc, out SpriteInfo? sprite))
			return sprite;

		Texture2D texture;
		try {
			texture = Game1.content.Load<Texture2D>(@"Characters\" + npc.getTextureName());
		} catch (Exception) {
			texture = npc.Sprite.Texture;
		}

		Models.HeadSize? info = null;

		if (Overrides != null && Overrides.TryGetValue(npc.Name, out var ovr))
			info = ovr.Head;

		if (info == null)
			Mod.HeadSizes?.TryGetValue(npc.Name, out info);

		sprite = new SpriteInfo(
			texture,
			new Rectangle(
				info?.OffsetX ?? 0,
				info?.OffsetY ?? 0,
				info?.Width ?? 16,
				info?.Height ?? 15
			)
		);

		Heads[npc] = sprite;
		return sprite;
	}

	public override void Update() {
		base.Update();

		Nodes = new IFlowNode[ModEntry.DaysPerMonth];
		Birthdays = new List<NPC>?[ModEntry.DaysPerMonth];
		Sprites = new List<SpriteInfo>?[ModEntry.DaysPerMonth];
		WorldDate date = new(Menu.Date);

		FlowBuilder builder = new();

		builder.FormatText(
				I18n.Page_Notices(),
				fancy: true,
				align: Alignment.HCenter
			);

		// Build a map of this month's birthdays.
		List<NPC>? chars = null;
		try {
			chars = Utility.getAllCharacters();
		} catch (Exception ex) {
			Mod.Log($"Unable to load list of characters: {ex}", StardewModdingAPI.LogLevel.Error);
			builder
				.Text("\n\n")
				.Sprite(new SpriteInfo(Game1.mouseCursors, new Rectangle(268, 470, 16, 16)), scale: 2f)
				.Text(" ")
				.Text(I18n.Page_Notices_BirthdayError(), color: Color.Red);
		}

		if (chars is not null)
			foreach (NPC npc in chars)
				if (npc.IsVillager && date.Season.Equals(npc.Birthday_Season)) {
					// Don't show villagers we can't socialize with.
					if (!npc.CanSocialize && !Game1.player.friendshipData.ContainsKey(npc.Name))
						continue;

					// Don't show forbidden villagers.
					if (Overrides != null && Overrides.TryGetValue(npc.Name, out var ovr) && !ovr.Visible)
						continue;

					int day = npc.Birthday_Day - 1;

					// Sanity check the birthday for being valid.
					if (day >= Birthdays.Length)
						continue;

					if (Birthdays[day] is null)
						Birthdays[day] = new();

					Birthdays[day]!.Add(npc);
				}

		for (int day = 1; day <= ModEntry.DaysPerMonth; day++) {

			FlowBuilder db = new();

			// Other Events
			date.DayOfMonth = day;
			List<SpriteInfo> sprites = new();

			foreach (var evt in Mod.Notices.GetEventsForDate(0, date)) {
				if (evt == null)
					continue;

				bool has_simple = !string.IsNullOrEmpty(evt.SimpleLabel);
				bool has_line = has_simple || evt.AdvancedLabel != null;

				Func<IFlowNodeSlice, int, int, bool>? onHover = null;

				if (has_line) {
					db.Text("\n");
					if (evt.Item != null)
						onHover = (_, _, _) => {
							Menu.HoveredItem = evt.Item;
							return true;
						};
				}

				if (evt.Sprite != null) {
					sprites.Add(evt.Sprite);

					if (has_line)
						db
							.Sprite(evt.Sprite, 3f, onHover: onHover)
							.Text(" ");
				}

				if (evt.AdvancedLabel != null)
					db.AddRange(evt.AdvancedLabel);
				else if (has_simple)
					db.FormatText(
						evt.SimpleLabel!,
						align: Alignment.VCenter,
						onHover: onHover,
						noComponent: true
					);
			}

			Sprites[day - 1] = sprites.Count > 0 ? sprites : null;

			// Birthdays
			List<NPC>? birthdays = Birthdays[day - 1];
			if (birthdays != null) {
				foreach (NPC npc in birthdays) {
					char last = npc.displayName.Last<char>();

					bool no_s = last == 's' ||
						LocalizedContentManager.CurrentLanguageCode ==
						LocalizedContentManager.LanguageCode.de &&
							(last == 'x' || last == 'ß' || last == 'z');

					var name = new Common.UI.FlowNode.TextNode(
						npc.displayName,
						align: Alignment.VCenter
					);

					db
						.Text("\n")
						.Sprite(GetHead(npc), 3f)
						.Text(" ")
						.Translate(
							Mod.Helper.Translation.Get(
								no_s ?
									"page.notices.birthday.no-s" :
									"page.notices.birthday.s"
							),
							new { name },
							align: Alignment.VCenter
						);

					if (Mod.Config.DebugMode)
						db.Text($" (#{npc.Name})", align: Alignment.VCenter);
				}
			}

			if (db.Count == 0)
				continue;

			SDate sdate = new(day, date.Season);

			var node = new Common.UI.FlowNode.TextNode(
				sdate.ToLocaleString(withYear: false),
				new TextStyle(
					font: Game1.dialogueFont
				)
			);

			Nodes[day - 1] = node;

			builder
				.Text("\n\n")
				.Add(node)
				.AddRange(db.Build());
		}

		SetRightFlow(
			builder.Build(),
			4,
			0
		);
	}

	#endregion

	#region ITab

	public override int SortKey => 11;
	public override string TabSimpleTooltip => I18n.Page_Notices();

	public override Texture2D TabTexture => Game1.mouseCursors;
	public override Rectangle? TabSource => new(208, 320, 16, 16);

	#endregion

	#region IAlmanacPage

	public override void Refresh() {
		LoadOverrides();
		Heads.Clear();
		Portraits.Clear();

		base.Refresh();
	}

	#endregion

	#region ICalendarPage

	public bool ShouldDimPastCells => true;
	public bool ShouldHighlightToday => true;

	public void DrawUnderCell(SpriteBatch b, WorldDate date, Rectangle bounds) {
		List<SpriteInfo>? sprites = Sprites?[date.DayOfMonth - 1];
		List<NPC>? bdays = Birthdays?[date.DayOfMonth - 1];

		double ms = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;

		if (sprites != null) {
			if (sprites.Count > 1 && sprites.Count < 4 && bdays == null) {
				for (int i = 0; i < sprites.Count; i++) {
					sprites[i]?.Draw(
						b,
						new Vector2(
							bounds.X + bounds.Width - 36,
							bounds.Y + 4 + (i * 36)
						),
						2f
					);
				}

			} else {
				int to_show = Math.Min(sprites.Count, bdays == null ? 3 : 1);
				int idx = (int) (ms / Mod.Config.CycleTime) % (int) Math.Ceiling(sprites.Count / (float) to_show) * to_show;

				for (int i = 0; i < to_show; i++) {
					if (i + idx >= sprites.Count)
						break;

					SpriteInfo sprite = sprites[(idx + i) % sprites.Count];
					sprite?.Draw(
						b,
						new Vector2(
							bounds.X + bounds.Width - 36,
							bounds.Y + 4 + (i * 36)
						),
						2f
					);
				}
			}
		}

		if (bdays != null) {
			int idx = (int) (ms / Mod.Config.CycleTime) % bdays.Count;

			NPC? npc = bdays?[idx];
			SpriteInfo? sprite = GetPortrait(npc);
			sprite?.Draw(
				b,
				new Vector2(
					bounds.X + 4,
					bounds.Y + bounds.Height - 72
				),
				3f,
				new Vector2(16, 24)
			);
		}
	}

	public void DrawOverCell(SpriteBatch b, WorldDate date, Rectangle bounds) {

	}

	public bool ReceiveCellLeftClick(int x, int y, WorldDate date, Rectangle bounds) {
		int day = date.DayOfMonth - 1;

		while (day >= 0) {
			// Do we have something click-worthy?
			if ((Sprites?[day]?.Count ?? 0) == 0 && (Birthdays?[day]?.Count ?? 0) == 0)
				return false;

			if (Nodes?[day] is IFlowNode node) {
				if (Menu.ScrollRightFlow(node))
					Game1.playSound("shiny4");
				return true;
			}

			day--;
		}

		return false;
	}

	public bool ReceiveCellRightClick(int x, int y, WorldDate date, Rectangle bounds) {
		return false;
	}

	public void PerformCellHover(int x, int y, WorldDate date, Rectangle bounds) {
		//HoveredDate = date;
		//Menu.HoverNode = CalendarTip.Value;
	}

	#endregion

}

