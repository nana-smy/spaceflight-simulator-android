using System;
using System.Collections.Generic;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Terrain
{
	public class TerrainColliderModule : MonoBehaviour
	{
		public Player player;

		public WorldLoader loader;

		private List<int> chunkIndexes = new List<int>();

		private List<int> newChunkIndexes = new List<int>();

		private readonly List<int> removeIndexes = new List<int>();

		private readonly List<int> addIndexes = new List<int>();

		private Planet currentPlanet;

		private void Start()
		{
			player.location.position.OnChange += new Action(UpdateChunks);
			loader.onLoadedChange_After += OnLoadedChange;
		}

		private void OnDestroy()
		{
			if (player != null)
			{
				player.location.position.OnChange -= new Action(UpdateChunks);
			}
			if (loader != null)
			{
				loader.onLoadedChange_After -= OnLoadedChange;
			}
			Clear();
		}

		private void OnLoadedChange(bool loaded, bool arg)
		{
			UpdateChunks();
		}

		private void UpdateChunks()
		{
			if (!loader.Loaded || !player.location.position.Value.Mag_LessThan(player.location.planet.Value.Radius + player.location.planet.Value.maxTerrainHeight + (double)player.GetSizeRadius()))
			{
				Clear();
				return;
			}
			Planet value = player.location.planet.Value;
			Planet previousPlanet = currentPlanet;
			currentPlanet = value;
			double num = (player.location.position.Value.AngleRadians / (Math.PI * 2.0) + 10.0) % 1.0;
			double num2 = (double)(player.GetSizeRadius() + 5f) / value.SurfaceArea;
			double angularChunkSize = TerrainColliderManager.GetAngularChunkSize(value);
			int num3 = (int)((num - num2) / angularChunkSize + 100.0) - 100;
			int num4 = (int)((num + num2) / angularChunkSize + 100.0) - 100;
			newChunkIndexes.Clear();
			for (int i = num3; i <= num4; i++)
			{
				newChunkIndexes.Add(i);
			}
			removeIndexes.Clear();
			addIndexes.Clear();
			foreach (int a in chunkIndexes)
			{
				if (!newChunkIndexes.Contains(a))
				{
					removeIndexes.Add(a);
				}
			}
			foreach (int a in newChunkIndexes)
			{
				if (!chunkIndexes.Contains(a))
				{
					addIndexes.Add(a);
				}
			}
			if (previousPlanet != value)
			{
				// 星球切换：旧星球的碰撞块全部按旧星球移除，避免残留
				TerrainColliderManager.main.RemoveChunks(chunkIndexes, this, previousPlanet);
				chunkIndexes.Clear();
			}
			else
			{
				TerrainColliderManager.main.RemoveChunks(removeIndexes, this, previousPlanet);
			}
			List<int> oldChunkIndexes = chunkIndexes;
			chunkIndexes = newChunkIndexes;
			newChunkIndexes = oldChunkIndexes;
			newChunkIndexes.Clear();
			TerrainColliderManager.main.AddChunks(addIndexes, this, value);
		}

		private void Clear()
		{
			if (chunkIndexes.Count != 0)
			{
				TerrainColliderManager.main.RemoveChunks(chunkIndexes, this, currentPlanet);
				chunkIndexes.Clear();
			}
		}
	}
}
