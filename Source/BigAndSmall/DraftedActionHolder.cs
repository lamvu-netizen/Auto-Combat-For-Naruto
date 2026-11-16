using System.Collections.Generic;
using Verse;

namespace BigAndSmall;

public class DraftedActionHolder : GameComponent
{
	public static Dictionary<string, DraftedActionData> pawnDraftActionData = new Dictionary<string, DraftedActionData>();

	public static DraftedActionData GetData(Pawn pawn)
	{
		if (pawnDraftActionData == null)
		{
			pawnDraftActionData = new Dictionary<string, DraftedActionData>();
		}
		if (pawnDraftActionData.TryGetValue(((Thing)pawn).ThingID, out var value))
		{
			return value;
		}
		pawnDraftActionData[((Thing)pawn).ThingID] = new DraftedActionData(pawn);
		return pawnDraftActionData[((Thing)pawn).ThingID];
	}

	public DraftedActionHolder(Game game)
	{
	}

	public override void ExposeData()
	{
		((GameComponent)this).ExposeData();
		Scribe_Collections.Look<string, DraftedActionData>(ref pawnDraftActionData, "draftedActions", (LookMode)1, (LookMode)2);
	}
}
