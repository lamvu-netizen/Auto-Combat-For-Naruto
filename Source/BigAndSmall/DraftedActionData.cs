using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BigAndSmall;

[StaticConstructorOnStartup]
public class DraftedActionData : IExposable
{
	protected delegate void VEFAbilityVerbPostfixDelegate(Pawn __instance, ref Verb __result, Thing target);

	private static readonly bool VEFLoaded = ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");

	private static bool delegatesInitialized = false;

	protected static VEFAbilityVerbPostfixDelegate vefDelegate = null;

	private Pawn pawn = null;

	public string pawnID;

	public bool hunt = false;

	public bool takeCover = false;

	public bool meleeCharge = true;

	public bool fullAIControl = false;

	public List<AbilityDef> autocastAbilities = new List<AbilityDef>();

	public List<string> autocastTMFAbilities = new List<string>();

	public Pawn Pawn
	{
		get
		{
			if (pawn == null)
			{
				IEnumerable<Pawn> source = Find.Maps?.SelectMany((Map x) => x.mapPawns.AllPawns);
				using (IEnumerator<Pawn> enumerator = source.Where((Pawn x) => ((Thing)x).ThingID == pawnID).GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						Pawn current = enumerator.Current;
						pawn = current;
					}
				}
				if (pawn == null)
				{
					Log.Error("DraftedActionData could not find pawn with ID " + pawnID + ".");
				}
			}
			return pawn;
		}
	}

	public static Verb TryGetVEFAbilityVerb(Pawn pawn, Thing target)
	{
		if (VEFLoaded && !delegatesInitialized)
		{
			InitializeVEFDelegates();
		}
		if (vefDelegate != null)
		{
			Verb __result = null;
			vefDelegate(pawn, ref __result, target);
			return __result;
		}
		return null;
	}

	public static void InitializeVEFDelegates()
	{
		if (delegatesInitialized)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("VEF.Abilities.VanillaExpandedFramework_Pawn_TryGetAttackVerb_Patch");
			MethodInfo method = AccessTools.Method(type, "Postfix", (Type[])null, (Type[])null);
			VEFAbilityVerbPostfixDelegate vEFAbilityVerbPostfixDelegate = (VEFAbilityVerbPostfixDelegate)Delegate.CreateDelegate(typeof(VEFAbilityVerbPostfixDelegate), method);
			vefDelegate = vEFAbilityVerbPostfixDelegate;
			delegatesInitialized = true;
		}
		catch (Exception arg)
		{
			Log.Error($"Big and Small failed to initialize VEF Ability Verb delegate: {arg}");
		}
	}

	public DraftedActionData(Pawn pawn)
	{
		this.pawn = pawn;
		pawnID = ((Thing)pawn).ThingID;
	}

	public DraftedActionData()
	{
	}

	private void RefreshDraft()
	{
		Pawn.jobs.EndCurrentJob((JobCondition)16, true, true);
	}

	public bool ToggleCoverMode(bool? force = null)
	{
		bool flag;
		if (force.HasValue)
		{
			bool valueOrDefault = force == true;
			flag = valueOrDefault;
		}
		else
		{
			flag = !takeCover;
		}
		bool flag2 = flag;
		takeCover = flag2;
		RefreshDraft();
		return takeCover;
	}

	public bool ToggleMeleeCharge(bool? force = null)
	{
		bool flag;
		if (force.HasValue)
		{
			bool valueOrDefault = force == true;
			flag = valueOrDefault;
		}
		else
		{
			flag = !meleeCharge;
		}
		bool flag2 = flag;
		meleeCharge = flag2;
		RefreshDraft();
		return meleeCharge;
	}

	public bool ToggleHuntMode()
	{
		hunt = !hunt;
		RefreshDraft();
		return hunt;
	}

	public bool ToggleFullAIControl(bool? force = null)
	{
		bool flag;
		if (force.HasValue)
		{
			bool valueOrDefault = force == true;
			flag = valueOrDefault;
		}
		else
		{
			flag = !fullAIControl;
		}
		bool flag2 = flag;
		fullAIControl = flag2;
		RefreshDraft();
		return fullAIControl;
	}

	public bool AutoCastFor(AbilityDef def)
	{
		return autocastAbilities.Contains(def);
	}

	public bool AutoCastFor(string defName)
	{
		if (string.IsNullOrEmpty(defName)) return false;
		return autocastTMFAbilities.Contains(defName);
	}

	public void ToggleAutoCastFor(string defName)
	{
		if (string.IsNullOrEmpty(defName)) return;
		if (autocastTMFAbilities.Contains(defName))
		{
			autocastTMFAbilities.Remove(defName);
		}
		else
		{
			autocastTMFAbilities.Add(defName);
		}
		RefreshDraft();
	}

	public void ToggleAutoForAll(bool? force = null)
	{
		bool flag;
		if (force.HasValue)
		{
			bool valueOrDefault = force == true;
			flag = valueOrDefault;
		}
		else
		{
			flag = GenCollection.Empty<AbilityDef>(autocastAbilities) && (autocastTMFAbilities.Count == 0);
		}
		if (flag)
		{
			Pawn val = Pawn;
			object obj;
			if (val == null)
			{
				obj = null;
			}
			else
			{
				Pawn_AbilityTracker abilities = val.abilities;
				obj = ((abilities != null) ? abilities.AllAbilitiesForReading : null);
			}
			if (obj != null)
			{
				foreach (Ability item in Pawn.abilities.AllAbilitiesForReading)
				{
					if (item.def.aiCanUse || GenCollection.Any<AutoCombatExtension>(item.def.ExtensionsOnDef<AutoCombatExtension, AbilityDef>((List<Type>)null, (List<Type>)null, true), (Predicate<AutoCombatExtension>)((AutoCombatExtension x) => x.canMassToggle)))
					{
						autocastAbilities.Add(item.def);
					}
				}
			}
			if (TMFAutoCombatIntegration.IsAvailable)
			{
				TMFAutoCombatIntegration.ToggleAutoForAll(Pawn, this, true);
			}
			goto IL_00fe;
		}
		autocastAbilities.Clear();
		if (TMFAutoCombatIntegration.IsAvailable)
		{
			TMFAutoCombatIntegration.ToggleAutoForAll(Pawn, this, false);
		}
		goto IL_00fe;
		IL_00fe:
		RefreshDraft();
	}

	public void ToggleAutoCastFor(AbilityDef def)
	{
		if (autocastAbilities.Contains(def))
		{
			autocastAbilities.Remove(def);
		}
		else
		{
			autocastAbilities.Add(def);
		}
		RefreshDraft();
	}

	public void ExposeData()
	{
		Scribe_Values.Look<string>(ref pawnID, "pawnID", (string)null, false);
		Scribe_Values.Look<bool>(ref hunt, "huntMode", false, false);
		Scribe_Values.Look<bool>(ref takeCover, "takeCoverMode", false, false);
		Scribe_Values.Look<bool>(ref meleeCharge, "meleeChargeMode", true, false);
		Scribe_Values.Look<bool>(ref fullAIControl, "fullAIControl", false, false);
		Scribe_Collections.Look<AbilityDef>(ref autocastAbilities, "autocastAbilities", (LookMode)4, Array.Empty<object>());
		Scribe_Collections.Look<string>(ref autocastTMFAbilities, "autocastTMFAbilities", LookMode.Value, Array.Empty<object>());
		if (autocastTMFAbilities == null)
		{
			autocastTMFAbilities = new List<string>();
		}
	}
}
