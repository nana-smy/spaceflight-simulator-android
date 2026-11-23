using System;
using System.Collections.Generic;
using UnityEngine;

public class GLDrawer : MonoBehaviour
{
	public static GLDrawer main;

	public Material material;

	private Dictionary<float, Material> sortedMaterials = new Dictionary<float, Material>();

	public static List<I_GLDrawer> drawers = new List<I_GLDrawer>();

	private void Awake()
	{
		main = this;
	}

	public static void Register(I_GLDrawer drawer)
	{
		drawers.Add(drawer);
	}

	public static void Unregister(I_GLDrawer drawer)
	{
		drawers.Remove(drawer);
	}

	private void OnPostRender()
	{
		foreach (I_GLDrawer drawer in drawers)
		{
			drawer.Draw();
		}
	}

	public static void DrawLine(Vector3 start, Vector3 end, Color color, float width, float sortingOrder = 1f)
	{
		GL.Begin(7);
		GetMaterial(sortingOrder).SetPass(0);
		GL.Color(color);
		Vector3 vector = Vector2.Perpendicular((start - end).normalized) * width * 0.5f;
		GL.Vertex(start - vector);
		GL.Vertex(end - vector);
		GL.Vertex(end + vector);
		GL.Vertex(start + vector);
		GL.End();
	}

	public static void DrawCircle(Vector2 position, float radius, int resolution, Color color, float sortingOrder = 1f)
	{
		GL.Begin(7);
		GetMaterial(sortingOrder)?.SetPass(0);
		GL.Color(color);
		Vector2[] array = GetUnitCirclePoints(resolution);
		DrawCircleAt(position, array, radius);
		GL.End();
	}

	public static void DrawCircles(List<Vector2> positions, float radius, int resolution, Color color, float sortingOrder = 1f)
	{
		GL.Begin(7);
		GetMaterial(sortingOrder)?.SetPass(0);
		GL.Color(color);
		Vector2[] array = GetUnitCirclePoints(resolution);
		foreach (Vector2 position in positions)
		{
			DrawCircleAt(position, array, radius);
		}
		GL.End();
	}

	private static readonly Dictionary<int, Vector2[]> unitCirclePointsCache = new Dictionary<int, Vector2[]>();

	private static Vector2[] GetUnitCirclePoints(int resolution)
	{
		if (unitCirclePointsCache.TryGetValue(resolution, out Vector2[] points))
		{
			return points;
		}
		points = new Vector2[resolution];
		float num = MathF.PI * 2f / (float)resolution;
		for (int i = 0; i < resolution; i++)
		{
			points[i] = new Vector2(Mathf.Cos(num * (float)i), Mathf.Sin(num * (float)i));
		}
		unitCirclePointsCache[resolution] = points;
		return points;
	}

	private static void DrawCircleAt(Vector2 position, Vector2[] points, float radius)
	{
		for (int j = 0; j < points.Length; j++)
		{
			GL.Vertex(position);
			GL.Vertex(position + points[(j + 1) % points.Length] * radius);
			GL.Vertex(position + points[j] * radius);
			GL.Vertex(position);
		}
	}

	private static Material GetMaterial(float sortingOrder)
	{
		if (!main)
		{
			return null;
		}
		if (!main.sortedMaterials.ContainsKey(sortingOrder))
		{
			Material material = new Material(main.material);
			material.SetFloat("_Depth", sortingOrder);
			main.sortedMaterials.Add(sortingOrder, material);
			return material;
		}
		return main.sortedMaterials[sortingOrder];
	}
}
