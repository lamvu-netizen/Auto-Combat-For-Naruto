using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BigAndSmall;

public class JobGiver_AutoCombat : JobGiver_AIFightEnemy
{
	public DraftedActionData actionData = null;

	public List<AbilityDef> blacklist = new List<AbilityDef>();

	public bool Hunt => actionData.hunt;

	public override bool OnlyUseAbilityVerbs => !actionData.hunt;

	public override bool OnlyUseRangedSearch => false;

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		return (ThinkNode)(object)(JobGiver_AutoCombat)(object)((JobGiver_AIFightEnemy)this).DeepCopy(resolve);
	}

	public override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
	{
		return TryFindShootinPositionInner(pawn, out dest, verbToUse);
	}

	protected bool TryFindShootinPositionInner(Pawn pawn, out IntVec3 dest, Verb verbToUse, bool requestNewPos = false)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		dest = ((Thing)pawn).Position;
		if (Hunt)
		{
			Thing enemyTarget = pawn.mindState.enemyTarget;
			CastPositionRequest val = new CastPositionRequest
			{
				caster = pawn,
				target = enemyTarget,
				wantCoverFromTarget = actionData.takeCover,
				preferredCastPosition = (requestNewPos ? null : (IntVec3?)pawn.Position)
			};
			if (verbToUse == null && CanTargetWithAbillities(pawn, enemyTarget, out var pickedAbility))
			{
				val.verb = pickedAbility.verb;
				val.maxRangeFromTarget = pickedAbility.verb.verbProps.range - 0.5f;
				return CastPositionFinder.TryFindCastPosition(val, out dest);
			}
			if (verbToUse == null && TMFAutoCombatIntegration.IsAvailable && TMFAutoCombatIntegration.CanTargetWithAbilities(pawn, enemyTarget, actionData, out var pickedTmfVerb))
			{
				if (pickedTmfVerb != null)
				{
					val.verb = pickedTmfVerb;
					val.maxRangeFromTarget = pickedTmfVerb.verbProps.range - 0.5f;
					return CastPositionFinder.TryFindCastPosition(val, out dest);
				}
			}
			if (verbToUse != null)
			{
				val.verb = verbToUse;
				val.maxRangeFromTarget = verbToUse.verbProps.range - 0.5f;
				return CastPositionFinder.TryFindCastPosition(val, out dest);
			}
		}
		return true;
	}

	public override bool ExtraTargetValidator(Pawn pawn, Thing target)
	{
		if (pawn == null || !pawn.Drafted)
		{
			return false;
		}
		Ability pickedAbility;
		if (((JobGiver_AIFightEnemy)this).ExtraTargetValidator(pawn, target))
		{
			return Hunt || CanTargetWithAbillities(pawn, target, out pickedAbility);
		}
		return false;
	}

	public override Job TryGiveJob(Pawn pawn)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		if (pawn == null || !pawn.Drafted)
		{
			return null;
		}
		actionData = DraftedActionHolder.GetData(pawn);
		if (!Hunt)
		{
			bool hasAutocast = !GenCollection.Empty<AbilityDef>(actionData.autocastAbilities)
				|| (TMFAutoCombatIntegration.IsAvailable && TMFAutoCombatIntegration.HasAnyAutocastEnabled(actionData));
			if (!hasAutocast)
			{
				return null;
			}
			goto IL_00a1;
		}
		goto IL_00a1;
		IL_00a1:
		HostilityResponseMode hostilityResponse = pawn.playerSettings.hostilityResponse;
		pawn.playerSettings.hostilityResponse = (HostilityResponseMode)1;
		Job val = GiveDraftedHuntJob(pawn);
		pawn.playerSettings.hostilityResponse = hostilityResponse;
		if (val != null)
		{
			val.checkOverrideOnExpire = true;
			return val;
		}
		return GetWaitForTimeJob(pawn, 100);
	}

	protected static Job GetWaitForTimeJob(Pawn pawn, int ticks)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		Job val = JobMaker.MakeJob(JobDefOf.Wait_Combat, (LocalTargetInfo)(((Thing)pawn).Position));
		val.expiryInterval = ticks;
		val.checkOverrideOnExpire = true;
		return val;
	}

	public override bool ShouldLoseTarget(Pawn pawn)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		Thing enemyTarget = pawn.mindState.enemyTarget;
		float num = (actionData.fullAIControl ? 999f : base.targetKeepRadius);
		if (enemyTarget.Destroyed || Find.TickManager.TicksGame - pawn.mindState.lastEngageTargetTick > ((JobGiver_AIFightEnemy)this).TicksSinceEngageToLoseTarget)
		{
			return true;
		}
		Thing obj = ((enemyTarget is IAttackTarget) ? enemyTarget : null);
		if (obj == null || !((IAttackTarget)obj).ThreatDisabled((IAttackTargetSearcher)(object)pawn))
		{
			Pawn val = (Pawn)(object)((enemyTarget is Pawn) ? enemyTarget : null);
			if (val == null || !val.DeadOrDowned)
			{
				if (actionData.fullAIControl)
				{
					if (!ReachabilityUtility.CanReach(pawn, (LocalTargetInfo)(enemyTarget), (PathEndMode)2, (Danger)3, true, false, (TraverseMode)0))
					{
						return true;
					}
					goto IL_0114;
				}
				if (ReachabilityUtility.CanReach(pawn, (LocalTargetInfo)(enemyTarget), (PathEndMode)2, (Danger)3, false, false, (TraverseMode)0))
				{
					IntVec3 val2 = ((Thing)pawn).Position - enemyTarget.Position;
					if ((float)val2.LengthHorizontalSquared > num * num)
					{
						goto IL_0114;
					}
				}
				return true;
			}
		}
		return true;
		IL_0114:
		if (!Hunt || !CanTargetWithAbillities(pawn, pawn.mindState.enemyTarget, out var _))
		{
			return true;
		}
		return false;
	}

	protected bool CanTargetWithAbillities(Pawn pawn, Thing target, out Ability pickedAbility)
	{
		pickedAbility = null;
		if (!pawn.Drafted)
		{
			return false;
		}
		if (pawn.abilities?.abilities != null)
		{
			foreach (Ability ability in pawn.abilities.abilities)
			{
				pickedAbility = CanTargetWithAbility(target, ability);
				if (pickedAbility != null)
				{
					return true;
				}
			}
		}
		if (TMFAutoCombatIntegration.IsAvailable && actionData != null)
		{
			if (TMFAutoCombatIntegration.CanTargetWithAbilities(pawn, target, actionData, out _))
			{
				return true;
			}
		}
		return false;
	}

	private Ability CanTargetWithAbility(Thing target, Ability ability)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if (!(bool)(ability.CanCast))
		{
			return null;
		}
		if (blacklist.Contains(ability.def))
		{
			return null;
		}
		if (!ability.def.verbProperties.targetParams.CanTarget((TargetInfo)(target), (ITargetingSource)null))
		{
			return null;
		}
		if (!ability.CanApplyOn((LocalTargetInfo)(target)))
		{
			return null;
		}
		if (ability.def.aiCanUse && !ability.AICanTargetNow((LocalTargetInfo)(target)))
		{
			return null;
		}
		bool aiCanUse = ability.def.aiCanUse;
		bool flag = false;
		try
		{
			ability.def.aiCanUse = true;
			flag = ability.AICanTargetNow((LocalTargetInfo)(target));
		}
		finally
		{
			ability.def.aiCanUse = aiCanUse;
		}
		if (flag)
		{
			return ability;
		}
		return null;
	}

	protected Job GiveDraftedHuntJob(Pawn pawn)
	{
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		((JobGiver_AIFightEnemy)this).UpdateEnemyTarget(pawn);
		Thing enemyTarget = pawn.mindState.enemyTarget;
		if (enemyTarget == null)
		{
			return null;
		}
		Pawn val = (Pawn)(object)((enemyTarget is Pawn) ? enemyTarget : null);
		if (val != null && (InvisibilityUtility.IsPsychologicallyInvisible(val) || val.DeadOrDowned))
		{
			return null;
		}
		Job abilityJob = GetAbilityJob(pawn, enemyTarget);
		if (abilityJob != null)
		{
			return abilityJob;
		}
		if (!Hunt)
		{
			return null;
		}
		Verb val2 = TryGetAttackVerb(pawn, enemyTarget);
		if (val2 == null)
		{
			return null;
		}
		IntVec3 val3;
		if (val2.verbProps.IsMeleeAttack)
		{
			if (!actionData.fullAIControl && !actionData.meleeCharge)
			{
				val3 = ((Thing)pawn).Position - enemyTarget.Position;
				if (val3.LengthHorizontalSquared > 9)
				{
					return JobMaker.MakeJob(JobDefOf.Wait_Combat, 100, true);
				}
			}
			Job val4 = ((JobGiver_AIFightEnemy)this).MeleeAttackJob(pawn, enemyTarget);
			val4.checkOverrideOnExpire = true;
			val4.expiryInterval = 100;
			val4.canBashDoors = actionData.fullAIControl;
			return val4;
		}
		bool takeCover = actionData.takeCover;
		float num = (takeCover ? 0.24f : 0f);
		float num2 = CoverUtility.CalculateOverallBlockChance((LocalTargetInfo)((Thing)(object)pawn), enemyTarget.Position, ((Thing)pawn).Map);
		bool flag = CoverUtility.CalculateOverallBlockChance((LocalTargetInfo)((Thing)(object)pawn), enemyTarget.Position, ((Thing)pawn).Map) >= num;
		bool flag2 = GenGrid.Standable(((Thing)pawn).Position, ((Thing)pawn).Map) && ((Thing)pawn).Map.pawnDestinationReservationManager.CanReserve(((Thing)pawn).Position, pawn, pawn.Drafted);
		bool flag3 = val2.CanHitTarget((LocalTargetInfo)(enemyTarget));
		float range = val2.verbProps.range;
		val3 = ((Thing)pawn).Position - enemyTarget.Position;
		bool flag4 = (float)val3.LengthHorizontalSquared < range * range;
		IntRange expiryInterval_ShooterSucceeded;
		if (flag & flag2 & flag3 & flag4)
		{
			JobDef wait_Combat = JobDefOf.Wait_Combat;
			expiryInterval_ShooterSucceeded = JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded;
			return JobMaker.MakeJob(wait_Combat, expiryInterval_ShooterSucceeded.RandomInRange / 3, true);
		}
		IntVec3 val5 = default;
		if (!((JobGiver_AIFightEnemy)this).TryFindShootingPosition(pawn, out val5, val2))
		{
			Job val6 = TryMeleeAttackJob(pawn, enemyTarget);
			if (val6 != null)
			{
				return val6;
			}
			return JobMaker.MakeJob(JobDefOf.Wait_Combat, 100, true);
		}
		if (val5 == ((Thing)pawn).Position)
		{
			if (takeCover && !flag)
			{
				if (TryFindShootinPositionInner(pawn, out var dest, val2, requestNewPos: true) && dest != ((Thing)pawn).Position)
				{
					return MakeGotoJob(dest);
				}
				Job val7 = TryMeleeAttackJob(pawn, enemyTarget);
				if (val7 != null)
				{
					return val7;
				}
			}
			JobDef wait_Combat2 = JobDefOf.Wait_Combat;
			expiryInterval_ShooterSucceeded = JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded;
			return JobMaker.MakeJob(wait_Combat2, expiryInterval_ShooterSucceeded.RandomInRange / 3, true);
		}
		return MakeGotoJob(val5);
	}

	private Job TryMeleeAttackJob(Pawn pawn, Thing enemyTarget)
	{
		if (actionData.fullAIControl)
		{
			Job val = ((JobGiver_AIFightEnemy)this).MeleeAttackJob(pawn, enemyTarget);
			val.checkOverrideOnExpire = true;
			val.expiryInterval = 100;
			val.canBashDoors = true;
			val.maxNumMeleeAttacks = 1;
			return val;
		}
		return null;
	}

	protected static Job MakeGotoJob(IntVec3 shootingPos)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		Job val = JobMaker.MakeJob(JobDefOf.Goto, (LocalTargetInfo)(shootingPos));
		IntRange expiryInterval_ShooterSucceeded = JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded;
		val.expiryInterval = expiryInterval_ShooterSucceeded.RandomInRange / 3;
		val.checkOverrideOnExpire = true;
		return val;
	}

	public override void UpdateEnemyTarget(Pawn pawn)
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		Thing val = pawn.mindState.enemyTarget;
		if (val != null && ((JobGiver_AIFightEnemy)this).ShouldLoseTarget(pawn))
		{
			val = null;
		}
		if (val == null)
		{
			val = FindAttackTargetIfPossible(pawn);
			if (val != null)
			{
				Notify_EngagedTarget(pawn);
				Lord lord = LordUtility.GetLord(pawn);
				if (lord != null)
				{
					lord.Notify_PawnAcquiredTarget(pawn, val);
				}
			}
		}
		else
		{
			Thing val2 = FindAttackTargetIfPossible(pawn);
			if (val2 == null && !base.chaseTarget)
			{
				val = null;
			}
			else if (val2 != null && val2 != val)
			{
				Notify_EngagedTarget(pawn);
				val = val2;
			}
		}
		pawn.mindState.enemyTarget = val;
		Pawn val3;
		if ((val3 = (Pawn)(object)((val is Pawn) ? val : null)) != null && val.Faction == Faction.OfPlayer)
		{
			IntVec3 position = ((Thing)pawn).Position;
			if (position.InHorDistOf(val.Position, 40f) && !val3.IsShambler && !InvisibilityUtility.IsPsychologicallyInvisible(pawn))
			{
				Find.TickManager.slower.SignalForceNormalSpeed();
			}
		}
	}

	protected void Notify_EngagedTarget(Pawn pawn)
	{
		pawn.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;
	}

	protected Thing FindAttackTargetIfPossible(Pawn pawn)
	{
		if (pawn.TryGetAttackVerb((Thing)null, true, false) == null)
		{
			return null;
		}
		if (actionData.fullAIControl)
		{
			return FindAttackTargetAnywhere(pawn);
		}
		return ((JobGiver_AIFightEnemy)this).FindAttackTarget(pawn);
	}

	public virtual Thing FindAttackTargetAnywhere(Pawn pawn)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected Obj, but got Unknown
		TargetScanFlags val = (TargetScanFlags)292;
		return (Thing)AttackTargetFinder.BestAttackTarget((IAttackTargetSearcher)(object)pawn, val, (Predicate<Thing>)IsGoodTarget, 0f, 900f, default(IntVec3), float.MaxValue, true, true, false, false);
	}

	public virtual bool IsGoodTarget(Thing thing)
	{
		if (!PlayerCanSeeThing(thing))
		{
			return false;
		}
		Pawn val = (Pawn)(object)((thing is Pawn) ? thing : null);
		if (val != null && !val.Downed)
		{
			return !InvisibilityUtility.IsPsychologicallyInvisible(val);
		}
		return false;
	}

	public static bool PlayerCanSeeThing(Thing thing)
	{
		if (!thing.Spawned)
		{
			return false;
		}
		if (thing.MapHeld != null && !GridsUtility.Fogged(thing))
		{
			return true;
		}
		return false;
	}

	public Verb TryGetAttackVerb(Pawn pawn, Thing target, bool allowManualCastWeapons = false, bool allowOnlyManualCastWeapons = false)
	{
		Verb val = DraftedActionData.TryGetVEFAbilityVerb(pawn, target);
		if (val != null)
		{
			return val;
		}
		if (TMFAutoCombatIntegration.IsAvailable && actionData != null)
		{
			Verb tmfVerb = TMFAutoCombatIntegration.TryGetAttackVerb(pawn, target, actionData);
			if (tmfVerb != null)
			{
				return tmfVerb;
			}
		}
		if (allowManualCastWeapons)
		{
			Pawn_EquipmentTracker equipment = pawn.equipment;
			if (((equipment != null) ? equipment.Primary : null) != null && pawn.equipment.PrimaryEq.PrimaryVerb.Available() && pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.onlyManualCast)
			{
				return pawn.equipment.PrimaryEq.PrimaryVerb;
			}
			if (allowManualCastWeapons && pawn.apparel != null && pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.onlyManualCast)
			{
				Verb firstApparelVerb = pawn.apparel.FirstApparelVerb;
				if (firstApparelVerb != null && firstApparelVerb.Available())
				{
					return firstApparelVerb;
				}
			}
		}
		if (allowOnlyManualCastWeapons)
		{
			return null;
		}
		Pawn_EquipmentTracker equipment2 = pawn.equipment;
		if (((equipment2 != null) ? equipment2.Primary : null) != null && pawn.equipment.PrimaryEq.PrimaryVerb.Available() && !pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.onlyManualCast)
		{
			return pawn.equipment.PrimaryEq.PrimaryVerb;
		}
		if (pawn.kindDef.canMeleeAttack)
		{
			return pawn.meleeVerbs.TryGetMeleeVerb(target);
		}
		return null;
	}

	private Job GetAbilityJob(Pawn pawn, Thing enemyTarget)
	{
		if (TMFAutoCombatIntegration.IsAvailable && actionData != null)
		{
			Job tmfJob = TMFAutoCombatIntegration.TryGetTMFAbilityJob(pawn, enemyTarget, actionData);
			if (tmfJob != null)
			{
				return tmfJob;
			}
		}

		if (pawn.abilities == null)
		{
			return null;
		}
		List<Ability> abilities = pawn.abilities.AllAbilitiesForReading.Where((Ability x) => actionData.autocastAbilities.Contains(x.def)).ToList();
		Job val = TrySelfBuff(pawn, abilities);
		if (val != null)
		{
			return val;
		}
		Job val2 = TryOffesnsiveAbility(pawn, enemyTarget, abilities);
		if (val2 != null)
		{
			return val2;
		}
		return null;
	}

	private Job TrySelfBuff(Pawn pawn, List<Ability> abilities)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		LocalTargetInfo val = new LocalTargetInfo((Thing)(object)pawn);
		List<Ability> list = abilities.Where((Ability ability) =>
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			return ability.verb.targetParams.canTargetSelf && (bool)(ability.CanCast) && (!ability.def.aiCanUse || ability.AICanTargetNow((LocalTargetInfo)((Thing)(object)pawn)));
		}).ToList();
		if (GenCollection.Any<Ability>(list))
		{
			return GenCollection.RandomElement<Ability>((IEnumerable<Ability>)list).GetJob(val, val);
		}
		return null;
	}

	private Job TryOffesnsiveAbility(Pawn pawn, Thing enemyTarget, List<Ability> abilities)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		List<Ability> source = pawn.abilities.AICastableAbilities((LocalTargetInfo)(enemyTarget), true);
		source = source.Where(abilities.Contains).ToList();
		if (GenList.NullOrEmpty<Ability>((IList<Ability>)source))
		{
			return null;
		}
		source = source.Where((Ability ability) => CanTargetWithAbility(enemyTarget, ability) != null && actionData.AutoCastFor(ability.def)).ToList();
		if (!GenList.NullOrEmpty<Ability>((IList<Ability>)source) && GenGrid.Standable(((Thing)pawn).Position, ((Thing)pawn).Map) && ((Thing)pawn).Map.pawnDestinationReservationManager.CanReserve(((Thing)pawn).Position, pawn, pawn.Drafted))
		{
			for (int num = 0; num < source.Count; num++)
			{
				if (source[num].verb.CanHitTarget((LocalTargetInfo)(enemyTarget)))
				{
					return source[num].GetJob((LocalTargetInfo)(enemyTarget), (LocalTargetInfo)(enemyTarget));
				}
			}
			for (int num2 = 0; num2 < source.Count; num2++)
			{
				LocalTargetInfo val = source[num2].AIGetAOETarget();
				if (val.IsValid)
				{
					return source[num2].GetJob(val, val);
				}
			}
			for (int num3 = 0; num3 < source.Count; num3++)
			{
				if (source[num3].verb.targetParams.canTargetSelf)
				{
					return source[num3].GetJob((LocalTargetInfo)((Thing)(object)pawn), (LocalTargetInfo)((Thing)(object)pawn));
				}
			}
		}
		return null;
	}
}
