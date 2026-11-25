using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Membangun HUD + layar akhir (Game Over, Bad Ending, Victory) otomatis,
/// lalu menyambungkannya ke ContaminationGaugeManager dan PlayerSweep.
/// Menu: Valtera > Bangun UI Sektor (scene aktif)
/// Letakkan file ini di folder bernama "Editor" (Assets/Editor).
/// </summary>
public static class ValteraUIBuilder
{
    // ---------- Palet (selaras dengan tombol Main Menu yang cokelat) ----------
    static readonly Color PanelFill = Hex("2A1810", 0.95f);
    static readonly Color Border = Hex("C9A15A");
    static readonly Color Parchment = Hex("F3E6C8");
    static readonly Color Gold = Hex("E0B84A");
    static readonly Color BtnBrown = Hex("8A4A2C");
    static readonly Color Dark = Hex("120B07");
    static readonly Color Danger = Hex("FF6B5E");

    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TR = new Vector2(1f, 1f);
    static readonly Vector2 TC = new Vector2(0.5f, 1f);
    static readonly Vector2 BL = new Vector2(0f, 0f);
    static readonly Vector2 BR = new Vector2(1f, 0f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);

    static TMP_FontAsset font;
    static Sprite sprite;

    // =====================================================================
    [MenuItem("Valtera/Bangun UI Sektor (scene aktif)")]
    public static void Build()
    {
        var manager = Object.FindAnyObjectByType<ContaminationGaugeManager>();
        var sweep = Object.FindAnyObjectByType<PlayerSweep>();

        if (manager == null || sweep == null)
        {
            EditorUtility.DisplayDialog("Valtera",
                "Scene ini harus punya ContaminationGaugeManager dan PlayerSweep (di Player).\n\nBuka Scn_Sector1 dulu, lalu coba lagi.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Bangun UI Sektor",
            "Tool ini akan:\n" +
            "• membuat Canvas baru 'UI_Valtera' (HUD + 3 layar akhir)\n" +
            "• menyambungkannya ke ContaminationGaugeManager & PlayerSweep\n" +
            "• MENONAKTIFKAN Canvas lama (tidak dihapus)\n\nLanjut?",
            "Lanjut", "Batal"))
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        font = PickFont();
        sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Bersihkan hasil build sebelumnya & nonaktifkan Canvas lama.
        foreach (var root in manager.gameObject.scene.GetRootGameObjects())
        {
            if (root.name == "UI_Valtera")
                Undo.DestroyObjectImmediate(root);
        }
        foreach (var root in manager.gameObject.scene.GetRootGameObjects())
        {
            if (root != null && root.name != "UI_Valtera" && root.GetComponent<Canvas>() != null)
            {
                Undo.RecordObject(root, "Nonaktifkan Canvas lama");
                root.SetActive(false);
            }
        }

        // Pastikan GameOverUI & SectorNavigation ada.
        var gameOver = Object.FindAnyObjectByType<GameOverUI>();
        if (gameOver == null)
        {
            var g = new GameObject("GameOverUI");
            Undo.RegisterCreatedObjectUndo(g, "Buat GameOverUI");
            gameOver = Undo.AddComponent<GameOverUI>(g);
        }
        var nav = Object.FindAnyObjectByType<SectorNavigation>();
        if (nav == null)
        {
            var g = new GameObject("SectorNavigation");
            Undo.RegisterCreatedObjectUndo(g, "Buat SectorNavigation");
            nav = Undo.AddComponent<SectorNavigation>(g);
        }
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            Debug.LogWarning("[Valtera] Tidak ada EventSystem di scene. Buat lewat GameObject > UI > Event System agar tombol bisa diklik.");

        // ---------- Canvas ----------
        var canvasGO = new GameObject("UI_Valtera", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Bangun UI Valtera");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform cv = canvasGO.transform;

        // ================= HUD: PANEL GAUGE =================
        var gauge = BorderPanel(cv, "GaugePanel", Border, PanelFill, 3f);
        Anchor(gauge.outer, TC, TC, TC, new Vector2(0f, -24f), new Vector2(900f, 140f));

        Txt(gauge.inner, "GlobalLabel", "KONTAMINASI GLOBAL", 22, Gold, TextAlignmentOptions.Left, FontStyles.Bold, TL, TL, new Vector2(21f, -10f), new Vector2(520f, 30f));
        var globalText = Txt(gauge.inner, "GlobalValue", "0%", 30, Parchment, TextAlignmentOptions.Right, FontStyles.Bold, TR, TR, new Vector2(-21f, -8f), new Vector2(200f, 36f));
        var globalFill = MakeBar(gauge.inner, "GlobalBar", new Vector2(21f, -44f), new Vector2(852f, 26f), true);

        Txt(gauge.inner, "LocalLabel", "KONTAMINASI LOKAL", 18, Parchment, TextAlignmentOptions.Left, FontStyles.Bold, TL, TL, new Vector2(21f, -78f), new Vector2(520f, 26f));
        var localText = Txt(gauge.inner, "LocalValue", "0%", 24, Parchment, TextAlignmentOptions.Right, FontStyles.Bold, TR, TR, new Vector2(-21f, -76f), new Vector2(200f, 30f));
        var localFill = MakeBar(gauge.inner, "LocalBar", new Vector2(21f, -106f), new Vector2(852f, 18f), true);

        // ================= HUD: PERINGATAN =================
        var warn = BorderPanel(cv, "WarningChip", Danger, Hex("4A1414", 0.96f), 2f);
        Anchor(warn.outer, TC, TC, TC, new Vector2(0f, -182f), new Vector2(640f, 48f));
        Txt(warn.inner, "Text", "GAUGE LOKAL KRITIS · BERSIHKAN SEKARANG", 22, Hex("FFD9D4"), TextAlignmentOptions.Center, FontStyles.Bold, C, C, Vector2.zero, new Vector2(620f, 40f));
        var warnGroup = warn.outer.gameObject.AddComponent<CanvasGroup>();
        warnGroup.alpha = 0f;
        warnGroup.interactable = false;
        warnGroup.blocksRaycasts = false;

        // ================= HUD: OBJEKTIF =================
        var obj = BorderPanel(cv, "ObjectivePanel", Border, PanelFill, 3f);
        Anchor(obj.outer, TR, TR, TR, new Vector2(-24f, -24f), new Vector2(360f, 92f));
        Txt(obj.inner, "Label", "OBJEKTIF", 18, Gold, TextAlignmentOptions.Left, FontStyles.Bold, TL, TL, new Vector2(18f, -10f), new Vector2(320f, 26f));
        var progressText = Txt(obj.inner, "ProgressText", "Kotoran 0/0", 32, Parchment, TextAlignmentOptions.Left, FontStyles.Bold, TL, TL, new Vector2(18f, -38f), new Vector2(320f, 44f));

        // ================= HUD: KOMBO & PETUNJUK =================
        var comboText = Txt(cv, "ComboText", "", 52, Gold, TextAlignmentOptions.Right, FontStyles.Bold, BR, BR, new Vector2(-36f, 36f), new Vector2(520f, 76f));
        Txt(cv, "ControlsHint", "WASD  Gerak     ·     Shift  Lari     ·     E  Tahan untuk membersihkan", 20, Hex("F3E6C8", 0.75f), TextAlignmentOptions.Left, FontStyles.Normal, BL, BL, new Vector2(32f, 24f), new Vector2(900f, 34f));

        // ================= HUD: PROMPT BERSIHKAN =================
        var prompt = BorderPanel(cv, "PromptRoot", Border, PanelFill, 3f);
        Anchor(prompt.outer, C, C, C, Vector2.zero, new Vector2(420f, 84f));
        var promptText = Txt(prompt.inner, "PromptText", "Tahan [E] untuk membersihkan", 26, Parchment, TextAlignmentOptions.Center, FontStyles.Bold, C, C, Vector2.zero, new Vector2(400f, 60f));
        var slider = MakeSlider(prompt.inner, "SweepSlider", new Vector2(360f, 24f));
        prompt.outer.gameObject.SetActive(false);

        // ================= LAYAR AKHIR =================
        TMP_Text goStats, beStats, vStats;
        RectTransform goArea, beArea, vArea;

        var goPanel = EndPanel(cv, "GameOverPanel", Hex("1C0A0C", 0.93f), "SEKTOR TERCEMAR", "GAME OVER", Danger,
            "Gauge lokal mencapai 100%. Wabah merebut kembali sektor ini.",
            out goStats, out goArea, new Vector2(864f, 168f), new Vector2(0f, -190f), 2);
        var beP = EndPanel(cv, "BadEndingPanel", Hex("120A1E", 0.95f), "KERAJAAN VALTERA", "BAD ENDING", Hex("C9A7FF"),
            "Gauge global mencapai 100%. Wabah menyebar ke seluruh kerajaan.",
            out beStats, out beArea, new Vector2(420f, 264f), new Vector2(0f, -210f), 1);
        var vP = EndPanel(cv, "VictoryPanel", Hex("1E1708", 0.92f), "SEKTOR BERSIH", "VICTORY", Gold,
            "Semua kotoran berhasil dibersihkan. Sektor berikutnya terbuka.",
            out vStats, out vArea, new Vector2(620f, 72f), new Vector2(0f, -170f), 1);

        // Tombol Game Over: Ulang dari Awal, Ulang dari Sektor, Main Menu, Exit
        Wire(MakeButton(goArea, "Ulang dari Awal"), gameOver.RestartFromBeginning);
        Wire(MakeButton(goArea, "Ulang dari Sektor"), gameOver.RetryCurrentSector);
        Wire(MakeButton(goArea, "Main Menu"), gameOver.BackToMainMenu);
        Wire(MakeButton(goArea, "Exit"), gameOver.ExitGame);

        // Tombol Bad Ending: Ulang dari Awal, Main Menu, Exit
        Wire(MakeButton(beArea, "Ulang dari Awal"), gameOver.RestartFromBeginning);
        Wire(MakeButton(beArea, "Main Menu"), gameOver.BackToMainMenu);
        Wire(MakeButton(beArea, "Exit"), gameOver.ExitGame);

        // Tombol Victory: lanjut ke sektor berikutnya
        Wire(MakeButton(vArea, "Lanjut ke Sektor Berikutnya"), nav.NextSector);

        goPanel.SetActive(false);
        beP.SetActive(false);
        vP.SetActive(false);

        // ================= SAMBUNGKAN KE SCRIPT =================
        SetRef(manager, "globalGaugeFill", globalFill);
        SetRef(manager, "globalGaugeText", globalText);
        SetRef(manager, "localGaugeFill", localFill);
        SetRef(manager, "localGaugeText", localText);
        SetRef(manager, "warningChip", warnGroup);
        SetRef(manager, "comboText", comboText);
        SetRef(manager, "badEndingPanel", beP);
        SetRef(manager, "badEndingStatsText", beStats);
        SetRef(manager, "localGameOverPanel", goPanel);
        SetRef(manager, "gameOverStatsText", goStats);

        SetRef(sweep, "promptRoot", prompt.outer);
        SetRef(sweep, "interactionText", promptText);
        SetRef(sweep, "sweepSlider", slider);
        SetRef(sweep, "progressText", progressText);
        SetRef(sweep, "sectorCompletePanel", vP);
        SetRef(sweep, "victoryStatsText", vStats);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(sweep);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = canvasGO;

        EditorUtility.DisplayDialog("Valtera",
            "UI berhasil dibuat di objek 'UI_Valtera'.\n\nTekan Ctrl+S untuk menyimpan scene, lalu tekan Play.\n(Canvas lama hanya dinonaktifkan, bukan dihapus.)",
            "OK");
    }

    // =====================================================================
    //  HELPER
    // =====================================================================
    static Color Hex(string hex, float alpha = 1f)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        c.a = alpha;
        return c;
    }

