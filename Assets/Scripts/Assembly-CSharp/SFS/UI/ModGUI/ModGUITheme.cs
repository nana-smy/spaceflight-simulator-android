using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI.ModGUI
{
    /// <summary>
    /// 统一窗口皮肤：灰色半透明 + 更方一点的圆角矩形。
    /// 运行时生成 9-slice 圆角 Sprite，替换 ModGUI 控件的背景。
    /// </summary>
    public static class ModGUITheme
    {
        public static bool Enabled = true;
        public static float CornerRadius = 10f;

        public static readonly Color C_BG        = new Color(0.18f, 0.19f, 0.21f, 1f);
        public static readonly Color C_TITLE_BG  = new Color(0.12f, 0.13f, 0.15f, 1f);
        public static readonly Color C_VAR_BG    = new Color(0.25f, 0.26f, 0.28f, 1f);
        public static readonly Color C_TOGGLE_BG = new Color(0.10f, 0.11f, 0.13f, 1f);
        public static readonly Color C_HOVER_BG  = new Color(0.85f, 0.87f, 0.90f, 0.55f);
        public static readonly Color C_SLIDER_FILL = new Color(0.85f, 0.85f, 0.85f, 1f);
        public static readonly Color C_LINE      = new Color(0.55f, 0.55f, 0.55f, 1f);

        private static Sprite _panelSprite;
        private static Sprite _controlSprite;
        private static Texture2D _panelTexture;
        private static Texture2D _controlTexture;

        private static Sprite PanelSprite
        {
            get
            {
                if (_panelSprite == null)
                {
                    _panelTexture = CreateRoundedTexture(CornerRadius);
                    _panelSprite = CreateSlicedSprite(_panelTexture, CornerRadius);
                }
                return _panelSprite;
            }
        }

        private static Sprite ControlSprite
        {
            get
            {
                if (_controlSprite == null)
                {
                    _controlTexture = CreateRoundedTexture(CornerRadius);
                    _controlSprite = CreateSlicedSprite(_controlTexture, CornerRadius);
                }
                return _controlSprite;
            }
        }

        public static bool IsLightBackground()
        {
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return scene.Contains("Build") || scene.Contains("World");
        }

        // 正常状态背景：深灰黑半透明（半透明灰黑按钮）；浅色场景更不透明。
        public static Color ButtonBaseColor => IsLightBackground()
            ? new Color(0.10f, 0.11f, 0.13f, 0.55f)
            : new Color(0.10f, 0.11f, 0.13f, 0.45f);

        // 高亮：中深灰，适度提高不透明度但不泛白。
        public static Color ButtonHoverColor => IsLightBackground()
            ? new Color(0.28f, 0.30f, 0.33f, 0.60f)
            : new Color(0.28f, 0.30f, 0.33f, 0.50f);

        public static void ApplyWindow(Window window, float opacity = 1f)
        {
            if (!Enabled || window == null || window.gameObject == null) return;

            Color color = new Color(C_BG.r, C_BG.g, C_BG.b, C_BG.a * opacity);
            window.WindowColor = color;
            window.TitleColor = Color.white;

            Transform back = window.gameObject.transform.Find("Back (Game)");
            if (back != null) ApplyImage(back.GetComponent<Image>(), PanelSprite, color);

            Transform backInGame = window.gameObject.transform.Find("Back (InGame)");
            if (backInGame != null) backInGame.gameObject.SetActive(false);
        }

        public static void ApplyButton(Button button)
        {
            if (!Enabled || button == null || button.gameObject == null) return;
            button.TextColor = Color.white;

            Transform back = button.gameObject.transform.Find("BackTranslucent");
            if (back != null) FlattenButton(back.GetComponent<Image>());
        }

        private static bool IsInsideSlider(Transform t)
        {
            // 注意：本命名空间里 Slider 是 ModGUI 包装类，必须用完全限定名 UnityEngine.UI.Slider
            while (t != null)
            {
                if (t.GetComponent<UnityEngine.UI.Slider>() != null) return true;
                t = t.parent;
            }
            return false;
        }

        private static void FlattenButton(Image backImage)
        {
            if (backImage == null) return;

            // 滑块内部（音量/UI 缩放等）的 ButtonBack 是白色填充条，不能隐藏/压平
            if (IsInsideSlider(backImage.transform)) return;

            ApplyImage(backImage, ControlSprite, ButtonBaseColor);

            Transform buttonRoot = backImage.transform.parent;
            if (buttonRoot == null) return;

            string[] hideNames = { "ButtonBack", "BackOverTint" };
            for (int i = 0; i < hideNames.Length; i++)
            {
                Transform layer = buttonRoot.Find(hideNames[i]);
                if (layer != null) layer.gameObject.SetActive(false);
            }

            Transform over = buttonRoot.Find("ButtonBackOver");
            if (over != null)
            {
                RectTransform overRect = over as RectTransform;
                if (overRect != null)
                {
                    overRect.anchorMin = Vector2.zero;
                    overRect.anchorMax = Vector2.one;
                    overRect.offsetMin = Vector2.zero;
                    overRect.offsetMax = Vector2.zero;
                    overRect.sizeDelta = Vector2.zero;
                }

                Image overImage = over.GetComponent<Image>();
                if (overImage != null)
                {
                    overImage.sprite = ControlSprite;
                    overImage.type = Image.Type.Sliced;
                    overImage.color = new Color(ButtonHoverColor.r, ButtonHoverColor.g, ButtonHoverColor.b, 0f);
                    if (overImage is global::TranslucentImage.TranslucentImage overTranslucent)
                    {
                        overTranslucent.spriteBlending = 1f;
                    }
                }
            }

            SFS.UI.ButtonPC buttonPC = buttonRoot.GetComponent<SFS.UI.ButtonPC>();
            if (buttonPC != null)
            {
                buttonPC.Color = Color.white;
                buttonPC.OverColor = Color.white;
                buttonPC.ButtonOverIcon = null;
                buttonPC.ButtonBackOverAlpha = ButtonHoverColor.a;
                buttonPC.SelectionOutline = EnsureSelectionOutline(buttonRoot);
            }
        }

        public static void ApplyLabel(Label label)
        {
            if (!Enabled || label == null || label.gameObject == null) return;
            label.Color = Color.white;
        }

        public static void ApplyBox(Box box, float opacity = 1f)
        {
            if (!Enabled || box == null || box.gameObject == null) return;

            Color color = new Color(C_VAR_BG.r, C_VAR_BG.g, C_VAR_BG.b, C_VAR_BG.a * opacity);
            box.Color = color;

            Transform back = box.gameObject.transform.Find("Back (Game)");
            if (back != null) ApplyImage(back.GetComponent<Image>(), PanelSprite, color);
        }

        public static void ApplyTextInput(TextInput input)
        {
            if (!Enabled || input == null || input.gameObject == null) return;
            input.FieldColor = C_TOGGLE_BG;

            Transform back = input.gameObject.transform.Find("Text Box/BackTranslucent");
            if (back != null) ApplyImage(back.GetComponent<Image>(), ControlSprite, C_TOGGLE_BG);
        }

        private static void ApplyImage(Image image, Sprite sprite, Color color)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;

            if (image is global::TranslucentImage.TranslucentImage translucentImage)
            {
                translucentImage.spriteBlending = 1f;
            }
        }

        private static Image EnsureSelectionOutline(Transform buttonRoot)
        {
            Transform outline = buttonRoot.Find("Selection Outline");
            if (outline == null)
            {
                var outlineGo = new GameObject("Selection Outline", typeof(RectTransform), typeof(Image));
                outlineGo.transform.SetParent(buttonRoot, false);
                outline = outlineGo.transform;
            }

            Image image = outline.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = ControlSprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = false;
                image.color = Color.white;
                image.raycastTarget = false;
            }

            RectTransform rect = outline as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            outline.gameObject.SetActive(false);
            return image;
        }

        private static Texture2D CreateRoundedTexture(float radius)
        {
            const int size = 64;
            int r = Mathf.Clamp(Mathf.RoundToInt(radius), 1, size / 2 - 1);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 1f;

                    if (x < r && y < r)
                    {
                        float dx = r - x - 0.5f;
                        float dy = r - y - 0.5f;
                        if (dx * dx + dy * dy > r * r) alpha = 0f;
                    }
                    else if (x >= size - r && y < r)
                    {
                        float dx = x - (size - r) + 0.5f;
                        float dy = r - y - 0.5f;
                        if (dx * dx + dy * dy > r * r) alpha = 0f;
                    }
                    else if (x < r && y >= size - r)
                    {
                        float dx = r - x - 0.5f;
                        float dy = y - (size - r) + 0.5f;
                        if (dx * dx + dy * dy > r * r) alpha = 0f;
                    }
                    else if (x >= size - r && y >= size - r)
                    {
                        float dx = x - (size - r) + 0.5f;
                        float dy = y - (size - r) + 0.5f;
                        if (dx * dx + dy * dy > r * r) alpha = 0f;
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        private static Sprite CreateSlicedSprite(Texture2D texture, float radius)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(radius), 1, texture.width / 2 - 1);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(r, r, r, r));
        }
    }
}
