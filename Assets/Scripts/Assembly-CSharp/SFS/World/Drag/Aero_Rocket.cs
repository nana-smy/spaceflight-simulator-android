using System.Collections.Generic;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.World.Drag
{
	public class Aero_Rocket : AeroModule
	{
		[Space]
		public Rocket rocket;

		protected override bool PhysicsMode => rocket.physics.PhysicsMode;

		protected override Location GetLocation()
		{
			return rocket.location.Value;
		}

		protected override List<Surface> GetDragSurfaces(Matrix2x2 rotate)
		{
			return GetDragSurfaces(rocket.partHolder, rotate);
		}

		public static List<Surface> GetDragSurfaces(PartHolder partsHolder, Matrix2x2 rotate)
		{
			List<Surface> output = dragSurfacesCache;
			output.Clear();
			SurfaceData[] modules = partsHolder.GetModules<SurfaceData>();
			foreach (SurfaceData surfaceData in modules)
			{
				if (!surfaceData.Drag)
				{
					continue;
				}
				Transform t = surfaceData.transform;
				Vector3 lossyScale = t.lossyScale;
				bool flip = lossyScale.x > 0f != lossyScale.y > 0f;
				Matrix4x4 matrix = t.localToWorldMatrix;
				HeatModuleBase heatModule = surfaceData.heatModule;
				foreach (Surfaces item in surfaceData.surfacesFast)
				{
					int num = item.points.Length;
					Vector2[] array = GetTransformedPoints(item.points, matrix, rotate, num);
					for (int num2 = 0; num2 < num - 1; num2++)
					{
						AddLine(array[num2], array[num2 + 1]);
					}
					if (item.loop)
					{
						AddLine(array[num - 1], array[0]);
					}
				}
				void AddLine(Vector2 a, Vector2 b)
				{
					if (flip)
					{
						Vector2 vector = b;
						Vector2 vector2 = a;
						a = vector;
						b = vector2;
					}
					if (a.x < b.x && a.x != b.x)
					{
						output.Add(new Surface(heatModule, heatModule.valid, new Line2(a, b)));
					}
				}
			}
			return output;
		}

		private static readonly List<Surface> dragSurfacesCache = new List<Surface>();

		private static readonly Dictionary<int, Vector2[]> transformedPointsCache = new Dictionary<int, Vector2[]>();

		private static Vector2[] GetTransformedPoints(Vector2[] points, Matrix4x4 matrix, Matrix2x2 rotate, int length)
		{
			if (!transformedPointsCache.TryGetValue(length, out Vector2[] array))
			{
				array = new Vector2[length];
				transformedPointsCache[length] = array;
			}
			for (int i = 0; i < length; i++)
			{
				array[i] = matrix.MultiplyPoint(points[i]) * rotate;
			}
			return array;
		}

		public void ApplyParachuteDrag(ref float force, ref Vector2 centerOfDrag_World)
		{
			ParachuteModule[] modules = rocket.partHolder.GetModules<ParachuteModule>();
			foreach (ParachuteModule parachuteModule in modules)
			{
				if (parachuteModule.targetState.Value == 1f || parachuteModule.targetState.Value == 2f)
				{
					float num = (float)WorldView.ToGlobalVelocity(rocket.rb2d.GetPointVelocity(parachuteModule.parachute.position)).sqrMagnitude * parachuteModule.drag.Evaluate(parachuteModule.state.Value);
					centerOfDrag_World = (centerOfDrag_World * force + (Vector2)parachuteModule.parachute.position * num) / (force + num);
					force += num;
				}
			}
		}

		protected override void AddForceAtPosition(Vector2 force, Vector2 position)
		{
			rocket.rb2d.AddForceAtPosition(force, position, ForceMode2D.Force);
		}

		protected override float GetMass()
		{
			return rocket.rb2d.mass;
		}
	}
}
