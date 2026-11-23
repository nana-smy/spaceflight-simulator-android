using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MultiplayerSFS.Server;
using Newtonsoft.Json;

namespace MultiplayerSFS.ServerCommon
{
	public class WorldState
	{
		public double initWorldTime;

		public Stopwatch worldTimer = Stopwatch.StartNew();

		public int difficulty;

		public string solarSystemName = "";

		public Dictionary<int, RocketState> rockets;

		public bool infiniteFuel;

		public bool noAtmosphericDrag;

		public bool unbreakableParts;

		public bool noGravity;

		public bool noHeatDamage;

		public bool noBurnMarks;

		public bool infiniteBuildArea;

		public bool partClipping;

		private string savePath;

		private double timeScale = 1.0;

		public double WorldTime
		{
			get
			{
				if (worldTimer.ElapsedTicks > 1000 * Stopwatch.Frequency)
				{
					initWorldTime += worldTimer.Elapsed.TotalSeconds * timeScale;
					worldTimer.Restart();
				}
				return initWorldTime + worldTimer.Elapsed.TotalSeconds * timeScale;
			}
			set
			{
				initWorldTime = value;
				worldTimer.Restart();
			}
		}

		public void SetTimeScale(double scale)
		{
			initWorldTime += worldTimer.Elapsed.TotalSeconds * timeScale;
			worldTimer.Restart();
			timeScale = Math.Max(0.0, scale);
		}

		public WorldState()
		{
			InitializeWithDefaults();
		}

		public WorldState(string path)
		{
			savePath = path;
			try
			{
				if (!File.Exists(Path.Combine(path, "WorldSettings.txt")))
				{
					CreateNewWorld(path);
				}
				LoadWorldStateFromFile(path);
				Logger.Info("World state loaded from " + path);
			}
			catch (Exception ex)
			{
				Logger.Warning("Failed to load world state: " + ex.Message);
				Logger.Warning("Using default world state values.");
				InitializeWithDefaults();
			}
		}

		public void SaveWorld()
		{
			if (string.IsNullOrEmpty(savePath))
			{
				Logger.Warning("Cannot save world: save path is not set");
				return;
			}
			try
			{
				Logger.Info("Saving world state...");
				long ticks = DateTime.Now.Ticks;
				double worldTime = WorldTime;
				string contents = "{\"solarSystem\":{\"name\":\"" + EscapeJson(solarSystemName) + "\"},\"mode\":{\"mode\":0,\"allowQuicksaves\":true},\"difficulty\":{\"difficulty\":" + difficulty + "},\"playtime\":{\"lastPlayedTime_Ticks\":" + ticks + ",\"totalPlayTime_Seconds\":" + worldTime + "},\"cheats\":{\"infiniteFuel\":" + BoolJson(infiniteFuel) + ",\"noAtmosphericDrag\":" + BoolJson(noAtmosphericDrag) + ",\"unbreakableParts\":" + BoolJson(unbreakableParts) + ",\"noGravity\":" + BoolJson(noGravity) + ",\"noHeatDamage\":" + BoolJson(noHeatDamage) + ",\"noBurnMarks\":" + BoolJson(noBurnMarks) + ",\"infiniteBuildArea\":" + BoolJson(infiniteBuildArea) + ",\"partClipping\":" + BoolJson(partClipping) + "}}";
				string persistent = Path.Combine(savePath, "Persistent");
				Directory.CreateDirectory(persistent);
				File.WriteAllText(Path.Combine(savePath, "WorldSettings.txt"), contents);
				File.WriteAllText(Path.Combine(persistent, "WorldState.txt"), "{\"worldTime\":" + worldTime + ",\"timewarpPhase\":0,\"mapView\":false,\"mapPosition\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"mapAddress\":\"\",\"targetAddress\":\"null\",\"playerAddress\":\"null\",\"cameraDistance\":0.0}");
				File.WriteAllText(Path.Combine(persistent, "Rockets.txt"), (rockets == null || rockets.Count == 0) ? "[]" : JsonConvert.SerializeObject(rockets));
				Logger.Info("World saved successfully at: " + savePath);
			}
			catch (Exception ex)
			{
				Logger.Error("Failed to save world: " + ex.Message);
			}
		}

