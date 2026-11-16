using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BigAndSmall;

[HarmonyPatch]
[StaticConstructorOnStartup]
public static class DraftGizmos
{
	public static readonly Texture2D AutoCastIcon = ContentFinder<Texture2D>.Get("BS_UI/Auto_Tiny", true);

	public static readonly Texture2D HuntIcon = ContentFinder<Texture2D>.Get("BS_UI/Hunt", true);

	public static readonly Texture2D TakeCoverIcon = ContentFinder<Texture2D>.Get("BS_UI/TakeCover", true);

	public static readonly Texture2D MeleeChargeIcon = ContentFinder<Texture2D>.Get("BS_UI/MeleeCharge", true);

	public static readonly Texture2D AIControlIcon = ContentFinder<Texture2D>.Get("BS_UI/AIControl", true);

	public static readonly Texture2D AutoUseAllIcon = ContentFinder<Texture2D>.Get("BS_UI/Auto_Large", true);

	public static bool IsDraftedPlayerPawn(Pawn pawn)
	{
		return ((pawn != null) ? ((Thing)pawn).Faction : null) == Faction.OfPlayerSilentFail && pawn.Drafted;
	}

	[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
	[HarmonyPostfix]
	[HarmonyPriority(200)]
	public static IEnumerable<Gizmo> GetGizmosPostfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
	{
		Pawn pawn = __instance.pawn;
		if (IsDraftedPlayerPawn(pawn))
		{
			Command_ToggleWithRClick item = AddHuntGizmo(pawn);
			List<Command_ToggleWithRClick> list = new List<Command_ToggleWithRClick>(1) { item };
			DraftedActionData data = DraftedActionHolder.GetData(pawn);
			if (data.hunt)
			{
				if (BS.settings.showTakeCoverBtn)
				{
					Command_ToggleWithRClick item2 = AddCoverGizmo(pawn);
					list.Add(item2);
				}
				if (BS.settings.showMeleeChargeBtn)
				{
					Command_ToggleWithRClick item3 = AddChargeGizmo(pawn, data.fullAIControl);
					list.Add(item3);
				}
				if (BS.settings.showAutoUseAllAbilitiesBtn)
				{
					Command_ToggleWithRClick item4 = AddToggleAllAbilitiesButton(pawn);
					list.Add(item4);
				}
				if (BS.settings.showFullAIControlBtn)
				{
					Command_ToggleWithRClick item5 = AddAIControlGizmo(pawn);
					list.Add(item5);
				}
				return UpdateEnumerable(__result, list);
			}
			return UpdateEnumerable(__result, new List<Command_ToggleWithRClick>(1) { item });
		}
		return __result;
		static IEnumerable<Gizmo> UpdateEnumerable(IEnumerable<Gizmo> gizmos, List<Command_ToggleWithRClick> commands)
		{
			foreach (Gizmo gizmo in gizmos)
			{
				Command_Toggle draftCommand = (Command_Toggle)(object)((gizmo is Command_Toggle) ? gizmo : null);
				if (draftCommand != null && (UnityEngine.Object)(object)((Command)draftCommand).icon == (UnityEngine.Object)(object)TexCommand.Draft)
				{
					yield return (Gizmo)(object)draftCommand;
					foreach (Command_ToggleWithRClick command in commands)
					{
						yield return (Gizmo)(object)command;
					}
				}
				else
				{
					yield return gizmo;
				}
			}
		}
	}

	private static Command_ToggleWithRClick AddToggleAllAbilitiesButton(Pawn pawn)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		return new Command_ToggleWithRClick
		{
			defaultLabel = Translator.Translate("BS_AutoUseAllAbilitiesLabel"),
			defaultDesc = Translator.Translate("BS_AutoUseAllAbilitiesDescription"),
			icon = (Texture)(object)AutoUseAllIcon,
			isActive = () => !GenCollection.Empty<AbilityDef>(DraftedActionHolder.GetData(pawn).autocastAbilities) || DraftedActionHolder.GetData(pawn).autocastTMFAbilities.Count > 0,
			toggleAction = () =>
			{
				DraftedActionHolder.GetData(pawn).ToggleAutoForAll();
			},
			rightClickAction = () =>
			{
			},
			activateSound = SoundDefOf.Click,
			groupKey = 6173616,
			hotKey = KeyBindingDefOf.Misc6
		};
	}

