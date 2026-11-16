using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BigAndSmall;

[StaticConstructorOnStartup]
public static class TMFAutoCombatIntegration
{
	private static bool initialized = false;
	private static bool isAvailable = false;

	// Cached Types
	private static Type compAbilitiesType;
	private static Type abilityType;
	private static Type abilityDefType;
	private static Type abilityTierDefType;
	private static Type tmagicUtilsType;
	private static Type targetDataType;
	private static Type aiAbilityWorkerType;
	private static Type hediffPropsType;

	// Cached PropertyInfos and FieldInfos
	private static PropertyInfo allLearnedAbilitiesProp;
	private static FieldInfo pawnField;
	private static FieldInfo defField;
	private static PropertyInfo abilityTierProp;
	private static PropertyInfo verbsProp;
	private static PropertyInfo activeProp;
	private static PropertyInfo energyCostProp;
	private static FieldInfo abilityResourceField;
	private static FieldInfo energyField;
	private static FieldInfo cooldownPeriodField;
	private static FieldInfo lastActivatedTickField;
	private static PropertyInfo hasChargeProp;
	private static PropertyInfo curChargesProp;
	private static FieldInfo curTargetField;
	private static PropertyInfo targetingParametersProp;
	private static FieldInfo autocastEnabledField;

	// AbilityTierDef fields
	private static FieldInfo jobDefField;
	private static FieldInfo effectRadiusField;
	private static FieldInfo requireLineOfSightField;
	private static FieldInfo instantActionField;
	private static PropertyInfo aiAbilityWorkerProp;
	private static FieldInfo hediffPropsField;
	private static FieldInfo hediffField;

	// Methods
	private static MethodInfo makeJobAbilityMethod;
	private static MethodInfo startMethod;
	private static MethodInfo doCastMethod;
	private static MethodInfo canBeActivatedMethod;
	private static MethodInfo canBeActivatedValidatorMethod;
	private static MethodInfo workerCanActivateMethod;

	public static bool IsAvailable
	{
		get
		{
			if (!initialized)
			{
				Initialize();
			}
			return isAvailable;
		}
	}

