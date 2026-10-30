using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public const string VolumeKey = "MasterVolume";

    [SerializeField] private string gameTitle = "WHOLE CLASS MAD";
    [SerializeField] private string gameScene = "kelas";
    [Tooltip("Optional. Empty = copy the font from the existing text in the scene.")]
    [SerializeField] private TMP_FontAsset font;

    private static readonly string[] Credits =
    {
        "Fabio Mana'ahil Gani Akbar",
        "Davin Varian Ikhbar",
        "Muhammad Atho'ur Rohman",
        "Rayhan Naufal Sefriko",
    };

    private static readonly Color Ink = new Color(0.10f, 0.10f, 0.13f, 1f);
    private static readonly Color Paper = new Color(0.97f, 0.97f, 0.97f, 1f);
    private static readonly Color ButtonColor = new Color(0.16f, 0.17f, 0.21f, 1f);
    private static readonly Color ButtonHover = new Color(0.30f, 0.32f, 0.40f, 1f);

    private GameObject settingsPanel;
    private Transform canvasRoot;
    private TextMeshProUGUI volumeValueText;
    private Color textColor = Ink;

    // every scene that starts later uses the saved volume
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedVolume()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        CopyFontFromScene();
        EnsureEventSystem();
        BuildMenu();
        BuildSettings();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) CloseSettings();
            return;
        }

        // Space still starts the game, like before
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Play();
        }
    }

    // ------------------------------------------------------------------ buttons

    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameScene);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OpenSettings()
    {
        ClearSelection();
        settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        ClearSelection();
        settingsPanel.SetActive(false);
    }

    private static void ClearSelection()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // ------------------------------------------------------------------ building the screens

    private void CopyFontFromScene()
    {
        TMP_Text existing = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        if (existing == null) return;

        if (font == null) font = existing.font;
        textColor = existing.color.a > 0.5f ? existing.color : Ink;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildMenu()
    {
        GameObject canvasGo = new GameObject("MenuCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        canvasRoot = canvasGo.transform;

        MakeText(canvasRoot, gameTitle, 130f, new Vector2(0.5f, 0.62f), Vector2.zero,
                 new Vector2(1800f, 200f), textColor);

        MakeButton(canvasRoot, "SETTINGS", new Vector2(0.5f, 0.36f), Vector2.zero,
                   new Vector2(460f, 95f), OpenSettings);
    }

    private void BuildSettings()
    {
        // a full-screen panel that covers the menu
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(canvasRoot, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = Paper;
        settingsPanel = panel;
        Transform p = panel.transform;

        Vector2 mid = new Vector2(0.5f, 0.5f);

        MakeText(p, "SETTINGS", 80f, mid, new Vector2(0f, 390f), new Vector2(1400f, 120f), textColor);

        // volume
        MakeText(p, "VOLUME", 44f, mid, new Vector2(-420f, 250f), new Vector2(500f, 70f), textColor,
                 TextAlignmentOptions.MidlineLeft);
        volumeValueText = MakeText(p, "100%", 44f, mid, new Vector2(420f, 250f), new Vector2(300f, 70f),
                                   textColor, TextAlignmentOptions.MidlineRight);
        Slider slider = MakeSlider(p, mid, new Vector2(0f, 175f), new Vector2(1000f, 50f));
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, 1f));
        slider.onValueChanged.AddListener(SetVolume);
        SetVolume(slider.value);

        // credits
        MakeText(p, "CREDITS", 52f, mid, new Vector2(0f, 20f), new Vector2(1400f, 80f), textColor);
        MakeText(p, "Game Engineers", 34f, mid, new Vector2(0f, -40f), new Vector2(1400f, 60f),
                 new Color(textColor.r, textColor.g, textColor.b, 0.7f));

        for (int i = 0; i < Credits.Length; i++)
        {
            MakeText(p, Credits[i], 38f, mid, new Vector2(0f, -110f - i * 62f), new Vector2(1400f, 60f), textColor);
        }

        MakeButton(p, "BACK", mid, new Vector2(0f, -420f), new Vector2(360f, 90f), CloseSettings);

        panel.SetActive(false);
    }

    // ------------------------------------------------------------------ small UI helpers

    private RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    private TextMeshProUGUI MakeText(Transform parent, string text, float size, Vector2 anchor, Vector2 position,
                                     Vector2 box, Color color,
                                     TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        RectTransform rt = MakeRect("Text", parent, anchor, position, box);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void MakeButton(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size,
                            UnityAction onClick)
    {
        RectTransform rt = MakeRect(label + " Button", parent, anchor, position, size);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = Color.white;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = Ink;
        colors.selectedColor = ButtonColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform textRect = MakeRect("Label", rt, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 44f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private Slider MakeSlider(Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform root = MakeRect("Volume Slider", parent, anchor, position, size);

        // track
        RectTransform bg = MakeRect("Background", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        bg.anchorMin = new Vector2(0f, 0.3f);
        bg.anchorMax = new Vector2(1f, 0.7f);
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = new Color(0.80f, 0.80f, 0.83f, 1f);

        // filled part
        RectTransform fillArea = MakeRect("Fill Area", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        fillArea.anchorMin = new Vector2(0f, 0.3f);
        fillArea.anchorMax = new Vector2(1f, 0.7f);
        fillArea.offsetMin = new Vector2(0f, 0f);
        fillArea.offsetMax = new Vector2(-15f, 0f);

        RectTransform fill = MakeRect("Fill", fillArea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(15f, 0f));
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.gameObject.AddComponent<Image>().color = ButtonColor;

        // handle
        RectTransform handleArea = MakeRect("Handle Slide Area", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(15f, 0f);
        handleArea.offsetMax = new Vector2(-15f, 0f);

        RectTransform handle = MakeRect("Handle", handleArea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 0f));
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = ButtonColor;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        ColorBlock colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.75f, 0.80f, 1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.65f, 0.9f, 1f);
        colors.selectedColor = Color.white;
        slider.colors = colors;

        return slider;
    }
}