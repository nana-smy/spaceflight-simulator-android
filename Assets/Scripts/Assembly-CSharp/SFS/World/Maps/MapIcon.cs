using System;
using UnityEngine;

namespace SFS.World.Maps
{
	public class MapIcon : MonoBehaviour
	{
		public GameObject mapIcon;

		public WorldLocation location;

		public float shake;

		private void Start()
		{
			location.planet.OnChange += new Action(OnPlanetChange);
			location.position.OnChange += new Action(OnPositionChange);
			Map.view.view.distance.OnChange += new Action(UpdateAlpha);
			Map.view.view.distance.OnChange += new Action(OnPositionChange);
			// 其它火箭加载时 planet 早已设置好，不会触发 OnChange，需要主动挂载一次
			OnPlanetChange();
		}

		private void LateUpdate()
		{
			// 兜底：每帧强制图标处于正确状态（挂到地图、位置正确、渲染器开启、不透明）
			if (location != null && location.planet.Value != null && location.planet.Value.mapHolder != null && mapIcon != null)
			{
				if (mapIcon.transform.parent != location.planet.Value.mapHolder.transform)
				{
					OnPlanetChange();
				}
				OnPositionChange();
				SpriteRenderer sr = mapIcon.GetComponentInChildren<SpriteRenderer>();
				if (sr != null)
				{
					sr.enabled = true;
					sr.color = new Color(1f, 1f, 1f, 1f);
				}
				// 物体包围盒放大到覆盖地图范围，避免图标被视锥裁剪（着色器用固定尺寸，不受此缩放影响）
				mapIcon.transform.localScale = new Vector3(100000f, 100000f, 1f);
			}
		}

		private void OnDestroy()
		{
			if (mapIcon != null)
			{
				UnityEngine.Object.Destroy(mapIcon.gameObject);
			}
			if (Map.view != null && Map.view.view != null)
			{
				Map.view.view.distance.OnChange -= new Action(UpdateAlpha);
				Map.view.view.distance.OnChange -= new Action(OnPositionChange);
			}
		}

		private void OnPlanetChange()
		{
			if (!(location.planet.Value == null) && location.planet.Value.mapHolder != null)
			{
				mapIcon.transform.SetParent(location.planet.Value.mapHolder.transform);
				OnPositionChange();
				UpdateAlpha();
			}
		}

		private void OnPositionChange()
		{
			mapIcon.transform.localPosition = location.position.Value / 1000.0 + ((shake > 0f) ? (UnityEngine.Random.insideUnitCircle * shake * 0.0008f * (float)(double)Map.view.view.distance / 1000f) : Vector2.zero);
		}

		private void UpdateAlpha()
		{
			SpriteRenderer componentInChildren = mapIcon.GetComponentInChildren<SpriteRenderer>();
			if (!(componentInChildren == null) && !(location.planet.Value == null))
			{
			double num = location.position.Value.magnitude * 50.0 + location.planet.Value.SOI * 5.0;
			// 图标不随缩放淡出（原版会淡出导致“缩小后消失”），保持始终可见
			float fadeOut = 1f;
			componentInChildren.color = new Color(1f, 1f, 1f, fadeOut);
			}
			// IL2CPP 兼容：联机时按玩家颜色叠加地图图标（原 Harmony Postfix）
			MultiplayerSFS.Mod.Patches.Dispatcher.UpdateMapIconAlpha(this);
		}

		public void SetRotation(float newRotation)
		{
			mapIcon.transform.rotation = Quaternion.Euler(0f, 0f, newRotation - 90f);
		}
	}
}
