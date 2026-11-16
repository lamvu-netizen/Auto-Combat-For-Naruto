using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BigAndSmall;

public static class ModExtHelper
{
	public class ModExtWrapper<T>(T extension, Type sourceType, int priority) where T : DefModExtension
	{
		public T extension = extension;

		public Type sourceType = sourceType;

		public int priority = priority;
	}

	public static List<T> ExtensionsOnDef<T, TDef>(this TDef def, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension where TDef : Def
	{
		List<ModExtWrapper<T>> allMatchingExtensions = GetAllMatchingExtensions<T>((Def)(object)def, ((object)def).GetType());
		return GetFilteredResult(allMatchingExtensions, parentWhitelist, parentBlacklist, doSort);
	}

	public static List<T> ExtensionsOnDefList<T, TDef>(this List<TDef> def, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension where TDef : Def
	{
		List<T> list = new List<T>();
		foreach (TDef item in def)
		{
			list.AddRange(item.ExtensionsOnDef<T, TDef>(parentWhitelist, parentBlacklist, doSort));
		}
		return list;
	}

	private static List<T> GetFilteredResult<T>(List<ModExtWrapper<T>> matches, List<Type> parentWhitelist = null, List<Type> parentBlacklist = null, bool doSort = true) where T : DefModExtension
	{
		List<T> list = new List<T>();
		if (doSort)
		{
			matches.OrderByDescending((ModExtWrapper<T> a) => a.priority);
		}
		if (parentWhitelist == null && parentBlacklist == null)
		{
			return matches.Select((ModExtWrapper<T> a) => a.extension).ToList();
		}
		foreach (ModExtWrapper<T> match in matches)
		{
			if ((parentWhitelist == null || parentWhitelist.Contains(match.sourceType)) && (parentBlacklist == null || !parentBlacklist.Contains(match.sourceType)))
			{
				list.Add(match.extension);
			}
		}
		return list;
	}

	private static List<ModExtWrapper<T>> GetAllMatchingExtensions<T>(Def def, Type source) where T : DefModExtension
	{
		List<ModExtWrapper<T>> list = new List<ModExtWrapper<T>>();
		if (def?.modExtensions == null)
		{
			return list;
		}
		int priority = 0;
		if (def is PawnKindDef)
		{
			priority = -100;
		}
		else
		{
			TraitDef val = (TraitDef)(object)((def is TraitDef) ? def : null);
			if (val != null)
			{
				priority = -10;
			}
			else if (def is GeneDef)
			{
				priority = 10;
			}
			else if (def is HediffDef)
			{
				priority = 100;
			}
		}
		foreach (DefModExtension modExtension in def.modExtensions)
		{
			T val2 = (T)(object)((modExtension is T) ? modExtension : null);
			if (val2 != null)
			{
				list.Add(new ModExtWrapper<T>(val2, source, priority));
			}
		}
		return list;
	}
}
