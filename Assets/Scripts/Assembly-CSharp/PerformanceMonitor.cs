using SFS.Input;
using SFS.Translations;
using SFS.World.Maps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SFS
{
	// 性能监视器（UGUI 文本，与游戏 UI 同层）：显示画面帧与物理帧，每 0.5 秒刷新。
	// World 界面：左上角（map 等选项下方），地图模式隐藏；Build 界面：左侧零件栏旁；
	// 首页 / Hub / 加载中不显示。
	public class PerformanceMonitor : MonoBehaviour
	{
		private float renderFps;
		private float physicsFps;

		private int frameCount;
		private int fixedUpdateCount;
		private float elapsed;

		private const float UpdateInterval = 0.5f;

		private Canvas canvas;
		private Text label;

		private void Awake()
		{
			GameObject canvasGo = new GameObject("PerfCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasGo.transform.SetParent(transform, false);
			canvas = canvasGo.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100;

			CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
			scaler.scaleFactor = 1f;

			GameObject textGo = new GameObject("PerfLabel", typeof(Text));
			textGo.transform.SetParent(canvasGo.transform, false);
			label = textGo.GetComponent<Text>();
			label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			label.fontSize = 12;
			label.alignment = TextAnchor.UpperLeft;
			label.color = Color.white;
			label.horizontalOverflow = HorizontalWrapMode.Overflow;
			label.verticalOverflow = VerticalWrapMode.Overflow;
			label.raycastTarget = false;

			Shadow shadow = textGo.AddComponent<Shadow>();
			shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
			shadow.effectDistance = new Vector2(1f, -1f);

			RectTransform rt = textGo.GetComponent<RectTransform>();
			rt.anchorMin = new Vector2(0f, 1f);
			rt.anchorMax = new Vector2(0f, 1f);
			rt.pivot = new Vector2(0f, 1f);
			rt.sizeDelta = new Vector2(200f, 40f);
		}

		private void Update()
		{
			frameCount++;
			elapsed += Time.unscaledDeltaTime;

			if (elapsed >= UpdateInterval)
			{
				renderFps = frameCount / elapsed;
				physicsFps = fixedUpdateCount / elapsed;
				frameCount = 0;
				fixedUpdateCount = 0;
				elapsed = 0f;
			}

			UpdateVisibilityAndPosition();
			UpdateText();
		}

		private void FixedUpdate()
		{
			fixedUpdateCount++;
		}

		private void UpdateVisibilityAndPosition()
		{
			string sceneName = SceneManager.GetActiveScene().name;
			bool visible;
			Vector2 pos;
			if (sceneName.StartsWith("World"))
			{
				visible = Map.manager == null || !Map.manager.mapMode.Value;
				pos = new Vector2(10f, -30f);
			}
			else if (sceneName.StartsWith("Build"))
			{
				visible = true;
				pos = new Vector2(60f, -(Screen.height - 30f));
			}
			else
			{
				visible = false;
				pos = Vector2.zero;
			}
			if (visible && ScreenManager.main != null && ScreenManager.main.GetStackCount() > 1)
			{
				visible = false;
			}
			canvas.gameObject.SetActive(visible);
			if (visible)
			{
				label.rectTransform.anchoredPosition = pos;
			}
		}

		private void UpdateText()
		{
			if (label != null)
			{
				label.text = Loc.main.Perf_RenderFPS + " " + renderFps.ToString("F0") + "\n" + Loc.main.Perf_PhysicsFPS + " " + physicsFps.ToString("F0");
			}
		}
	}

	public static class PerformanceMonitorBootstrap
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize()
		{
			if (Object.FindObjectOfType<PerformanceMonitor>() == null)
			{
				GameObject gameObject = new GameObject("PerformanceMonitor");
				Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<PerformanceMonitor>();
			}
		}
	}
}
