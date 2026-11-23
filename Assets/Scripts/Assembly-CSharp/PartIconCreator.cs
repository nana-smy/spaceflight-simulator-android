using System;
using System.Linq;
using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PartIconCreator : MonoBehaviour
{
	public static PartIconCreator main;

	public GameObject _camera;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
	}

	public Texture2D CreatePartIcon_TechTree(VariantRef[] parts, int height, bool center, float maxAspectRatio)
	{
		Part[] array = parts.Select((VariantRef part) => PartsLoader.CreatePart(part, updateAdaptation: true)).ToArray();
		PositionParts(center, array);
		Part_Utility.GetFramingBounds_WorldSpace(out var bounds, array);
		int width = (int)((float)height * Mathf.Min(bounds.width / bounds.height, maxAspectRatio));
		return RenderAndDestroy(array, bounds, width, height, Color.clear);
	}

	private static void PositionParts(bool center, Part[] createdParts)
	{
		float num = 0f;
		foreach (Part part in createdParts)
		{
			Part_Utility.GetFramingBounds_WorldSpace(out var bounds, part);
			part.transform.position = new Vector3(num + part.transform.position.x - bounds.xMin, part.transform.position.y - (center ? bounds.center.y : bounds.yMin));
			num += bounds.width + 0.5f;
		}
	}

	public Texture2D CreatePartIcon_Staging(PartSave a, int texWidth)
	{
		OwnershipState ownershipState;
		Part[] array = new Part[1] { PartsLoader.CreatePart(a, null, null, OnPartNotOwned.Allow, out ownershipState) };
		CloseLandingLegs(array);
		Part_Utility.GetFramingBounds_WorldSpace(out var bounds, array);
		if (bounds.width < 2f)
		{
			float x = 1.8f - bounds.width;
			Vector2 vector = new Vector2(x, 0f);
			bounds = new Rect(bounds.position - vector / 1.8f, bounds.size + vector);
		}
		return RenderAndDestroy(array, bounds, texWidth, (int)((float)texWidth * (bounds.height / bounds.width)), Color.clear);
	}

	public Texture2D CreatePartIcon_Sharing(Blueprint blueprint, int width, int height)
	{
		OwnershipState[] ownershipState;
		Part[] array = PartsLoader.CreateParts(blueprint.parts, null, null, OnPartNotOwned.Allow, out ownershipState);
		Part_Utility.GetFramingBounds_WorldSpace(out var bounds, array);
		Vector2 vector = Vector2.one * 2f;
		bounds = new Rect(bounds.position - vector / 2f, bounds.size + vector);
		return RenderAndDestroy(array, bounds, width, height, new Color(0.239f, 0.376f, 0.576f, 1f));
	}

	public Texture2D CreatePartIcon_PickGrid(Part part, out Vector2 size)
	{
		Part_Utility.GetFramingBounds_WorldSpace(out var bounds, part);
		if (bounds.width < 2f)
		{
			bounds = Expand(bounds, (2f - bounds.width) / 3f, 0f);
		}
		float num = 0.75f;
		float num2 = 1.5f;
		size = new Vector2(200f, 200f * Mathf.Clamp(bounds.height / bounds.width, num, num2));
		if (bounds.height < bounds.width * num)
		{
			bounds = Expand(bounds, 0f, (bounds.width * num - bounds.height) / 2f);
		}
		if (bounds.width < bounds.height / num2)
		{
			bounds = Expand(bounds, (bounds.height / num2 - bounds.width) / 2f, 0f);
		}
		return RenderTextureToTexture2D(Render(new Part[1] { part }, bounds, Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), Color.clear));
	}

	private static Rect Expand(Rect rect, float x, float y)
	{
		return new Rect(rect.position - new Vector2(x, y), rect.size + new Vector2(x, y) * 2f);
	}

	private Texture2D RenderAndDestroy(Part[] createdParts, Rect rect, int width, int height, Color back)
	{
		RenderTexture renderTexture = Render(createdParts, rect, width, height, back);
		Texture2D result = RenderTextureToTexture2D(renderTexture);
		foreach (Part obj in createdParts)
		{
			obj.gameObject.SetActive(value: false);
			UnityEngine.Object.Destroy(obj.gameObject);
		}
		return result;
	}

	private static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
	{
		RenderTexture previous = RenderTexture.active;
		Texture2D texture2D = ImageTools.RenderTextureTo2D(renderTexture, renderTexture.width, renderTexture.height);
		RenderTexture.active = previous;
		renderTexture.Release();
		UnityEngine.Object.Destroy(renderTexture);
		return texture2D;
	}

	private RenderTexture Render(Part[] createdParts, Rect rect, int width, int height, Color back)
	{
		// 数据兜底：异常零件的包围盒可能为 NaN/0，避免 RenderTexture 创建失败导致整个图标列表中断
		if (width < 1)
		{
			width = 1;
		}
		if (height < 1)
		{
			height = 1;
		}
		if (float.IsNaN(rect.x) || float.IsNaN(rect.y) || float.IsNaN(rect.width) || float.IsNaN(rect.height) || rect.width < 0.01f || rect.height < 0.01f)
		{
			rect = new Rect(0f, 0f, 2f, 2f);
		}
		MoveToLayer(createdParts);
		Camera component = GetComponent<Camera>();
		component.targetTexture = new RenderTexture(width, height, 24);
		float a = rect.height / Mathf.Tan(0.5f * component.fieldOfView * (MathF.PI / 180f)) / 2f;
		float b = rect.width / (Mathf.Tan(0.5f * component.fieldOfView * (MathF.PI / 180f)) * component.aspect) / 2f;
		component.transform.position = (Vector3)rect.center + Vector3.back * Mathf.Max(a, b);
		component.forceIntoRenderTexture = true;
		component.backgroundColor = back;
		bool wasEnabled = component.enabled;
		component.enabled = true;
		component.Render();
		component.enabled = wasEnabled;
		return component.targetTexture;
	}

	private static void MoveToLayer(Part[] createdParts)
	{
		for (int i = 0; i < createdParts.Length; i++)
		{
			Transform[] componentsInChildren = createdParts[i].GetComponentsInChildren<Transform>(includeInactive: true);
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].gameObject.layer = LayerMask.NameToLayer("Part Icon");
			}
		}
	}

	private static void CloseLandingLegs(Part[] createdParts)
	{
		foreach (Part part in createdParts)
		{
			if (part.name == "Landing Leg" || part.name == "Landing Leg Big")
			{
				part.variablesModule.doubleVariables.SetValue("state", 0.0);
				part.variablesModule.doubleVariables.SetValue("state_Target", 0.0);
			}
		}
	}
}
