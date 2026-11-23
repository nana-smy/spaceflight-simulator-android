using System;
using System.Collections.Generic;
using System.Linq;
using SFS.World.PlanetModules;
using SFS.World.Terrain;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World
{
	public class WorldEnvironment : MonoBehaviour
	{
		private static readonly int Fog = Shader.PropertyToID("_Fog");
		private static readonly Vector2[] ViewCorners = new Vector2[4]
		{
			new Vector2(0f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f),
			new Vector2(1f, 0f)
		};

		public Transform chunkPrefab;

		public Transform[] rockPrefabs;

		public Transform atmospherePrefab;

		public Environment[] environments;

		public WorldParticle particle;

		private WorldParticle[] Options => ResourcesLoader.GetFiles_Array<WorldParticle>("");

		public void Initialize(bool launchPlanetOnly)
		{
			environments = CreateEnvironments(launchPlanetOnly);
			WorldView.main.scaledSpace.OnChange += new Action(OnScaledSpaceChange);
			WorldView.main.viewDistance.OnChange += new Action(UpdateFog);
			WorldView.main.positionOffset.OnChange += new Action(UpdateViewPosition);
		}

		private void OnScaledSpaceChange()
		{
			string layer = (WorldView.main.scaledSpace ? "Scaled Space" : "Celestial Body");
			Environment[] array = environments;
			foreach (Environment obj in array)
			{
				obj.holder.transform.localScale = Vector3.one / (WorldView.main.scaledSpace ? 10000f : 1f);
				obj.terrain?.SetLayer(layer);
				obj.atmosphere?.SetLayer(layer);
			}
			UpdateViewPosition();
		}

		private static void UpdateFog()
		{
			foreach (Planet value2 in Base.planetLoader.planets.Values)
			{
				if (value2.data.hasAtmosphereVisuals && value2.data.hasTerrain)
				{
					Color value = value2.data.atmosphereVisuals.FOG.Evaluate(WorldView.main.viewDistance);
					value2.terrainMaterial.SetColor(Fog, value);
				}
			}
		}

		private void UpdateAtmospherePosition()
		{
			if (WorldView.main == null)
			{
				return;
			}
			Location viewLocation = WorldView.main.ViewLocation;
			float val = (viewLocation.planet != null && viewLocation.planet.data != null && viewLocation.planet.data.hasAtmosphereVisuals ? ((float)Math.Min(viewLocation.Height, viewLocation.planet.data.atmosphereVisuals.GRADIENT.height)) : 0f) * 0.7f / GetViewRangeNormal();
			Environment[] array = environments;
			foreach (Environment environment in array)
			{
				Planet planet = environment.planet;
				if (planet.data != null && planet.data.hasAtmosphereVisuals)
				{
					// 原版逻辑：大气盘随视野距离动态定位（拉远时盘向相机靠近，环投影保持相对宽度）
					float num = planet.data.atmosphereVisuals.GRADIENT.positionZ;
					float num2 = ((planet == viewLocation.planet) ? Math.Max(val, num) : num);
					num2 = Mathf.Max(num2 - WorldView.main.viewDistance.Value * 0.25f, 0f);
					environment.atmosphere.transform.localPosition = Vector3.forward * num2;
				}
			}
		}
		private static float GetViewRangeNormal()
		{
			Camera worldCamera = ((!(GameManager.main != null)) ? WorldView.main.worldCamera.camera : (WorldView.main.scaledSpace.Value ? GameCamerasManager.main.scaledWorld_Camera.camera : GameCamerasManager.main.world_Camera.camera));
			Double2 position = WorldView.main.ViewLocation.position;
			Vector2 size = new Vector2(Screen.width, Screen.height);
			Vector3 center = worldCamera.ScreenToWorldPoint((size / 2f).ToVector3(1f));
			float maxY = float.MinValue;
			Vector2[] viewCorners = ViewCorners;
			for (int i = 0; i < viewCorners.Length; i++)
			{
				Vector2 diff = worldCamera.ScreenToWorldPoint((size * viewCorners[i]).ToVector3(1f)) - center;
				diff = diff.Rotate_Radians(0f - (float)position.AngleRadians).Rotate_90();
				if (diff.y > maxY)
				{
					maxY = diff.y;
				}
			}
			return Mathf.Lerp(maxY, WorldView.main.worldCamera.ViewSizeNormal, 0.1f);
		}

		private void LateUpdate()
		{
			UpdateAtmospherePosition();
			UpdateViewPosition();
		}

		private void UpdateViewPosition()
		{
			Planet planet = WorldView.main.ViewLocation.planet;
			if (planet == null)
			{
				return;
			}
			double time = ((WorldTime.main != null) ? WorldTime.main.worldTime : 0.0);
			Double2 solarSystemPosition = WorldView.main.ViewLocation.GetSolarSystemPosition(time);
			Environment[] array;
			if (!WorldView.main.scaledSpace)
			{
				Double2 solarSystemPosition2 = planet.GetSolarSystemPosition(time);
				array = environments;
				foreach (Environment environment in array)
				{
					Double2 solarSystemPosition3 = environment.planet.GetSolarSystemPosition(time);
					if (double.IsNaN(solarSystemPosition3.x) || double.IsNaN(solarSystemPosition3.y) || double.IsInfinity(solarSystemPosition3.x) || double.IsInfinity(solarSystemPosition3.y))
						{
							continue;
						}
					environment.holder.position = WorldView.ToLocalPosition(solarSystemPosition3 - solarSystemPosition2);
					if (environment.terrain != null)
					{
						environment.terrain.SetViewPosition(new Double3(solarSystemPosition.x - solarSystemPosition3.x, solarSystemPosition.y - solarSystemPosition3.y, (float)WorldView.main.viewDistance));
					}
				}
				return;
			}
			array = environments;
			foreach (Environment environment2 in array)
			{
				Double2 @double = environment2.planet.GetSolarSystemPosition() - solarSystemPosition;
				if (double.IsNaN(@double.x) || double.IsNaN(@double.y) || double.IsInfinity(@double.x) || double.IsInfinity(@double.y))
					{
						continue;
					}
				environment2.holder.position = new Vector3((float)@double.x / 10000f, (float)@double.y / 10000f, (float)WorldView.main.viewDistance / 10000f);
				if (environment2.terrain != null)
				{
					environment2.terrain.SetViewPosition(new Double3(0.0 - @double.x, 0.0 - @double.y, (float)WorldView.main.viewDistance));
				}
			}
		}

		private Environment[] CreateEnvironments(bool launchPlanetOnly)
		{
			double worldTime = ((WorldTime.main != null) ? WorldTime.main.worldTime : 0.0);
			Double2 viewSolarSystemPosition = WorldView.main.ViewLocation.GetSolarSystemPosition(worldTime);
			List<Environment> output = new List<Environment>();
			if (launchPlanetOnly)
			{
				Create(Base.planetLoader.spaceCenter.address.GetPlanet());
			}
			else
			{
				foreach (Planet value in Base.planetLoader.planets.Values)
				{
					Create(value);
				}
			}
			return output.ToArray();
			void Create(Planet planet)
			{
				Double2 solarSystemPosition = planet.GetSolarSystemPosition(worldTime);
				output.Add(CreateEnvironment(planet, new Double3(viewSolarSystemPosition.x - solarSystemPosition.x, viewSolarSystemPosition.y - solarSystemPosition.y, (float)WorldView.main.viewDistance)));
			}
		}

		private Environment CreateEnvironment(Planet planet, Double3 viewPosition)
		{
			Environment environment = new Environment
			{
				planet = planet,
				holder = new GameObject(planet.codeName + " Environment").transform
			};
			environment.holder.parent = base.transform;
			if (planet.data.hasTerrain)
			{
				TerrainModule.RockData rockData = planet.data.terrain.rocks;
				Transform transform = ((rockData != null) ? rockPrefabs.FirstOrDefault((Transform a) => a.name == rockData.rockType) : null);
				environment.terrain = DynamicTerrain.Create(planet, viewPosition, null, 1f, 1f, "Default", planet.terrainMaterial, useTerrainUV: true, chunkPrefab, (transform != null) ? rockData : null, transform);
				environment.terrain.transform.parent = environment.holder;
			}
			if (planet.HasAtmosphereVisuals)
			{
				environment.atmosphere = Atmosphere.Create(planet, environment.holder, atmospherePrefab);
			}
			return environment;
		}

		public void SpawnGroundParticles(Vector2 position, Vector2 velocity, Vector2 surfaceNormal, int count)
		{
			Color terrainColor = WorldView.main.ViewLocation.planet.GetTerrainColor(WorldView.ToGlobalPosition(position));
			ParticleSystem.EmitParams[] array = new ParticleSystem.EmitParams[count];
			for (int i = 0; i < array.Length; i++)
			{
				Color color = terrainColor * UnityEngine.Random.Range(0.8f, 1.1f);
				color.a = 1f;
				array[i] = new ParticleSystem.EmitParams
				{
					position = position + surfaceNormal * UnityEngine.Random.Range(-1f, 1f),
					velocity = velocity + UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(0f, 2f),
					startSize3D = new Vector3(UnityEngine.Random.Range(0.5f, 1f), UnityEngine.Random.Range(0.5f, 1f), 1f) * UnityEngine.Random.Range(0.05f, 0.25f),
					startColor = color
				};
			}
			particle.Spawn(array);
		}
	}
}
