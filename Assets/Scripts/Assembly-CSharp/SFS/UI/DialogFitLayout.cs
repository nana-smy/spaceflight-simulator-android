using System.Collections;
using UnityEngine;

namespace SFS.UI
{
	// 竖屏适配：PC 横屏弹窗根面板（居中、固定大宽）在竖屏下超出屏幕宽度时，等比缩进，
	// 避免横排按钮被裁切、面板溢出屏幕。只缩不放；不改变子布局。
	public class DialogFitLayout : MonoBehaviour
	{
		private Canvas rootCanvas;
		private Vector2Int lastScreen;

		private void Start()
		{
			rootCanvas = GetComponentInParent<Canvas>();
			if (rootCanvas == null)
			{
				rootCanvas = FindObjectOfType<Canvas>();
			}
			StartCoroutine(DelayedFit());
		}

		private IEnumerator DelayedFit()
		{
			// 等弹窗/子布局激活完成，多轮刷新覆盖菜单打开时机
			for (int i = 0; i < 10; i++)
			{
				yield return new WaitForSeconds(0.15f);
				FitAll();
			}
		}

		private void Update()
		{
			if (Screen.width != lastScreen.x || Screen.height != lastScreen.y)
			{
				lastScreen = new Vector2Int(Screen.width, Screen.height);
				FitAll();
			}
		}

		private void FitAll()
		{
			RectTransform[] all = GetComponentsInChildren<RectTransform>(true);
			foreach (RectTransform rt in all)
			{
				if (IsDialogRoot(rt))
				{
					FitOne(rt);
				}
			}
		}

		// 居中固定大宽的弹窗根；父级若已是同类根则跳过，避免嵌套二次缩放
		private bool IsDialogRoot(RectTransform rt)
		{
			bool centered = Mathf.Approximately(rt.anchorMin.x, 0.5f) && Mathf.Approximately(rt.anchorMax.x, 0.5f)
				&& Mathf.Approximately(rt.anchorMin.y, 0.5f) && Mathf.Approximately(rt.anchorMax.y, 0.5f);
			if (!centered || rt.sizeDelta.x < 1000f)
			{
				return false;
			}
			RectTransform parent = rt.parent as RectTransform;
			if (parent != null && IsDialogRoot(parent))
			{
				return false;
			}
			return true;
		}

		private void FitOne(RectTransform rt)
		{
			Vector3[] corners = new Vector3[4];
			rt.GetWorldCorners(corners);
			float width = corners[3].x - corners[0].x;
			float screenWidth = ScreenWidthWorld();
			if (width <= 0.001f || screenWidth <= 0.001f)
			{
				return;
			}
			float maxWidth = screenWidth * 0.96f;
			if (width > maxWidth)
			{
				float scale = maxWidth / width;
				Vector3 ls = rt.localScale;
				float next = Mathf.Min(ls.x, scale); // 只缩不放
				rt.localScale = new Vector3(next, next, ls.z);
			}
		}

		private float ScreenWidthWorld()
		{
			if (rootCanvas != null)
			{
				RectTransform crt = rootCanvas.transform as RectTransform;
				if (crt != null)
				{
					Vector3[] corners = new Vector3[4];
					crt.GetWorldCorners(corners);
					return corners[3].x - corners[0].x;
				}
			}
			return Screen.width;
		}
	}
}
