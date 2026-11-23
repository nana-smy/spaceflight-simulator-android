using SFS.WorldBase;
using SFS.World.PlanetModules;
using UnityEngine;

namespace SFS.World
{
	public class Atmosphere : MonoBehaviour
	{
		public Planet planet;

		private Mesh mesh;

		private void OnDestroy()
		{
			Object.Destroy(mesh);
		}

		private void Update()
		{
			if (planet.HasAtmosphereVisuals && WorldTime.main != null && planet.data != null)
			{
				planet.atmosphereMaterial.SetFloat("_PosX", (float)(WorldTime.main.worldTime * 5.0 / planet.Radius * (double)planet.data.atmosphereVisuals.CLOUDS.velocity % 1.0));
			}
			if (planet.atmosphereMaterial != null && WorldView.main != null && planet.data != null)
			{
				try
				{
					float ppHeight = (float)WorldView.main.ViewLocation.Height + (float)WorldView.main.viewDistance;
					PostProcessingModule.Key ppKey = ((planet.data.hasPostProcessing && planet.data.postProcessing != null && planet.data.postProcessing.keys != null && planet.data.postProcessing.keys.Length > 0) ? planet.data.postProcessing.Evaluate(ppHeight) : new PostProcessingModule.Key(0f, 1.75f, 1f, 0f, 1f, 1.1f, 1f, 1f, 1f));
					planet.atmosphereMaterial.SetFloat("_PP_Hue", ppKey.hueShift);
					planet.atmosphereMaterial.SetFloat("_PP_Saturation", ppKey.saturation);
					// 扩展包暗色大气（渐变贴图为 None）时对比度取中性值，避免暗蓝被压成黑
					float ppContrast = ((planet.data.atmosphereVisuals != null && planet.data.atmosphereVisuals.GRADIENT.texture == "None") ? 1f : ppKey.contrast);
					planet.atmosphereMaterial.SetFloat("_PP_Contrast", ppContrast);
					planet.atmosphereMaterial.SetVector("_PP_Multiplier", new Vector4(ppKey.red, ppKey.green, ppKey.blue, 1f));
				}
				catch
				{
					// 场景切换期间数据失效，跳过本帧
				}
			}
		}
		public static Atmosphere Create(Planet planet, Transform parent, Transform atmospherePrefab)
		{
			Transform obj = Object.Instantiate(atmospherePrefab.transform);
			obj.name = planet.codeName + " Atmosphere";
			obj.parent = parent;
			obj.localPosition = Vector3.forward * planet.data.atmosphereVisuals.GRADIENT.positionZ;
			obj.transform.localScale = Vector3.one * (float)(planet.data.atmosphereVisuals.GRADIENT.height + planet.Radius);
			MeshFilter filter = obj.GetComponent<MeshFilter>();
			Mesh mesh = filter.sharedMesh;
			if (mesh == null || mesh.vertexCount == 0)
			{
				mesh = GenerateSphereMesh(48, 24);
				filter.sharedMesh = mesh;
			}
			else
			{
				mesh = filter.mesh;
			}
			// 强制 3D 包围盒（圆盘资产包围盒 Z 厚度为 0，会被视锥剔除导致不显示）
			mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
			// 原版代码：网格 UV 为 u=环绕一周 / v=径向渐变，着色器用它采样云层（避免世界坐标重复纹理）
			mesh.uv = GetUV(mesh.uv.Length);
			obj.GetComponent<MeshRenderer>().material = planet.atmosphereMaterial;
			Atmosphere atmosphere = obj.gameObject.AddComponent<Atmosphere>();
			atmosphere.planet = planet;
			atmosphere.mesh = mesh;
			return atmosphere;
		}

		private static Mesh GenerateSphereMesh(int segments, int rings)
		{
			Mesh mesh = new Mesh();
			Vector3[] vertices = new Vector3[(rings + 1) * (segments + 1)];
			Vector2[] uv = new Vector2[vertices.Length];
			int[] triangles = new int[rings * segments * 6];
			int v = 0;
			for (int i = 0; i <= rings; i++)
			{
				float phi = Mathf.PI * i / rings;
				float y = Mathf.Cos(phi);
				float r = Mathf.Sin(phi);
				for (int j = 0; j <= segments; j++)
				{
					float theta = 2f * Mathf.PI * j / segments;
					vertices[v] = new Vector3(r * Mathf.Cos(theta), y, r * Mathf.Sin(theta));
					uv[v] = new Vector2((float)j / segments, 1f - (float)i / rings);
					v++;
				}
			}
			int t = 0;
			for (int i = 0; i < rings; i++)
			{
				for (int j = 0; j < segments; j++)
				{
					int a = i * (segments + 1) + j;
					int b = a + segments + 1;
					triangles[t++] = a;
					triangles[t++] = b;
					triangles[t++] = a + 1;
					triangles[t++] = b;
					triangles[t++] = b + 1;
					triangles[t++] = a + 1;
				}
			}
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		public void SetLayer(string layer)
		{
			base.gameObject.layer = LayerMask.NameToLayer(layer);
		}

		private static Vector2[] GetUV(int verticeCount)
		{
			Vector2[] array = new Vector2[verticeCount];
			float num = 1f / (float)(verticeCount - 2);
			for (int i = 1; i < array.Length; i++)
			{
				array[i] = new Vector2((float)(i - 1) * num, 1f);
			}
			array[0] = Vector2.zero;
			return array;
		}
	}
}

