using System;
using System.Collections.Generic;
using System.Linq;
using SFS.IO;
using SFS.Input;
using SFS.Translations;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI
{
	public class WorldsMenu : MonoBehaviour
	{
		public RectTransform worldElementHolder;

		public ScrollElement scrollElement;

		public ReorderingGroup reorderingGroup;

		public RadioGroup worldsGroup;

		public WorldElement worldElementPrefab;

		public RectMask2D mask;

		public Button[] worldButtons;

		private List<WorldElement> elements = new List<WorldElement>();

		private OrderedPathList orderer;

		private WorldElement selected;

		private Transform mainMenuButton;

		private Transform newWorldButton;

		private Button startButton;

		private Button resumeButton;

		private Button deleteButton;

		private Button renameButton;

		private WorldElement expandedElement;

		private void Awake()

		{

			ApplyWorldsMenuLayout();

		}

		private void Start()
		{
			worldElementPrefab.gameObject.SetActive(value: false);
			// worldElementHolder 的 VerticalLayoutGroup 会覆盖子元素高度，禁用高度控制以让 LayoutElement 生效
			if (worldElementHolder != null)
			{
				UnityEngine.UI.VerticalLayoutGroup vlg = worldElementHolder.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
				if (vlg != null)
				{
					vlg.childControlHeight = false;
					vlg.childForceExpandHeight = false;
				}
			}
			// 重新应用布局（Awake 后场景可能重置了 RectTransform）
			ApplyWorldsMenuLayout();
			UpdateOrder();
			Loc.OnChange += new Action(LoadUI);
			RadioGroup radioGroup = worldsGroup;
			radioGroup.onSelectionChanged = (Action<RadioButton>)Delegate.Combine(radioGroup.onSelectionChanged, new Action<RadioButton>(UpdateWorldActions));
			ReorderingGroup obj = reorderingGroup;
			obj.onReorder = (Action<ReorderingModule>)Delegate.Combine(obj.onReorder, new Action<ReorderingModule>(OnReorder));
		}

		private void OnDestroy()
		{
			Loc.OnChange -= new Action(LoadUI);
		}

		private void OnEnable()
		{
			LoadUI((selected != null) ? selected.Name : null);
			ApplyWorldsMenuLayout();
		}



		private void UpdateWorldActions(RadioButton selectedButton)
		{
			selected = selectedButton != null ? elements.SingleOrDefault((WorldElement element) => element.radioButton == selectedButton) : null;
			if (expandedElement == selected)
			{
				return;
			}
			if (expandedElement != null)
			{
				CollapseWorldElement(expandedElement);
			}
			expandedElement = selected;
			if (selected != null)
			{
				ExpandWorldElement(selected);
			}
		}

		public void OpenHub()
		{
			WorldBaseManager.EnterWorld(selected.Name, Base.sceneLoader.LoadHubScene);
		}

		public void OpenWorld()
		{
			if (selected.World.worldPersistentPath.FolderExists())
			{
				WorldBaseManager.EnterWorld(selected.Name, delegate
				{
					Base.sceneLoader.LoadWorldScene();
				});
			}
		}

		public void DeleteWorld()
		{
			MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.World_Delete, () => Loc.main.Delete, delegate
			{
				selected.World.path.DeleteFolder();
				orderer.Remove(selected.Name);
				LoadUI();
			});
		}

		public void RenameWorld()
		{
			Menu.textInput.Open(Loc.main.Cancel, Loc.main.Rename, delegate(string[] newName)
			{
				string text = newName[0];
				text = PathUtility.MakeUsable(text, Loc.main.Default_World_Name);
				if (!(text == selected.Name))
				{
					text = PathUtility.AutoNameExisting(text, FileLocations.WorldsFolder);
					orderer.Rename(selected.Name, text);
					selected.World.path.RenameFolder(text);
					LoadUI(text);
				}
			}, CloseMode.Current, TextInputMenu.Element(Loc.main.Create_World_Title, selected.Name));
		}

		private void OnReorder(ReorderingModule reorderedModule)
		{
			WorldElement worldElement = elements.First((WorldElement x) => x.reorderingModule == reorderedModule);
			orderer.Move(worldElement.Name, worldElement.transform.GetSiblingIndex() - 1);
		}

		private void LoadUI()
		{
			LoadUI(null);
		}

		private void LoadUI(string autoSelectTarget)
		{
			expandedElement = null;
			elements.ForEach(delegate(WorldElement x)
			{
				UnityEngine.Object.Destroy(x.gameObject);
			});
			elements.Clear();
			UpdateOrder();
			WorldElement worldElement = null;
			foreach (string item in orderer.GetOrder())
			{
				WorldElement worldElement2 = UnityEngine.Object.Instantiate(worldElementPrefab, worldElementHolder);
				worldElement2.gameObject.SetActive(value: true);
				worldElement2.Setup(new WorldReference(FileLocations.WorldsFolder.Extend(item).FolderName));
				LegacyUITheme.ApplyButton(worldElement2.gameObject);
				if (worldElement2.radioButton != null && worldElement2.radioButton.selectedIndicator != null)
				{
					LegacyUITheme.ConfigureSelectionIndicator(worldElement2.radioButton.selectedIndicator);
				}
				worldElement2.radioButton.group = worldsGroup;
				elements.Add(worldElement2);
				reorderingGroup.Add(worldElement2.reorderingModule);
				scrollElement.RegisterScrolling(worldElement2.button, () => !reorderingGroup.holding);
				if (item == autoSelectTarget)
				{
					worldElement2.radioButton.Select();
					worldElement = worldElement2;
				}
			}
			if (newWorldButton != null)
			{
				newWorldButton.SetAsFirstSibling();
			}
			UpdateWorldActions((worldElement != null) ? worldElement.radioButton : null);
		}

		private void UpdateOrder()
		{
			orderer = GetOrdered();
		}

		public static OrderedPathList GetOrdered()
		{
			return new OrderedPathList(FileLocations.WorldsFolder, FileLocations.WorldsFolder.FolderExists() ? FileLocations.WorldsFolder.GetFoldersInFolder(recursively: false).Select((Func<FolderPath, BasePath>)((FolderPath f) => f)).ToArray() : new BasePath[0]);
		}

		public void OnWorldCreated(string worldName)
		{
			UpdateOrder();
			LoadUI(worldName);
		}

		private void ApplyWorldsMenuLayout()
		{
			Transform panel = transform.Find("Panel");

			if (panel == null)
			{
				return;
			}
			// 放大整个"选择世界"窗口面板：高度 1200 → 1400，宽度适配画布（竖屏 1100 时不超出屏幕）
			RectTransform panelRect = panel as RectTransform;
			if (panelRect != null)
			{
				float canvasW = 1500f;
				RectTransform canvasRt = transform.root as RectTransform;
				if (canvasRt != null && canvasRt.rect.width > 10f)
				{
					canvasW = canvasRt.rect.width;
				}
				panelRect.sizeDelta = new Vector2(canvasW - 80f, 1400f);
			}
			for (int ci = 0; ci < panel.childCount; ci++)
			{

			}
			Transform backWindows = panel.Find("BackWindows");
			if (backWindows != null)
			{
				backWindows.gameObject.SetActive(false);
			}
			RectTransform scrollRect = panel.Find("Scroll Window") as RectTransform;
			if (scrollRect != null)
			{
				scrollRect.anchorMin = Vector2.zero;
				scrollRect.anchorMax = Vector2.one;
				scrollRect.pivot = new Vector2(0.5f, 0.5f);
				// 世界列表上下拉宽：填满整个面板高度（面板 1200 高，已是最大）。
				// 注意：offsetMin/offsetMax 由 anchoredPosition+sizeDelta 派生，直接设尺寸避免互相覆盖
				scrollRect.anchoredPosition = Vector2.zero;
				scrollRect.sizeDelta = new Vector2(-50f, 0f);
				Debug.Log("[WM] ScrollWindow 应用: rect=" + scrollRect.rect + " anchoredPos=" + scrollRect.anchoredPosition + " sizeDelta=" + scrollRect.sizeDelta);
				StartCoroutine(LogScrollWindowLate(scrollRect));

			}
			if (worldElementHolder != null)
			{
				RectTransform hr = worldElementHolder as RectTransform;
				Debug.Log("[WM] 内容(WorldList) 应用: rect=" + (hr != null ? hr.rect.ToString() : "null") + " anchoredPos=" + (hr != null ? hr.anchoredPosition.ToString() : "null") + " 父=" + (worldElementHolder.parent != null ? worldElementHolder.parent.name : "null"));
				if (hr != null && scrollRect != null)
				{
					Debug.Log("[WM] 屏幕坐标: 窗口屏上=" + RectTransformUtility.WorldToScreenPoint(null, scrollRect.position) + " 窗口rect=" + scrollRect.rect + " | 内容屏上=" + RectTransformUtility.WorldToScreenPoint(null, hr.position) + " 内容rect=" + hr.rect);
				}
			}
			else
			{

			}
			Transform bottomPanel = panel.Find("BottomPanel");
			if (bottomPanel == null)
			{
				return;
			}
			mainMenuButton = bottomPanel.Find("Main Menu");
			newWorldButton = bottomPanel.Find("Create World");
			if (mainMenuButton != null)
			{
				// 挂到根对象（全屏），锚定屏幕底部——面板放大后也不会被推出屏幕
				mainMenuButton.SetParent(transform, false);
				RectTransform rt = mainMenuButton as RectTransform;
				if (rt != null)
				{
					rt.anchorMin = new Vector2(0f, 0f);
					rt.anchorMax = new Vector2(1f, 0f);
					rt.pivot = new Vector2(0.5f, 0.5f);
					// 居中贴下：左右缝 25，底部 20，高 75（顶到 95）。直接设 anchoredPosition+sizeDelta 避免 offset 互相覆盖
					rt.anchoredPosition = new Vector2(0f, 57.5f);
					rt.sizeDelta = new Vector2(-50f, 75f);

				}
				mainMenuButton.gameObject.SetActive(true);
				LegacyUITheme.ApplyButton(mainMenuButton.gameObject);
			}
			if (newWorldButton != null && worldElementHolder != null)
			{
				newWorldButton.SetParent(worldElementHolder, false);
				// 清除 ContainerElement/ScaleTextToFit：防止按钮自动缩放到文字宽度、以及 "Text cannot be root" 报错
				StripCloneComponents(newWorldButton.gameObject);
				RectTransform rt = newWorldButton as RectTransform;
				if (rt != null)
				{
					rt.anchorMin = new Vector2(0.5f, 1f);
					rt.anchorMax = new Vector2(0.5f, 1f);
					rt.pivot = new Vector2(0.5f, 1f);
					rt.anchoredPosition = Vector2.zero;
					// 宽度 = 世界列表内容宽度（竖屏下自动适配），高度 = 折叠世界项(约338)的 1/3 ≈ 113
					float cwWidth = (worldElementHolder != null && worldElementHolder.rect.width > 10f) ? worldElementHolder.rect.width : 1350f;
					rt.sizeDelta = new Vector2(cwWidth, 113f);
				}
				UnityEngine.UI.LayoutElement cwLayout = newWorldButton.GetComponent<UnityEngine.UI.LayoutElement>();
				if (cwLayout == null)
				{
					cwLayout = newWorldButton.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
				}
				float cwPref = (worldElementHolder != null && worldElementHolder.rect.width > 10f) ? worldElementHolder.rect.width : 1350f;
				cwLayout.preferredWidth = cwPref;
				cwLayout.preferredHeight = 113f;
				newWorldButton.gameObject.SetActive(true);
				LegacyUITheme.ApplyButton(newWorldButton.gameObject);
				// 创建新世界文字大小与世界名称一致（字号+缩放，保证渲染尺寸相同）
				TextAdapter cwText = newWorldButton.GetComponentInChildren<TextAdapter>(true);
				TextAdapter nameText = worldElementPrefab != null ? worldElementPrefab.worldNameText : null;
				if (cwText != null && nameText != null)
				{
					TMPro.TextMeshProUGUI cwTmp = cwText.GetComponent<TMPro.TextMeshProUGUI>();
					TMPro.TextMeshProUGUI nameTmp = nameText.GetComponent<TMPro.TextMeshProUGUI>();
					if (cwTmp != null && nameTmp != null)
					{
						RectTransform cwTr = cwText.transform as RectTransform;
						RectTransform nameTr = nameText.transform as RectTransform;
						// 不修改文字缩放：SFS Button.OnKeyChange 按下/抬起时用 Start 捕获的 initialIconScale
						// 重置文字缩放（并 ×1.06 略微放大）。保持文字原始缩放(1,1.1)与 initialIconScale 一致，
						// 点击后缩放不会跳变；用字号补偿达到"世界名称渲染尺寸的一半"。
						// 目标渲染高度 = 世界名称 fontSize × 名称scale.y 的 0.8
						float nameScaleY = (nameTr != null && nameTr.localScale.y != 0f) ? nameTr.localScale.y : 1f;
						float cwScaleY = (cwTr != null && cwTr.localScale.y != 0f) ? cwTr.localScale.y : 1f;
						float targetRender = nameTmp.fontSize * nameScaleY * 0.8f;
						float targetFont = targetRender / cwScaleY;
						cwTmp.fontSize = targetFont;
						cwTmp.enableAutoSizing = false;
						Debug.Log("[CW] 初始: font=" + cwTmp.fontSize + " scale=" + (cwTr != null ? cwTr.localScale.ToString() : "null") + " 目标渲染=" + targetRender);
					}
					else
					{
						UnityEngine.UI.Text cwU = cwText.GetComponent<UnityEngine.UI.Text>();
						UnityEngine.UI.Text nameU = nameText.GetComponent<UnityEngine.UI.Text>();
						if (cwU != null && nameU != null)
						{
							cwU.fontSize = nameU.fontSize;
						}
					}
				}
			}
			// 克隆操作按钮（保留原始按钮在 BottomPanel，避免重复 ApplyWorldsMenuLayout 时 Find 失败导致字段为 null）
			if (startButton == null)
			{
				Transform playWorld = bottomPanel.Find("Play World");
				if (playWorld != null)
				{
					GameObject clone = UnityEngine.Object.Instantiate(playWorld.gameObject, panel);
					clone.name = "Play World";
					startButton = clone.GetComponent<Button>();
					StripCloneComponents(clone);
					clone.SetActive(false);
				}
			}
			if (deleteButton == null)
			{
				Transform deleteWorld = bottomPanel.Find("Delete World");
				if (deleteWorld != null)
				{
					GameObject clone = UnityEngine.Object.Instantiate(deleteWorld.gameObject, panel);
					clone.name = "Delete World";
					deleteButton = clone.GetComponent<Button>();
					StripCloneComponents(clone);
					clone.SetActive(false);
				}
			}
			if (renameButton == null)
			{
				Transform rename = bottomPanel.Find("Rename");
				if (rename != null)
				{
					GameObject clone = UnityEngine.Object.Instantiate(rename.gameObject, panel);
					clone.name = "Rename";
					renameButton = clone.GetComponent<Button>();
					StripCloneComponents(clone);
					clone.SetActive(false);
				}
			}
			if (startButton != null && resumeButton == null)
			{
				GameObject clone = UnityEngine.Object.Instantiate(startButton.gameObject, panel);
				clone.name = "Resume Game";
				resumeButton = clone.GetComponent<Button>();
				// startButton 的组件销毁是延迟到帧末的，克隆会复制到尚未销毁的组件，需再清理一次
				StripCloneComponents(clone);
				clone.SetActive(false);
			}
			bottomPanel.gameObject.SetActive(false);
		}

		// 清除克隆按钮上会改动矩形尺寸/报错的组件：
		// ContainerElement(NewElement) 首次激活时会 Start→ActAsRoot→SetRectSize，
		// 把按钮宽度改成文字 preferred 宽度（导致"第一次展开按钮缩小"）。
		// 文字上的 ScaleTextToFit(NewElement) 的 parent 依赖 ContainerElement.Awake 设置，
		// ContainerElement 移除后 parent 为 null，每帧 LateUpdate→UpdateHierarchy→ActAsRoot
		// 报 "Text cannot be root"。全部禁用并销毁。
		private static void StripCloneComponents(GameObject clone)
		{
			ContainerElement ce = clone.GetComponent<ContainerElement>();
			if (ce != null)
			{
				ce.enabled = false;
				UnityEngine.Object.Destroy(ce);
			}
			ScaleTextToFit[] fits = clone.GetComponentsInChildren<ScaleTextToFit>(true);
			for (int i = 0; i < fits.Length; i++)
			{
				fits[i].enabled = false;
				UnityEngine.Object.Destroy(fits[i]);
			}
		}

		private System.Collections.IEnumerator LogScrollWindowLate(RectTransform scrollRect)
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			if (scrollRect != null)
			{
				Debug.Log("[WM] ScrollWindow late: rect=" + scrollRect.rect + " anchoredPos=" + scrollRect.anchoredPosition + " sizeDelta=" + scrollRect.sizeDelta);
			}
			if (worldElementHolder != null)
			{
				RectTransform hr = worldElementHolder as RectTransform;
				Debug.Log("[WM] 内容 late: rect=" + (hr != null ? hr.rect.ToString() : "null") + " anchoredPos=" + (hr != null ? hr.anchoredPosition.ToString() : "null") + " worldPos=" + (hr != null ? hr.position.ToString() : "null"));
				if (elements.Count > 0)
				{
					RectTransform e0 = elements[0].transform as RectTransform;
					Debug.Log("[WM] 首个元素 late: rect=" + (e0 != null ? e0.rect.ToString() : "null") + " worldPos=" + (e0 != null ? e0.position.ToString() : "null"));
				}
			}
		}

		private void CollapseWorldElement(WorldElement element)
		{
			element.SetExpanded(false);
			if (element.buttonHolder != null)
			{
				element.buttonHolder.SetActive(false);
			}
		}

		private void ExpandWorldElement(WorldElement element)
		{

			element.SetExpanded(true);
			if (element.buttonHolder == null)
			{
				BuildWorldActions(element);
			}
			if (element.buttonHolder != null)
			{
				element.buttonHolder.SetActive(true);
				PlaceActionButtons(element);
				StartCoroutine(LogButtonsTextLate());
				// 继续游戏按钮：无法继续（存档无火箭/玩家地址）时置灰，和 ESC 菜单一致
				if (resumeButton != null)
				{
					bool canResume = selected != null && selected.World != null && selected.World.CanResumeGame();
					resumeButton.SetEnabled(canResume);
				}
			}
		}

		private System.Collections.IEnumerator LogButtonsTextLate()
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			// 修复：第一次展开时 resume 文字被某处改回"开始游戏"，这里重新设回"继续游戏"
			if (resumeButton != null)
			{
				string resumeText = (string)Loc.main.Resume_Game;
				TextAdapter ta = resumeButton.GetComponentInChildren<TextAdapter>(true);
				if (ta != null)
				{
					ta.Text = resumeText;
				}
				TMPro.TMP_Text[] tmps = resumeButton.GetComponentsInChildren<TMPro.TMP_Text>(true);
				for (int i = 0; i < tmps.Length; i++)
				{
					tmps[i].text = resumeText;
				}
				UnityEngine.UI.Text[] utes = resumeButton.GetComponentsInChildren<UnityEngine.UI.Text>(true);
				for (int i = 0; i < utes.Length; i++)
				{
					utes[i].text = resumeText;
				}
			}
			LogButtonText("开始游戏", startButton);
			LogButtonText("继续游戏", resumeButton);
			LogButtonText("删除", deleteButton);
			LogButtonText("重命名", renameButton);
		}

		private void LogButtonText(string label, Button button)
		{
			if (button == null)
			{
				return;
			}
			string s = "[WM] late " + label + ": ";
			TMPro.TMP_Text[] tmps = button.GetComponentsInChildren<TMPro.TMP_Text>(true);
			for (int i = 0; i < tmps.Length; i++)
			{
				s += "TMP" + i + "='" + tmps[i].text + "'(" + tmps[i].gameObject.activeSelf + ") ";
			}
			UnityEngine.UI.Text[] utes = button.GetComponentsInChildren<UnityEngine.UI.Text>(true);
			for (int i = 0; i < utes.Length; i++)
			{
				s += "UI" + i + "='" + utes[i].text + "'(" + utes[i].gameObject.activeSelf + ") ";
			}
			Debug.Log(s);
		}

		private void BuildWorldActions(WorldElement element)
		{
			GameObject holder = new GameObject("World Actions", typeof(RectTransform));
			holder.transform.SetParent(element.transform, false);
			RectTransform holderRect = holder.GetComponent<RectTransform>();
			// holder 锚定在元素底部，向上展开 160px（两行按钮 + 间距）
			holderRect.anchorMin = new Vector2(0f, 0f);
			holderRect.anchorMax = new Vector2(1f, 0f);
			holderRect.pivot = new Vector2(0.5f, 0f);
			holderRect.anchoredPosition = new Vector2(0f, 15f);
			holderRect.sizeDelta = new Vector2(0f, 160f);
			// 世界元素自带 VerticalLayoutGroup，会把所有直接子节点重新排布
			// （曾把本 holder 吸进竖直堆叠并压成 0 高，导致按钮出现在世界信息两侧）。
			// ignoreLayout=true 让布局组完全跳过本 holder，保持锚定在元素底部。
			UnityEngine.UI.LayoutElement holderLayout = holder.AddComponent<UnityEngine.UI.LayoutElement>();
			holderLayout.ignoreLayout = true;
			element.buttonHolder = holder;
		}

		private void PlaceActionButtons(WorldElement element)
		{
			if (element.buttonHolder == null || startButton == null || resumeButton == null || deleteButton == null || renameButton == null)
			{
				return;
			}
			Transform holder = element.buttonHolder.transform;
			// 绝对定位 4 个按钮：第一行(开始/继续)底部，第二行(删除/重命名)上方；左右各占半宽
			BindActionButton(startButton, Loc.main.Play, OpenHub, holder, leftHalf: true, bottom: 0f, top: 70f);
			BindActionButton(resumeButton, Loc.main.Resume_Game, OpenWorld, holder, leftHalf: false, bottom: 0f, top: 70f);
			BindActionButton(deleteButton, Loc.main.Delete, DeleteWorld, holder, leftHalf: true, bottom: 85f, top: 155f);
			BindActionButton(renameButton, Loc.main.Rename, RenameWorld, holder, leftHalf: false, bottom: 85f, top: 155f);
		}

		private static void BindActionButton(Button button, string text, Action onClick, Transform parent, bool leftHalf, float bottom, float top)
		{
			if (button == null)
			{
				return;
			}
			button.gameObject.SetActive(true);
			button.transform.SetParent(parent, false);
			RectTransform rect = button.GetComponent<RectTransform>();
			// 绝对定位：锚定父级底部；leftHalf 控制占左半/右半，bottom~top 控制 Y 位置和高度，X 留缝。
			// 注意：offsetMin/offsetMax 是由 anchoredPosition+sizeDelta 派生出来的，
			// 先设 offset 再清 anchoredPosition/sizeDelta 会把矩形清零(高度 0 → 按钮不可见)，所以直接设 anchoredPosition+sizeDelta。
			float midY = (bottom + top) / 2f;
			float height = top - bottom;
			if (leftHalf)
			{
				// 左半：x 从 10 到 0.5W-6，y 从 bottom 到 top
				rect.anchorMin = new Vector2(0f, 0f);
				rect.anchorMax = new Vector2(0.5f, 0f);
				rect.pivot = new Vector2(0.5f, 0.5f);
				rect.anchoredPosition = new Vector2(2f, midY);
				rect.sizeDelta = new Vector2(-16f, height);
			}
			else
			{
				// 右半：x 从 0.5W+6 到 W-10
				rect.anchorMin = new Vector2(0.5f, 0f);
				rect.anchorMax = new Vector2(1f, 0f);
				rect.pivot = new Vector2(0.5f, 0.5f);
				rect.anchoredPosition = new Vector2(-2f, midY);
				rect.sizeDelta = new Vector2(-16f, height);
			}
			button.clickEvent = new ClickUnityEvent();
			button.onClick.Clear();
			if (onClick != null)
			{
				button.onClick += (Action<OnInputEndData>)delegate
				{
					onClick();
				};
			}
			LegacyUITheme.ApplyButton(button.gameObject);
			TextAdapter textAdapter = button.GetComponentInChildren<TextAdapter>(true);
			if (textAdapter != null)
			{
				textAdapter.Text = text;
				Debug.Log("[WM] 按钮文字: name=" + button.name + " 目标='" + text + "' 设置后='" + textAdapter.Text + "' adapterParent=" + (textAdapter.transform.parent != null ? textAdapter.transform.parent.name : "null"));
			}
			else
			{
				Debug.Log("[WM] 按钮文字: name=" + button.name + " 目标='" + text + "' 但找不到 TextAdapter");
			}
			// 兜底：按钮下所有 TMP 和旧版 Text 都设置（防止有多个文字组件时只改了隐藏的）
			TMPro.TMP_Text[] allTmp = button.GetComponentsInChildren<TMPro.TMP_Text>(true);
			for (int i = 0; i < allTmp.Length; i++)
			{
				allTmp[i].text = text;
			}
			UnityEngine.UI.Text[] allLegacy = button.GetComponentsInChildren<UnityEngine.UI.Text>(true);
			for (int i = 0; i < allLegacy.Length; i++)
			{
				allLegacy[i].text = text;
			}
		}
}
}