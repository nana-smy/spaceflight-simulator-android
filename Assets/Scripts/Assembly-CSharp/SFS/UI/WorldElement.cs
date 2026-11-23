using System;
using System.Text;
using SFS.Translations;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.UI
{
public class WorldElement : MonoBehaviour
{
public TextAdapter worldNameText;

public TextAdapter infoText;

public ReorderingModule reorderingModule;

public RadioButton radioButton;

public Button button;

public GameObject buttonHolder;

public Button resumeGameButton;

public Button renameButton;

public WorldReference World { get; private set; }

public string Name => World.worldName;

public void Setup(WorldReference world)
{
World = world;
SetExpanded(false);
worldNameText.Text = World.worldName;
WorldSettings worldSettings = world.LoadWorldSettings();
StringBuilder stringBuilder = new StringBuilder();
stringBuilder.Append(Loc.main.World_Mode_Name.Inject(worldSettings.mode.GetModeName(), "value"));
stringBuilder.Append("\n" + Loc.main.World_Difficulty_Name.Inject(worldSettings.difficulty.GetName(), "value"));
if (!worldSettings.mode.allowQuicksaves)
{
stringBuilder.Append("\n" + Loc.main.Allow_Quicksaves_Name.Inject(Loc.main.Off, "value"));
}
stringBuilder.Append("<size=25>\n\n</size>");
stringBuilder.AppendLine((DateTime.Now - new DateTime((worldSettings.playtime.lastPlayedTime_Ticks == 0L) ? DateTime.Now.Ticks : worldSettings.playtime.lastPlayedTime_Ticks)).ToLastPlayedString());
stringBuilder.Append(TimeSpan.FromSeconds((int)worldSettings.playtime.totalPlayTime_Seconds).ToTimePlayedString());
infoText.Text = stringBuilder.ToString();
SetExpanded(false);
HorizontalLayout[] componentsInChildren = GetComponentsInChildren<HorizontalLayout>();
for (int i = 0; i < componentsInChildren.Length; i++)
{
componentsInChildren[i].UpdateHierarchy();
}
}

public void SetExpanded(bool expanded)
{
// 元素自带 ContentSizeFitter 会按子元素 preferred 高度重算元素高度，
// 与这里手动设置的高度冲突，直接禁用，让 sizeDelta 说了算
UnityEngine.UI.ContentSizeFitter fitter = GetComponent<UnityEngine.UI.ContentSizeFitter>();
if (fitter != null)
{
fitter.enabled = false;
}
// 元素自带 VerticalLayoutGroup 会按子节点 preferred 高度(未乘缩放)拉伸矩形：
// 信息文本是 TMP，preferred ≈ 384，但实际按 scale 0.6 渲染只有约 230 高，
// 矩形被拉大导致底部一大片空白。直接禁用布局组，由 LayoutTexts 按渲染尺寸精确布局。
UnityEngine.UI.VerticalLayoutGroup vlg = GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
if (vlg != null)
{
vlg.enabled = false;
}
// 渲染高度 = TMP preferred(内容高度) × scale.y
float nameH = 36f;
if (worldNameText != null)
{
RectTransform nr = worldNameText.transform as RectTransform;
nameH = UnityEngine.UI.LayoutUtility.GetPreferredHeight(nr) * nr.localScale.y;
}
float infoH = 165f;
if (infoText != null)
{
RectTransform ir = infoText.transform as RectTransform;
infoH = UnityEngine.UI.LayoutUtility.GetPreferredHeight(ir) * ir.localScale.y;
}
// 信息底部距元素顶部的距离
float infoBottom = 12f + nameH + 10f + infoH;
// 折叠：信息下方留 15px 底边距即可；
// 展开：信息下方 30px 间距 + 按钮区（底部锚定15 + 按钮区160 + 顶部留白）≈ 信息底部+205
float height = expanded ? Mathf.Max(480f, infoBottom + 205f) : Mathf.Max(230f, infoBottom + 15f);
RectTransform rect = transform as RectTransform;
if (rect != null)
{
// 宽度适配父级（世界列表内容）宽度，竖屏下不会超出边框
float width = 1350f;
RectTransform parent = transform.parent as RectTransform;
if (parent != null && parent.rect.width > 10f)
{
width = parent.rect.width;
}
rect.sizeDelta = new Vector2(width, height);
}
// worldElementHolder 上有 VerticalLayoutGroup，会覆盖子元素高度。
// 用 LayoutElement 的 preferredHeight 强制布局系统采用我们设置的高度。
UnityEngine.UI.LayoutElement layout = GetComponent<UnityEngine.UI.LayoutElement>();
if (layout == null)
{
layout = gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
}
layout.preferredHeight = height;
float prefWidth = 1350f;
RectTransform parentRect = transform.parent as RectTransform;
if (parentRect != null && parentRect.rect.width > 10f)
{
prefWidth = parentRect.rect.width;
}
layout.preferredWidth = prefWidth;
// 信息（模式/难度/游玩时间）始终可见；展开时在下方显示动作按钮
if (infoText != null)
{
infoText.gameObject.SetActive(true);
}
// 布局重排：世界名顶部，信息在其下方（收起/展开都显示）
LayoutTexts(expanded, nameH, infoH);
}

private void LayoutTexts(bool expanded, float nameH, float infoH)
{
if (worldNameText != null)
{
RectTransform nameRect = worldNameText.transform as RectTransform;
if (nameRect != null)
{
nameRect.anchorMin = new Vector2(0f, 1f);
nameRect.anchorMax = new Vector2(1f, 1f);
nameRect.pivot = new Vector2(0.5f, 1f);
nameRect.anchoredPosition = new Vector2(0f, -12f);
nameRect.sizeDelta = new Vector2(0f, nameH);
}
}
if (infoText != null)
{
RectTransform infoRect = infoText.transform as RectTransform;
if (infoRect != null)
{
// 名字下方 10px 处开始，高度 = 渲染高度(内容刚好填满，无空白)
infoRect.anchorMin = new Vector2(0f, 1f);
infoRect.anchorMax = new Vector2(1f, 1f);
infoRect.pivot = new Vector2(0.5f, 1f);
infoRect.anchoredPosition = new Vector2(0f, -12f - nameH - 10f);
infoRect.sizeDelta = new Vector2(0f, infoH);
}
}
}
}
}
