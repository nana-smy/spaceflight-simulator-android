using System;
using SFS.Translations;

namespace MultiplayerSFS.Mod {
public static class MultiplayerLoc
{
	public static bool IsChinese()
	{
		if (Loc.languageName == "Default")
		{
			return false;
		}
		if (Loc.main.Cancel == "取消" || Loc.main.None == "没有")
		{
			return true;
		}
		try
		{
			string languageName = LanguageSettings.main?.settings?.name;
			if (!string.IsNullOrEmpty(languageName) && (languageName.Contains("中文") || languageName.Contains("Chinese")))
			{
				return true;
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	public static string L(string zh, string en)
	{
		return IsChinese() ? zh : en;
	}
}
}