	public static void Initialize()
	{
		if (initialized) return;
		initialized = true;

		compAbilitiesType = AccessTools.TypeByName("TaranMagicFramework.CompAbilities");
		if (compAbilitiesType == null)
		{
			isAvailable = false;
			return;
		}

		try
		{
			abilityType = AccessTools.TypeByName("TaranMagicFramework.Ability");
			abilityDefType = AccessTools.TypeByName("TaranMagicFramework.AbilityDef");
			abilityTierDefType = AccessTools.TypeByName("TaranMagicFramework.AbilityTierDef");
			tmagicUtilsType = AccessTools.TypeByName("TaranMagicFramework.TMagicUtils");
			targetDataType = AccessTools.TypeByName("TaranMagicFramework.TargetData");
			aiAbilityWorkerType = AccessTools.TypeByName("TaranMagicFramework.AIAbilityWorker");
			hediffPropsType = AccessTools.TypeByName("TaranMagicFramework.HediffProps");

			allLearnedAbilitiesProp = AccessTools.Property(compAbilitiesType, "AllLearnedAbilities");
			pawnField = AccessTools.Field(abilityType, "pawn");
			defField = AccessTools.Field(abilityType, "def");
			abilityTierProp = AccessTools.Property(abilityType, "AbilityTier");
			verbsProp = AccessTools.Property(abilityType, "Verbs");
			activeProp = AccessTools.Property(abilityType, "Active");
			energyCostProp = AccessTools.Property(abilityType, "EnergyCost");
			abilityResourceField = AccessTools.Field(abilityType, "abilityResource");
			cooldownPeriodField = AccessTools.Field(abilityType, "cooldownPeriod");
			lastActivatedTickField = AccessTools.Field(abilityType, "lastActivatedTick");
			hasChargeProp = AccessTools.Property(abilityType, "HasCharge");
			curChargesProp = AccessTools.Property(abilityType, "CurCharges");
			curTargetField = AccessTools.Field(abilityType, "curTarget");
			targetingParametersProp = AccessTools.Property(abilityType, "TargetingParameters");
			autocastEnabledField = AccessTools.Field(abilityType, "autocastEnabled");

			Type resourceType = AccessTools.TypeByName("TaranMagicFramework.AbilityResource");
			if (resourceType != null)
			{
				energyField = AccessTools.Field(resourceType, "energy");
			}

			if (abilityTierDefType != null)
			{
				jobDefField = AccessTools.Field(abilityTierDefType, "jobDef");
				effectRadiusField = AccessTools.Field(abilityTierDefType, "effectRadius");
				requireLineOfSightField = AccessTools.Field(abilityTierDefType, "requireLineOfSight");
				instantActionField = AccessTools.Field(abilityTierDefType, "instantAction");
				aiAbilityWorkerProp = AccessTools.Property(abilityTierDefType, "AIAbilityWorker");
				hediffPropsField = AccessTools.Field(abilityTierDefType, "hediffProps");
			}

			if (hediffPropsType != null)
			{
				hediffField = AccessTools.Field(hediffPropsType, "hediff");
			}

			if (tmagicUtilsType != null)
			{
				makeJobAbilityMethod = AccessTools.Method(tmagicUtilsType, "MakeJobAbility", new Type[] { abilityType, typeof(LocalTargetInfo) });
			}

			startMethod = AccessTools.Method(abilityType, "Start", new Type[] { typeof(bool) });
			doCastMethod = AccessTools.Method(abilityType, "DoCast", new Type[] { typeof(LocalTargetInfo) });
			canBeActivatedMethod = AccessTools.Method(abilityType, "CanBeActivated", new Type[] {
				typeof(float), typeof(string).MakeByRefType(), typeof(bool), typeof(Func<string>)
			});
			canBeActivatedValidatorMethod = AccessTools.Method(abilityType, "CanBeActivatedValidator");

			if (aiAbilityWorkerType != null && targetDataType != null)
			{
				workerCanActivateMethod = AccessTools.Method(aiAbilityWorkerType, "CanActivate", new Type[] {
					abilityType, targetDataType.MakeByRefType()
				});
			}

			isAvailable = true;
			Log.Message("[AutoCombat] Successfully initialized integration with Taran Magic Framework (Naruto & Naruto Expanded).");
		}
		catch (Exception ex)
		{
			isAvailable = false;
			Log.Warning($"[AutoCombat] Failed to initialize TMF integration: {ex}");
		}
	}

	public static ThingComp GetCompAbilities(Pawn pawn)
	{
		if (!IsAvailable || pawn == null || pawn.AllComps == null) return null;
		for (int i = 0; i < pawn.AllComps.Count; i++)
		{
			ThingComp comp = pawn.AllComps[i];
			if (comp != null && comp.GetType().FullName == "TaranMagicFramework.CompAbilities")
			{
				return comp;
			}
		}
		return null;
	}

	public static object ResolveAbility(Command instance, Pawn contextPawn = null)
	{
		if (instance == null || abilityType == null) return null;