	private static Command_ToggleWithRClick AddChargeGizmo(Pawn pawn, bool disabled)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		Command_ToggleWithRClick command_ToggleWithRClick = new Command_ToggleWithRClick
		{
			defaultLabel = Translator.Translate("BS_MeleeChargeLabel"),
			defaultDesc = Translator.Translate("BS_MeleeChargeDescription"),
			icon = (Texture)(object)MeleeChargeIcon,
			isActive = () => DraftedActionHolder.GetData(pawn).meleeCharge,
			toggleAction = () =>
			{
				DraftedActionHolder.GetData(pawn).ToggleMeleeCharge();
			},
			rightClickAction = () =>
			{
			}
		};
		((Gizmo)command_ToggleWithRClick).Disabled = disabled;
		((Command)command_ToggleWithRClick).activateSound = SoundDefOf.Click;
		((Command)command_ToggleWithRClick).groupKey = 6173615;
		((Command)command_ToggleWithRClick).hotKey = KeyBindingDefOf.Misc5;
		return command_ToggleWithRClick;
	}

	private static Command_ToggleWithRClick AddCoverGizmo(Pawn pawn)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		return new Command_ToggleWithRClick
		{
			defaultLabel = Translator.Translate("BS_TakeCoverLabel"),
			defaultDesc = Translator.Translate("BS_TakeCoverDescription"),
			icon = (Texture)(object)TakeCoverIcon,
			isActive = () => DraftedActionHolder.GetData(pawn).takeCover,
			toggleAction = () =>
			{
				DraftedActionHolder.GetData(pawn).ToggleCoverMode();
			},
			rightClickAction = () =>
			{
			},
			activateSound = SoundDefOf.Click,
			groupKey = 6173614
		};
	}

	private static Command_ToggleWithRClick AddHuntGizmo(Pawn pawn)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		Action rightClickAction = ((!BS.settings.rightClickAutoCombatShowsMenu) ? ((Action)(() =>
		{
			DraftedActionData data = DraftedActionHolder.GetData(pawn);
			data.ToggleAutoForAll();
		})) : ((Action)(() =>
		{
			ShowHuntContextMenu(pawn);
		})));
		return new Command_ToggleWithRClick
		{
			defaultLabel = Translator.Translate("BS_DraftHuntLabel"),
			defaultDesc = Translator.Translate("BS_HuntDescription"),
			icon = (Texture)(object)HuntIcon,
			isActive = () => DraftedActionHolder.GetData(pawn).hunt,
			toggleAction = () =>
			{
				DraftedActionHolder.GetData(pawn).ToggleHuntMode();
			},
			rightClickAction = rightClickAction,
			activateSound = SoundDefOf.Click,
			groupKey = 6173613,
			hotKey = KeyBindingDefOf.Misc3
		};
	}

	private static Command_ToggleWithRClick AddAIControlGizmo(Pawn pawn)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		return new Command_ToggleWithRClick
		{
			defaultLabel = Translator.Translate("BS_FullAIControlLabel"),
			defaultDesc = Translator.Translate("BS_FullAIControlDescription"),
			icon = (Texture)(object)AIControlIcon,
			isActive = () => DraftedActionHolder.GetData(pawn).fullAIControl,
			toggleAction = () =>
			{
				DraftedActionHolder.GetData(pawn).ToggleFullAIControl();
			},
			rightClickAction = () =>
			{
			},
			activateSound = SoundDefOf.Click,
			groupKey = 6173612,
			hotKey = KeyBindingDefOf.Misc4
		};
	}

	private static void ShowHuntContextMenu(Pawn pawn)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected Obj, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected Obj, but got Unknown
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Expected Obj, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Expected Obj, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Expected Obj, but got Unknown
		bool takeCoverActive = DraftedActionHolder.GetData(pawn).takeCover;
		bool meleeChargeActive = DraftedActionHolder.GetData(pawn).meleeCharge;
		bool autoUseAllAbilitiesActive = DraftedActionHolder.GetData(pawn).autocastAbilities.Count > 0 || DraftedActionHolder.GetData(pawn).autocastTMFAbilities.Count > 0;
		List<FloatMenuOption> list = new List<FloatMenuOption>(4)
		{
			new FloatMenuOption(Translator.Translate("BS_TakeCoverLabel" + " " + (DraftedActionHolder.GetData(pawn).takeCover ? Translator.Translate("BS_IsEnabledP") : Translator.Translate("BS_IsDisabledP"))), (Action)(() =>
			{
				foreach (Pawn selectedPawn in Find.Selector.SelectedPawns)
				{
					DraftedActionHolder.GetData(selectedPawn).ToggleCoverMode(!takeCoverActive);
				}
			}), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0),
			new FloatMenuOption(Translator.Translate("BS_MeleeChargeLabel" + " " + (DraftedActionHolder.GetData(pawn).meleeCharge ? Translator.Translate("BS_IsEnabledP") : Translator.Translate("BS_IsDisabledP"))), (Action)(() =>
			{
				foreach (Pawn selectedPawn2 in Find.Selector.SelectedPawns)
				{
					DraftedActionHolder.GetData(selectedPawn2).ToggleMeleeCharge(!meleeChargeActive);
				}
			}), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0),
			new FloatMenuOption(Translator.Translate("BS_FullAIControlLabel" + " " + (DraftedActionHolder.GetData(pawn).fullAIControl ? Translator.Translate("BS_IsEnabledP") : Translator.Translate("BS_IsDisabledP"))), (Action)(() =>
			{
				foreach (Pawn selectedPawn3 in Find.Selector.SelectedPawns)
				{
					DraftedActionHolder.GetData(selectedPawn3).ToggleFullAIControl(!DraftedActionHolder.GetData(selectedPawn3).fullAIControl);
				}
			}), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0),
			new FloatMenuOption(Translator.Translate("BS_AutoUseAllAbilitiesLabel" + " " + ((DraftedActionHolder.GetData(pawn).autocastAbilities.Count > 0) ? Translator.Translate("BS_IsEnabledP") : Translator.Translate("BS_IsDisabledP"))), (Action)(() =>
			{
				foreach (Pawn selectedPawn4 in Find.Selector.SelectedPawns)
				{
					DraftedActionHolder.GetData(selectedPawn4).ToggleAutoForAll(!autoUseAllAbilitiesActive);
				}
			}), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0)
		};
		Find.WindowStack.Add((Window)new FloatMenu(list));
	}

	[HarmonyPatch(typeof(Command), "GizmoOnGUIInt")]
	[HarmonyPostfix]
	public static void GizmoOnGUIPostfix(Command __instance, GizmoResult __result, Rect butRect, GizmoRenderParms parms)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Invalid comparison between Unknown and I4
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (__instance is Command_Ability val)
		{
			Pawn pawn = val.Pawn;
			if (IsDraftedPlayerPawn(pawn))
			{
				DraftedActionData data = DraftedActionHolder.GetData(pawn);
				if (data.AutoCastFor(val.Ability.def))
				{
					float num = (parms.shrunk ? 12f : 24f);
					Rect val2 = new Rect(butRect.x + butRect.width - num, butRect.y, num, num);
					GUI.DrawTexture(val2, (Texture)(object)AutoCastIcon);
				}
				if ((int)__result.State == 3)
				{
					data.ToggleAutoCastFor(val.Ability.def);
				}
			}
			return;
		}

		if (TMFAutoCombatIntegration.IsAvailable)
		{
			TMFAutoCombatIntegration.HandleGizmo(__instance, __result, butRect, parms, AutoCastIcon);
		}
	}

	[HarmonyPatch(typeof(PriorityWork), "ClearPrioritizedWorkAndJobQueue")]
	[HarmonyPostfix]
	public static void ClearPrioritizedWorkAndJobQueuePostfix(PriorityWork __instance)
	{
		if (BS.settings.autoCombatResets && !__instance.pawn.Drafted)
		{
			Pawn pawn = __instance.pawn;
			DraftedActionData data = DraftedActionHolder.GetData(pawn);
			if (data != null)
			{
				data.hunt = false;
			}
		}
		if (BS.settings.autoCombatResetsLongCharge && !__instance.pawn.Drafted)
		{
			Pawn pawn2 = __instance.pawn;
			DraftedActionData data2 = DraftedActionHolder.GetData(pawn2);
			if (data2 != null)
			{
				data2.meleeCharge = false;
			}
		}
	}
}
