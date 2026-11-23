using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using SFS.IO;
using SFS.Parsers.Json;
using SFS.World;
using SFS.WorldBase;

namespace MultiplayerSFS.Common {
public class WorldState
{
	public double initWorldTime;

	public Stopwatch worldTimer = Stopwatch.StartNew();

	public Difficulty.DifficultyType difficulty;

	public string solarSystemName = "";

	public Dictionary<int, RocketState> rockets;

	public double WorldTime
	{
		get
		{
			if (worldTimer.ElapsedTicks > 1000 * Stopwatch.Frequency)
			{
				initWorldTime += worldTimer.Elapsed.TotalSeconds;
				worldTimer.Restart();
			}
			return initWorldTime + worldTimer.Elapsed.TotalSeconds;
		}
		set
		{
			initWorldTime = value;
			worldTimer.Restart();
		}
	}

	public WorldState()
	{
		initWorldTime = 1000000.0;
		difficulty = Difficulty.DifficultyType.Normal;
		solarSystemName = "";
		rockets = new Dictionary<int, RocketState>();
	}

	public WorldState(string path)
	{
		try
		{
			LoadWorldStateWithReflection(path);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[WARNING] Failed to load world state: " + ex.Message);
			Console.WriteLine("[WARNING] Using default world state values.");
			InitializeWithDefaults();
		}
	}

	private void LoadWorldStateWithReflection(string path)
	{
		FolderPath folderPath = new FolderPath(path);
		FolderPath folderPath2 = folderPath.CloneAndExtend("Persistent");
		if (!folderPath.FolderExists())
		{
			throw new Exception("Save folder cannot be found or does not exist.");
		}
		if (!folderPath2.FolderExists())
		{
			throw new Exception("'Persistent' folder cannot be found or does not exist.");
		}
		// IL2CPP 兼容：原实现用反射调用泛型方法（MakeGenericMethod），AOT 下不可用，改为直接调用。
		if (!JsonWrapper.TryLoadJson(folderPath.ExtendToFile("WorldSettings.txt"), out WorldSettings worldSettings))
		{
			throw new Exception("'WorldSettings.txt' file cannot be found or could not be loaded.");
		}
		solarSystemName = worldSettings?.solarSystem?.name ?? "";
		Console.WriteLine("[INFO] Loaded solar system: '" + solarSystemName + "'");
		if (!JsonWrapper.TryLoadJson(folderPath2.ExtendToFile("WorldState.txt"), out WorldSave.WorldState worldState))
		{
			throw new Exception("'WorldState.txt' file cannot be found or could not be loaded.");
		}
		if (!JsonWrapper.TryLoadJson(folderPath2.ExtendToFile("Rockets.txt"), out List<RocketSave> obj))
		{
			throw new Exception("'Rockets.txt' file cannot be found or could not be loaded.");
		}
		initWorldTime = worldState.worldTime;
		difficulty = worldSettings.difficulty.difficulty;
		rockets = new Dictionary<int, RocketState>();
		foreach (RocketSave item in obj)
		{
			rockets.InsertNew(new RocketState(item));
		}
		Console.WriteLine("[INFO] Successfully loaded world state from " + path);
	}

	private void InitializeWithDefaults()
	{
		initWorldTime = 0.0;
		difficulty = Difficulty.DifficultyType.Normal;
		solarSystemName = "";
		rockets = new Dictionary<int, RocketState>();
	}
}
}
