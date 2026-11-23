using System.Text.RegularExpressions;

namespace MultiplayerSFS.Server {
public static class CommandExtensions
{
	public static string FormatCommand(this string name, string args = null)
	{
		string text = "\"/" + name;
		if (args != null)
		{
			text = text + " " + args;
		}
		return text + "\"";
	}

	public static string CleanDescription(this string desc)
	{
		return Regex.Replace(desc, "[ \\t]+\\|", string.Empty).Trim();
	}

	public static bool TryParseBool(string arg, out bool result)
	{
		if (arg == "true")
		{
			result = true;
			return true;
		}
		if (arg == "false")
		{
			result = false;
			return true;
		}
		result = false;
		return false;
	}
}
}
