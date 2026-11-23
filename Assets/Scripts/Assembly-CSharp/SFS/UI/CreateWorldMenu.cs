using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SFS.IO;
using SFS.Input;
using SFS.Translations;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI
{
public class CreateWorldMenu : BasicMenu
{
public WorldsMenu worldsMenu;

public Button createWorldButton;

public TextBoxAdapter worldNameField;

public GameObject solarSystemSelectorHolder;

public TextAdapter solarSystemButtonText;

public ButtonGroup difficultyGroup;

public ButtonGroup modeGroup;

public ToggleButton quicksavesToggle;

public GameObject togglesHolder;

public TextAdapter difficultyInfo;

private static readonly List<WorldMode.Mode> ModeIndices = new List<WorldMode.Mode>
{
WorldMode.Mode.Sandbox,
WorldMode.Mode.Challenge
};

private static readonly List<Difficulty> DifficultyIndices = new List<Difficulty>
{
Difficulty.Normal,
Difficulty.Hard,
Difficulty.Realistic
};

private string solarSystemName;

private bool quicksaves;

// 标签文字（绑定 Loc，随语言切换更新）
private TMPro.TextMeshProUGUI nameLabel;

private Func<string> nameLabelText;

private TMPro.TextMeshProUGUI solarLabel;

private Func<string> solarLabelText;

private TMPro.TextMeshProUGUI modeLabel;

private Func<string> modeLabelText;

private TMPro.TextMeshProUGUI diffLabel;

private Func<string> diffLabelText;

private void Start()
{
ApplyCreateWorldLayout();
menuHolder.gameObject.SetActive(value: false);
quicksavesToggle.Bind(ToggleQuicksaves, () => quicksaves);
Loc.OnChange += UpdateLabelTexts;
}

private void OnDestroy()
{
Loc.OnChange -= UpdateLabelTexts;
}

// 语言切换时更新四个标签文字
private void UpdateLabelTexts()
{
if (nameLabel != null && nameLabelText != null)
{
nameLabel.text = nameLabelText();
}
if (solarLabel != null && solarLabelText != null)
{
solarLabel.text = solarLabelText();
}
if (modeLabel != null && modeLabelText != null)
{
modeLabel.text = modeLabelText();
}
if (diffLabel != null && diffLabelText != null)
{
diffLabel.text = diffLabelText();
}
}

public override void Open()
{
worldNameField.Text = Loc.main.Default_World_Name;
solarSystemSelectorHolder.SetActive(FileLocations.SolarSystemsFolder.GetFoldersInFolder(recursively: false).Any((FolderPath a) => a.FolderName != "Example"));
SetSolarSystem("");
modeGroup.SetSelectedIndex(0);
quicksaves = true;
quicksavesToggle.UpdateUI(instantAnimation: true);
difficultyGroup.SetSelectedIndex(0);
base.Open();
}

public override void Close()
{
worldNameField.Close();
base.Close();
}

private void SetSolarSystem(string a)
{
solarSystemName = a;
solarSystemButtonText.Text = ((a == "") ? ((string)Loc.main.Default_Solar_System) : a);
}

public void OpenSelectSolarSystemMenu()
{
SizeSyncerBuilder.Carrier horizontal;
List<MenuElement> buttons = new List<MenuElement>
{
TextBuilder.CreateText(() => Loc.main.Select_Solar_System),
ElementGenerator.VerticalSpace(50),
new SizeSyncerBuilder(out horizontal)
};
AddButton(Loc.main.Default_Solar_System, "");
if (DevSettings.FullVersion && FileLocations.SolarSystemsFolder.FolderExists())
{
foreach (FolderPath item in FileLocations.SolarSystemsFolder.GetFoldersInFolder(recursively: false))
{
if (item.FolderName != "Example")
{
AddButton(Loc.main.Custom_Solar_System.Inject(item.FolderName, "name"), item.FolderName);
}
}
}
MenuGenerator.OpenMenu(CancelButton.Cancel, CloseMode.Current, buttons.ToArray());
void AddButton(string text, string solarSystemName)
{
buttons.Add(ButtonBuilder.CreateButton(horizontal, () => text, delegate
{
SetSolarSystem(solarSystemName);
}, CloseMode.Current));
}
// 改造生成的菜单：去蓝色底框、放大字体和按钮、底部返回键
RestyleGeneratedMenu();
}

// 递归查找子对象
private static Transform FindRecursive(Transform root, string name)
{
if (root == null)
{
return null;
}
if (root.name == name)
{
return root;
}
for (int i = 0; i < root.childCount; i++)
{
Transform r = FindRecursive(root.GetChild(i), name);
if (r != null)
{
return r;
}
}
return null;
}

private void RestyleGeneratedMenu()
{
Screen_Base screen = ScreenManager.main != null ? ScreenManager.main.CurrentScreen : null;
Transform root = screen != null ? screen.transform : null;
if (root == null)
{
return;
}
Debug.Log("[SOLAR] screen=" + (screen != null ? screen.name : "null"));
Transform[] all = root.GetComponentsInChildren<Transform>(true);
string names = "";
for (int i = 0; i < all.Length && i < 50; i++)
{
names += all[i].name + "|";
}
Debug.Log("[SOLAR] 结构: " + names);
// 去掉所有蓝色底框（菜单里可能有多个）
Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
int blueCount = 0;
for (int i = 0; i < allTransforms.Length; i++)
{
if (allTransforms[i].name == "SFS_BackPanel Blue")
{
allTransforms[i].gameObject.SetActive(false);
blueCount++;
}
}
Debug.Log("[SOLAR] 禁用的蓝底数量=" + blueCount);
// 放大文字并居中
TMPro.TMP_Text[] texts = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
string titleText = (string)Loc.main.Select_Solar_System;
for (int i = 0; i < texts.Length; i++)
{
texts[i].alignment = TMPro.TextAlignmentOptions.Center;
if (texts[i].fontSize < 42f)
{
texts[i].fontSize = 42f;
}
// 标题"选择该世界使用的星系"上移一点（SFS 菜单顶部内边距把它压低了）
if (texts[i].text != null && texts[i].text.Trim() == titleText)
{
RectTransform titleRt = texts[i].transform as RectTransform;
if (titleRt != null)
{
titleRt.anchoredPosition = new Vector2(titleRt.anchoredPosition.x, titleRt.anchoredPosition.y + 30f);
}
}
}
// 放大按钮矩形（SFS Button 的容器）
SFS.UI.Button[] buttons = root.GetComponentsInChildren<SFS.UI.Button>(true);
for (int i = 0; i < buttons.Length; i++)
{
RectTransform br = buttons[i].transform as RectTransform;
if (br != null)
{
br.sizeDelta = new Vector2(Mathf.Max(br.sizeDelta.x, 700f), Mathf.Max(br.sizeDelta.y, 90f));
}
// 按钮文字居中
CenterText(buttons[i].transform);
ApplyMainFont(buttons[i].transform);
}
// 找到当前菜单实例（用于检测菜单关闭）
Transform holderRoot = root;
OptionsMenuDrawer drawer = screen as OptionsMenuDrawer;
if (drawer != null && drawer.contentHolder != null)
{
for (int i = 0; i < drawer.contentHolder.childCount; i++)
{
Transform c = drawer.contentHolder.GetChild(i);
if (c.gameObject.activeSelf)
{
holderRoot = c;
break;
}
}
}
// 清理旧的返回按钮（防止每次进入累积）
Transform[] allTransforms2 = root.GetComponentsInChildren<Transform>(true);
for (int i = 0; i < allTransforms2.Length; i++)
{
if (allTransforms2[i].name == "Back Button")
{
UnityEngine.Object.Destroy(allTransforms2[i].gameObject);
}
}
// 新建返回按钮：挂到根上（全屏），锚定底部居中，位置简单可靠
GameObject backGo = new GameObject("Back Button", typeof(RectTransform), typeof(Image));
backGo.transform.SetParent(root, false);
RectTransform brt = backGo.GetComponent<RectTransform>();
brt.anchorMin = new Vector2(0.5f, 0f);
brt.anchorMax = new Vector2(0.5f, 0f);
brt.pivot = new Vector2(0.5f, 0.5f);
brt.anchoredPosition = new Vector2(0f, 57.5f);
brt.sizeDelta = new Vector2(600f, 85f);
Image bimg = backGo.GetComponent<Image>();
bimg.sprite = LegacyUITheme.RoundedSprite;
bimg.type = Image.Type.Sliced;
bimg.color = LegacyUITheme.ButtonBaseColor;
bimg.raycastTarget = true;
GameObject bText = new GameObject("Text", typeof(RectTransform));
bText.transform.SetParent(backGo.transform, false);
RectTransform btr = bText.GetComponent<RectTransform>();
btr.anchorMin = Vector2.zero;
btr.anchorMax = Vector2.one;
btr.offsetMin = Vector2.zero;
btr.offsetMax = Vector2.zero;
TMPro.TextMeshProUGUI btmp = bText.AddComponent<TMPro.TextMeshProUGUI>();
btmp.text = "返回";
btmp.alignment = TMPro.TextAlignmentOptions.Center;
btmp.fontSize = 42f;
btmp.color = Color.white;
btmp.raycastTarget = false;
ApplyMainFont(backGo.transform);
SFS.UI.Button bbtn = backGo.AddComponent<SFS.UI.Button>();
// 动态创建的 Button 需要初始化事件字段，否则点击/按住会 NRE
bbtn.clickEvent = new ClickUnityEvent();
bbtn.holdEvent = new HoldUnityEvent();
bbtn.onClick += (Action<OnInputEndData>)delegate
{
if (ScreenManager.main != null)
{
ScreenManager.main.CloseCurrent();
}
};
// 菜单关闭时销毁返回按钮
StartCoroutine(WatchAndDestroyBack(backGo, holderRoot));
}

// 返回按钮随菜单关闭而销毁：监视当前屏幕变化（菜单关闭时 CurrentScreen 会切换）或菜单实例销毁
private System.Collections.IEnumerator WatchAndDestroyBack(GameObject backGo, Transform holderRoot)
{
Screen_Base expected = ScreenManager.main != null ? ScreenManager.main.CurrentScreen : null;
while (backGo != null)
{
Screen_Base now = ScreenManager.main != null ? ScreenManager.main.CurrentScreen : null;
bool menuGone = now != expected;
if (holderRoot != null)
{
menuGone = menuGone || !holderRoot.gameObject.activeSelf || holderRoot == null;
}
if (menuGone)
{
break;
}
yield return new WaitForEndOfFrame();
}
if (backGo != null)
{
UnityEngine.Object.Destroy(backGo);
}
}

private void ToggleQuicksaves()
{
quicksaves = !quicksaves;
}

public void OnDifficultyCycle()
{
string text = "";
if (DifficultyIndices.IsValidIndex(difficultyGroup.SelectedIndex))
{
Difficulty difficulty = DifficultyIndices[difficultyGroup.SelectedIndex];
text += Loc.main.Difficulty_Scale_Stat.Inject((20.0 / difficulty.DefaultRadiusScale).ToString(CultureInfo.InvariantCulture), "scale");
text = text + "\n" + Loc.main.Difficulty_Isp_Stat.Inject(difficulty.IspMultiplier.ToString(CultureInfo.InvariantCulture), "value");
text = text + "\n" + Loc.main.Difficulty_Dry_Mass_Stat.Inject(difficulty.DryMassMultiplier.ToString(CultureInfo.InvariantCulture), "value");
text = text + "\n" + Loc.main.Difficulty_Engine_Mass_Stat.Inject(difficulty.EngineMassMultiplier.ToString(CultureInfo.InvariantCulture), "value");
}
difficultyInfo.Text = text;
UpdateCanCreate();
}

public void OnModeCycle()
{
// 挑战模式的快速存档小弹窗已删除
UpdateCanCreate();
}

private void UpdateCanCreate()
{
createWorldButton.SetEnabled(modeGroup.SelectedIndex != -1 && difficultyGroup.SelectedIndex != -1);
}

public void CreateWorld()
{
if (!FileLocations.WorldsFolder.FolderExists())
{
FileLocations.WorldsFolder.CreateFolder();
}
string text = ((worldNameField.Text == "") ? ((string)Loc.main.Default_World_Name) : worldNameField.Text);
text = PathUtility.MakeUsable(text, Loc.main.Default_World_Name);
text = PathUtility.AutoNameExisting(text, FileLocations.WorldsFolder);
WorldReference worldReference = new WorldReference(text);
SolarSystemReference solarSystem = new SolarSystemReference(solarSystemName);
Difficulty difficulty = DifficultyIndices[difficultyGroup.SelectedIndex];
WorldMode worldMode = new WorldMode(ModeIndices[modeGroup.SelectedIndex]);
worldMode.allowQuicksaves = worldMode.mode == WorldMode.Mode.Sandbox || quicksaves;
int num;
if (text.ToLower() == "test career")
{
num = 0;
if (num != 0)
{
worldMode.mode = WorldMode.Mode.Career;
}
}
else
{
num = 0;
}
WorldSettings settings = new WorldSettings(solarSystem, difficulty, worldMode, new WorldPlaytime(DateTime.Now.Ticks, 0.0), new SandboxSettings.Data());
worldReference.SaveWorldSettings(settings);
Close();
worldsMenu.OnWorldCreated(text);
if (num != 0)
{
Menu.read.Open(() => "Career mode is currently a very minimal prototype with only a few missions and unlocks\n\nIf this prototype gets positive feedback, we will add tons of missions, unlocks and polish it to perfection\n\n- Spaceflight Simulator developer team");
}
}

private void ApplyCreateWorldLayout()
{
if (menuHolder == null)
{
return;
}
// 窗口宽度适配画布（竖屏 1100 时不超出屏幕），内容随之居中
RectTransform win = transform as RectTransform;
if (win != null)
{
float canvasW = 1500f;
RectTransform canvasRt = transform.root as RectTransform;
if (canvasRt != null && canvasRt.rect.width > 10f)
{
canvasW = canvasRt.rect.width;
}
win.sizeDelta = new Vector2(canvasW - 80f, 1200f);
// 强制窗口对齐到画布（root）中心：用世界坐标差补偿父级偏移
RectTransform rootRt = transform.root as RectTransform;
RectTransform parentRt = win.parent as RectTransform;
if (rootRt != null && parentRt != null)
{
win.anchorMin = new Vector2(0.5f, 0.5f);
win.anchorMax = new Vector2(0.5f, 0.5f);
win.pivot = new Vector2(0.5f, 0.5f);
Vector3 ps = parentRt.lossyScale;
float sx = (ps.x != 0f) ? ps.x : 1f;
float sy = (ps.y != 0f) ? ps.y : 1f;
win.anchoredPosition = new Vector2((rootRt.position.x - parentRt.position.x) / sx, (rootRt.position.y - parentRt.position.y) / sy);
Debug.Log("[CW3] root位置=" + rootRt.position + " parent位置=" + parentRt.position + " 窗口AP=" + win.anchoredPosition);
}
}
Transform backWindows = menuHolder.transform.Find("BackWindows");
if (backWindows != null)
{
backWindows.gameObject.SetActive(false);
}
Transform title = menuHolder.transform.Find("World Title");
if (title != null)
{
title.gameObject.SetActive(false);
}
Transform rows = menuHolder.transform.Find("Rows");
if (rows == null)
{
return;
}
// 禁用 Rows 容器上的布局组件：VerticalLayoutGroup / SFS VerticalLayout / ContentSizeFitter
// 会在布局重排时覆盖我们设置的行位置（和世界列表元素被 VLG 重排是同一类问题）
UnityEngine.UI.VerticalLayoutGroup rowsVlg = rows.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
if (rowsVlg != null)
{
rowsVlg.enabled = false;
}
SFS.UI.VerticalLayout rowsSvl = rows.GetComponent<SFS.UI.VerticalLayout>();
if (rowsSvl != null)
{
rowsSvl.enabled = false;
}
UnityEngine.UI.ContentSizeFitter rowsCsf = rows.GetComponent<UnityEngine.UI.ContentSizeFitter>();
if (rowsCsf != null)
{
rowsCsf.enabled = false;
}
// 删除挑战模式的快速存档小弹窗（Toggles 面板），不再显示
if (togglesHolder != null)
{
togglesHolder.SetActive(false);
}
// 隐藏旧 Info 面板和分隔符
for (int i = rows.childCount - 1; i >= 0; i--)
{
Transform child = rows.GetChild(i);
if (child.name == "Info" || child.name == "-")
{
child.gameObject.SetActive(false);
}
}
// 四行从上往下：世界名称 / 扩展包 / 模式 / 难度（标签文字绑定 Loc，随语言切换）
TMPro.TMP_FontAsset mainFont = GetMainFont();
StyleCreateRow(rows.Find("Name"), 0, mainFont, () => (string)Loc.main.World_Name_Label);
StyleCreateRow(rows.Find("Solar System"), 1, mainFont, () => (string)Loc.main.Solar_System_Label);
StyleCreateRow(rows.Find("Mode"), 2, mainFont, () => (string)Loc.main.Mode_Label);
StyleCreateRow(rows.Find("Difficulty"), 3, mainFont, () => (string)Loc.main.Difficulty_Label);
// 诊断：打印每个标签的实际状态
Transform[] labelRows = new Transform[] { rows.Find("Name"), rows.Find("Solar System"), rows.Find("Mode"), rows.Find("Difficulty") };
for (int i = 0; i < labelRows.Length; i++)
{
Transform lbl = labelRows[i] != null ? labelRows[i].Find("Label") : null;
if (lbl == null)
{
continue;
}
RectTransform lr2 = lbl as RectTransform;
Debug.Log("[CWL] 标签#" + i + " 名=" + lbl.name + " rect=" + (lr2 != null ? lr2.rect.ToString() : "null") + " pos=" + (lr2 != null ? lr2.position.ToString() : "null"));
TMPro.TextMeshProUGUI[] tmps = lbl.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
for (int j = 0; j < tmps.Length; j++)
{
Debug.Log("[CWL]   TMP#" + j + " active=" + tmps[j].gameObject.activeSelf + " text='" + tmps[j].text + "' align=" + tmps[j].alignment + " rect=" + ((RectTransform)tmps[j].transform).rect.ToString() + " parent=" + tmps[j].transform.parent.name);
}
UnityEngine.UI.Text[] utes = lbl.GetComponentsInChildren<UnityEngine.UI.Text>(true);
for (int j = 0; j < utes.Length; j++)
{
Debug.Log("[CWL]   UIText#" + j + " active=" + utes[j].gameObject.activeSelf + " text='" + utes[j].text + "' align=" + utes[j].alignment + " parent=" + utes[j].transform.parent.name);
}
}
StartCoroutine(LogLabelsLate(labelRows));
// 模式/难度：保留旧按钮组，按钮排布居中
SetupSelectionRow(rows.Find("Mode"), modeGroup);
SetupSelectionRow(rows.Find("Difficulty"), difficultyGroup);
// 扩展包：方框显示当前选择，点击打开列表
SetupSolarSystemRow(rows.Find("Solar System"));
// 世界名称：输入框文字放大居中
SetupNameInput(rows.Find("Name"));
// 底部按钮：取消 / 创建
PositionBottomButtons(rows);
Debug.Log("[CW2] 窗口rect=" + ((transform as RectTransform) != null ? ((RectTransform)transform).rect.ToString() : "null") + " root=" + (transform.root != null ? transform.root.name : "null") + " rootRect=" + ((transform.root as RectTransform) != null ? ((RectTransform)transform.root).rect.ToString() : "null"));
Debug.Log("[CW2] Rows容器rect=" + ((rows as RectTransform) != null ? ((RectTransform)rows).rect.ToString() : "null") + " 位置=" + ((rows as RectTransform) != null ? ((RectTransform)rows).position.ToString() : "null"));
Debug.Log("[CW2] Name行rect=" + ((rows.Find("Name") as RectTransform) != null ? ((RectTransform)rows.Find("Name")).rect.ToString() : "null") + " 位置=" + ((rows.Find("Name") as RectTransform) != null ? ((RectTransform)rows.Find("Name")).position.ToString() : "null"));
Debug.Log("[CW2] Solar行rect=" + ((rows.Find("Solar System") as RectTransform) != null ? ((RectTransform)rows.Find("Solar System")).rect.ToString() : "null") + " 位置=" + ((rows.Find("Solar System") as RectTransform) != null ? ((RectTransform)rows.Find("Solar System")).position.ToString() : "null"));
}

private System.Collections.IEnumerator LogLabelsLate(Transform[] labelRows)
{
yield return new WaitForEndOfFrame();
yield return new WaitForEndOfFrame();
for (int i = 0; i < labelRows.Length; i++)
{
Transform lbl = labelRows[i] != null ? labelRows[i].Find("Label") : null;
if (lbl == null)
{
continue;
}
RectTransform lr2 = lbl as RectTransform;
Debug.Log("[CWL2] 标签#" + i + " rect=" + (lr2 != null ? lr2.rect.ToString() : "null") + " pos=" + (lr2 != null ? lr2.position.ToString() : "null"));
TMPro.TextMeshProUGUI[] tmps = lbl.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
for (int j = 0; j < tmps.Length; j++)
{
Debug.Log("[CWL2]   TMP#" + j + " active=" + tmps[j].gameObject.activeSelf + " text='" + tmps[j].text + "' align=" + tmps[j].alignment + " rect=" + ((RectTransform)tmps[j].transform).rect.ToString());
}
}
}

// 获取主字体（中英文统一）：优先从世界名称文字复制，其次用全局 CJK fallback
private TMPro.TMP_FontAsset GetMainFont()
{
if (worldsMenu != null && worldsMenu.worldElementPrefab != null && worldsMenu.worldElementPrefab.worldNameText != null)
{
TMPro.TMP_Text t = worldsMenu.worldElementPrefab.worldNameText.GetComponent<TMPro.TMP_Text>();
if (t != null && t.font != null)
{
return t.font;
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

// 给 root 下所有 TMP 文字统一字体（中英文一致）
private void ApplyMainFont(Transform root)
{
TMPro.TMP_FontAsset font = GetMainFont();
if (font == null || root == null)
{
return;
}
TMPro.TMP_Text[] texts = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
for (int i = 0; i < texts.Length; i++)
{
texts[i].font = font;
}
}

// 行的整体定位 + 标签（无底纯文字居中，绑定 Loc 随语言切换）+ 透明方框
private void StyleCreateRow(Transform row, int rowIndex, TMPro.TMP_FontAsset mainFont, Func<string> labelText)
{
if (row == null)
{
return;
}
RectTransform rowRect = row as RectTransform;
// 行宽适配父级（Rows 容器）宽度，竖屏下不超出边框
float rowW = 1400f;
float containerH = 1115f;
RectTransform parentRt = row.parent as RectTransform;
if (parentRt != null && parentRt.rect.width > 10f)
{
rowW = parentRt.rect.width;
containerH = parentRt.rect.height;
}
if (rowRect != null)
{
rowRect.anchorMin = new Vector2(0.5f, 1f);
rowRect.anchorMax = new Vector2(0.5f, 1f);
rowRect.pivot = new Vector2(0.5f, 1f);
// 上下顶满、均匀排布：4 行均分可用高度（顶部留 10，底部留按钮区）
float rowH = 175f;
float bottomZone = 170f;
float usable = containerH - bottomZone;
float gap = (usable - 4f * rowH) / 3f;
if (gap < 20f)
{
gap = 20f;
}
float y = -10f - rowIndex * (rowH + gap);
rowRect.anchoredPosition = new Vector2(0f, y);
rowRect.sizeDelta = new Vector2(rowW, rowH);
}
// 标签：重新布局——销毁旧标签（连带 ContainerElement 等干扰组件），新建纯文字居中标签
Transform label = row.Find("Label");
if (label != null)
{
UnityEngine.Object.Destroy(label.gameObject);
label = null;
}
if (label == null)
{
GameObject labelGo = new GameObject("Label", typeof(RectTransform));
labelGo.transform.SetParent(row, false);
RectTransform lr = labelGo.GetComponent<RectTransform>();
lr.anchorMin = new Vector2(0.5f, 1f);
lr.anchorMax = new Vector2(0.5f, 1f);
lr.pivot = new Vector2(0.5f, 1f);
lr.anchoredPosition = new Vector2(0f, -6f);
lr.sizeDelta = new Vector2(rowW, 55f);
TMPro.TextMeshProUGUI tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
if (mainFont != null)
{
tmp.font = mainFont;
}
tmp.text = labelText != null ? labelText() : "";
tmp.alignment = TMPro.TextAlignmentOptions.Center;
tmp.fontSize = 42f;
tmp.color = Color.white;
tmp.raycastTarget = false;
// 记录标签，语言切换时更新文字
if (rowIndex == 0)
{
nameLabel = tmp;
nameLabelText = labelText;
}
else if (rowIndex == 1)
{
solarLabel = tmp;
solarLabelText = labelText;
}
else if (rowIndex == 2)
{
modeLabel = tmp;
modeLabelText = labelText;
}
else if (rowIndex == 3)
{
diffLabel = tmp;
diffLabelText = labelText;
}
}
// 透明圆角方框 → 深色半透明底（与其他按钮同款）
Transform box = row.Find("Text Box");
if (box != null)
{
RectTransform br = box as RectTransform;
if (br != null)
{
br.anchorMin = new Vector2(0f, 1f);
br.anchorMax = new Vector2(1f, 1f);
br.pivot = new Vector2(0.5f, 1f);
br.anchoredPosition = new Vector2(0f, -68f);
br.sizeDelta = new Vector2(0f, 100f);
}
MakeDarkBox(box);
}
}

// 圆角方形深色半透明底（与主菜单按钮同款），隐藏内部旧背景
private static void MakeDarkBox(Transform box)
{
if (box == null)
{
return;
}
Image img = box.GetComponent<Image>();
if (img == null)
{
img = box.gameObject.AddComponent<Image>();
}
img.sprite = LegacyUITheme.RoundedSprite;
img.type = Image.Type.Sliced;
img.fillCenter = true;
img.color = LegacyUITheme.ButtonBaseColor;
img.raycastTarget = true;
Transform back = box.Find("BackTranslucent");
if (back != null)
{
back.gameObject.SetActive(false);
}
Transform over = box.Find("BackTranslucentOver");
if (over != null)
{
over.gameObject.SetActive(false);
}
}

// 模式/难度：保留旧按钮组（Sandbox/Challenge），按钮排布居中、深色底、白色选中边框
private void SetupSelectionRow(Transform row, ButtonGroup group)
{
if (row == null || group == null)
{
return;
}
Transform groupT = row.Find("ButtonGroup");
if (groupT == null)
{
return;
}
// 行宽适配父级
float rowW = 1400f;
RectTransform rowRt = row as RectTransform;
if (rowRt != null && rowRt.rect.width > 10f)
{
rowW = rowRt.rect.width;
}
// ButtonGroup 撑满行宽并居中，按钮再相对组居中排布
RectTransform gr = groupT as RectTransform;
if (gr != null)
{
gr.anchorMin = new Vector2(0.5f, 1f);
gr.anchorMax = new Vector2(0.5f, 1f);
gr.pivot = new Vector2(0.5f, 1f);
gr.anchoredPosition = new Vector2(0f, -118f);
gr.sizeDelta = new Vector2(rowW, 100f);
}
int count = groupT.childCount;
for (int i = 0; i < count; i++)
{
Transform btn = groupT.GetChild(i);
RectTransform br = btn as RectTransform;
if (br != null)
{
br.anchorMin = new Vector2(0.5f, 0.5f);
br.anchorMax = new Vector2(0.5f, 0.5f);
br.pivot = new Vector2(0.5f, 0.5f);
float btnW = rowW / count - 30f;
float x = (i - (count - 1) * 0.5f) * (btnW + 30f);
br.anchoredPosition = new Vector2(x, 0f);
br.sizeDelta = new Vector2(btnW, 90f);
}
StripNewElementComponents(btn.gameObject);
LegacyUITheme.ApplyButton(btn.gameObject);
CenterText(btn);
}
// 确保有选中项（默认第一项）
if (group.SelectedIndex < 0 && group.buttons != null && group.buttons.Count > 0)
{
group.SetSelectedIndex(0);
}
// 统一字体
ApplyMainFont(row);
}

// 清除对象上的 ContainerElement（会重置矩形）和 ScaleTextToFit（文字缩放干扰布局）
private static void StripNewElementComponents(GameObject go)
{
if (go == null)
{
return;
}
ContainerElement ce = go.GetComponent<ContainerElement>();
if (ce != null)
{
ce.enabled = false;
UnityEngine.Object.Destroy(ce);
}
ScaleTextToFit[] fits = go.GetComponentsInChildren<ScaleTextToFit>(true);
for (int i = 0; i < fits.Length; i++)
{
fits[i].enabled = false;
UnityEngine.Object.Destroy(fits[i]);
}
}

// 文字水平居中（不改字号），并把文字矩形拉伸到整个 root
private static void CenterText(Transform root)
{
if (root == null)
{
return;
}
TMPro.TextMeshProUGUI tmp = root.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
if (tmp != null)
{
tmp.alignment = TMPro.TextAlignmentOptions.Center;
RectTransform textRt = tmp.transform as RectTransform;
if (textRt != null)
{
textRt.anchorMin = Vector2.zero;
textRt.anchorMax = Vector2.one;
textRt.offsetMin = Vector2.zero;
textRt.offsetMax = Vector2.zero;
}
return;
}
UnityEngine.UI.Text ut = root.GetComponentInChildren<UnityEngine.UI.Text>(true);
if (ut != null)
{
ut.alignment = TextAnchor.MiddleCenter;
RectTransform textRt = ut.transform as RectTransform;
if (textRt != null)
{
textRt.anchorMin = Vector2.zero;
textRt.anchorMax = Vector2.one;
textRt.offsetMin = Vector2.zero;
textRt.offsetMax = Vector2.zero;
}
}
}

// 扩展包：旧选择按钮铺满框（深色底 + 文字居中），点击仍打开列表
private void SetupSolarSystemRow(Transform row)
{
if (row == null)
{
return;
}
Transform box = row.Find("Text Box");
Transform btn = row.Find("Button");
if (btn != null)
{
RectTransform br = btn as RectTransform;
if (br != null)
{
br.anchorMin = new Vector2(0f, 1f);
br.anchorMax = new Vector2(1f, 1f);
br.pivot = new Vector2(0.5f, 0.5f);
br.anchoredPosition = new Vector2(0f, -118f);
br.sizeDelta = new Vector2(0f, 100f);
}
StripNewElementComponents(btn.gameObject);
LegacyUITheme.ApplyButton(btn.gameObject);
CenterText(btn);
}
if (box != null)
{
MakeDarkBox(box);
}
// 统一字体
ApplyMainFont(row);
}

// 世界名称：输入框铺满框、文字居中
private void SetupNameInput(Transform row)
{
if (row == null || worldNameField == null)
{
return;
}
TMPro.TMP_InputField input = worldNameField.GetComponentInChildren<TMPro.TMP_InputField>(true);
if (input != null)
{
RectTransform inputRt = input.transform as RectTransform;
if (inputRt != null)
{
inputRt.anchorMin = Vector2.zero;
inputRt.anchorMax = Vector2.one;
inputRt.offsetMin = Vector2.zero;
inputRt.offsetMax = Vector2.zero;
}
TMPro.TextMeshProUGUI text = input.textComponent as TMPro.TextMeshProUGUI;
if (text != null)
{
text.alignment = TMPro.TextAlignmentOptions.Center;
text.fontSize = 42f;
text.enableAutoSizing = false;
}
TMPro.TextMeshProUGUI placeholder = input.placeholder as TMPro.TextMeshProUGUI;
if (placeholder != null)
{
placeholder.alignment = TMPro.TextAlignmentOptions.Center;
placeholder.fontSize = 42f;
}
// 统一字体
ApplyMainFont(row);
}
}

// 底部按钮：取消 / 创建 等大、居中并排、靠下
private void PositionBottomButtons(Transform rows)
{
if (rows == null)
{
return;
}
// 底部按钮在 'Bottom Buttons' 容器下（不是 Rows 的直接子级）
Transform bottomBtns = menuHolder.transform.Find("Bottom Buttons");
if (bottomBtns != null)
{
// 禁用容器上的布局组件，防止覆盖按钮位置
UnityEngine.UI.HorizontalLayoutGroup blg = bottomBtns.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
if (blg != null)
{
blg.enabled = false;
}
UnityEngine.UI.VerticalLayoutGroup bvlg = bottomBtns.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
if (bvlg != null)
{
bvlg.enabled = false;
}
}
float rowW = 1400f;
RectTransform rowsRt = rows as RectTransform;
if (rowsRt != null && rowsRt.rect.width > 10f)
{
rowW = rowsRt.rect.width;
}
float btnW = rowW * 0.32f;
float gap = 30f;
float half = btnW * 0.5f + gap * 0.5f;
Transform cancel = null;
if (bottomBtns != null)
{
cancel = bottomBtns.Find("Cancel");
}
if (cancel == null)
{
cancel = rows.Find("Cancel");
}
if (cancel == null)
{
cancel = menuHolder.transform.Find("Cancel");
}
if (cancel != null)
{
RectTransform cr = cancel as RectTransform;
if (cr != null)
{
cr.anchorMin = new Vector2(0.5f, 0f);
cr.anchorMax = new Vector2(0.5f, 0f);
cr.pivot = new Vector2(0.5f, 0.5f);
cr.anchoredPosition = new Vector2(-half, 40f);
cr.sizeDelta = new Vector2(btnW, 85f);
}
LegacyUITheme.ApplyButton(cancel.gameObject);
CenterText(cancel);
}
if (createWorldButton != null)
{
RectTransform bt = createWorldButton.transform as RectTransform;
if (bt != null)
{
bt.anchorMin = new Vector2(0.5f, 0f);
bt.anchorMax = new Vector2(0.5f, 0f);
bt.pivot = new Vector2(0.5f, 0.5f);
bt.anchoredPosition = new Vector2(half, 40f);
bt.sizeDelta = new Vector2(btnW, 85f);
}
LegacyUITheme.ApplyButton(createWorldButton.gameObject);
CenterText(createWorldButton.transform);
}
}
}
}