		private void CreateNewWorld(string path)
		{
			try
			{
				Directory.CreateDirectory(path);
				string persistent = Path.Combine(path, "Persistent");
				Directory.CreateDirectory(persistent);
				long ticks = DateTime.Now.Ticks;
				string contents = "{\"solarSystem\":{\"name\":\"" + EscapeJson(solarSystemName) + "\"},\"mode\":{\"mode\":0,\"allowQuicksaves\":true},\"difficulty\":{\"difficulty\":" + difficulty + "},\"playtime\":{\"lastPlayedTime_Ticks\":" + ticks + ",\"totalPlayTime_Seconds\":0.0},\"cheats\":{\"infiniteFuel\":" + BoolJson(infiniteFuel) + ",\"noAtmosphericDrag\":" + BoolJson(noAtmosphericDrag) + ",\"unbreakableParts\":" + BoolJson(unbreakableParts) + ",\"noGravity\":" + BoolJson(noGravity) + ",\"noHeatDamage\":" + BoolJson(noHeatDamage) + ",\"noBurnMarks\":" + BoolJson(noBurnMarks) + ",\"infiniteBuildArea\":" + BoolJson(infiniteBuildArea) + ",\"partClipping\":" + BoolJson(partClipping) + "}}";
				File.WriteAllText(Path.Combine(path, "WorldSettings.txt"), contents);
				File.WriteAllText(Path.Combine(persistent, "Achievements.txt"), "[]");
				File.WriteAllText(Path.Combine(persistent, "Branches.txt"), "{}");
				File.WriteAllText(Path.Combine(persistent, "Challenges.txt"), "[]");
				File.WriteAllText(Path.Combine(persistent, "Rockets.txt"), "[]");
				File.WriteAllText(Path.Combine(persistent, "Version.txt"), "\"1.5.10.2\"");
				File.WriteAllText(Path.Combine(persistent, "WorldState.txt"), "{}");
			}
			catch (Exception ex)
			{
				Logger.Error("Failed to create world structure: " + ex.Message);
				throw;
			}
		}

