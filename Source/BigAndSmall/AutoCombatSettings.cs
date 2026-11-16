using Verse;

namespace BigAndSmall;

public class AutoCombatSettings : ModSettings
{
	private static readonly bool defaultEnableDraftedJobs = false;

	public bool enableDraftedJobs = defaultEnableDraftedJobs;

	public static readonly bool defaultAutoCombatResets = false;

	public bool autoCombatResets = defaultAutoCombatResets;

	public static readonly bool defaultAutoCombatResetsLongCharge = false;

	public bool autoCombatResetsLongCharge = defaultAutoCombatResetsLongCharge;

	public static readonly bool defaultShowMeleeChargeBtn = true;

	public bool showMeleeChargeBtn = defaultShowMeleeChargeBtn;

	public static readonly bool defaultShowTakeCoverBtn = true;

	public bool showTakeCoverBtn = defaultShowTakeCoverBtn;

	public static readonly bool defaultShowFullAiControlBtn = true;

	public bool showFullAIControlBtn = defaultShowFullAiControlBtn;

	public static readonly bool defaultShowAutoUseAllAbilitiesBtn = true;

	public bool showAutoUseAllAbilitiesBtn = defaultShowAutoUseAllAbilitiesBtn;

	public static readonly bool defaultRightClickAutoCombatShowsMenu = false;

	public bool rightClickAutoCombatShowsMenu = defaultRightClickAutoCombatShowsMenu;

	public override void ExposeData()
	{
		((ModSettings)this).ExposeData();
		Scribe_Values.Look<bool>(ref autoCombatResets, "autoCombatResets", defaultAutoCombatResets, false);
		Scribe_Values.Look<bool>(ref autoCombatResetsLongCharge, "autoCombatResetsLongCharge", defaultAutoCombatResetsLongCharge, false);
		Scribe_Values.Look<bool>(ref showMeleeChargeBtn, "showMeleeChargeBtn", defaultShowMeleeChargeBtn, false);
		Scribe_Values.Look<bool>(ref showTakeCoverBtn, "showTakeCoverBtn", defaultShowTakeCoverBtn, false);
		Scribe_Values.Look<bool>(ref showFullAIControlBtn, "showFullAIControlBtn", defaultShowFullAiControlBtn, false);
		Scribe_Values.Look<bool>(ref showAutoUseAllAbilitiesBtn, "showAutoUseAllAbilitiesBtn", defaultShowAutoUseAllAbilitiesBtn, false);
		Scribe_Values.Look<bool>(ref rightClickAutoCombatShowsMenu, "rightClickAutoCombatShowsMenu", defaultRightClickAutoCombatShowsMenu, false);
	}
}