    static TMP_FontAsset PickFont()
    {
        // Utamakan font buatan sendiri (bukan bawaan paket TextMesh Pro).
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("TextMeshPro") || path.Contains("TextMesh Pro") ||
                path.Contains("LiberationSans") || path.StartsWith("Packages/"))
                continue;
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f != null) return f;
        }
        return TMP_Settings.defaultFontAsset;
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    static RectTransform Rect(GameObject go) { return (RectTransform)go.transform; }

    static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = min;
        r.anchorMax = max;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    static void Stretch(RectTransform r, float inset = 0f)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.pivot = C;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    static Image AddImage(GameObject go, Color color, bool useSprite = true)
    {
        var img = go.AddComponent<Image>();
        if (useSprite && sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TMP_Text Txt(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, FontStyles style, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 rectSize)
    {
        var go = NewUI(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        Anchor(Rect(go), anchor, anchor, pivot, pos, rectSize);
        return t;
    }

    // Panel dengan garis tepi emas. outer = bingkai, inner = tempat isi.
    static (RectTransform outer, RectTransform inner) BorderPanel(Transform parent, string name, Color border, Color fill, float thickness)
    {
        var outer = NewUI(name, parent);
        AddImage(outer, border);
        var inner = NewUI("Fill", outer.transform);
        AddImage(inner, fill);
        Stretch(Rect(inner), thickness);
        return (Rect(outer), Rect(inner));
    }

    static Image MakeBar(Transform parent, string name, Vector2 pos, Vector2 size, bool ticks)
    {
        var frame = NewUI(name, parent);
        AddImage(frame, Border);
        Anchor(Rect(frame), TL, TL, TL, pos, size);

        var back = NewUI("Back", frame.transform);
        AddImage(back, Dark);
        Stretch(Rect(back), 2f);

        var fillGO = NewUI("Fill", back.transform);
        var fill = fillGO.AddComponent<Image>();
        fill.sprite = sprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0.5f;
        fill.color = Gold;
        fill.raycastTarget = false;
        Stretch(Rect(fillGO));

        if (ticks)
        {
            foreach (float t in new[] { 0.25f, 0.5f, 0.75f })
            {
                var tick = NewUI("Tick" + (int)(t * 100), back.transform);
                AddImage(tick, Hex("F3E6C8", 0.4f), false);
                var r = Rect(tick);
                r.anchorMin = new Vector2(t, 0f);
                r.anchorMax = new Vector2(t, 1f);
                r.pivot = C;
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(2f, 0f);
            }
        }
        return fill;
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 size)
    {
        var go = NewUI(name, parent);
        Anchor(Rect(go), C, C, C, Vector2.zero, size);

        var bg = NewUI("Background", go.transform);
        AddImage(bg, Dark);
        Stretch(Rect(bg));

        var area = NewUI("Fill Area", go.transform);
        Stretch(Rect(area), 3f);

        var fill = NewUI("Fill", area.transform);
        AddImage(fill, Gold);
        Stretch(Rect(fill));

        var s = go.AddComponent<Slider>();
        s.fillRect = Rect(fill);
        s.targetGraphic = null;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.wholeNumbers = false;
        s.transition = Selectable.Transition.None;
        s.interactable = false;
        s.value = 0f;
        go.SetActive(false);
        return s;
    }

    static Button MakeButton(RectTransform parent, string label)
    {
        var go = NewUI("Btn_" + label, parent);
        var img = go.AddComponent<Image>();
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
        img.color = BtnBrown;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(2f, -2f);

        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.highlightedColor = Color.white;
        cb.selectedColor = Color.white;
        cb.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        b.colors = cb;

        var t = Txt(go.transform, "Text", label, 30, Parchment, TextAlignmentOptions.Center, FontStyles.Bold, C, C, Vector2.zero, Vector2.zero);
        Stretch(Rect(t.gameObject));
        return b;
    }

    static void Wire(Button b, UnityAction action)
    {
        UnityEventTools.AddPersistentListener(b.onClick, action);
    }

    // Panel layar penuh: judul, subjudul, statistik, dan area tombol (grid).
    static GameObject EndPanel(Transform parent, string name, Color overlay, string kicker, string title, Color titleColor,
        string subtitle, out TMP_Text stats, out RectTransform buttonArea, Vector2 areaSize, Vector2 areaPos, int columns)
    {
        var root = NewUI(name, parent);
        AddImage(root, overlay, false).raycastTarget = true; // menahan klik ke game di belakangnya
        Stretch(Rect(root));
        root.AddComponent<CanvasGroup>();

        Txt(root.transform, "Kicker", kicker, 30, Gold, TextAlignmentOptions.Center, FontStyles.Bold, C, C, new Vector2(0f, 300f), new Vector2(1400f, 50f)).characterSpacing = 10f;
        var tt = Txt(root.transform, "Title", title, 140, titleColor, TextAlignmentOptions.Center, FontStyles.Bold, C, C, new Vector2(0f, 190f), new Vector2(1600f, 180f));
        tt.characterSpacing = 12f;
        Txt(root.transform, "Subtitle", subtitle, 34, Parchment, TextAlignmentOptions.Center, FontStyles.Normal, C, C, new Vector2(0f, 70f), new Vector2(1500f, 60f));
        stats = Txt(root.transform, "Stats", "", 28, Gold, TextAlignmentOptions.Center, FontStyles.Bold, C, C, new Vector2(0f, 0f), new Vector2(1500f, 50f));

        var area = NewUI("Buttons", root.transform);
        Anchor(Rect(area), C, C, C, areaPos, areaSize);
        var grid = area.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(columns == 2 ? 420f : areaSize.x, 72f);
        grid.spacing = new Vector2(24f, 24f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        buttonArea = Rect(area);
        return root;
    }

    static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogWarning("[Valtera] Field '" + field + "' tidak ditemukan di " + target.GetType().Name + ". Pastikan script terbaru sudah dipasang.");
            return;
        }
        p.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
