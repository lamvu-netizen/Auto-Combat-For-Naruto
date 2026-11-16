using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BigAndSmall;

public class AutoCombat : Mod
{
	public static AutoCombatSettings settings;

	public static Harmony harmony = new Harmony("RedMattis.AutoCombat");

	private static readonly Type patchType;

	private static Vector2 scrollPosition = Vector2.zero;

	public AutoCombat(ModContentPack content)
		: base(content)
	{
		settings = (BS.settings = ((Mod)this).GetSettings<AutoCombatSettings>());
		harmony.PatchAll();
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		Listing_Standard val = new Listing_Standard();
		Rect val2 = GenUI.ContractedBy(inRect, 10f);
		val2.height -= ((Listing)val).CurHeight;
		val2.y += ((Listing)val).CurHeight;
		Widgets.DrawBoxSolid(val2, Color.grey);
		Rect val3 = GenUI.ContractedBy(val2, 1f);
		ColorInt val4 = new ColorInt(42, 43, 44);
		Widgets.DrawBoxSolid(val3, val4.ToColor);
		Rect val5 = GenUI.ContractedBy(val3, 5f);
		Rect val6 = val5;
		val6.x = 0f;
		val6.y = 0f;
		val6.width -= 20f;
		val6.height = 950f;
		Widgets.BeginScrollView(val5, ref scrollPosition, val6, true);
		((Listing)val).Begin(GenUI.AtZero(val6));
		if (val.ButtonText(Translator.Translate("BS_SetToDefault"), (string)null, 1f))
		{
			SetToDefault();
		}
		MakeAutoCombatSettings(val2);
		Widgets.EndScrollView();
	}

	private void BeginScrollArea(Rect inRect, ref Vector2 scrollPos, out Rect viewRect, float height = 600f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Rect val = inRect;
		viewRect = new Rect(0f, 0f, val.width - 16f, height);
		Widgets.BeginScrollView(val, ref scrollPos, viewRect, true);
	}

	private void EndScrollArea()
	{
		Widgets.EndScrollView();
	}

	public void MakeAutoCombatSettings(Rect inRect)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		Listing_Standard val = new Listing_Standard();
		BeginScrollArea(inRect, ref scrollPosition, out var viewRect, 200f);
		((Listing)val).Begin(viewRect);
		((Listing)val).Begin(viewRect);
		((Listing)val).GapLine(12f);
		SettingsWidgets.CreateSettingCheckbox(val, Translator.Translate("BS_AutoCombatResets"), ref settings.autoCombatResets);
		SettingsWidgets.CreateSettingCheckbox(val, Translator.Translate("BS_LongChargeDefaultOff"), ref settings.autoCombatResetsLongCharge);
		((Listing)val).GapLine(12f);
		SettingsWidgets.CreateSettingCheckbox(val, Translator.Translate("BS_ShowMeleeChargeBtn"), ref settings.showMeleeChargeBtn);
		SettingsWidgets.CreateSettingCheckbox(val, Translator.Translate("BS_ShowTakeCoverBtn"), ref settings.showTakeCoverBtn);
		SettingsWidgets.CreateSettingCheckbox(val, Translator.Translate("BS_FullAIControl"), ref settings.showFullAIControlBtn);
		SettingsWidgets.CreateSettingCheckbox(val, Translator.Translate("BS_ShowAutoUseAllAbilitiesBtn"), ref settings.showAutoUseAllAbilitiesBtn);
		SettingsWidgets.CreateRadioButtonsTwoOptions(val, Translator.Translate("BS_RightClickAutoCombat"), ref settings.rightClickAutoCombatShowsMenu, Translator.Translate("BS_RightClickAutoCombat_ShowMenu"), Translator.Translate("BS_RightClickAutoCombat_Toggle"));
		((Listing)val).End();
		EndScrollArea();
	}

	public void SetToDefault()
	{
		settings.autoCombatResets = AutoCombatSettings.defaultAutoCombatResets;
		settings.showMeleeChargeBtn = AutoCombatSettings.defaultShowMeleeChargeBtn;
		settings.showTakeCoverBtn = AutoCombatSettings.defaultShowTakeCoverBtn;
		settings.showAutoUseAllAbilitiesBtn = AutoCombatSettings.defaultShowAutoUseAllAbilitiesBtn;
		settings.rightClickAutoCombatShowsMenu = AutoCombatSettings.defaultRightClickAutoCombatShowsMenu;
	}

	public override string SettingsCategory()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return Translator.Translate("BS_AutoCombatSettings");
	}

	static AutoCombat()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected Obj, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
	}
}