		// 1. Check property Ability (public or non-public)
		PropertyInfo prop = instance.GetType().GetProperty("Ability", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (prop != null)
		{
			try
			{
				object val = prop.GetValue(instance, null);
				if (val != null && abilityType.IsAssignableFrom(val.GetType())) return val;
			}
			catch { }
		}

		// 2. Check field ability or Ability
		FieldInfo fi = instance.GetType().GetField("ability", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					?? instance.GetType().GetField("Ability", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (fi != null)
		{
			try
			{
				object val = fi.GetValue(instance);
				if (val != null && abilityType.IsAssignableFrom(val.GetType())) return val;
			}
			catch { }
		}

		// 3. Check delegate closures (for Command_Action or Command_Toggle)
		if (instance is Command_Action ca && ca.action != null)
		{
			object ab = ExtractAbilityFromTarget(ca.action.Target);
			if (ab != null) return ab;
		}
		if (instance is Command_Toggle ct && ct.toggleAction != null)
		{
			object ab = ExtractAbilityFromTarget(ct.toggleAction.Target);
			if (ab != null) return ab;
		}

		// 4. Try matching with learned abilities on selected pawn
		Pawn selPawn = contextPawn ?? (Find.Selector?.SingleSelectedThing as Pawn);
		if (selPawn != null)
		{
			ThingComp comp = GetCompAbilities(selPawn);
			if (comp != null)
			{
				IEnumerable allAbilities = allLearnedAbilitiesProp?.GetValue(comp, null) as IEnumerable;
				if (allAbilities != null)
				{
					foreach (object ab in allAbilities)
					{
						Def def = defField?.GetValue(ab) as Def;
						if (def == null) continue;

						if (!string.IsNullOrEmpty(instance.defaultLabel))
						{
							if (string.Equals(instance.defaultLabel, def.label, StringComparison.OrdinalIgnoreCase) ||
								string.Equals(instance.defaultLabel, def.defName, StringComparison.OrdinalIgnoreCase))
							{
								return ab;
							}

							MethodInfo labelMethod = ab.GetType().GetMethod("AbilityLabel", BindingFlags.Public | BindingFlags.Instance);
							if (labelMethod != null)
							{
								try
								{
									string lbl = labelMethod.Invoke(ab, null) as string;
									if (!string.IsNullOrEmpty(lbl) && string.Equals(instance.defaultLabel, lbl, StringComparison.OrdinalIgnoreCase))
									{
										return ab;
									}
								}
								catch { }
							}
						}

						if (instance.icon != null)
						{
							MethodInfo iconMethod = ab.GetType().GetMethod("AbilityIcon", BindingFlags.Public | BindingFlags.Instance);
							if (iconMethod != null)
							{
								try
								{
									Texture2D ic = iconMethod.Invoke(ab, null) as Texture2D;
									if (ic != null && ic == instance.icon)
									{
										return ab;
									}
								}
								catch { }
							}
						}
					}
				}
			}
		}

		return null;
	}

	private static object ExtractAbilityFromTarget(object target)
	{
		if (target == null || abilityType == null) return null;
		if (abilityType.IsAssignableFrom(target.GetType()))
		{
			return target;
		}
		FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		foreach (FieldInfo f in fields)
		{
			if (abilityType.IsAssignableFrom(f.FieldType))
			{
				try
				{
					object val = f.GetValue(target);
					if (val != null) return val;
				}
				catch { }
			}
		}
		return null;
	}

	public static bool HandleGizmo(Command instance, GizmoResult result, Rect butRect, GizmoRenderParms parms, Texture2D autoCastIcon)
	{
		if (!IsAvailable || instance == null) return false;

		object abilityObj = ResolveAbility(instance);
		if (abilityObj == null) return false;

		Pawn pawn = pawnField?.GetValue(abilityObj) as Pawn;
		if (pawn == null) pawn = Find.Selector?.SingleSelectedThing as Pawn;
		Def def = defField?.GetValue(abilityObj) as Def;
		if (pawn == null || def == null) return false;

		if (DraftGizmos.IsDraftedPlayerPawn(pawn))
		{
			DraftedActionData data = DraftedActionHolder.GetData(pawn);
			if (data != null)
			{
				if (data.AutoCastFor(def.defName))
				{
					float num = parms.shrunk ? 12f : 24f;
					Rect val2 = new Rect(butRect.x + butRect.width - num, butRect.y, num, num);
					GUI.DrawTexture(val2, (Texture)(object)autoCastIcon);
				}

				if ((int)result.State == 3) // GizmoState.RightClicked
				{
					data.ToggleAutoCastFor(def.defName);
					if (autocastEnabledField != null)
					{
						autocastEnabledField.SetValue(abilityObj, data.AutoCastFor(def.defName));
					}
				}
			}
			return true;
		}

		return false;
	}

	public static void ToggleAutoForAll(Pawn pawn, DraftedActionData data, bool? force = null)
	{
		if (!IsAvailable || pawn == null || data == null) return;

		ThingComp comp = GetCompAbilities(pawn);
		if (comp == null) return;

		IEnumerable allAbilities = allLearnedAbilitiesProp?.GetValue(comp, null) as IEnumerable;
		if (allAbilities == null) return;

		bool turnOn = force.HasValue ? force.Value : (data.autocastTMFAbilities.Count == 0);

		if (turnOn)
		{
			foreach (object ability in allAbilities)
			{
				Def def = defField?.GetValue(ability) as Def;
				if (def != null && IsEligibleForAutoCombat(ability))
				{
					if (!data.autocastTMFAbilities.Contains(def.defName))
					{
						data.autocastTMFAbilities.Add(def.defName);
					}
					if (autocastEnabledField != null)
					{
						autocastEnabledField.SetValue(ability, true);
					}
				}
			}
		}
		else
		{
			data.autocastTMFAbilities.Clear();
			foreach (object ability in allAbilities)
			{
				if (autocastEnabledField != null)
				{
					autocastEnabledField.SetValue(ability, false);
				}
			}
		}
	}

	public static bool HasAnyAutocastEnabled(DraftedActionData data)
	{
		return data != null && data.autocastTMFAbilities != null && data.autocastTMFAbilities.Count > 0;
	}

	public static bool IsEligibleForAutoCombat(object ability)
	{
		if (ability == null) return false;
		string typeName = ability.GetType().Name;
		if (typeName.Contains("Passive")) return false;

		Def def = defField?.GetValue(ability) as Def;
		if (def == null) return false;

		// Skip non-combat utilities if any
		if (def.defName == "WN_SummonWoodMaterials") return false;

		return true;
	}

	public static bool CanBeActivated(object ability)
	{
		if (ability == null) return false;
		Pawn pawn = pawnField?.GetValue(ability) as Pawn;
		if (pawn == null || pawn.Downed || pawn.MentalState != null) return false;

		// Check cooldown
		if (lastActivatedTickField != null && cooldownPeriodField != null)
		{
			int lastTick = (int)lastActivatedTickField.GetValue(ability);
			int cd = (int)cooldownPeriodField.GetValue(ability);
			if (lastTick > 0 && Find.TickManager.TicksGame - lastTick < cd)
			{
				return false;
			}
		}

		// Check charges
		if (hasChargeProp != null && curChargesProp != null)
		{
			if ((bool)hasChargeProp.GetValue(ability, null))
			{
				int charges = (int)curChargesProp.GetValue(ability, null);
				if (charges <= 0) return false;
			}
		}

		// Check energy / chakra
		if (abilityResourceField != null && energyField != null && energyCostProp != null)
		{
			object res = abilityResourceField.GetValue(ability);
			if (res != null)
			{
				float curEnergy = (float)energyField.GetValue(res);
				float cost = (float)energyCostProp.GetValue(ability, null);
				if (curEnergy < cost) return false;
			}
		}

		// Call CanBeActivated method
		if (canBeActivatedMethod != null)
		{
			try
			{
				Func<string> validator = canBeActivatedValidatorMethod?.Invoke(ability, null) as Func<string>;
				float cost = energyCostProp != null ? (float)energyCostProp.GetValue(ability, null) : 0f;
				object[] args = new object[] { cost, null, false, validator };
				bool result = (bool)canBeActivatedMethod.Invoke(ability, args);
				return result;
			}
			catch
			{
				return true;
			}
		}

		return true;
	}

	public static bool IsActive(object ability)
	{
		if (ability == null) return false;
		if (activeProp != null)
		{
			try
			{
				if ((bool)activeProp.GetValue(ability, null)) return true;
			}
			catch { }
		}

		Pawn pawn = pawnField?.GetValue(ability) as Pawn;
		if (pawn != null && pawn.health != null && abilityTierProp != null)
		{
			object tier = abilityTierProp.GetValue(ability, null);
			if (tier != null && hediffPropsField != null)
			{
				object hp = hediffPropsField.GetValue(tier);
				if (hp != null && hediffField != null)
				{
					HediffDef hd = hediffField.GetValue(hp) as HediffDef;
					if (hd != null && pawn.health.hediffSet.HasHediff(hd))
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	public static bool IsSelfBuffOrToggle(object ability)
	{
		if (ability == null) return false;
		string typeName = ability.GetType().Name;
		if (typeName.Contains("Toggle") || typeName.Contains("Mode") || typeName.Contains("Sharingan") || 
			typeName.Contains("Byakugan") || typeName.Contains("Gates") || typeName.Contains("Cloak") || 
			typeName.Contains("Mantle") || typeName.Contains("Armour") || typeName.Contains("Armor") || 
			typeName.Contains("State") || typeName.Contains("Form") || typeName.Contains("Sage") || 
			typeName.Contains("Aura") || typeName.Contains("PathShift") || typeName.Contains("Ascendant") ||
			typeName.Contains("Summon") || typeName.Contains("Clone") || typeName.Contains("CurseMark") ||
			typeName.Contains("Curse") || typeName.Contains("Substitute") || typeName.Contains("Susanoo") ||
			typeName.Contains("Transformation") || typeName.Contains("Transform") || typeName.Contains("ClayConstruct") ||
			typeName.Contains("Beast") || typeName.Contains("Buff") || typeName.Contains("Stance") ||
			typeName.Contains("Golem"))
		{
			return true;
		}

		TargetingParameters tp = targetingParametersProp?.GetValue(ability, null) as TargetingParameters;
		if (tp != null)
		{
			if (tp.canTargetSelf && !tp.canTargetLocations && !tp.canTargetAnimals && !tp.canTargetHumans)
			{
				return true;
			}
			if (tp.canTargetItems && !tp.canTargetPawns)
			{
				return true;
			}
		}

		return false;
	}

	public static float GetAbilityRange(object ability, object tier)
	{
		IEnumerable verbs = verbsProp?.GetValue(ability, null) as IEnumerable;
		if (verbs != null)
		{
			foreach (object v in verbs)
			{
				if (v is Verb verb && verb.verbProps != null && verb.verbProps.range > 0f)
				{
					return verb.verbProps.range;
				}
			}
		}

		if (tier != null && effectRadiusField != null)
		{
			float rad = (float)effectRadiusField.GetValue(tier);
			if (rad > 0f) return rad;
		}

		return 25f;
	}

	public static Job TryGetTMFAbilityJob(Pawn pawn, Thing enemyTarget, DraftedActionData actionData)
	{
		if (!IsAvailable || pawn == null || !pawn.Spawned || enemyTarget == null || enemyTarget.Destroyed || actionData == null) return null;
		if (actionData.autocastTMFAbilities == null || actionData.autocastTMFAbilities.Count == 0) return null;

		ThingComp comp = GetCompAbilities(pawn);
		if (comp == null) return null;

		IEnumerable allAbilities = allLearnedAbilitiesProp?.GetValue(comp, null) as IEnumerable;
		if (allAbilities == null) return null;

		List<object> candidateAbilities = new List<object>();
		foreach (object ab in allAbilities)
		{
			Def def = defField?.GetValue(ab) as Def;
			if (def != null && actionData.AutoCastFor(def.defName))
			{
				candidateAbilities.Add(ab);
			}
		}

		if (candidateAbilities.Count == 0) return null;

		// 1. Prioritize self-buffs/stances that are not yet active
		foreach (object ability in candidateAbilities)
		{
			if (IsSelfBuffOrToggle(ability) && !IsActive(ability))
			{
				if (CanBeActivated(ability))
				{
					Job buffJob = ActivateSelfBuff(ability, pawn);
					if (buffJob != null)
					{
						return buffJob;
					}
				}
			}
		}

		// 2. Try offensive abilities on enemyTarget
		foreach (object ability in candidateAbilities)
		{
			if (IsSelfBuffOrToggle(ability)) continue;

			if (CanBeActivated(ability))
			{
				Job offensiveJob = TryCastOffensiveAbility(ability, pawn, enemyTarget);
				if (offensiveJob != null)
				{
					return offensiveJob;
				}
			}
		}

		return null;
	}

	private static Job ActivateSelfBuff(object ability, Pawn pawn)
	{
		object tier = abilityTierProp?.GetValue(ability, null);
		JobDef jobDef = tier != null && jobDefField != null ? jobDefField.GetValue(tier) as JobDef : null;

		LocalTargetInfo castTarget = (LocalTargetInfo)(Thing)pawn;
		TargetingParameters tp = targetingParametersProp?.GetValue(ability, null) as TargetingParameters;
		if (tp != null)
		{
			if (tp.canTargetItems && !tp.canTargetPawns)
			{
				Thing chunk = GenClosest.ClosestThingReachable(
					pawn.Position, pawn.Map,
					ThingRequest.ForGroup(ThingRequestGroup.Chunk),
					PathEndMode.Touch,
					TraverseParms.For(pawn),
					25f,
					(Thing t) => tp.CanTarget((TargetInfo)t)
				);
				if (chunk != null)
				{
					castTarget = new LocalTargetInfo(chunk);
				}
			}
			else if (tp.canTargetLocations && !tp.canTargetSelf && !tp.canTargetPawns)
			{
				castTarget = new LocalTargetInfo(pawn.Position + pawn.Rotation.FacingCell);
			}
		}

		if (jobDef != null && makeJobAbilityMethod != null)
		{
			return (Job)makeJobAbilityMethod.Invoke(null, new object[] { ability, castTarget });
		}

		if (doCastMethod != null)
		{
			doCastMethod.Invoke(ability, new object[] { castTarget });
		}
		else
		{
			curTargetField?.SetValue(ability, castTarget);
			startMethod?.Invoke(ability, new object[] { true });
		}
		return JobMaker.MakeJob(JobDefOf.Wait_Combat, 15, true);
	}

	private static Job TryCastOffensiveAbility(object ability, Pawn pawn, Thing enemyTarget)
	{
		object tier = abilityTierProp?.GetValue(ability, null);
		if (tier == null) return null;

		// 1. Try AI worker if present
		object worker = aiAbilityWorkerProp?.GetValue(tier, null);
		if (worker != null && workerCanActivateMethod != null)
		{
			object[] workerParams = new object[] { ability, null };
			try
			{
				bool canAct = (bool)workerCanActivateMethod.Invoke(worker, workerParams);
				if (canAct && workerParams[1] != null)
				{
					object targetData = workerParams[1];
					Verb workerVerb = AccessTools.Field(targetDataType, "verb")?.GetValue(targetData) as Verb;
					LocalTargetInfo workerTarget = (LocalTargetInfo)AccessTools.Field(targetDataType, "target").GetValue(targetData);

					if (workerVerb != null)
					{
						return GetJobFromVerb(workerVerb, workerTarget);
					}
					if (workerTarget.IsValid)
					{
						JobDef jd = jobDefField?.GetValue(tier) as JobDef;
						if (jd != null && makeJobAbilityMethod != null)
						{
							return (Job)makeJobAbilityMethod.Invoke(null, new object[] { ability, workerTarget });
						}
						if (doCastMethod != null)
						{
							doCastMethod.Invoke(ability, new object[] { workerTarget });
						}
						else
						{
							curTargetField?.SetValue(ability, workerTarget);
							startMethod?.Invoke(ability, new object[] { true });
						}
						return JobMaker.MakeJob(JobDefOf.Wait_Combat, 20, true);
					}
				}
			}
			catch
			{
				// Worker threw, proceed to generic check
			}
		}

		// 2. Check ability verbs
		IEnumerable verbs = verbsProp?.GetValue(ability, null) as IEnumerable;
		if (verbs != null)
		{
			foreach (object vObj in verbs)
			{
				if (vObj is Verb verb && verb.Available())
				{
					if (verb.CanHitTarget((LocalTargetInfo)enemyTarget))
					{
						if (verb.verbProps.IsMeleeAttack)
						{
							if (pawn.Position.InHorDistOf(enemyTarget.Position, 2f))
							{
								return GetJobFromVerb(verb, (LocalTargetInfo)enemyTarget);
							}
						}
						else
						{
							return GetJobFromVerb(verb, (LocalTargetInfo)enemyTarget);
						}
					}
				}
			}
		}

		// 3. Fallback: Range and targeting parameters
		float maxRange = GetAbilityRange(ability, tier);
		if (pawn.Position.InHorDistOf(enemyTarget.Position, maxRange))
		{
			bool losRequired = true;
			if (requireLineOfSightField != null)
			{
				losRequired = (bool)requireLineOfSightField.GetValue(tier);
			}

			if (!losRequired || GenSight.LineOfSight(pawn.Position, enemyTarget.Position, pawn.Map))
			{
				TargetingParameters tp = targetingParametersProp?.GetValue(ability, null) as TargetingParameters;
				LocalTargetInfo castTarget = (LocalTargetInfo)enemyTarget;
				if (tp != null && !tp.canTargetPawns && tp.canTargetLocations)
				{
					castTarget = new LocalTargetInfo(enemyTarget.Position);
				}

				if (tp == null || tp.CanTarget((TargetInfo)castTarget.ToTargetInfo(pawn.Map)))
				{
					JobDef jd = jobDefField?.GetValue(tier) as JobDef;
					if (jd != null && makeJobAbilityMethod != null)
					{
						return (Job)makeJobAbilityMethod.Invoke(null, new object[] { ability, castTarget });
					}
					if (doCastMethod != null)
					{
						doCastMethod.Invoke(ability, new object[] { castTarget });
					}
					else
					{
						curTargetField?.SetValue(ability, castTarget);
						startMethod?.Invoke(ability, new object[] { true });
					}
					return JobMaker.MakeJob(JobDefOf.Wait_Combat, 20, true);
				}
			}
		}

		return null;
	}

	public static Job GetJobFromVerb(Verb verb, LocalTargetInfo target)
	{
		MethodInfo getVerbJobMethod = verb.GetType().GetMethod("GetVerbJob", new Type[] { typeof(LocalTargetInfo) });
		if (getVerbJobMethod != null)
		{
			try
			{
				return (Job)getVerbJobMethod.Invoke(verb, new object[] { target });
			}
			catch
			{
			}
		}

		if (verb.verbProps.IsMeleeAttack)
		{
			Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
			job.verbToUse = verb;
			job.playerForced = true;
			return job;
		}
		else
		{
			Job job = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStatic, target);
			job.verbToUse = verb;
			return job;
		}
	}

	public static Verb TryGetAttackVerb(Pawn pawn, Thing target, DraftedActionData actionData)
	{
		if (!IsAvailable || pawn == null || target == null || actionData == null) return null;
		if (actionData.autocastTMFAbilities == null || actionData.autocastTMFAbilities.Count == 0) return null;

		ThingComp comp = GetCompAbilities(pawn);
		if (comp == null) return null;

		IEnumerable allAbilities = allLearnedAbilitiesProp?.GetValue(comp, null) as IEnumerable;
		if (allAbilities == null) return null;

		foreach (object ab in allAbilities)
		{
			Def def = defField?.GetValue(ab) as Def;
			if (def != null && actionData.AutoCastFor(def.defName))
			{
				if (CanBeActivated(ab))
				{
					IEnumerable verbs = verbsProp?.GetValue(ab, null) as IEnumerable;
					if (verbs != null)
					{
						foreach (object v in verbs)
						{
							if (v is Verb verb && verb.Available() && verb.CanHitTarget((LocalTargetInfo)target))
							{
								return verb;
							}
						}
					}
				}
			}
		}

		return null;
	}

	public static bool CanTargetWithAbilities(Pawn pawn, Thing target, DraftedActionData actionData, out Verb pickedVerb)
	{
		pickedVerb = null;
		if (!IsAvailable || pawn == null || target == null || actionData == null) return false;
		if (actionData.autocastTMFAbilities == null || actionData.autocastTMFAbilities.Count == 0) return false;

		ThingComp comp = GetCompAbilities(pawn);
		if (comp == null) return false;

		IEnumerable allAbilities = allLearnedAbilitiesProp?.GetValue(comp, null) as IEnumerable;
		if (allAbilities == null) return false;

		foreach (object ab in allAbilities)
		{
			Def def = defField?.GetValue(ab) as Def;
			if (def != null && actionData.AutoCastFor(def.defName))
			{
				if (CanBeActivated(ab))
				{
					IEnumerable verbs = verbsProp?.GetValue(ab, null) as IEnumerable;
					if (verbs != null)
					{
						foreach (object v in verbs)
						{
							if (v is Verb verb && verb.Available() && verb.CanHitTarget((LocalTargetInfo)target))
							{
								pickedVerb = verb;
								return true;
							}
						}
					}
					else
					{
						object tier = abilityTierProp?.GetValue(ab, null);
						float maxRange = GetAbilityRange(ab, tier);
						bool losRequired = true;
						if (tier != null && requireLineOfSightField != null)
						{
							losRequired = (bool)requireLineOfSightField.GetValue(tier);
						}

						if (pawn.Position.InHorDistOf(target.Position, maxRange) && (!losRequired || GenSight.LineOfSight(pawn.Position, target.Position, pawn.Map)))
						{
							return true;
						}
					}
				}
			}
		}

		return false;
	}
}
