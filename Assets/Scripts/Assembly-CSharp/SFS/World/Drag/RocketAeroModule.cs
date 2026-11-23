using System.Collections.Generic;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.World.Drag
{
	// 2.5D 气动模块：升力 = 0.5 * rho * V^2 * CL(aoa) * (暴露面长度 * width差值)
	// 性能优化：
	//   1. 缓存零件 PolygonData / 包围盒 / BaseDepth；
	//   2. 包围盒粗筛，只对候选零件做精确 Raycast；
	//   3. 零件变动时标记脏，分帧重建缓存，避免解体/爆炸单帧卡顿。
	public class RocketAeroModule : MonoBehaviour
	{
		public Rocket rocket;

		[Header("升力")]
		public float liftCoefficient = 1f;
		public float stallAngle = 35f;

		[Header("阻力")]
		public float dragCoefficient = 1.5f;

		[Header("音速")]
		public float speedOfSoundSeaLevel = 340f;

		[Header("缓存重建")]
		public int rebuildBatchSize = 64;
		public float boundsPadding = 0.1f;

		[Header("最大动压记录")]
		public float maxQ;
		public float maxQHeight;
		public float maxQSpeed;
		public float maxQTime;

		private sealed class PartAeroData
		{
			public Part part;
			public readonly List<PolygonData> polygons = new List<PolygonData>();
			public Bounds bounds;
		}

		private readonly List<PartAeroData> cachedParts = new List<PartAeroData>();
		private readonly List<Part> pendingParts = new List<Part>();
		private bool cacheDirty = true;
		private bool rebuildInProgress;
		private int rebuildIndex;
		private bool subscribed;

		private static readonly List<float> depthBuffer = new List<float>();

		private void FixedUpdate()
		{
			if (rocket == null)
			{
				rocket = GetComponent<Rocket>();
			}
			if (rocket == null || rocket.rb2d == null || rocket.partHolder == null)
			{
				return;
			}

			TrySubscribe();
			ProcessCacheRebuild();

			Location location = rocket.location.Value;
			if (!AeroModule.IsInsideAtmosphereAndIsMoving(location))
			{
				return;
			}

			// 与作弊菜单"无大气阻力"开关联动（与原版 AeroModule 的 flag 一致）：
			// 开启 noAtmosphericDrag 或物理未运行时，跳过全部气动力（阻力/升力/降落伞）
			if (!rocket.physics.PhysicsMode || SandboxSettings.main.settings.noAtmosphericDrag)
			{
				return;
			}

			Vector2 velocity = location.velocity.ToVector2;
			float speed = velocity.magnitude;
			if (speed < 0.1f)
			{
				return;
			}

			float rho = (float)location.planet.GetAtmosphericDensity(location.Height);
			if (rho <= 0.000001f)
			{
				return;
			}

			float q = 0.5f * rho * speed * speed;
			TrackMaxQ(q, location, speed);

			float speedOfSound = GetSpeedOfSound(location, rho);
			float mach = speed / speedOfSound;
			float cdTransonic = GetTransonicDragMultiplier(mach);

			// 和原版 AeroModule 保持一致的坐标系：+Y = 气流方向
			float velocityAngle = (float)location.velocity.AngleRadians - Mathf.PI / 2f;
			Matrix2x2 rotate = Matrix2x2.Angle(-velocityAngle);
			Matrix2x2 localToWorld = Matrix2x2.Angle(velocityAngle);

			List<Surface> surfaces = Aero_Rocket.GetDragSurfaces(rocket.partHolder, rotate);
			List<Surface> exposedSurfaces = AeroModule.GetExposedSurfaces(surfaces);
			if (exposedSurfaces.Count == 0)
			{
				return;
			}

			Vector2 velocityDir = velocity / speed;
			ApplyParachuteDrag(velocityDir, rho);

			// 和原版 AeroModule 一样：先汇总总阻力和总压心，再把合力加到压心。
			// 不在每个暴露面中点分别加力，避免产生额外力矩导致火箭乱转。
			float totalDrag = 0f;
			Vector2 dragCenterWeighted = Vector2.zero;

			foreach (Surface surface in exposedSurfaces)
			{
				Vector2 line = surface.line.end - surface.line.start;
				float exposedLength = line.magnitude;
				if (exposedLength < 0.001f)
				{
					continue;
				}

				// 升力系数：CL=2·sinα·cosα。sinα 必须用带符号的 line.x——
				// 暴露面左右两侧线段 dx 同号但 dy 反号，带符号后左右升力自动反向、
				// 0 攻角精确抵消，攻角下由暴露面不对称产生恢复力矩。
				// （原实现误用 |line.x| 导致左右升力同向叠加、压心前置、发散乱转）
				float sinA = line.x / exposedLength;
				float cosA = line.y / exposedLength;
				float aoaDeg = Mathf.Asin(Mathf.Clamp(Mathf.Abs(sinA), 0f, 1f)) * Mathf.Rad2Deg;
				float cl = 2f * sinA * cosA;
				if (aoaDeg > stallAngle)
				{
					cl *= Mathf.Max(0f, (90f - aoaDeg) / (90f - stallAngle));
				}

				Vector2 surfaceCenterLocal = (surface.line.start + surface.line.end) * 0.5f;
				Vector2 surfaceCenterWorld = localToWorld * surfaceCenterLocal;

				CollectDepths(surfaceCenterWorld, depthBuffer);
				if (depthBuffer.Count == 0)
				{
					continue;
				}

				float previousDepth = 0f;
				foreach (float depth in depthBuffer)
				{
					float delta = Mathf.Max(0f, depth - previousDepth);
					previousDepth = Mathf.Max(previousDepth, depth);
					if (delta < 0.001f)
					{
						continue;
					}

					float referenceArea = exposedLength * delta;

					// 阻力汇总后在总压心统一施加
					float drag = dragCoefficient * cdTransonic * q * referenceArea;
					totalDrag += drag;
					dragCenterWeighted += surfaceCenterWorld * drag;

					// 升力按各面压心施加
					if (liftCoefficient > 0.001f)
					{
						float lift = liftCoefficient * cl * q * referenceArea;
						Vector2 liftWorld = localToWorld * new Vector2(lift, 0f);
						if (!float.IsNaN(liftWorld.x + liftWorld.y + surfaceCenterWorld.x + surfaceCenterWorld.y))
						{
							rocket.rb2d.AddForceAtPosition(liftWorld, surfaceCenterWorld);
						}
					}
				}
			}

			if (totalDrag > 0.0001f)
			{
				Vector2 centerOfDrag = dragCenterWeighted / totalDrag;
				centerOfDrag = Vector2.Lerp(rocket.rb2d.worldCenterOfMass, centerOfDrag, 0.2f);
				Vector2 dragForce = -velocityDir * totalDrag;
				if (!float.IsNaN(dragForce.x + dragForce.y + centerOfDrag.x + centerOfDrag.y))
				{
					rocket.rb2d.AddForceAtPosition(dragForce, centerOfDrag);
				}
			}
		}

		private void TrySubscribe()
		{
			if (subscribed || rocket.partHolder == null)
			{
				return;
			}

			rocket.partHolder.onPartsAdded += OnPartsAdded;
			rocket.partHolder.onPartsRemoved += OnPartsRemoved;
			rocket.partHolder.onPartsChanged += OnPartsChanged;
			subscribed = true;
			MarkDirty();
		}

		private void OnPartsAdded(Part[] _) => MarkDirty();
		private void OnPartsRemoved(Part[] _) => MarkDirty();
		private void OnPartsChanged() => MarkDirty();

		private void MarkDirty()
		{
			cacheDirty = true;
			rebuildInProgress = false;
		}

		private void ProcessCacheRebuild()
		{
			if (!cacheDirty)
			{
				return;
			}

			if (!rebuildInProgress)
			{
				pendingParts.Clear();
				foreach (Part part in rocket.partHolder.parts)
				{
					if (part != null)
					{
						pendingParts.Add(part);
					}
				}

				cachedParts.Clear();
				rebuildIndex = 0;
				rebuildInProgress = true;
			}

			int end = Mathf.Min(rebuildIndex + rebuildBatchSize, pendingParts.Count);
			for (int i = rebuildIndex; i < end; i++)
			{
				PartAeroData data = BuildAeroData(pendingParts[i], rocket.partHolder.transform);
				if (data != null)
				{
					cachedParts.Add(data);
				}
			}
			rebuildIndex = end;

			if (rebuildIndex >= pendingParts.Count)
			{
				rebuildInProgress = false;
				cacheDirty = false;
			}
		}

		private PartAeroData BuildAeroData(Part part, Transform parentTransform)
		{
			if (part == null || !part.isActiveAndEnabled)
			{
				return null;
			}

			PolygonData[] polygonModules = part.GetModules<PolygonData>();
			if (polygonModules == null || polygonModules.Length == 0)
			{
				return null;
			}

			PartAeroData data = new PartAeroData { part = part };
			bool hasBounds = false;
			foreach (PolygonData polygonData in polygonModules)
			{
				if (polygonData == null || !polygonData.isActiveAndEnabled)
				{
					continue;
				}

				data.polygons.Add(polygonData);
				Bounds partBounds = ComputeLocalBounds(polygonData, parentTransform);
				if (!hasBounds)
				{
					data.bounds = partBounds;
					hasBounds = true;
				}
				else
				{
					data.bounds.Encapsulate(partBounds);
				}
			}

			if (!hasBounds)
			{
				return null;
			}

			return data;
		}

		private Bounds ComputeLocalBounds(PolygonData polygonData, Transform parentTransform)
		{
			Vector2[] worldVertices = polygonData.polygon.GetVerticesWorld(polygonData.transform);
			if (worldVertices == null || worldVertices.Length == 0)
			{
				Vector3 localPosition = parentTransform.InverseTransformPoint(polygonData.transform.position);
				return new Bounds(localPosition, new Vector3(boundsPadding, boundsPadding, Mathf.Abs(polygonData.BaseDepth) + boundsPadding));
			}

			Vector3 min = parentTransform.InverseTransformPoint(new Vector3(worldVertices[0].x, worldVertices[0].y, 0f));
			Vector3 max = min;
			for (int i = 1; i < worldVertices.Length; i++)
			{
				Vector3 localVertex = parentTransform.InverseTransformPoint(new Vector3(worldVertices[i].x, worldVertices[i].y, 0f));
				min = Vector3.Min(min, localVertex);
				max = Vector3.Max(max, localVertex);
			}

			Vector3 size = (max - min) + new Vector3(boundsPadding * 2f, boundsPadding * 2f, boundsPadding);
			Vector3 center = (min + max) * 0.5f;
			size.z = Mathf.Abs(polygonData.BaseDepth) + boundsPadding;
			return new Bounds(center, size);
		}

		private void CollectDepths(Vector2 worldPoint, List<float> depths)
		{
			depths.Clear();
			Vector3 localPoint = rocket.partHolder.transform.InverseTransformPoint(new Vector3(worldPoint.x, worldPoint.y, 0f));

			foreach (PartAeroData data in cachedParts)
			{
				if (data.part == null || !data.part.isActiveAndEnabled)
				{
					MarkDirty();
					continue;
				}

				if (!data.bounds.Contains(localPoint))
				{
					continue;
				}

				float depth = GetPartDepthAtPoint(data, worldPoint);
				if (depth > 0.001f)
				{
					depths.Add(depth);
				}
			}

			depths.Sort();
		}

		private static float GetPartDepthAtPoint(PartAeroData data, Vector2 worldPoint)
		{
			foreach (PolygonData polygonData in data.polygons)
			{
				Vector2 localPoint = polygonData.transform.InverseTransformPoint(worldPoint);
				if (polygonData.Raycast(localPoint, out float depth))
				{
					return Mathf.Abs(depth);
				}
			}
			return 0f;
		}

		private void ApplyParachuteDrag(Vector2 velocityDir, float rho)
		{
			Aero_Rocket aeroRocket = GetComponent<Aero_Rocket>();
			if (aeroRocket == null)
			{
				return;
			}

			float parachuteForce = 0f;
			Vector2 parachuteCenter = rocket.rb2d.worldCenterOfMass;
			aeroRocket.ApplyParachuteDrag(ref parachuteForce, ref parachuteCenter);

			if (parachuteForce > 0.0001f && !float.IsNaN(parachuteCenter.x + parachuteCenter.y))
			{
				rocket.rb2d.AddForceAtPosition(-velocityDir * (parachuteForce * rho), parachuteCenter);
			}
		}

		private void TrackMaxQ(float q, Location location, float speed)
		{
			if (q > maxQ)
			{
				maxQ = q;
				maxQHeight = (float)location.Height;
				maxQSpeed = speed;
				maxQTime = Time.unscaledTime;
			}
		}

		private float GetSpeedOfSound(Location location, float rho)
		{
			float rho0 = (float)location.planet.GetAtmosphericDensity(0.0);
			if (rho0 <= 0.000001f)
			{
				return speedOfSoundSeaLevel;
			}
			// T/T0 ≈ (rho/rho0)^(gamma-1)，a ≈ a0 * sqrt(T/T0)
			float ratio = Mathf.Clamp(rho / rho0, 0.001f, 1f);
			return speedOfSoundSeaLevel * Mathf.Pow(ratio, 0.2f);
		}

		private static float GetTransonicDragMultiplier(float mach)
		{
			if (mach < 0.7f)
			{
				return 1f;
			}
			if (mach <= 1.2f)
			{
				float t = (mach - 0.7f) / 0.5f;
				return 1f + 1.2f * Mathf.Sin(t * Mathf.PI);
			}
			return 1f + 0.2f / Mathf.Max(1f, mach - 1f);
		}

		private void OnDestroy()
		{
			if (subscribed && rocket != null && rocket.partHolder != null)
			{
				rocket.partHolder.onPartsAdded -= OnPartsAdded;
				rocket.partHolder.onPartsRemoved -= OnPartsRemoved;
				rocket.partHolder.onPartsChanged -= OnPartsChanged;
			}
		}
	}
}
