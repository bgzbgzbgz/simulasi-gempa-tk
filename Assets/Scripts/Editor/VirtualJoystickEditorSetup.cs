#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class VirtualJoystickEditorSetup
{
    [MenuItem("Tools/Buat Virtual Joystick UI (Mobile)")]
    public static void CreateJoystickUI()
    {
        // 1. Pastikan EventSystem tersedia
        EnsureEventSystem();

        // 2. Buat atau temukan Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        // Cek apakah VirtualJoystick sudah pernah dibuat
        VirtualJoystick existingJoystick = Object.FindFirstObjectByType<VirtualJoystick>();
        if (existingJoystick != null)
        {
            Selection.activeGameObject = existingJoystick.gameObject;
            EditorUtility.DisplayDialog("Sudah Ada", "Virtual Joystick sudah terpasang di scene!", "OK");
            return;
        }

        // 3. Buat Objek Background Joystick (Lingkaran Luar)
        GameObject joystickObj = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(Image), typeof(VirtualJoystick));
        joystickObj.transform.SetParent(canvas.transform, false);

        RectTransform bgRect = joystickObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(0f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(220f, 220f);
        bgRect.sizeDelta = new Vector2(240f, 240f);

        Image bgImage = joystickObj.GetComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.25f); // Transparan lembut
        bgImage.raycastTarget = true;

        // Coba cari sprite lingkaran bawaan jika ada
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (knobSprite != null)
        {
            bgImage.sprite = knobSprite;
        }

        // 4. Buat Objek Handle Joystick (Tombol Dalam)
        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(joystickObj.transform, false);

        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(100f, 100f);

        Image handleImage = handleObj.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.75f);
        handleImage.raycastTarget = false;
        if (knobSprite != null)
        {
            handleImage.sprite = knobSprite;
        }

        // 5. Hubungkan referensi ke VirtualJoystick component
        VirtualJoystick joystickScript = joystickObj.GetComponent<VirtualJoystick>();
        SerializedObject serializedJoystick = new SerializedObject(joystickScript);
        serializedJoystick.FindProperty("background").objectReferenceValue = bgRect;
        serializedJoystick.FindProperty("handle").objectReferenceValue = handleRect;
        serializedJoystick.FindProperty("handleRange").floatValue = 85f;
        serializedJoystick.ApplyModifiedProperties();

        // 6. Hubungkan ke BoyController jika ada di scene
        BoyController boy = Object.FindFirstObjectByType<BoyController>();
        if (boy != null)
        {
            SerializedObject serializedBoy = new SerializedObject(boy);
            serializedBoy.FindProperty("virtualJoystick").objectReferenceValue = joystickScript;
            serializedBoy.ApplyModifiedProperties();
        }

        // 7. Buat Tombol Merunduk (Crouch Button) di Pojok Kanan Bawah
        EnsureCrouchButton(canvas, knobSprite);

        Undo.RegisterCreatedObjectUndo(joystickObj, "Create Virtual Joystick");
        Selection.activeGameObject = joystickObj;

        EditorUtility.DisplayDialog(
            "Berhasil!",
            "UI Kontrol Mobile Lengkap Berhasil Dibuat!\n\n" +
            "• Jempol Kiri: Analog Virtual Joystick di pojok kiri bawah\n" +
            "• Jempol Kanan: Tombol RUNDUK di pojok kanan bawah\n" +
            "• Geser Layar Kanan: Memutar kamera (tidak terganggu saat tekan tombol)\n" +
            "• Collider: Tinggi karakter berkurang 50% saat merunduk sehingga bisa masuk kolong meja!",
            "Mantap");
    }

    [MenuItem("Tools/Tambah Tombol Merunduk (Crouch Button)")]
    public static void CreateCrouchButtonMenu()
    {
        EnsureEventSystem();
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            CreateJoystickUI();
            return;
        }

        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        EnsureCrouchButton(canvas, knobSprite);

        EditorUtility.DisplayDialog("Sukses!", "Tombol Merunduk (RUNDUK) berhasil ditambahkan di pojok kanan bawah!", "OK");
    }

    private static void EnsureCrouchButton(Canvas canvas, Sprite knobSprite)
    {
        CrouchButton existing = Object.FindFirstObjectByType<CrouchButton>();
        if (existing != null)
        {
            return;
        }

        GameObject crouchObj = new GameObject("Tombol_Runduk", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CrouchButton));
        crouchObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = crouchObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-190f, 190f);
        rect.sizeDelta = new Vector2(160f, 160f);

        Image img = crouchObj.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.35f);
        img.raycastTarget = true;
        if (knobSprite != null)
        {
            img.sprite = knobSprite;
        }

        // Teks Label
        GameObject textObj = new GameObject("Text_Label", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(crouchObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text label = textObj.GetComponent<Text>();
        label.text = "RUNDUK\n(DROP)";
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(crouchObj, "Create Crouch Button");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }
    }
}
#endif
