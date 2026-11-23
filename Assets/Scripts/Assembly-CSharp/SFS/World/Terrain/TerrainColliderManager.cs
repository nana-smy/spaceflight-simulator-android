using System;
using System.Collections.Generic;
using SFS.Variables;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Terrain
{
	public class TerrainColliderManager : MonoBehaviour
	{
		[Serializable]
		public class Chunk
		{
			public List<TerrainColliderModule> owners = new List<TerrainColliderModule>();

			private GameObject gameObject;

			private PolygonCollider2D collider;

			private Double2 position;

			public Chunk(Transform holder)
			{
				gameObject = new GameObject("Collider");
				gameObject.transform.SetParent(holder, worldPositionStays: false);
				gameObject.layer = holder.gameObject.layer;
				WorldView.main.positionOffset.OnChange += new Action(Position);
			}

			// 复用/重建：采样并更新碰撞体与位置，最后启用碰撞体。
			// 用 collider.enabled 开关替代 GameObject 启停，避免反复销毁/创建 Box2D 形状。
			public void Rebuild(double from, double size, Planet planet)
			{
				if (gameObject == null)
				{
					return;
				}
				if (planet.data.hasTerrain && planet.data.terrain.collider)
				{
					SFS.World.Terrain.Chunk.TerrainPoints terrainPoints = planet.data.terrain.GetTerrainPoints(from, size, 11, offset: true, forCollider: true, planet, out position);
					if (collider == null)
					{
						collider = gameObject.AddComponent<PolygonCollider2D>();
					}
					collider.points = terrainPoints.points;
					collider.enabled = true;
				}
				else
				{
					position = Double2.zero;
					if (collider != null)
					{
						collider.enabled = false;
					}
				}
				Position();
			}

			public void Position()
			{
				if (gameObject != null)
				{
					gameObject.transform.position = WorldView.ToLocalPosition(position);
				}
			}

			// 归还对象池：清空共享者并停用碰撞体，而不是销毁/启停整个对象
			public void Release()
			{
				owners.Clear();
				if (collider != null)
				{
					collider.enabled = false;
				}
			}

			public void Destroy()
			{
				if (WorldView.main != null)
				{
					WorldView.main.positionOffset.OnChange -= new Action(Position);
				}
				if (gameObject != null)
				{
					UnityEngine.Object.Destroy(gameObject);
				}
			}
		}

		public static TerrainColliderManager main;

		// 键加入星球维度，避免不同星球同一角度索引复用旧星球的碰撞块
		private Dictionary<(Planet, int), Chunk> chunks = new Dictionary<(Planet, int), Chunk>();

		// 空闲碰撞块对象池：复用 GameObject 与 PolygonCollider2D，避免反复销毁/新建触发 Box2D 重建
		private List<Chunk> pool = new List<Chunk>();

		public Bool_Local hasColliders;

		private const int VerticesPerChunk = 10;

		private void Awake()
		{
			main = this;
		}

		public void AddChunks(List<int> indexes, TerrainColliderModule owner, Planet planet)
		{
			double angularChunkSize = GetAngularChunkSize(planet);
			foreach (int index in indexes)
			{
				(Planet, int) key = (planet, index);
				if (!chunks.ContainsKey(key))
				{
					chunks[key] = GetPooledChunk(angularChunkSize * (double)index, angularChunkSize, planet);
				}
				chunks[key].owners.Add(owner);
			}
			if (chunks.Count > 0)
			{
				hasColliders.Value = true;
			}
		}

		private Chunk GetPooledChunk(double from, double size, Planet planet)
		{
			Chunk chunk;
			if (pool.Count > 0)
			{
				int last = pool.Count - 1;
				chunk = pool[last];
				pool.RemoveAt(last);
				chunk.Rebuild(from, size, planet);
			}
			else
			{
				chunk = new Chunk(base.transform);
				chunk.Rebuild(from, size, planet);
			}
			return chunk;
		}

		public void RemoveChunks(List<int> indexes, TerrainColliderModule owner, Planet planet)
		{
			foreach (int index in indexes)
			{
				(Planet, int) key = (planet, index);
				if (!chunks.TryGetValue(key, out Chunk chunk))
				{
					Debug.LogError("Couldn't remove chunk: " + index);
					continue;
				}
				chunk.owners.Remove(owner);
				if (chunk.owners.Count <= 0)
				{
					chunks.Remove(key);
					chunk.Release();
					pool.Add(chunk);
				}
			}
			if (chunks.Count == 0)
			{
				hasColliders.Value = false;
			}
		}

		public static double GetAngularChunkSize(Planet planet)
		{
			if (planet.angularChunkSizeCache.HasValue)
			{
				return planet.angularChunkSizeCache.Value;
			}
			double chunkSize_Angular = planet.data.terrain.GetChunkSize_Angular(8, planet.GetMaxLOD());
			int verticeCount = planet.GetVerticeCount(chunkSize_Angular, planet.data.terrain.verticeSize);
			double size = chunkSize_Angular / (double)(verticeCount - 1) * 10.0;
			planet.angularChunkSizeCache = size;
			return size;
		}
	}
}