		private void LoadWorldStateFromFile(string path)
		{
			rockets = new Dictionary<int, RocketState>();
			try
			{
				string text = Path.Combine(path, "WorldSettings.txt");
				if (!File.Exists(text))
				{
					Logger.Warning("WorldSettings.txt not found at: " + text);
					return;
				}
				string json = File.ReadAllText(text);
				solarSystemName = ExtractSolarSystemNameFromJson(json);
				difficulty = ExtractDifficultyFromJson(json);
				ExtractCheatSettingsFromJson(json);
				string path2 = Path.Combine(Path.Combine(path, "Persistent"), "WorldState.txt");
				if (File.Exists(path2))
				{
					double num = ExtractWorldTimeFromJson(File.ReadAllText(path2));
					if (num > 0.0)
					{
						WorldTime = num;
					}
				}
				string rocketsPath = Path.Combine(Path.Combine(path, "Persistent"), "Rockets.txt");
				if (File.Exists(rocketsPath))
				{
					string rocketsJson = File.ReadAllText(rocketsPath);
					if (!string.IsNullOrWhiteSpace(rocketsJson) && rocketsJson != "{}" && rocketsJson != "[]")
					{
						rockets = JsonConvert.DeserializeObject<Dictionary<int, RocketState>>(rocketsJson) ?? new Dictionary<int, RocketState>();
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error("Failed to load world settings: " + ex.Message);
				throw;
			}
		}

		private double ExtractWorldTimeFromJson(string json)
		{
			try
			{
				int num = json.IndexOf("\"worldTime\"");
				if (num == -1)
				{
					return 0.0;
				}
				int num2 = json.IndexOf(":", num);
				if (num2 == -1)
				{
					return 0.0;
				}
				int i;
				for (i = num2 + 1; i < json.Length && (json[i] == ' ' || json[i] == '\n' || json[i] == '\r'); i++)
				{
				}
				int j;
				for (j = i; j < json.Length && (char.IsDigit(json[j]) || json[j] == '.'); j++)
				{
				}
				if (double.TryParse(json.Substring(i, j - i), out var result))
				{
					return result;
				}
				return 0.0;
			}
			catch
			{
				return 0.0;
			}
		}

		private int ExtractDifficultyFromJson(string json)
		{
			try
			{
				int num = json.IndexOf("\"difficulty\"");
				if (num == -1)
				{
					return 0;
				}
				int num2 = json.IndexOf("{", num);
				if (num2 == -1)
				{
					return 0;
				}
				int num3 = json.IndexOf("\"difficulty\"", num2);
				if (num3 == -1)
				{
					return 0;
				}
				int num4 = json.IndexOf(":", num3);
				if (num4 == -1)
				{
					return 0;
				}
				int i;
				for (i = num4 + 1; i < json.Length && (json[i] == ' ' || json[i] == '\n' || json[i] == '\r'); i++)
				{
				}
				int j;
				for (j = i; j < json.Length && char.IsDigit(json[j]); j++)
				{
				}
				if (int.TryParse(json.Substring(i, j - i), out var result))
				{
					return result;
				}
				return 0;
			}
			catch
			{
				return 0;
			}
		}

		private void ExtractCheatSettingsFromJson(string json)
		{
			try
			{
				int num = json.IndexOf("\"cheats\"");
				if (num == -1)
				{
					return;
				}
				int num2 = json.IndexOf("{", num);
				if (num2 == -1)
				{
					return;
				}
				int num3 = FindMatchingBrace(json, num2);
				if (num3 == -1)
				{
					return;
				}
				string json2 = json.Substring(num2, num3 - num2 + 1);
				infiniteFuel = ExtractBoolFromJson(json2, "infiniteFuel");
				noAtmosphericDrag = ExtractBoolFromJson(json2, "noAtmosphericDrag");
				unbreakableParts = ExtractBoolFromJson(json2, "unbreakableParts");
				noGravity = ExtractBoolFromJson(json2, "noGravity");
				noHeatDamage = ExtractBoolFromJson(json2, "noHeatDamage");
				noBurnMarks = ExtractBoolFromJson(json2, "noBurnMarks");
				infiniteBuildArea = ExtractBoolFromJson(json2, "infiniteBuildArea");
				partClipping = ExtractBoolFromJson(json2, "partClipping");
			}
			catch
			{
			}
		}

		private int FindMatchingBrace(string json, int startIndex)
		{
			int num = 0;
			for (int i = startIndex; i < json.Length; i++)
			{
				if (json[i] == '{')
				{
					num++;
				}
				else if (json[i] == '}')
				{
					num--;
					if (num == 0)
					{
						return i;
					}
				}
			}
			return -1;
		}

		private bool ExtractBoolFromJson(string json, string key)
		{
			try
			{
				int num = json.IndexOf("\"" + key + "\"");
				if (num == -1)
				{
					return false;
				}
				int num2 = json.IndexOf(":", num);
				if (num2 == -1)
				{
					return false;
				}
				int i;
				for (i = num2 + 1; i < json.Length && (json[i] == ' ' || json[i] == '\n' || json[i] == '\r'); i++)
				{
				}
				return json.Substring(i, 4).Equals("true", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		private string ExtractSolarSystemNameFromJson(string json)
		{
			try
			{
				int num = json.IndexOf("\"solarSystem\"");
				if (num == -1)
				{
					return "";
				}
				int num2 = json.IndexOf("{", num);
				if (num2 == -1)
				{
					return "";
				}
				int num3 = json.IndexOf("\"name\"", num2);
				if (num3 == -1)
				{
					return "";
				}
				int num4 = json.IndexOf("\"", num3 + 6);
				if (num4 == -1)
				{
					return "";
				}
				int num5 = json.IndexOf("\"", num4 + 1);
				if (num5 == -1)
				{
					return "";
				}
				return json.Substring(num4 + 1, num5 - num4 - 1);
			}
			catch
			{
				return "";
			}
		}

		private static string BoolJson(bool value)
		{
			return value ? "true" : "false";
		}

		private static string EscapeJson(string value)
		{
			if (value == null)
			{
				return "";
			}
			return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
		}

		private void InitializeWithDefaults()
		{
			initWorldTime = 1000000.0;
			difficulty = 0;
			solarSystemName = "";
			rockets = new Dictionary<int, RocketState>();
			infiniteFuel = false;
			noAtmosphericDrag = false;
			unbreakableParts = false;
			noGravity = false;
			noHeatDamage = false;
			noBurnMarks = false;
			infiniteBuildArea = true;
			partClipping = true;
		}
	}
}
