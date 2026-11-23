using System;
using SFS.Cameras;
using SFS.Input;
using SFS.Translations;
using SFS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.World
{
	// World 界面大改：下半部分原版 UI 全部清空（Holder 整块隐藏），
	// 保留本脚本新建的底部触屏控制：旋转键/6向箭头（居中靠下）、staging↔节流阀切换键（右下），
	// 左上角 [汉堡菜单, 地图]。由 VideoSettingsPC 在 World_PC 场景加载时挂载。
	public class WorldUILayout : MonoBehaviour
	{
		public static WorldUILayout main;

		// 逻辑引用
		private ArrowkeysDrawer arrowkeysDrawer;
		private ThrottleDrawer throttleDrawer;
		private StagingDrawer stagingDrawer;

		// 场景对象
		private Transform bottomArea;       // "Throttle, Arrowkeys, Location"
		private Transform rotateGroup;      // 旋转键容器（RCS 关）
		private Transform sixGroup;         // 6向箭头容器（RCS 开）
		private TextMeshProUGUI switchText; // 切换键文字
		private RectTransform switchBtnRt;  // 切换键（右下角，每帧钉死位置防强制覆盖）
		private Vector2 switchTargetAP;     // 切换键目标位置（右下角 (0,0)）

		// 节流阀列（右下角，切换键上方，staging 模式隐藏）
		private RectTransform throttleColumnRt;     // 列容器（每帧钉死右下角）
		private Vector2 throttleColumnAP;           // 容器目标位置
		private RectTransform throttleBarRt;        // 进度条轨道（绝对式定位基准）
		private RectTransform throttleFillRt;       // 竖直进度条填充（高度 = 轨道高 × fillAmount，底部起）
		private float throttleFillW;                // 填充宽（轨道内缩后）
		private float throttleFillH;                // 填充高（轨道内缩后，拖动基准）
		private TextMeshProUGUI throttlePercentTxt; // 百分比文字
		private UnityEngine.UI.Image throttleToggleBg; // 开关底
		private TextMeshProUGUI throttleToggleText;   // 开关文字（"开"/"关"）
		// 右上角速度/高度（无底纯文字，复用 LocationDrawer 每帧格式化结果）
		private TextMeshProUGUI speedTmp;
		private TextMeshProUGUI heightTmp;

		private bool stagingMode;
		private bool lastRcs;
		// 触屏轴状态（按钮按下/抬起更新；LateUpdate 合并键盘轴写入 rocket.arrowkeys，绕开 keysNode 每帧清零）
		private float pressedTurn;
		private Vector2 pressedMove;
		private bool loggedNoControl;
		private bool loggedTimewarp;

		private void LateUpdate()
		{
			WriteAxes();
		}

		private void Awake()
		{
			main = this;
		}

		private void Start()
		{
			try
			{
				Build();
				Debug.Log("[WorldUI] 布局构建完成");
			}
			catch (Exception e)
			{
				Debug.LogError("[WorldUI] 布局异常: " + e);
			}
		}

		private void Update()
		{
			try
			{
				TickRcs();
				TickThrottle();
			}
			catch (Exception)
			{
			}
		}

		private void Build()
		{
			arrowkeysDrawer = FindObjectOfType<ArrowkeysDrawer>();
			throttleDrawer = FindObjectOfType<ThrottleDrawer>();
			stagingDrawer = FindObjectOfType<StagingDrawer>();

			// 分级（staging）竖条整体上移，与底部切换键拉开间距、右缘对齐
			if (stagingDrawer != null)
			{
				RectTransform srt = stagingDrawer.transform as RectTransform;
				if (srt != null)
				{
					srt.anchoredPosition = new Vector2(0f, 150f);
					Debug.Log("[WorldUI] 分级条上移: AP=" + srt.anchoredPosition);
				}
			}

			bottomArea = FindTransform("Throttle, Arrowkeys, Location");
			if (bottomArea != null)
			{
				RestructureBottomArea();
				if (arrowkeysDrawer != null)
				{
					BuildRotateKeys();
					BuildSixWayKeys();
				}
				BuildSwitchButton();
			}
			else
			{
				Debug.LogError("[WorldUI] 找不到底部区域");
			}
			RestructureTopLeft();
			BuildThrottleColumn();
			BuildSpeedHeight();
			SetStagingMode(false);
			Debug.Log("[WorldUI] 布局构建完成 bottomArea=" + (bottomArea != null) + " arrows=" + (arrowkeysDrawer != null) + " throttle=" + (throttleDrawer != null) + " staging=" + (stagingDrawer != null));
		}

		// ---------- 左上角：第1个按钮=汉堡菜单，第2个=地图，其余隐藏 ----------
		private void RestructureTopLeft()
		{
			Transform panel = FindTransform("Top Left Panel");
			if (panel == null)
			{
				return;
			}
			foreach (string name in new[] { "Save", "Load", "Help", "Top Left Button", "TopBackgroundLeft" })
			{
				Transform child = panel.Find(name);
				if (child != null)
				{
					child.gameObject.SetActive(false);
				}
			}

			// 汉堡按钮（第1位）
			SFS.UI.Button burger = CreateButton(panel, "Hamburger Menu", Vector2.zero, new Vector2(100f, 62f), delegate
			{
				if (GameManager.main != null)
				{
					GameManager.main.OpenMenu();
				}
			});
			LayoutElement le = burger.gameObject.AddComponent<LayoutElement>();
			le.minWidth = 100f;
			le.preferredHeight = 62f;
			le.preferredWidth = 100f;
			for (int i = 0; i < 3; i++)
			{
				GameObject line = new GameObject("Line " + i, typeof(RectTransform), typeof(UnityEngine.UI.Image));
				RectTransform lrt = line.GetComponent<RectTransform>();
				lrt.SetParent(burger.transform, false);
				lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
				lrt.anchoredPosition = new Vector2(0f, 19 - i * 19);
				lrt.sizeDelta = new Vector2(54f, 7f);
				UnityEngine.UI.Image limg = line.GetComponent<UnityEngine.UI.Image>();
				limg.color = Color.white;
				limg.raycastTarget = false;
			}
			burger.transform.SetSiblingIndex(0);

			// 地图按钮（第2位）
			Transform map = panel.Find("Map");
			if (map != null)
			{
				map.SetSiblingIndex(1);
			}
		}

		// ---------- 底部区域：先清空原版下半部分 UI（Holder 全部子物体 + Holder 本身） ----------
		// 原版控件（节流阀进度条/百分比/开关键/RCS/高度速度/残留）全部隐藏，
		// 只保留本脚本新建的：旋转键/6向键（居中靠下）、切换键（右下）。
		private void RestructureBottomArea()
		{
			RectTransform br = bottomArea.GetComponent<RectTransform>();
			br.sizeDelta = new Vector2(700f, 320f);

			Transform holder = bottomArea.Find("Holder");
			if (holder != null)
			{
				for (int i = 0; i < holder.childCount; i++)
				{
					holder.GetChild(i).gameObject.SetActive(false);
				}
				holder.gameObject.SetActive(false);
			}
			Debug.Log("[WorldUI] 下半部分原版UI全部删除 holder=" + (holder != null));
		}

		// ---------- 底部居中靠下：旋转键（RCS 关时 ◀ ▶ 三角形） ----------
		private void BuildRotateKeys()
		{
			rotateGroup = new GameObject("Rotate Keys", typeof(RectTransform)).transform;
			rotateGroup.SetParent(bottomArea, false);
			SetCenter(rotateGroup, new Vector2(0f, -30f));

			MakeAxisButton(rotateGroup, "Turn Left", new Vector2(-120f, 0f), new Vector2(124f, 124f), "◀", 0f,
				delegate { pressedTurn = -1f; pressedMove = Vector2.zero; WriteAxes(); },
				delegate { pressedTurn = 0f; WriteAxes(); });
			MakeAxisButton(rotateGroup, "Turn Right", new Vector2(120f, 0f), new Vector2(124f, 124f), "◀", 180f,
				delegate { pressedTurn = 1f; pressedMove = Vector2.zero; WriteAxes(); },
				delegate { pressedTurn = 0f; WriteAxes(); });
		}

		// ---------- 底部居中靠下：6向箭头（RCS 开时两行三角形 ←↑→ / ←↓→，等同 QWEASD） ----------
		private void BuildSixWayKeys()
		{
			sixGroup = new GameObject("Six Way Keys", typeof(RectTransform)).transform;
			sixGroup.SetParent(bottomArea, false);
			SetCenter(sixGroup, new Vector2(0f, -30f));

			// 三角形字符：◀(U+25C0) ▲(U+25B2) 已静态烘焙；▶=◀旋转180°，▼=▲旋转180°（零动态烘焙零报错）
			// 布局：上排 ◀(Q左转) ▲(W前进) ▶(E右转)；下排 ◀(A左移) ▼(S后退) ▶(D右移)
			MakeAxisButton(sixGroup, "Turn Left Q", new Vector2(-120f, 86f), new Vector2(124f, 124f), "◀", 0f,
				delegate { pressedTurn = -1f; pressedMove = Vector2.zero; WriteAxes(); },
				delegate { pressedTurn = 0f; WriteAxes(); });
			MakeAxisButton(sixGroup, "Up W", new Vector2(0f, 86f), new Vector2(124f, 124f), "▲", 0f,
				delegate { pressedMove = new Vector2(0f, 1f); pressedTurn = 0f; WriteAxes(); },
				delegate { pressedMove = Vector2.zero; WriteAxes(); });
			MakeAxisButton(sixGroup, "Turn Right E", new Vector2(120f, 86f), new Vector2(124f, 124f), "◀", 180f,
				delegate { pressedTurn = 1f; pressedMove = Vector2.zero; WriteAxes(); },
				delegate { pressedTurn = 0f; WriteAxes(); });
			MakeAxisButton(sixGroup, "Left A", new Vector2(-120f, -86f), new Vector2(124f, 124f), "◀", 0f,
				delegate { pressedMove = new Vector2(-1f, 0f); pressedTurn = 0f; WriteAxes(); },
				delegate { pressedMove = Vector2.zero; WriteAxes(); });
			MakeAxisButton(sixGroup, "Down S", new Vector2(0f, -86f), new Vector2(124f, 124f), "▲", 180f,
				delegate { pressedMove = new Vector2(0f, -1f); pressedTurn = 0f; WriteAxes(); },
				delegate { pressedMove = Vector2.zero; WriteAxes(); });
			MakeAxisButton(sixGroup, "Right D", new Vector2(120f, -86f), new Vector2(124f, 124f), "◀", 180f,
				delegate { pressedMove = new Vector2(1f, 0f); pressedTurn = 0f; WriteAxes(); },
				delegate { pressedMove = Vector2.zero; WriteAxes(); });

			sixGroup.gameObject.SetActive(false);
		}

		// 按住驱动轴、抬起归零的按钮（glyph 为静态烘焙字符；rotateDeg 旋转字形得到缺失方向，如 ◀+180°=▶）
		private void MakeAxisButton(Transform parent, string name, Vector2 pos, Vector2 size, string glyph, float rotateDeg, Action onDown, Action onUp)
		{
			SFS.UI.Button b = CreateButton(parent, name, pos, size, null);
			b.onDown += (Action<OnInputStartData>)delegate(OnInputStartData d)
			{
				onDown();
			};
			b.onUp += (Action<OnInputEndData>)delegate(OnInputEndData d)
			{
				onUp();
			};
			TextMeshProUGUI tmp = CreateText(b.transform, glyph, Vector2.zero, size, 48f);
			if (rotateDeg != 0f)
			{
				tmp.transform.localRotation = Quaternion.Euler(0f, 0f, rotateDeg);
			}
		}

		// 每帧（LateUpdate，晚于 InputManager 的 keysNode 轴清零）把触屏轴 + 键盘轴合并写进 rocket.arrowkeys
		private void WriteAxes()
		{
			if (PlayerController.main == null || !(PlayerController.main.player.Value is Rocket rocket) || rocket.arrowkeys == null)
			{
				return;
			}
			// 键盘轴状态（keysNode 每帧写入 ArrowkeysDrawer 的轴字段，读出来与触屏按钮合并）
			float keyTurn = 0f;
			Vector2 keyMove = Vector2.zero;
			if (arrowkeysDrawer != null)
			{
				keyTurn = arrowkeysDrawer.GetKeyboardTurn();
				keyMove = arrowkeysDrawer.GetKeyboardMove();
			}
			float turn = keyTurn + pressedTurn;
			Vector2 move = keyMove + pressedMove;
			if (!PlayerController.main.hasControl)
			{
				if (turn != 0f || move != Vector2.zero)
				{
					if (!loggedNoControl)
					{
						MsgDrawer.main.Log(Loc.main.No_Control_Msg);
						loggedNoControl = true;
					}
				}
				else
				{
					loggedNoControl = false;
				}
				turn = 0f;
				move = Vector2.zero;
			}
			else if (WorldTime.main != null && !WorldTime.main.realtimePhysics)
			{
				if (turn != 0f || move != Vector2.zero)
				{
					if (!loggedTimewarp)
					{
						MsgDrawer.main.Log(Loc.main.Cannot_Turn_While_Timewarping);
						loggedTimewarp = true;
					}
				}
				else
				{
					loggedTimewarp = false;
				}
				turn = 0f;
				move = Vector2.zero;
			}
			else
			{
				loggedNoControl = false;
				loggedTimewarp = false;
			}
			rocket.arrowkeys.turnAxis.Value = turn;
			rocket.arrowkeys.rawArrowkeysAxis.Value = move;
			rocket.arrowkeys.horizontalAxis.Value = new Double2(move.x, 0.0).Rotate((float)ActiveCamera.Camera.rotation * (MathF.PI / 180f));
			rocket.arrowkeys.verticalAxis.Value = new Double2(0.0, move.y).Rotate((float)ActiveCamera.Camera.rotation * (MathF.PI / 180f));
		}

		// ---------- 屏幕右下角：staging ↔ 节流阀 切换键（锚 Main UI 右下角，不被 bottomArea 框住） ----------
		private void BuildSwitchButton()
		{
			// 父 = Main UI（全屏 stretch）→ 按钮锚(1,0) 即屏幕右下角；bottomArea 仅 700 宽居中，右缘离屏幕右缘 350px，不能用
			Transform mainUI = FindTransform("Main UI");
			Transform parent = mainUI != null ? mainUI : (Transform)bottomArea;
			SFS.UI.Button b = CreateButton(parent, "Staging Switch", Vector2.zero, new Vector2(132f, 60f), delegate
			{
				SetStagingMode(!stagingMode);
			});
			// 紧贴屏幕右下角：锚(1,0)+pivot(1,0)+AP(0,-10) = 按钮右缘贴 Main UI 右缘（与分级条右缘对齐），底贴屏幕
			switchBtnRt = b.GetComponent<RectTransform>();
			switchBtnRt.anchorMin = switchBtnRt.anchorMax = new Vector2(1f, 0f);
			switchBtnRt.pivot = new Vector2(1f, 0f);
			switchBtnRt.anchoredPosition = new Vector2(0f, -10f);
			switchTargetAP = new Vector2(0f, -10f);
			switchText = CreateText(b.transform, "分级", Vector2.zero, new Vector2(132f, 60f), 28f);
			Debug.Log("[WorldUI] 切换键创建: anchor=" + switchBtnRt.anchorMin + " pivot=" + switchBtnRt.pivot + " AP=" + switchBtnRt.anchoredPosition
				+ " 父=" + parent.name
				+ " 父rect=" + parent.GetComponent<RectTransform>().rect
				+ " 世界=" + switchBtnRt.position);
		}

		// ---------- 屏幕右下角：竖直节流阀（进度条贴底 → 上方百分比 → 最上开关），右缘与切换键对齐 ----------
		private void BuildThrottleColumn()
		{
			Transform mainUI = FindTransform("Main UI");
			Transform parent = mainUI != null ? mainUI : (Transform)bottomArea;

			// 列容器：锚 Main UI 右下角，底贴切换键顶（切换键 y -10..50 → 容器 y 50）
			GameObject colGo = new GameObject("Throttle Column", typeof(RectTransform));
			throttleColumnRt = colGo.GetComponent<RectTransform>();
			throttleColumnRt.SetParent(parent, false);
			throttleColumnRt.anchorMin = throttleColumnRt.anchorMax = new Vector2(1f, 0f);
			throttleColumnRt.pivot = new Vector2(1f, 0f);
			throttleColumnRt.anchoredPosition = new Vector2(0f, 50f);
			throttleColumnRt.sizeDelta = new Vector2(150f, 640f);
			throttleColumnAP = new Vector2(0f, 50f);

			// 竖直进度条轨道（粗 80 宽 × 530 高，可拖动）：容器内 y0..530（底贴切换键顶）
			SFS.UI.Button barBtn = CreateButton(throttleColumnRt, "Throttle Bar", new Vector2(0f, 0f), new Vector2(80f, 530f), null);
			throttleBarRt = barBtn.GetComponent<RectTransform>();
			throttleBarRt.anchorMin = throttleBarRt.anchorMax = new Vector2(1f, 0f);
			throttleBarRt.pivot = new Vector2(1f, 0f);
			// 绝对式跟手：手指按哪/拖到哪，白条就到哪
			barBtn.onDown += (Action<OnInputStartData>)delegate(OnInputStartData d)
			{
				SetThrottleFromScreenY(d.position.pixel.y);
			};
			barBtn.onHold += (Action<OnInputStayData>)delegate(OnInputStayData d)
			{
				SetThrottleFromScreenY(d.position.pixel.y);
			};

			// 填充：锚左下角、pivot(0,0)，高度由 RectTransform 控制（不依赖 Image Filled，100% 可见）
			// 锚(0,0) + pivot(0,0) + AP(6,6) → 从轨道左下角往上生长
			GameObject fillGo = new GameObject("Throttle Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			throttleFillRt = fillGo.GetComponent<RectTransform>();
			throttleFillRt.SetParent(throttleBarRt, false);
			throttleFillRt.anchorMin = throttleFillRt.anchorMax = Vector2.zero;
			throttleFillRt.pivot = Vector2.zero;
			throttleFillRt.anchoredPosition = new Vector2(6f, 6f);
			throttleFillW = 80f - 12f;
			throttleFillH = 530f - 12f;
			throttleFillRt.sizeDelta = new Vector2(throttleFillW, 0f);
			UnityEngine.UI.Image fillImg = fillGo.GetComponent<UnityEngine.UI.Image>();
			fillImg.raycastTarget = false;

			// 百分比（圆角深色底框 + 白字）：容器内 y540..584（进度条顶上方 10）
			GameObject pctGo = new GameObject("Throttle Percent", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			RectTransform pctRt = pctGo.GetComponent<RectTransform>();
			pctRt.SetParent(throttleColumnRt, false);
			pctRt.anchorMin = pctRt.anchorMax = new Vector2(1f, 0f);
			pctRt.pivot = new Vector2(1f, 0f);
			pctRt.anchoredPosition = new Vector2(0f, 540f);
			pctRt.sizeDelta = new Vector2(132f, 44f);
			UnityEngine.UI.Image pctImg = pctGo.GetComponent<UnityEngine.UI.Image>();
			pctImg.sprite = LegacyUITheme.RoundedSprite;
			pctImg.type = Image.Type.Sliced;
			pctImg.color = LegacyUITheme.ButtonBaseColor;
			pctImg.raycastTarget = false;
			throttlePercentTxt = CreateText(pctRt, "0%", Vector2.zero, new Vector2(132f, 44f), 28f);

			// 开关（文字按钮）：容器内 y595..639（百分比上方）
			SFS.UI.Button toggleBtn = CreateButton(throttleColumnRt, "Throttle Toggle", new Vector2(0f, 595f), new Vector2(132f, 44f), null);
			RectTransform toggleRt = toggleBtn.GetComponent<RectTransform>();
			toggleRt.anchorMin = toggleRt.anchorMax = new Vector2(1f, 0f);
			toggleRt.pivot = new Vector2(1f, 0f);
			throttleToggleBg = toggleRt.GetComponent<UnityEngine.UI.Image>();
			throttleToggleText = CreateText(toggleRt, "开", Vector2.zero, new Vector2(132f, 44f), 28f);
			toggleBtn.onClick += (Action<OnInputEndData>)delegate(OnInputEndData d)
			{
				if (throttleDrawer != null)
				{
					throttleDrawer.ToggleThrottle();
				}
			};

			Debug.Log("[WorldUI] 节流阀列创建: 容器AP=" + throttleColumnRt.anchoredPosition + " 父=" + parent.name + " 填充基准=" + throttleFillW + "x" + throttleFillH);
		}

		// 绝对式跟手：屏幕 y → 轨道本地 y（相对轨道底）→ raw → ApplyCurve 写回 throttlePercent
		private void SetThrottleFromScreenY(float screenPixelY)
		{
			if (WorldTime.main == null || !WorldTime.main.realtimePhysics.Value)
			{
				return;
			}
			if (PlayerController.main == null || !(PlayerController.main.player.Value is Rocket r) || r.throttle == null)
			{
				return;
			}
			if (throttleBarRt == null || throttleBarRt.rect.height <= 0f)
			{
				return;
			}
			Vector2 local;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(throttleBarRt, new Vector2(0f, screenPixelY), null, out local))
			{
				return;
			}
			// 轨道 pivot(1,0) → local.y 0 即轨道底，向上为正（逻辑像素，ScreenPointToLocal 已处理 canvas 缩放）
			float raw = Mathf.Clamp01(local.y / throttleBarRt.rect.height);
			r.throttle.throttlePercent.Value = Mathf.Clamp01(Mathf.Pow(raw, 1.6f)); // ApplyCurve
		}

		// ---------- 右上角：速度/高度（无底纯文字，Timewarp 正下方），数据复用 LocationDrawer 每帧格式化 ----------
		private void BuildSpeedHeight()
		{
			Transform mainUI = FindTransform("Main UI");
			Transform parent = mainUI != null ? mainUI : (Transform)bottomArea;
			// Timewarp 锚(1,1) AP(-35,0) SD(225,50) pivot(1,1) → 占 y 0..-50，速度/高度放其下
			speedTmp = CreateText(parent, "速度 --", new Vector2(-35f, -60f), new Vector2(225f, 40f), 30f);
			RectTransform srt = speedTmp.GetComponent<RectTransform>();
			srt.anchorMin = srt.anchorMax = new Vector2(1f, 1f);
			srt.pivot = new Vector2(1f, 1f);
			speedTmp.alignment = TextAlignmentOptions.Right;

			heightTmp = CreateText(parent, "高度 --", new Vector2(-35f, -100f), new Vector2(225f, 40f), 30f);
			RectTransform hrt = heightTmp.GetComponent<RectTransform>();
			hrt.anchorMin = hrt.anchorMax = new Vector2(1f, 1f);
			hrt.pivot = new Vector2(1f, 1f);
			heightTmp.alignment = TextAlignmentOptions.Right;

			Debug.Log("[WorldUI] 速度高度创建: 父=" + parent.name);
		}

		private void SetStagingMode(bool on)
		{
			stagingMode = on;
			// 直接控制面板显隐（不依赖 OnChange 时序；shown.Value 同步给逻辑用）
			// 原版节流阀 Holder 已整块隐藏（下半部分清空），throttle 模式只显示旋转键/6向键
			if (throttleDrawer != null)
			{
				throttleDrawer.shown.Value = !on;
			}
			if (stagingDrawer != null)
			{
				stagingDrawer.shown.Value = on;
				if (stagingDrawer.menuHolder != null)
				{
					stagingDrawer.menuHolder.SetActive(on);
				}
			}
			if (rotateGroup != null)
			{
				rotateGroup.gameObject.SetActive(!lastRcs);
			}
			if (sixGroup != null)
			{
				sixGroup.gameObject.SetActive(lastRcs);
			}
			if (throttleColumnRt != null)
			{
				throttleColumnRt.gameObject.SetActive(!on);
			}
			// 从 staging 切回 throttle 时刷新 RCS 状态（staging 期间键盘 R 可能切过）
			if (!on)
			{
				lastRcs = ReadRcs();
				if (rotateGroup != null)
				{
					rotateGroup.gameObject.SetActive(!lastRcs);
				}
				if (sixGroup != null)
				{
					sixGroup.gameObject.SetActive(lastRcs);
				}
			}
			if (switchText != null)
			{
				switchText.text = on ? "节流阀" : "分级";
			}
		}

		private bool ReadRcs()
		{
			if (PlayerController.main != null && PlayerController.main.player.Value is Rocket rocket && rocket.arrowkeys != null)
			{
				return rocket.arrowkeys.rcs.Value;
			}
			return false;
		}

		// ---------- 每帧：RCS 开 → 6向箭头，关 → 旋转键 ----------
		private void TickRcs()
		{
			// 每帧强制隐藏原版节流阀 Holder（下半部分已清空，防 ThrottleDrawer.UpdateShow 重新激活）
			if (throttleDrawer != null && throttleDrawer.menuHolder != null && throttleDrawer.menuHolder.activeSelf)
			{
				throttleDrawer.menuHolder.SetActive(false);
			}
			// 每帧钉死切换键位置（右下角），对抗任何强制覆盖
			if (switchBtnRt != null)
			{
				if (switchBtnRt.anchoredPosition != switchTargetAP)
				{
					Debug.Log("[WorldUI] 切换键被覆盖: AP=" + switchBtnRt.anchoredPosition + " -> 钉回 " + switchTargetAP + " 帧=" + Time.frameCount);
					switchBtnRt.anchoredPosition = switchTargetAP;
				}
			}
			if (rotateGroup == null || sixGroup == null)
			{
				return;
			}
			// 箭头始终显示（staging/节流阀模式都不隐藏），只跟随 RCS 状态切换
			bool rcs = ReadRcs();
			if (rcs != lastRcs)
			{
				lastRcs = rcs;
				rotateGroup.gameObject.SetActive(!rcs);
				sixGroup.gameObject.SetActive(rcs);
			}
		}

		// ---------- 每帧：节流阀填充高度/百分比/开关文字 + 右上角速度高度 + 钉死列位置 ----------
		private void TickThrottle()
		{
			if (throttleColumnRt != null && throttleColumnRt.anchoredPosition != throttleColumnAP)
			{
				Debug.Log("[WorldUI] 节流阀列被覆盖: AP=" + throttleColumnRt.anchoredPosition + " -> 钉回 " + throttleColumnAP + " 帧=" + Time.frameCount);
				throttleColumnRt.anchoredPosition = throttleColumnAP;
			}
			if (throttleFillRt == null || PlayerController.main == null)
			{
				return;
			}
			if (PlayerController.main.player.Value is Rocket r && r.throttle != null)
			{
				float p = r.throttle.throttlePercent.Value;
				float fill = Mathf.Pow(p, 0.625f); // ReverseCurve（全幅 0-1，与拖动 raw 一致）
				throttleFillRt.sizeDelta = new Vector2(throttleFillW, throttleFillH * fill);
				if (throttlePercentTxt != null)
				{
					throttlePercentTxt.text = Mathf.RoundToInt(p * 100f) + "%";
				}
				bool on = r.throttle.throttleOn.Value;
				if (throttleToggleBg != null)
				{
					throttleToggleBg.color = on ? new Color(0.20f, 0.22f, 0.26f, 0.85f) : LegacyUITheme.ButtonBaseColor;
				}
				if (throttleToggleText != null)
				{
					throttleToggleText.text = on ? "开" : "关";
				}
			}
			// 右上角速度/高度：复用 LocationDrawer 每帧的格式化结果（title 含冒号，value 已 TrimStart）
			if (LocationDrawer.main != null && speedTmp != null && heightTmp != null)
			{
				string vt = LocationDrawer.main.velocityTitle.Text;
				string vv = LocationDrawer.main.velocityText.Text;
				string ht = LocationDrawer.main.heightTitle.Text;
				string hv = LocationDrawer.main.heightText.Text;
				speedTmp.text = (vt ?? "") + (vv ?? "");
				heightTmp.text = (ht ?? "") + (hv ?? "");
			}
		}

		// ---------- 通用控件 ----------
		private static SFS.UI.Button CreateButton(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Action onClick)
		{
			GameObject go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
			RectTransform rt = go.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
			rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.anchoredPosition = anchoredPos;
			rt.sizeDelta = size;
			UnityEngine.UI.Image img = go.GetComponent<UnityEngine.UI.Image>();
			img.sprite = LegacyUITheme.RoundedSprite;
			img.type = Image.Type.Sliced;
			img.color = LegacyUITheme.ButtonBaseColor;
			img.raycastTarget = true;
			SFS.UI.Button b = go.AddComponent<SFS.UI.Button>();
			b.clickEvent = new ClickUnityEvent();
			b.holdEvent = new HoldUnityEvent();
			if (onClick != null)
			{
				b.onClick += (Action<OnInputEndData>)delegate(OnInputEndData d)
				{
					onClick();
				};
			}
			return b;
		}

		private static TextMeshProUGUI CreateText(Transform parent, string content, Vector2 anchoredPos, Vector2 size, float fontSize)
		{
			GameObject go = new GameObject("Text", typeof(RectTransform));
			RectTransform rt = go.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
			rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.anchoredPosition = anchoredPos;
			rt.sizeDelta = size;
			TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
			tmp.font = GetMainFont(parent);
			tmp.text = content;
			tmp.fontSize = fontSize;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.color = Color.white;
			tmp.raycastTarget = false;
			return tmp;
		}

		private static void SetCenter(Transform t, Vector2 anchoredPos)
		{
			RectTransform rt = t.GetComponent<RectTransform>();
			rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.anchoredPosition = anchoredPos;
			rt.sizeDelta = Vector2.zero;
		}

		private static Transform FindTransform(string name)
		{
			// 优先找 World_PC 场景的对象（Base_PC 常驻场景可能混有同名对象）
			Transform[] all = FindObjectsOfType<Transform>(true);
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i].name == name && all[i].gameObject.scene.name == "World_PC")
				{
					return all[i];
				}
			}
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i].name == name)
				{
					return all[i];
				}
			}
			return null;
		}

		private static TMP_FontAsset GetMainFont(Transform root)
		{
			// 必须返回字符表含 ◀(U+25C0) 的字体（NotoSansCJKsc 主字体），否则 ◀▲ 触发 TMP 动态烘焙 → m_AtlasTextures 报错 + 字符不显示
			TMP_FontAsset first = null;
			if (root != null)
			{
				TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
				for (int i = 0; i < texts.Length; i++)
				{
					TMP_FontAsset f = texts[i] != null ? texts[i].font : null;
					if (f == null)
					{
						continue;
					}
					if (first == null)
					{
						first = f;
					}
					if (HasGlyph(f, 0x25C0u))
					{
						return f;
					}
				}
			}
			// root 下没有含 ◀ 的字体 → 全局搜（World_PC 场景 86 处引用 NotoSansCJKsc，必有激活 TMP 用它）
			if (first == null || !HasGlyph(first, 0x25C0u))
			{
				TextMeshProUGUI[] all = FindObjectsOfType<TextMeshProUGUI>(true);
				for (int i = 0; i < all.Length; i++)
				{
					TMP_FontAsset f = all[i] != null ? all[i].font : null;
					if (f == null)
					{
						continue;
					}
					if (HasGlyph(f, 0x25C0u))
					{
						return f;
					}
				}
			}
			// 兜底：root 第一个 / fallback 列表
			if (first != null)
			{
				return first;
			}
			if (TMP_Settings.fallbackFontAssets != null)
			{
				foreach (TMP_FontAsset f in TMP_Settings.fallbackFontAssets)
				{
					if (f != null)
					{
						return f;
					}
				}
			}
			return null;
		}

		private static bool HasGlyph(TMP_FontAsset font, uint unicode)
		{
			if (font == null || font.characterTable == null)
			{
				return false;
			}
			for (int i = 0; i < font.characterTable.Count; i++)
			{
				if (font.characterTable[i] != null && font.characterTable[i].unicode == unicode)
				{
					return true;
				}
			}
			return false;
		}
	}
}
