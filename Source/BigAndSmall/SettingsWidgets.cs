using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BigAndSmall;

public static class SettingsWidgets
{
	private static Dictionary<string, string> inputBuffers = new Dictionary<string, string>();

	public static bool NearlyEquals(this float a, float b, float tolerance = 0.01f)
	{
		return Math.Abs(a - b) < tolerance;
	}

	private static string SanitizeNumericInput(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return string.Empty;
		}
		List<char> list = new List<char>(input.Length);
		int i = 0;
		if (input[0] == '-')
		{
			list.Add(input[i++]);
		}
		bool flag = false;
		for (; i < input.Length; i++)
		{
			char c = input[i];
			if (char.IsDigit(c))
			{
				list.Add(c);
			}
			else if ((c == '.' || c == ',') && !flag)
			{
				list.Add('.');
				flag = true;
			}
		}
		return new string(list.ToArray());
	}

	public static void CreateSettingsSlider(Listing_Standard listingStandard, string labelName, ref float value, float min = 0f, float max = 10f, Func<float, string> valueFormatter = null)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		Rect rect = ((Listing)listingStandard).GetRect(Text.LineHeight, 1f);
		float num = rect.width * 0.46f;
		float num2 = rect.width * 0.45f;
		float num3 = rect.width * 0.09f;
		Rect val = new Rect(rect.x, rect.y, num, rect.height);
		Rect val2 = new Rect(val.xMax, rect.y, num2, rect.height);
		Rect val3 = new Rect(val2.xMax, rect.y, num3, rect.height);
		Widgets.Label(val, labelName);
		value = Widgets.HorizontalSlider(val2, value, min, max, true, (string)null, (string)null, (string)null, -1f);
		if (valueFormatter != null)
		{
			Widgets.Label(val3, valueFormatter(value));
		}
		else
		{
			Widgets.Label(val3, $"{value:F1}");
		}
	}

	public static void CreateSettingCheckbox(Listing_Standard listingStandard, string labelName, ref bool value, bool disabled = false)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		Rect rect = ((Listing)listingStandard).GetRect(Text.LineHeight, 1f);
		float num = rect.width * 0.9f;
		float num2 = rect.width * 0.1f;
		Rect val = new Rect(rect.x, rect.y, num, rect.height);
		Rect val2 = new Rect(val.xMax, rect.y, num2, rect.height);
		Widgets.Label(val, labelName);
		Widgets.Checkbox(val2.position, ref value, 24f, disabled, false, (Texture2D)null, (Texture2D)null);
	}

	public static void CreateRadioButtonsTwoOptions(Listing_Standard lst, string labelName, ref bool value, string optionTrue, string optionFalse)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		((Listing)lst).GapLine(12f);
		Rect rect = ((Listing)lst).GetRect(Text.LineHeight * 2f, 1f);
		Rect val = new Rect(rect.x, rect.y, rect.width, Text.LineHeight);
		Rect val2 = new Rect(val.x, val.y, val.width * 0.55f, val.height);
		Rect val3 = new Rect(val2.xMax, val.y, val.width * 0.45f, val.height);
		Widgets.Label(val2, labelName);
		Widgets.Label(val3, optionTrue);
		if (Widgets.RadioButton(val3.x - 32f, val3.y, value, false))
		{
			value = true;
		}
		Rect val4 = new Rect(rect.x, val.yMax + ((Listing)lst).verticalSpacing, rect.width, Text.LineHeight);
		Rect val5 = new Rect(val4.x, val4.y, val4.width * 0.55f, val4.height);
		Rect val6 = new Rect(val5.xMax, val4.y, val4.width * 0.45f, val4.height);
		Widgets.Label(val6, optionFalse);
		if (Widgets.RadioButton(val6.x - 32f, val6.y, !value, false))
		{
			value = false;
		}
		((Listing)lst).GapLine(12f);
	}
}
