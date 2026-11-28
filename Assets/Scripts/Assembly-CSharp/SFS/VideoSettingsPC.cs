using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Career;
using SFS.Input;
using SFS.IO;
using SFS.Translations;
using SFS.UI;
using SFS.World;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SFS
{
	public class VideoSettingsPC : SettingsBase<VideoSettingsPC.Data>
	{
		[Serializable]
		public class Data
		{
			public int windowMode;

			public Vector2 currentResolution = new Vector2(0f, 0f);

			public float uiScale = 1f;

			public float uiOpacity = 1f;

			public int verticalSync = 1;

			public int fps = -1;

			public bool cameraShake = true;

			public int orbitLinesCount = 3;

			public bool FXAA = false;

			// 屏幕旋转开关：允许横屏（1400×1100）。World/Build 场景始终横屏。
			public bool allowLandscape;
		}

		public static VideoSettingsPC main;

		public TMP_Dropdown videoResolutionDropdown;

		public TMP_Dropdown windowModeDropdown;

		public Slider uiScaleSlider;

		public Slider uiOpacitySlider;

		public Slider conicSectionsCountSlider;

		public TMP_Dropdown verticalSyncDropdown;

		public TMP_Dropdown fpsDropdown;

		public TMP_Dropdown shakesDropdown;

		public TMP_Dropdown antiAliasingDropdown;

		public UnityEvent onScaleUIChanged;

		public UnityEvent onOpacityChanged;

		private List<Resolution> resolutionList = new List<Resolution>();

		private List<string> fpsOptions;

		private List<string> windowModeOptions;

		private List<string> verticalSyncOptions;

		private List<string> shakesOptions;

		private List<string> antiAliasingOptions;

		private bool isInit;

		// 左上角汉堡菜单按钮（Base 常驻，只在 航天大楼/建造/世界地图 界面显示）
		private GameObject hamburgerButton;

		protected override string FileName => "VideoSettings";

		private void Awake()
		{
			main = this;
			Loc.OnChange += new Action(OnLanguageChange);
		}

		private void OnDestroy()
		{
			Loc.OnChange -= new Action(OnLanguageChange);
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		protected override void OnLoad()
		{
			Init();
			Apply();
			UpdateUI();
		}

		private void Start()
		{
			Load();
			Init();
			Apply();
			UpdateUI();
			windowModeDropdown.onValueChanged.AddListener(WindowModeDropdownChanged);
			videoResolutionDropdown.onValueChanged.AddListener(VideoResolutionDropdownChanged);
			uiScaleSlider.onValueChanged.AddListener(UIScaleChanged);
			uiOpacitySlider.onValueChanged.AddListener(UIOpacityChanged);
			verticalSyncDropdown.onValueChanged.AddListener(VerticalSyncDropdownChanged);
			fpsDropdown.onValueChanged.AddListener(FpsDropdownChanged);
			shakesDropdown.onValueChanged.AddListener(ShakesDropdownChanged);
			conicSectionsCountSlider.onValueChanged.AddListener(OnConicSectionsCountChange);
			antiAliasingDropdown.onValueChanged.AddListener(AntiAliasingDropdownChanged);
			// 设置菜单大改（所有场景共享：VideoSettingsPC 在 Base 基础场景常驻，
			// 等设置菜单对象注册完 Menu.settings 后再执行，防止 Start 顺序问题）
			StartCoroutine(InitSettingsMenuLate());
			// 主菜单标题自适应屏幕：场景切换与分辨率/方向变化时刷新
			SceneManager.sceneLoaded += OnSceneLoaded;
			FitMainMenuTitle();
			// 左上角汉堡菜单按钮：Base 常驻，点击打开当前场景的 ESC 菜单
			BuildHamburgerMenuButton();
			UpdateHamburgerVisibility();
		}

		// 场景加载回调：主页标题按屏幕逻辑宽度重新缩放；汉堡按钮按场景显隐；Build 工具栏改造
		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			FitMainMenuTitle();
			// 用回调参数 scene.name 判断（此时 GetActiveScene 可能还是旧场景，时序不可靠）
			UpdateHamburgerVisibility(scene.name);
			if (scene.name == "Build_PC")
			{
				RestructureBuildToolbar();
			}
			if (scene.name == "World_PC")
			{
				EnsureWorldUILayout();
			}
		}

		// World 界面改版挂载：World_PC 加载后确保 WorldUILayout 存在（随场景销毁，重建幂等）
		private void EnsureWorldUILayout()
		{
			if (FindObjectOfType<SFS.World.WorldUILayout>() != null)
			{
				return;
			}
			GameObject go = new GameObject("World UI Layout");
			if (SFS.World.GameManager.main != null)
			{
				go.transform.SetParent(SFS.World.GameManager.main.transform, false);
			}
			go.AddComponent<SFS.World.WorldUILayout>();
		}

		// 主菜单大标题（GO13 "Title"，锚(0.5,0.81)，基准 1042×307 scale 1.25×1.3）
		// 宽度自适应为逻辑宽的 90%：竖屏缩小、横屏放大，任何分辨率/屏幕比例都不超出屏幕。
		// 逻辑宽 = Screen.width * 1600 / Screen.height（CanvasScaler matchHeight=1, refRes.y=1600）
		public void FitMainMenuTitle()
		{
			try
			{
				if (HomeManager.main == null || HomeManager.main.menuHolder == null)
				{
					return;
				}
				Transform title = HomeManager.main.menuHolder.transform.Find("Title");
				if (title == null)
				{
					return;
				}
				RectTransform rt = title as RectTransform;
				if (rt == null || rt.rect.width <= 0f)
				{
					return;
				}
				float logicalW = Screen.width * 1600f / Mathf.Max(1f, Screen.height);
				float targetW = logicalW * 0.9f;
				float s = targetW / rt.rect.width;
				// y/x 保持原比例（1.3/1.25 = 1.04）
				title.localScale = new Vector3(s, s * 1.04f, 1f);
				Debug.Log("[VideoSettingsPC] 标题自适应: 逻辑宽=" + logicalW + " scale=" + s.ToString("0.000"));
			}
			catch (Exception e)
			{
				Debug.Log("[VideoSettingsPC] FitMainMenuTitle 异常: " + e.Message);
			}
		}

		// 左上角汉堡菜单按钮（三条横杠）：挂常驻 canvas，点击打开当前场景的 ESC 菜单
		private void BuildHamburgerMenuButton()
		{
			try
			{
				if (hamburgerButton != null)
				{
					return;
				}
				// 必须挂在 Base 常驻场景的 canvas 下，否则切换场景时按钮随场景销毁
				// （VideoSettingsPC.Start 执行时激活场景可能是 Home_PC 等，FindObjectOfType 会找到会被卸载的 canvas）
				Canvas canvas = null;
				foreach (Canvas c in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
				{
					if (c.gameObject.scene.name == "Base_PC")
					{
						canvas = c;
						break;
					}
				}
				if (canvas == null)
				{
					canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
				}
				Transform parent = canvas != null ? canvas.transform : transform.root;
				GameObject btn = new GameObject("Hub Menu Button", typeof(RectTransform), typeof(UnityEngine.UI.Image));
				btn.transform.SetParent(parent, false);
				RectTransform rt = btn.GetComponent<RectTransform>();
				rt.anchorMin = new Vector2(0f, 1f);
				rt.anchorMax = new Vector2(0f, 1f);
				rt.pivot = new Vector2(0f, 1f);
				rt.anchoredPosition = new Vector2(30f, -30f);
				rt.sizeDelta = new Vector2(85f, 85f);
				UnityEngine.UI.Image img = btn.GetComponent<UnityEngine.UI.Image>();
				img.sprite = LegacyUITheme.RoundedSprite;
				img.type = UnityEngine.UI.Image.Type.Sliced;
				img.color = LegacyUITheme.ButtonBaseColor;
				img.raycastTarget = true;
				// 三条白色横杠
				for (int i = 0; i < 3; i++)
				{
					GameObject line = new GameObject("Line " + i, typeof(RectTransform), typeof(UnityEngine.UI.Image));
					line.transform.SetParent(btn.transform, false);
					RectTransform lr = line.GetComponent<RectTransform>();
					lr.anchorMin = new Vector2(0.5f, 1f);
					lr.anchorMax = new Vector2(0.5f, 1f);
					lr.pivot = new Vector2(0.5f, 0.5f);
					lr.anchoredPosition = new Vector2(0f, -22f - i * 20f);
					lr.sizeDelta = new Vector2(45f, 7f);
					UnityEngine.UI.Image limg = line.GetComponent<UnityEngine.UI.Image>();
					limg.color = Color.white;
					limg.raycastTarget = false;
				}
				SFS.UI.Button b = btn.AddComponent<SFS.UI.Button>();
				b.clickEvent = new ClickUnityEvent();
				b.holdEvent = new HoldUnityEvent();
				b.onClick += (Action<OnInputEndData>)delegate
				{
					// 和按 ESC 一样打开当前场景的游戏菜单
					if (HubManager.main != null)
					{
						HubManager.main.OpenMenu();
					}
					else if (BuildManager.main != null)
					{
						BuildManager.main.OpenMenu();
					}
					else if (GameManager.main != null)
					{
						GameManager.main.OpenMenu();
					}
				};
				hamburgerButton = btn;
			}
			catch (Exception e)
			{
				Debug.Log("[VideoSettingsPC] 汉堡按钮异常: " + e.Message);
			}
		}

		// 汉堡按钮只在 航天大楼 界面显示，其他任何界面不显示
		// （Build 蓝图界面由工具栏的分享键改造为汉堡按钮；World 界面左上角由 WorldUILayout 自建汉堡按钮，都不显示这个常驻的）
		private void UpdateHamburgerVisibility()
		{
			if (hamburgerButton == null)
			{
				return;
			}
			UpdateHamburgerVisibility(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
		}

		private void UpdateHamburgerVisibility(string scene)
		{
			if (hamburgerButton == null)
			{
				return;
			}
			bool show = scene == "Hub_PC";
			if (hamburgerButton.activeSelf != show)
			{
				hamburgerButton.SetActive(show);
			}
			Debug.Log("[VideoSettingsPC] 汉堡按钮显隐: 场景=" + scene + " 显示=" + show);
		}

		// Build 蓝图界面工具栏改造：
		// 隐藏 新的火箭(New)/保存(Save)/加载(Load)；分享(Share) 键改成汉堡按钮（隐藏图标文字 + 三条横杠，点击打开 ESC 菜单）
		private void RestructureBuildToolbar()
		{
			try
			{
				Transform share = null;
				foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
				{
					// 用父级存在 New 子物体来确认这是 Build 工具栏，避免误匹配其他同名物体
					if (t.name == "Share" && t.parent != null && t.parent.Find("New") != null)
					{
						share = t;
						break;
					}
				}
				if (share == null)
				{
					return;
				}
				// 隐藏 新的火箭/保存/加载（与分享同工具栏的兄弟按钮）
				Transform toolbar = share.parent;
				if (toolbar != null)
				{
					foreach (Transform child in toolbar)
					{
						if (child.name == "New" || child.name == "Save" || child.name == "Load")
						{
							child.gameObject.SetActive(false);
						}
					}
				}
				// 分享键改成汉堡：隐藏原有图标和文字
				Transform icon = share.Find("Icon");
				if (icon != null)
				{
					icon.gameObject.SetActive(false);
				}
				Transform text = share.Find("Text");
				if (text != null)
				{
					text.gameObject.SetActive(false);
				}
				// 幂等保护：已改造过（横杠已存在）就直接返回，避免重复创建横杠、重复挂 onClick
				if (share.Find("HamburgerLine 0") != null)
				{
					return;
				}
				// 三条白色横杠（必须加 LayoutElement ignoreLayout，否则会被按钮的 HorizontalLayoutGroup 重排压扁）
				for (int i = 0; i < 3; i++)
				{
					GameObject line = new GameObject("HamburgerLine " + i, typeof(RectTransform), typeof(UnityEngine.UI.Image));
					line.transform.SetParent(share, false);
					RectTransform lr = line.GetComponent<RectTransform>();
					lr.anchorMin = new Vector2(0.5f, 0.5f);
					lr.anchorMax = new Vector2(0.5f, 0.5f);
					lr.pivot = new Vector2(0.5f, 0.5f);
					lr.anchoredPosition = new Vector2(0f, 12f - i * 12f);
					lr.sizeDelta = new Vector2(40f, 6f);
					UnityEngine.UI.Image limg = line.GetComponent<UnityEngine.UI.Image>();
					limg.color = Color.white;
					limg.raycastTarget = false;
					LayoutElement le = line.AddComponent<LayoutElement>();
					le.ignoreLayout = true;
				}
				// 点击改为打开 ESC 菜单：替换 clickEvent 清掉 UploadPC 持久绑定
				// （Button.Start 里 onClick += delegate { clickEvent.Invoke(data); } 闭包引用的是字段，
				//   换成新事件实例后原绑定自动失效；再挂 onClick 委托即可，Start 执行前后都安全）
				SFS.UI.Button b = share.GetComponent<SFS.UI.Button>();
				if (b != null)
				{
					b.clickEvent = new ClickUnityEvent();
					b.holdEvent = new HoldUnityEvent();
					b.onClick += (Action<OnInputEndData>)delegate
					{
						if (BuildManager.main != null)
						{
							BuildManager.main.OpenMenu();
						}
					};
				}
				Debug.Log("[VideoSettingsPC] Build工具栏改造: Share 父=" + toolbar.name + " 横杠=3 点击=OpenMenu");
			}
			catch (Exception e)
			{
				Debug.Log("[VideoSettingsPC] Build工具栏改造异常: " + e.Message);
			}
		}

		// 延迟到场景就绪后执行设置菜单改造（Menu.settings 需要 Settings Menu 对象 Awake 完成）
		private IEnumerator InitSettingsMenuLate()
		{
			for (int i = 0; i < 30; i++)
			{
				if (Menu.settings != null)
				{
					break;
				}
				yield return null;
			}
			yield return null;
			yield return null;
			RestructureSettingsMenu();
			StartCoroutine(HideKeybindingsRetry());
			StartCoroutine(SettingsMenuWatcher());
		}

		private void Apply()
		{
			onScaleUIChanged.Invoke();
			onOpacityChanged.Invoke();
			QualitySettings.vSyncCount = settings.verticalSync;
			Application.targetFrameRate = settings.fps;
			string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			bool allowLandscape = settings.allowLandscape || scene.Contains("World") || scene.Contains("Build");
			// 桌面端：竖屏 1100×1400，World/Build 横屏 1400×1100（PC 窗口逻辑）。
			// Android/iOS：不强制 PC 窗口分辨率，沿用设备原生全屏分辨率，避免固定比例帧缓冲被拉伸导致 3D 星球与 UI 变形。
			bool mobile = Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
			if (!mobile)
			{
				Screen.SetResolution(allowLandscape ? 1400 : 1100, allowLandscape ? 1100 : 1400, FullScreenMode.Windowed);
			}
			Debug.Log("[VideoSettingsPC] 场景=" + scene + " 平台=" + Application.platform + " 分辨率=" + Screen.width + "x" + Screen.height);
			FitMainMenuTitle();
			if (settings.currentResolution.x == 0f && settings.currentResolution.y == 0f)
			{
				settings.currentResolution = new Vector2(1100f, 1400f);
			}
		}

		private void UpdateUI()
		{
			Init();
			Resolution item = resolutionList.FirstOrDefault((Resolution p) => p.width == Screen.currentResolution.width && p.height == Screen.currentResolution.height);
			videoResolutionDropdown.value = resolutionList.IndexOf(item, 0);
			windowModeDropdown.value = settings.windowMode;
			uiScaleSlider.value = settings.uiScale;
			verticalSyncDropdown.value = ((settings.verticalSync != 1) ? 1 : 0);
			fpsDropdown.value = ((settings.fps != -1) ? fpsOptions.IndexOf(settings.fps.ToString()) : 0);
			shakesDropdown.value = ((!settings.cameraShake) ? 1 : 0);
			conicSectionsCountSlider.value = settings.orbitLinesCount;
			antiAliasingDropdown.value = (settings.FXAA ? 1 : 0);
		}

		private void Init()
		{
			if (isInit)
			{
				return;
			}
			windowModeDropdown.ClearOptions();
			windowModeOptions = new List<string>
			{
				Loc.main.Fullscreen_Exclusive,
				Loc.main.Fullscreen_Borderless,
				Loc.main.Fullscreen_Windowed
			};
			windowModeDropdown.AddOptions(windowModeOptions);
			videoResolutionDropdown.ClearOptions();
			List<string> list = new List<string>();
			Resolution[] resolutions = Screen.resolutions;
			resolutionList = new List<Resolution>();
			Resolution[] array = resolutions;
			for (int i = 0; i < array.Length; i++)
			{
				Resolution res = array[i];
				if (resolutionList.Count((Resolution p) => p.width == res.width && p.height == res.height) != 1)
				{
					list.Add(res.width + "x" + res.height);
					resolutionList.Add(res);
				}
			}
			videoResolutionDropdown.AddOptions(list);
			verticalSyncDropdown.ClearOptions();
			verticalSyncOptions = new List<string>
			{
				Loc.main.State_On,
				Loc.main.State_Off
			};
			verticalSyncDropdown.AddOptions(verticalSyncOptions);
			fpsDropdown.ClearOptions();
			fpsOptions = new List<string>();
			fpsOptions.Add(Loc.main.Fps_Unlimited);
			fpsOptions.Add("30");
			fpsOptions.Add("60");
			fpsOptions.Add("100");
			fpsOptions.Add("120");
			fpsOptions.Add("144");
			fpsOptions.Add("240");
			fpsDropdown.AddOptions(fpsOptions);
			shakesDropdown.ClearOptions();
			shakesOptions = new List<string>
			{
				Loc.main.State_On,
				Loc.main.State_Off
			};
			shakesDropdown.AddOptions(shakesOptions);
			antiAliasingDropdown.ClearOptions();
			antiAliasingOptions = new List<string> { Loc.main.State_Off, "FXAA" };
			antiAliasingDropdown.AddOptions(antiAliasingOptions);
			if (settings.orbitLinesCount < 3)
			{
				settings.orbitLinesCount = 3;
			}
			isInit = true;
		}

		private void OnLanguageChange()
		{
			if (!isInit)
			{
				return;
			}
			int windowModeValue = windowModeDropdown.value;
			int fpsValue = fpsDropdown.value;
			int verticalSyncValue = verticalSyncDropdown.value;
			int shakesValue = shakesDropdown.value;
			int antiAliasingValue = antiAliasingDropdown.value;
			windowModeOptions[0] = Loc.main.Fullscreen_Exclusive;
			windowModeOptions[1] = Loc.main.Fullscreen_Borderless;
			windowModeOptions[2] = Loc.main.Fullscreen_Windowed;
			fpsOptions[0] = Loc.main.Fps_Unlimited;
			verticalSyncOptions[0] = Loc.main.State_On;
			verticalSyncOptions[1] = Loc.main.State_Off;
			shakesOptions[0] = Loc.main.State_On;
			shakesOptions[1] = Loc.main.State_Off;
			antiAliasingOptions[0] = Loc.main.State_Off;
			windowModeDropdown.ClearOptions();
			windowModeDropdown.AddOptions(windowModeOptions);
			fpsDropdown.ClearOptions();
			fpsDropdown.AddOptions(fpsOptions);
			verticalSyncDropdown.ClearOptions();
			verticalSyncDropdown.AddOptions(verticalSyncOptions);
			shakesDropdown.ClearOptions();
			shakesDropdown.AddOptions(shakesOptions);
			antiAliasingDropdown.ClearOptions();
			antiAliasingDropdown.AddOptions(antiAliasingOptions);
			windowModeDropdown.SetValueWithoutNotify(windowModeValue);
			fpsDropdown.SetValueWithoutNotify(fpsValue);
			verticalSyncDropdown.SetValueWithoutNotify(verticalSyncValue);
			shakesDropdown.SetValueWithoutNotify(shakesValue);
			antiAliasingDropdown.SetValueWithoutNotify(antiAliasingValue);
		}

		private void UIScaleChanged(float newValue)
		{
			settings.uiScale = newValue;
			onScaleUIChanged.Invoke();
			Save();
		}

		private void UIOpacityChanged(float newValue)
		{
			settings.uiOpacity = newValue;
			onOpacityChanged.Invoke();
			Save();
		}

		private void OnConicSectionsCountChange(float newValue)
		{
			settings.orbitLinesCount = (int)newValue;
			Save();
		}

		private void FpsDropdownChanged(int index)
		{
			settings.fps = ((index == 0) ? (-1) : Convert.ToInt32(fpsOptions[index]));
			Apply();
			Save();
		}

		private void ShakesDropdownChanged(int index)
		{
			settings.cameraShake = index == 0;
			Apply();
			Save();
		}

		private void VerticalSyncDropdownChanged(int index)
		{
			settings.verticalSync = ((index == 0) ? 1 : 0);
			Apply();
			Save();
		}

		private void VideoResolutionDropdownChanged(int index)
		{
			settings.currentResolution = new Vector2(resolutionList[index].width, resolutionList[index].height);
			Apply();
			Save();
		}

		private void WindowModeDropdownChanged(int index)
		{
			switch (index)
			{
			case 0:
				settings.windowMode = 0;
				break;
			case 1:
				settings.windowMode = 2;
				break;
			case 2:
				settings.windowMode = 3;
				break;
			}
			Apply();
			Save();
		}

		private void AntiAliasingDropdownChanged(int index)
		{
			settings.FXAA = index == 1;
			Save();
		}

		// ===== 设置菜单大改：供 HubManager 调用的公开设置方法（Save 是 protected，只能在此暴露） =====

		public void SetFps(int fps)
		{
			settings.fps = fps;
			Apply();
			Save();
		}

		public void SetCameraShake(bool on)
		{
			settings.cameraShake = on;
			Apply();
			Save();
		}

		public void SetAntiAliasing(bool on)
		{
			settings.FXAA = on;
			Save();
		}

		public void SetAllowLandscape(bool allow)
		{
			settings.allowLandscape = allow;
			Apply();
			Save();
		}

		// ===== 设置菜单大改（从 HubManager 迁移）：VideoSettingsPC 在 Base 基础场景常驻，
		//      所有场景（主页/Hub/World/Build）共享同一个 Settings Menu，改造在这里统一生效 =====

		private TMPro.TextMeshProUGUI fpsValueText;

		private TMPro.TextMeshProUGUI langValueText;

		private RectTransform fpsValueBg;

		private RectTransform langValueBg;

		private IEnumerator HideKeybindingsRetry()
		{
			yield return new WaitForSeconds(1f);
			yield return new WaitForSeconds(1f);
			HideKeybindingsButton();
		}

		// 隐藏设置菜单里的"键位绑定"部分（通过翻译值匹配按钮）
		private void HideKeybindingsButton()
		{
			if (Menu.settings == null)
			{
				return;
			}
			string kbText = (string)Loc.main.Keybindings_Title;
			TranslationSelector[] selectors = Menu.settings.GetComponentsInChildren<TranslationSelector>(true);
			int hidden = 0;
			for (int i = 0; i < selectors.Length; i++)
			{
				if (selectors[i].Selector != null)
				{
					string v = selectors[i].Selector.Field;
					if (v != null && v.Trim() == kbText)
					{
						hidden++;
						// 只隐藏 TranslationSelector 所在的对象本身（按钮），不向上找，避免误伤大容器
						selectors[i].gameObject.SetActive(false);
						Debug.Log("[Hub] 隐藏键位绑定对象: " + selectors[i].gameObject.name + " 父=" + (selectors[i].transform.parent != null ? selectors[i].transform.parent.name : "null"));
					}
				}
			}
			Debug.Log("[Hub] 键位绑定隐藏: selector数=" + selectors.Length + " 匹配隐藏=" + hidden + " 目标文字='" + kbText + "'");
			// 隐藏键位绑定栏目的所有内容（按键绑定行等）+ 设置蓝色背景（BackPanel）
			Transform[] all = Menu.settings.GetComponentsInChildren<Transform>(true);
			int kbHidden = 0;
			int backPanelHidden = 0;
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i].name.Contains("Keybinding"))
				{
					all[i].gameObject.SetActive(false);
					kbHidden++;
				}
				if (all[i].name == "BackPanel")
				{
					all[i].gameObject.SetActive(false);
					backPanelHidden++;
				}
			}
			Debug.Log("[Hub] 键位绑定内容隐藏=" + kbHidden + " 蓝底BackPanel隐藏=" + backPanelHidden);
			// 窗口放大 + 关闭键固定 + 设置区拉伸（统一在 ApplySettingsLayout 里做，两处调用保持一致）
			ApplySettingsLayout();
		}

		// 设置菜单打开时：重新应用布局（菜单打开会重建布局，可能覆盖 Start 时的设置）
		// + 白条兜底激活（防止任何逻辑隐藏滑块的白色填充条）
		private IEnumerator SettingsMenuWatcher()
		{
			bool wasActive = false;
			while (true)
			{
				bool isActive = Menu.settings != null && Menu.settings.menuHolder != null && Menu.settings.menuHolder.gameObject.activeInHierarchy;
				if (isActive && !wasActive)
				{
					// 等两帧，让 VerticalLayoutGroup + Slider.UpdateVisuals 先跑完
					yield return null;
					yield return null;
					HideKeybindingsButton();
					ApplySettingsLayout();
					EnsureSliderWhiteBars();
				}
				// 打开期间每帧刷新：语言/帧数选择器改完值后立即回显
				if (isActive)
				{
					RefreshSettingsValues();
				}
				wasActive = isActive;
				yield return null;
			}
		}

		// 兜底：滑块白条（ButtonBack/BackOverTint）如果被任何逻辑隐藏，恢复显示
		private void EnsureSliderWhiteBars()
		{
			if (Menu.settings == null)
			{
				return;
			}
			UnityEngine.UI.Slider[] sliders = Menu.settings.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
			for (int i = 0; i < sliders.Length; i++)
			{
				RectTransform fillR = sliders[i].fillRect;
				if (fillR == null)
				{
					continue;
				}
				Transform bar = fillR.Find("ButtonBack");
				if (bar != null && !bar.gameObject.activeSelf)
				{
					bar.gameObject.SetActive(true);
				}
				Transform tint = fillR.Find("BackOverTint");
				if (tint != null && !tint.gameObject.activeSelf)
				{
					tint.gameObject.SetActive(true);
				}
			}
		}

		// 单独抽出"设置布局"部分（窗口放大 + 关闭键固定 + Mask/Scroller 拉伸），可重复调用
		private void ApplySettingsLayout()
		{
			RectTransform holderRT = (Menu.settings.menuHolder != null ? Menu.settings.menuHolder.transform : null) as RectTransform;
			Transform[] all = Menu.settings.GetComponentsInChildren<Transform>(true);
			if (holderRT != null && holderRT.childCount > 0)
			{
				RectTransform windowRT = holderRT.GetChild(0) as RectTransform;
				if (windowRT != null)
				{
					windowRT.anchorMin = new Vector2(0.5f, 0.5f);
					windowRT.anchorMax = new Vector2(0.5f, 0.5f);
					windowRT.pivot = new Vector2(0.5f, 0.5f);
					windowRT.anchoredPosition = Vector2.zero;
					windowRT.sizeDelta = new Vector2(holderRT.rect.width - 40f, holderRT.rect.height - 40f);
					Debug.Log("[Hub] 设置布局: Window 放大到 " + windowRT.sizeDelta.x + "x" + windowRT.sizeDelta.y + " canvas=" + holderRT.rect.width + "x" + holderRT.rect.height);
				}
			}
			// 1. 关闭键：**移到全屏 holder 下**（不再作为 Window 子物体），锚 (0.5,0) 贴屏幕底部。
			//    这样无论 Window 被放大/界面比例怎么调，关闭键始终锚定屏幕底部，不会被挤出屏幕。
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i].name == "Close Button")
				{
					RectTransform cr = all[i] as RectTransform;
					if (cr != null)
					{
						if (holderRT != null && cr.parent != holderRT)
						{
							cr.SetParent(holderRT, false);
							cr.SetAsLastSibling();
						}
						cr.anchorMin = new Vector2(0.5f, 0f);
						cr.anchorMax = new Vector2(0.5f, 0f);
						cr.pivot = new Vector2(0.5f, 0.5f);
						cr.anchoredPosition = new Vector2(0f, 40f);
						cr.sizeDelta = new Vector2(350f, 80f);
					}
				}
			}
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i].name == "Mask" || all[i].name == "Scroller")
				{
					RectTransform mr = all[i] as RectTransform;
					if (mr != null)
					{
						mr.anchorMin = new Vector2(0f, 0f);
						mr.anchorMax = new Vector2(1f, 1f);
						mr.pivot = new Vector2(0.5f, 0.5f);
						mr.anchoredPosition = new Vector2(0f, 55f);
						mr.sizeDelta = new Vector2(-30f, -150f);
					}
				}
			}
		}

		// ===== 设置菜单大改：删分辨率/窗口模式/垂直同步、固定UI（禁滚动）、语言/帧数独立窗口、
		//      新增屏幕旋转开关、视角摇晃/抗锯齿改开关、按用户指定顺序重排 10 项 =====

		// 设置菜单大改主入口（InitSettingsMenuLate 调用一次；设置菜单是场景对象不会重建，无需重复执行）
		private void RestructureSettingsMenu()
		{
			if (Menu.settings == null)
			{
				return;
			}
			Transform scroller = null;
			Transform[] all = Menu.settings.GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i].name == "Scroller")
				{
					scroller = all[i];
					break;
				}
			}
			if (scroller == null)
			{
				Debug.Log("[Hub] 设置大改: 找不到 Scroller");
				return;
			}
			// 1. 固定 UI：禁用滚动（删掉窗口滚轮）
			ScrollElement se = scroller.GetComponent<ScrollElement>();
			if (se != null)
			{
				se.enabled = false;
			}
			// 2. 行分类：保留 9 个原行，其余全部隐藏
			Transform soundRow = null;
			Transform musicRow = null;
			Transform uiScaleRow = null;
			Transform uiOpacityRow = null;
			Transform fpsRow = null;
			Transform cameraShakeRow = null;
			Transform aaRow = null;
			Transform conicRow = null;
			Transform langRow = null;
			List<Transform> spacers = new List<Transform>();
			int hidden = 0;
			for (int i = 0; i < scroller.childCount; i++)
			{
				Transform row = scroller.GetChild(i);
				string n = row.name;
				if (n == "Sound")
				{
					soundRow = row;
				}
				else if (n == "Music")
				{
					musicRow = row;
				}
				else if (n == "UIScale")
				{
					uiScaleRow = row;
				}
				else if (n == "UIOpacity")
				{
					uiOpacityRow = row;
				}
				else if (n == "FPS")
				{
					fpsRow = row;
				}
				else if (n == "CameraShake")
				{
					cameraShakeRow = row;
				}
				else if (n == "Anti Aliasing")
				{
					aaRow = row;
				}
				else if (n == "ConicSections")
				{
					conicRow = row;
				}
				else if (n == "Language")
				{
					langRow = row;
				}
				else if (n == "Spacer")
				{
					// 保留空行（分组分隔）
					spacers.Add(row);
				}
				else
				{
					// Title/Separator/GeneralTitle/Resolution/WindowMode/VerticalSync/Keybindings/Key_Bind
					row.gameObject.SetActive(false);
					hidden++;
				}
			}
			Debug.Log("[Hub] 设置大改: 隐藏行=" + hidden);
			// 3. 行改造
			MakeSelectorRow(fpsRow, delegate
			{
				return FpsDisplayText();
			}, delegate
			{
				OpenFpsSelector();
			}, "FPS");
			MakeSelectorRow(langRow, delegate
			{
				return LanguageDisplayText();
			}, delegate
			{
				OpenLanguageSelectorHub();
			}, "Language");
			MakeToggleRow(cameraShakeRow, delegate(bool on)
			{
				if (VideoSettingsPC.main != null)
				{
					VideoSettingsPC.main.SetCameraShake(on);
				}
			}, delegate
			{
				return VideoSettingsPC.main != null && VideoSettingsPC.main.settings.cameraShake;
			});
			MakeToggleRow(aaRow, delegate(bool on)
			{
				if (VideoSettingsPC.main != null)
				{
					VideoSettingsPC.main.SetAntiAliasing(on);
				}
			}, delegate
			{
				return VideoSettingsPC.main != null && VideoSettingsPC.main.settings.FXAA;
			});
			Transform rotateRow = CreateScreenRotateRow(scroller, soundRow);
			// 4. 重排（带空行分组）：
			//    音效 音乐 [空] 页面比例 页面不透明度 [空] 屏幕旋转 视角摇晃 抗锯齿 [空] 帧数 轨道分辨率 语言
			Transform spacer1 = spacers.Count > 0 ? spacers[0] : null;
			Transform spacer2 = spacers.Count > 1 ? spacers[1] : null;
			Transform spacer3 = spacers.Count > 2 ? spacers[2] : null;
			Transform[] order = new Transform[]
			{
				soundRow,
				musicRow,
				spacer1,
				uiScaleRow,
				uiOpacityRow,
				spacer2,
				rotateRow,
				cameraShakeRow,
				aaRow,
				spacer3,
				fpsRow,
				conicRow,
				langRow
			};
			for (int i = 0; i < order.Length; i++)
			{
				if (order[i] != null)
				{
					order[i].SetSiblingIndex(i);
				}
			}
			Debug.Log("[Hub] 设置大改: 重排完成");
		}

		// 把"下拉选择"行改成按钮行：隐藏 Dropdown，右侧显示当前值，点击打开选择窗口
		private void MakeSelectorRow(Transform row, Func<string> valueText, Action onClick, string rowName)
		{
			if (row == null)
			{
				return;
			}
			Transform dd = row.Find("Dropdown");
			if (dd != null)
			{
				dd.gameObject.SetActive(false);
			}
			Transform oldVal = row.Find("Selector Value");
			if (oldVal != null)
			{
				UnityEngine.Object.Destroy(oldVal.gameObject);
			}
			// 圆角底只覆盖右侧选项区域（行名区域无底）。
			// 注意：Image 底与 TMP 文字不能放同一物体 —— 同物体时 AddComponent<TMP>() 缺 CanvasRenderer，
			// 设置 font 属性会 NullReferenceException。分离成两个子物体：BG（Image）在下，Value（TMP）在上。
			GameObject bgGo = new GameObject("Selector BG", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			bgGo.transform.SetParent(row, false);
			RectTransform br = bgGo.GetComponent<RectTransform>();
			br.anchorMin = new Vector2(0.5f, 0.5f);
			br.anchorMax = new Vector2(0.5f, 0.5f);
			br.pivot = new Vector2(0.5f, 0.5f);
			br.anchoredPosition = new Vector2(115f, 0f);
			br.sizeDelta = new Vector2(200f, 40f);
			UnityEngine.UI.Image valBg = bgGo.GetComponent<UnityEngine.UI.Image>();
			valBg.sprite = LegacyUITheme.RoundedSprite;
			valBg.type = UnityEngine.UI.Image.Type.Sliced;
			valBg.color = LegacyUITheme.ButtonBaseColor;
			valBg.raycastTarget = false;
			UnityEngine.UI.LayoutElement ble = bgGo.AddComponent<UnityEngine.UI.LayoutElement>();
			ble.ignoreLayout = true;
			// 圆角底宽度按文字内容手动计算（不用 ContentSizeFitter：设置菜单初始隐藏时
			// TMP 未生成网格，CSF 会把底缩成 0 宽"缩成一团"）
			// 值文字（BG 的子物体，stretch 撑满，右对齐，右侧留 12px 边距）
			GameObject valGo = new GameObject("Selector Value", typeof(RectTransform));
			valGo.transform.SetParent(bgGo.transform, false);
			RectTransform vr = valGo.GetComponent<RectTransform>();
			vr.anchorMin = new Vector2(0f, 0f);
			vr.anchorMax = new Vector2(1f, 1f);
			vr.offsetMin = new Vector2(0f, 0f);
			vr.offsetMax = new Vector2(-12f, 0f);
			TMPro.TextMeshProUGUI valTmp = valGo.AddComponent<TMPro.TextMeshProUGUI>();
			TMPro.TMP_FontAsset font = GetMainFont(row);
			if (font != null)
			{
				valTmp.font = font;
			}
			valTmp.text = valueText != null ? valueText() : "";
			valTmp.alignment = TMPro.TextAlignmentOptions.Right;
			valTmp.fontSize = 32f;
			valTmp.color = Color.white;
			valTmp.raycastTarget = false;
			valTmp.ForceMeshUpdate();
			float textW = valTmp.preferredWidth;
			if (textW <= 0f)
			{
				textW = 120f;
			}
			br.sizeDelta = new Vector2(Mathf.Max(textW + 24f, 60f), 40f);
			if (rowName == "FPS")
			{
				fpsValueText = valTmp;
				fpsValueBg = br;
			}
			else if (rowName == "Language")
			{
				langValueText = valTmp;
				langValueBg = br;
			}
			SFS.UI.Button rowBtn = row.GetComponent<SFS.UI.Button>();
			if (rowBtn == null)
			{
				rowBtn = row.gameObject.AddComponent<SFS.UI.Button>();
				rowBtn.clickEvent = new ClickUnityEvent();
				rowBtn.holdEvent = new HoldUnityEvent();
				rowBtn.onClick += (Action<OnInputEndData>)delegate
				{
					if (onClick != null)
					{
						onClick();
					}
				};
			}
		}

		// 把"下拉选择"行改成开关行：隐藏 Dropdown，右侧建 toggle 开关
		private void MakeToggleRow(Transform row, Action<bool> onToggle, Func<bool> getValue)
		{
			if (row == null)
			{
				return;
			}
			Transform dd = row.Find("Dropdown");
			if (dd != null)
			{
				dd.gameObject.SetActive(false);
			}
			Transform oldT = row.Find("Settings Toggle");
			if (oldT != null)
			{
				UnityEngine.Object.Destroy(oldT.gameObject);
			}
			GameObject tg = new GameObject("Settings Toggle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			tg.transform.SetParent(row, false);
			RectTransform tr = tg.GetComponent<RectTransform>();
			tr.anchorMin = new Vector2(0.5f, 0.5f);
			tr.anchorMax = new Vector2(0.5f, 0.5f);
			tr.pivot = new Vector2(0.5f, 0.5f);
			tr.anchoredPosition = new Vector2(115f, 0f);
			tr.sizeDelta = new Vector2(80f, 48f);
			UnityEngine.UI.Image bg = tg.GetComponent<UnityEngine.UI.Image>();
			bg.sprite = LegacyUITheme.RoundedSprite;
			bg.type = UnityEngine.UI.Image.Type.Sliced;
			bg.raycastTarget = false;
			// 滑块（参考作弊菜单样式：55×48 滑块从 x=0 滑到 x=25，内部白块内缩 6px）
			GameObject knob = new GameObject("Knob", typeof(RectTransform));
			knob.transform.SetParent(tg.transform, false);
			RectTransform kr = knob.GetComponent<RectTransform>();
			kr.anchorMin = new Vector2(0f, 0.5f);
			kr.anchorMax = new Vector2(0f, 0.5f);
			kr.pivot = new Vector2(0f, 0.5f);
			kr.anchoredPosition = new Vector2(0f, 0f);
			kr.sizeDelta = new Vector2(55f, 48f);
			GameObject fill = new GameObject("Knob Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			fill.transform.SetParent(knob.transform, false);
			RectTransform fr = fill.GetComponent<RectTransform>();
			fr.anchorMin = new Vector2(0f, 0f);
			fr.anchorMax = new Vector2(1f, 1f);
			fr.offsetMin = new Vector2(6f, 6f);
			fr.offsetMax = new Vector2(-6f, -6f);
			UnityEngine.UI.Image fimg = fill.GetComponent<UnityEngine.UI.Image>();
			fimg.sprite = LegacyUITheme.RoundedSprite;
			fimg.type = UnityEngine.UI.Image.Type.Sliced;
			fimg.raycastTarget = false;
			UnityEngine.UI.LayoutElement le = tg.AddComponent<UnityEngine.UI.LayoutElement>();
			le.ignoreLayout = true;
			SetToggleVisual(bg, kr, fimg, getValue != null && getValue());
			SFS.UI.Button b = tg.AddComponent<SFS.UI.Button>();
			b.clickEvent = new ClickUnityEvent();
			b.holdEvent = new HoldUnityEvent();
			b.onClick += (Action<OnInputEndData>)delegate
			{
				bool old = getValue != null && getValue();
				if (onToggle != null)
				{
					onToggle(!old);
				}
				SetToggleVisual(bg, kr, fimg, !old);
			};
		}

		// 开关视觉（参考作弊菜单样式）：on=底亮+滑块滑到右端；off=底暗+滑块在左端
		private static void SetToggleVisual(UnityEngine.UI.Image bg, RectTransform knob, UnityEngine.UI.Image fill, bool on)
		{
			if (bg != null)
			{
				bg.color = on ? new Color(0.20f, 0.22f, 0.26f, 0.85f) : LegacyUITheme.ButtonBaseColor;
			}
			if (knob != null)
			{
				knob.anchorMin = new Vector2(0f, 0.5f);
				knob.anchorMax = new Vector2(0f, 0.5f);
				knob.anchoredPosition = new Vector2(on ? 25f : 0f, 0f);
			}
			if (fill != null)
			{
				fill.color = on ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
			}
		}

		// 新建"屏幕旋转"行：Label + toggle，插在页面不透明度之后
		private Transform CreateScreenRotateRow(Transform scroller, Transform refRow)
		{
			float rowW = 1100f;
			RectTransform refRt = refRow != null ? refRow as RectTransform : null;
			if (refRt != null && refRt.rect.width > 10f)
			{
				rowW = refRt.rect.width;
			}
			GameObject go = new GameObject("Rotate Row", typeof(RectTransform));
			go.transform.SetParent(scroller, false);
			RectTransform rr = go.GetComponent<RectTransform>();
			rr.anchorMin = new Vector2(0.5f, 1f);
			rr.anchorMax = new Vector2(0.5f, 1f);
			rr.pivot = new Vector2(0.5f, 1f);
			rr.sizeDelta = new Vector2(rowW, 50f);
			// 必须有 LayoutElement，否则 Scroller 的 VerticalLayoutGroup 会把它高度压成 0
			UnityEngine.UI.LayoutElement rle = go.AddComponent<UnityEngine.UI.LayoutElement>();
			rle.preferredHeight = 50f;
			GameObject labelGo = new GameObject("Label", typeof(RectTransform));
			labelGo.transform.SetParent(go.transform, false);
			RectTransform lr = labelGo.GetComponent<RectTransform>();
			lr.anchorMin = new Vector2(0.5f, 0.5f);
			lr.anchorMax = new Vector2(0.5f, 0.5f);
			lr.pivot = new Vector2(0.5f, 0.5f);
			lr.anchoredPosition = new Vector2(-140f, 0f);
			lr.sizeDelta = new Vector2(400f, 50f);
			TMPro.TextMeshProUGUI lt = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
			TMPro.TMP_FontAsset font = GetMainFont(scroller);
			if (font != null)
			{
				lt.font = font;
			}
			lt.text = "屏幕旋转";
			lt.alignment = TMPro.TextAlignmentOptions.Center;
			lt.fontSize = 34f;
			lt.color = Color.white;
			lt.raycastTarget = false;
			UnityEngine.UI.LayoutElement lle = labelGo.AddComponent<UnityEngine.UI.LayoutElement>();
			lle.ignoreLayout = true;
			// toggle（参考作弊菜单样式）
			GameObject tg = new GameObject("Settings Toggle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			tg.transform.SetParent(go.transform, false);
			RectTransform tr = tg.GetComponent<RectTransform>();
			tr.anchorMin = new Vector2(0.5f, 0.5f);
			tr.anchorMax = new Vector2(0.5f, 0.5f);
			tr.pivot = new Vector2(0.5f, 0.5f);
			tr.anchoredPosition = new Vector2(115f, 0f);
			tr.sizeDelta = new Vector2(80f, 48f);
			UnityEngine.UI.Image bg = tg.GetComponent<UnityEngine.UI.Image>();
			bg.sprite = LegacyUITheme.RoundedSprite;
			bg.type = UnityEngine.UI.Image.Type.Sliced;
			bg.raycastTarget = false;
			GameObject knob = new GameObject("Knob", typeof(RectTransform));
			knob.transform.SetParent(tg.transform, false);
			RectTransform kr = knob.GetComponent<RectTransform>();
			kr.anchorMin = new Vector2(0f, 0.5f);
			kr.anchorMax = new Vector2(0f, 0.5f);
			kr.pivot = new Vector2(0f, 0.5f);
			kr.anchoredPosition = new Vector2(0f, 0f);
			kr.sizeDelta = new Vector2(55f, 48f);
			GameObject fill = new GameObject("Knob Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			fill.transform.SetParent(knob.transform, false);
			RectTransform fr = fill.GetComponent<RectTransform>();
			fr.anchorMin = new Vector2(0f, 0f);
			fr.anchorMax = new Vector2(1f, 1f);
			fr.offsetMin = new Vector2(6f, 6f);
			fr.offsetMax = new Vector2(-6f, -6f);
			UnityEngine.UI.Image fimg = fill.GetComponent<UnityEngine.UI.Image>();
			fimg.sprite = LegacyUITheme.RoundedSprite;
			fimg.type = UnityEngine.UI.Image.Type.Sliced;
			fimg.raycastTarget = false;
			UnityEngine.UI.LayoutElement tle = tg.AddComponent<UnityEngine.UI.LayoutElement>();
			tle.ignoreLayout = true;
			bool initOn = VideoSettingsPC.main != null && VideoSettingsPC.main.settings.allowLandscape;
			SetToggleVisual(bg, kr, fimg, initOn);
			SFS.UI.Button b = tg.AddComponent<SFS.UI.Button>();
			b.clickEvent = new ClickUnityEvent();
			b.holdEvent = new HoldUnityEvent();
			b.onClick += (Action<OnInputEndData>)delegate
			{
				bool old = VideoSettingsPC.main != null && VideoSettingsPC.main.settings.allowLandscape;
				if (VideoSettingsPC.main != null)
				{
					VideoSettingsPC.main.SetAllowLandscape(!old);
				}
				SetToggleVisual(bg, kr, fimg, !old);
			};
			return go.transform;
		}

		// 帧数行当前值文字
		private string FpsDisplayText()
		{
			if (VideoSettingsPC.main == null)
			{
				return "";
			}
			int fps = VideoSettingsPC.main.settings.fps;
			return fps == -1 ? (string)Loc.main.Fps_Unlimited : fps.ToString();
		}

		// 语言行当前值文字
		private string LanguageDisplayText()
		{
			if (LanguageSettings.main == null || LanguageSettings.main.settings == null)
			{
				return "";
			}
			return Base.language.GetDisplayName(LanguageSettings.main.settings);
		}

		// 打开设置菜单时刷新：帧数/语言当前值 + 圆角底宽度跟随文字
		private void RefreshSettingsValues()
		{
			if (fpsValueText != null)
			{
				fpsValueText.text = FpsDisplayText();
				SyncSelectorBgWidth(fpsValueText, fpsValueBg);
			}
			if (langValueText != null)
			{
				langValueText.text = LanguageDisplayText();
				SyncSelectorBgWidth(langValueText, langValueBg);
			}
		}

		// 圆角底宽度 = 文字宽 + 24（左 12 右 12 边距），最小 60
		private static void SyncSelectorBgWidth(TMPro.TextMeshProUGUI tmp, RectTransform bg)
		{
			if (tmp == null || bg == null)
			{
				return;
			}
			tmp.ForceMeshUpdate();
			float tw = tmp.preferredWidth;
			if (tw <= 0f)
			{
				tw = 120f;
			}
			bg.sizeDelta = new Vector2(Mathf.Max(tw + 24f, 60f), 40f);
		}

		// 帧数选择窗口：模仿语言选择器
		private void OpenFpsSelector()
		{
			SizeSyncerBuilder.Carrier carrier;
			List<MenuElement> list = new List<MenuElement>
			{
				TextBuilder.CreateText(() => "帧数"),
				ElementGenerator.VerticalSpace(30),
				new SizeSyncerBuilder(out carrier)
			};
			string[] labels = new string[]
			{
				Loc.main.Fps_Unlimited,
				"30",
				"60",
				"100",
				"120",
				"144",
				"240"
			};
			int[] values = new int[]
			{
				-1,
				30,
				60,
				100,
				120,
				144,
				240
			};
			for (int i = 0; i < labels.Length; i++)
			{
				string lab = labels[i];
				int val = values[i];
				list.Add(ButtonBuilder.CreateButton(carrier, () => lab, delegate
				{
					ScreenManager.main.CloseCurrent();
					if (VideoSettingsPC.main != null)
					{
						VideoSettingsPC.main.SetFps(val);
					}
				}, CloseMode.None).MinSize(300f, 60f));
			}
			ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.None, CloseMode.None, delegate
			{
			}, delegate
			{
			}, list.ToArray()));
			// 像扩展包选择一样：去蓝色底框 + 底部返回键
			RestyleSelectorMenu(ScreenManager.main != null ? ScreenManager.main.CurrentScreen : null);
		}

		// 语言选择窗口：打开原版选择器 + 延迟去蓝框（原版选择器是异步构建的）
		private void OpenLanguageSelectorHub()
		{
			if (LanguageSettings.main != null)
			{
				LanguageSettings.main.OpenLanguageSelector();
			}
			StartCoroutine(RestyleLangSelectorLate());
		}

		private IEnumerator RestyleLangSelectorLate()
		{
			// 语言选择器异步构建（RunAfterInitialization），轮询当前屏幕找蓝框
			for (int i = 0; i < 120; i++)
			{
				Screen_Base screen = ScreenManager.main != null ? ScreenManager.main.CurrentScreen : null;
				if (screen != null)
				{
					bool hasBlue = false;
					Transform[] all = screen.transform.GetComponentsInChildren<Transform>(true);
					for (int j = 0; j < all.Length; j++)
					{
						if (all[j].name == "SFS_BackPanel Blue")
						{
							hasBlue = true;
							break;
						}
					}
					if (hasBlue)
					{
						RestyleSelectorMenu(screen);
						yield break;
					}
				}
				yield return null;
			}
		}

		// 选择器窗口美化：去蓝色底框、补底部返回键（与扩展包选择一致）
		private void RestyleSelectorMenu(Screen_Base screen)
		{
			if (screen == null)
			{
				return;
			}
			Transform root = screen.transform;
			Transform[] all = root.GetComponentsInChildren<Transform>(true);
			int blue = 0;
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i].name == "SFS_BackPanel Blue")
				{
					all[i].gameObject.SetActive(false);
					blue++;
				}
			}
			Debug.Log("[Hub] 选择器去蓝框=" + blue);
			// 清理旧的返回键
			Transform[] all2 = root.GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < all2.Length; i++)
			{
				if (all2[i].name == "Hub Selector Back")
				{
					UnityEngine.Object.Destroy(all2[i].gameObject);
				}
			}
			// 底部返回键
			GameObject backGo = new GameObject("Hub Selector Back", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			backGo.transform.SetParent(root, false);
			RectTransform brt = backGo.GetComponent<RectTransform>();
			brt.anchorMin = new Vector2(0.5f, 0f);
			brt.anchorMax = new Vector2(0.5f, 0f);
			brt.pivot = new Vector2(0.5f, 0.5f);
			brt.anchoredPosition = new Vector2(0f, 57.5f);
			brt.sizeDelta = new Vector2(600f, 85f);
			UnityEngine.UI.Image bimg = backGo.GetComponent<UnityEngine.UI.Image>();
			bimg.sprite = LegacyUITheme.RoundedSprite;
			bimg.type = UnityEngine.UI.Image.Type.Sliced;
			bimg.color = LegacyUITheme.ButtonBaseColor;
			GameObject bText = new GameObject("Text", typeof(RectTransform));
			bText.transform.SetParent(backGo.transform, false);
			RectTransform btr = bText.GetComponent<RectTransform>();
			btr.anchorMin = Vector2.zero;
			btr.anchorMax = Vector2.one;
			btr.offsetMin = Vector2.zero;
			btr.offsetMax = Vector2.zero;
			TMPro.TextMeshProUGUI btmp = bText.AddComponent<TMPro.TextMeshProUGUI>();
			TMPro.TMP_FontAsset font = GetMainFont(root);
			if (font != null)
			{
				btmp.font = font;
			}
			btmp.text = "返回";
			btmp.alignment = TMPro.TextAlignmentOptions.Center;
			btmp.fontSize = 38f;
			btmp.color = Color.white;
			btmp.raycastTarget = false;
			SFS.UI.Button bb = backGo.AddComponent<SFS.UI.Button>();
			bb.clickEvent = new ClickUnityEvent();
			bb.holdEvent = new HoldUnityEvent();
			bb.onClick += (Action<OnInputEndData>)delegate
			{
				if (ScreenManager.main != null)
				{
					ScreenManager.main.CloseCurrent();
				}
			};
		}

		// 获取主字体（中英文统一）：优先从设置菜单已有 TMP 复制，其次用全局 CJK fallback
		private static TMPro.TMP_FontAsset GetMainFont(Transform root)
		{
			if (root != null)
			{
				TMPro.TMP_Text[] ts = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
				for (int i = 0; i < ts.Length; i++)
				{
					if (ts[i] != null && ts[i].font != null)
					{
						return ts[i].font;
					}
				}
			}
			if (TMPro.TMP_Settings.fallbackFontAssets != null)
			{
				for (int i = 0; i < TMPro.TMP_Settings.fallbackFontAssets.Count; i++)
				{
					TMPro.TMP_FontAsset f = TMPro.TMP_Settings.fallbackFontAssets[i];
					if (f != null && (f.name.Contains("CJK") || f.name.Contains("Han")))
					{
						return f;
					}
				}
			}
			return null;
		}
	}
}
