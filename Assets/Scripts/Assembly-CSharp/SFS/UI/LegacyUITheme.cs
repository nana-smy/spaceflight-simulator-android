using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI
{
    /// <summary>
    /// 旧版主菜单 UI（ButtonBuilder/TextBuilder/ElementGenerator 体系）的灰色半透明扁平皮肤。
    /// </summary>
    public static class LegacyUITheme
    {
        public static bool Enabled = true;
        public static float CornerRadius = 10f;

        public static readonly Color C_BG        = new Color(0.18f, 0.19f, 0.21f, 1f);
        public static readonly Color C_VAR_BG    = new Color(0.25f, 0.26f, 0.28f, 1f);
        public static readonly Color C_TOGGLE_BG = new Color(0.10f, 0.11f, 0.13f, 1f);
        public static readonly Color C_HOVER_BG  = new Color(0.85f, 0.87f, 0.90f, 0.55f);

        private static Sprite _buttonSprite;
        private static Texture2D _buttonTexture;

        private static Sprite ButtonSprite
        {
            get
            {
                if (_buttonSprite == null)
                {
                    _buttonTexture = CreateRoundedTexture(CornerRadius);
                    _buttonSprite = CreateSlicedSprite(_buttonTexture, CornerRadius);
                }
                return _buttonSprite;
            }
        }

        /// <summary>圆角九宫格精灵（白色，可 Sliced 拉伸；fillCenter=false 时只渲染圆角边框）</summary>
        public static Sprite RoundedSprite => ButtonSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoApplyOnSceneLoad()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                (UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => ApplyAll();
            ApplyAll();
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

        public static void ConfigureSelectionIndicator(GameObject indicator)
        {
            if (indicator == null) return;

            Image image = indicator.GetComponent<Image>();
            if (image == null) image = indicator.AddComponent<Image>();
            image.sprite = ButtonSprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.color = Color.white;
            image.raycastTarget = false;

            RectTransform rect = indicator.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }
        }

        public static void ApplyButton(GameObject root)
        {
            if (!Enabled || root == null) return;

            Transform back = root.transform.Find("BackTranslucent");
            if (back != null) FlattenButton(back.GetComponent<Image>());
        }

        // 祖先链里是否有 UnityEngine.UI.Slider（判断是否在滑块内部）
        private static bool IsInsideSlider(Transform t)
        {
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

            // 设置菜单的音量/UI 滑块内部也用了 BackTranslucent/ButtonBack 这套按钮模板命名，
            // 但那里的 ButtonBack 是白色填充条（滑块的白条），不能隐藏也不能压平成深灰。
            // 判断：BackTranslucent 祖先里有 Slider 就说明是滑块内部，整体跳过。
            // 注意：不能直接用 GetComponentInParent（会跳过非激活祖先——设置菜单关闭时就是非激活的），
            // 必须手动逐节点 GetComponent 遍历。
            if (IsInsideSlider(backImage.transform)) return;

            ApplyImage(backImage, ButtonSprite, ButtonBaseColor);

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
                    overImage.sprite = ButtonSprite;
                    overImage.type = Image.Type.Sliced;
                    overImage.color = new Color(ButtonHoverColor.r, ButtonHoverColor.g, ButtonHoverColor.b, 0f);
                    if (overImage is global::TranslucentImage.TranslucentImage overTranslucent)
                    {
                        overTranslucent.spriteBlending = 1f;
                    }
                }
            }

            ButtonPC buttonPC = buttonRoot.GetComponent<ButtonPC>();
            if (buttonPC != null)
            {
                buttonPC.Color = Color.white;
                buttonPC.OverColor = Color.white;
                buttonPC.ButtonOverIcon = null;
                buttonPC.ButtonBackOverAlpha = ButtonHoverColor.a;
                buttonPC.SelectionOutline = EnsureSelectionOutline(buttonRoot);
            }
        }

        /// <summary>
        /// 把当前加载的场景里所有静态 UI 背景统一成灰色半透明扁平样式。
        /// </summary>
        public static void ApplyAll()
        {
            if (!Enabled) return;

            Image[] images = UnityEngine.Object.FindObjectsOfType<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null) continue;

                if (image.name == "BackTranslucent")
                {
                    FlattenButton(image);
                }
                else if (image.name == "BackTranslucentOver")
                {
                    // 滑块内部（音量/UI 缩放等）保留原样
                    if (!IsInsideSlider(image.transform))
                    {
                        ApplyImage(image, ButtonSprite, ButtonBaseColor);
                    }
                }
            }

            global::TranslucentImage.TranslucentImage[] translucentImages =
                UnityEngine.Object.FindObjectsOfType<global::TranslucentImage.TranslucentImage>(true);
            for (int i = 0; i < translucentImages.Length; i++)
            {
                global::TranslucentImage.TranslucentImage image = translucentImages[i];
                if (image == null) continue;

                if (image.name == "BackTranslucent")
                {
                    FlattenButton(image);
                }
                else if (image.name == "BackTranslucentOver")
                {
                    // 滑块内部（音量/UI 缩放等）保留原样
                    if (!IsInsideSlider(image.transform))
                    {
                        ApplyImage(image, ButtonSprite, ButtonBaseColor);
                    }
                }
            }
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
                image.sprite = ButtonSprite;
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
