using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mengelola alur UI Menu Utama:
/// 1. Menu Utama (UI_01_MainMenu)
/// 2. Pemilihan Karakter Putra & Putri (UI_01B_CharacterSelect)
/// 3. Input Nama Pemain (UI_01C_PlayerNameInput)
/// 4. Pengaturan Suara & Voice Over (UI_02_SettingsPause)
/// 5. Panduan Edukasi Gempa
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMainMenu;
    public GameObject panelCharacterSelect;
    public GameObject panelNameInput;
    public GameObject panelSettings;
    public GameObject panelTutorial;

    [Header("Input Nama")]
    public InputField inputPlayerName;
    public Text textNameFeedback;

    [Header("Karakter Selection UI")]
    public Outline cardBoyOutline;
    public Outline cardGirlOutline;
    public Image cardBoyBorderImage;
    public Image cardGirlBorderImage;
    public Image cardBoyBg;
    public Image cardGirlBg;
    public Image btnSelectBoyImage;
    public Text btnSelectBoyText;
    public Image btnSelectGirlImage;
    public Text btnSelectGirlText;
    public GameObject badgeBoySelected;
    public GameObject badgeGirlSelected;
    public GameObject badgeGirlLocked;
    public Text textCharacterNotice;

    [Header("Demo Lock")]
    // shortcut: Girl card locked in UI for demo until 3D girl asset is repaired
    public bool lockGirlCharacter = true;

    [Header("Pengaturan Audio UI")]
    public Slider sliderBGM;
    public Slider sliderSFX;
    public Toggle toggleVoiceOver;
    public Image toggleVoiceOverBg;
    public Text toggleVoiceOverText;
    public GameObject toggleVoiceOverCheckIcon;
    public Text textBGMValue;
    public Text textSFXValue;

    [Header("Target Scene Simulasi")]
    public string simulationSceneName = "LECVEL_NewMap";

    private string selectedGender = "Boy"; // Default putra

    private void Awake()
    {
        // Pastikan Audio Listener / SoundManager aktif
        if (SoundManager.Instance == null)
        {
            GameObject smObj = new GameObject("SoundManager", typeof(SoundManager));
        }

        if (lockGirlCharacter)
        {
            selectedGender = "Boy";
            PlayerPrefs.SetString("PlayerGender", "Boy");
            PlayerPrefs.Save();
        }

        // Auto-wire semua tombol di scene agar selalu aktif & berfungsi saat Play mode
        AutoWireAllButtons();

        // Load setting tersimpan
        LoadSettings();
    }

    private void Start()
    {
        ShowOnlyPanel(panelMainMenu);

        if (lockGirlCharacter)
        {
            selectedGender = "Boy";
        }
        else
        {
            selectedGender = PlayerPrefs.GetString("PlayerGender", "Boy");
        }

        // Inisialisasi tampilan pilihan karakter default (Boy)
        UpdateCharacterSelectionVisuals();

        // Siapkan input nama dengan nama tersimpan bila ada
        if (inputPlayerName != null)
        {
            string savedName = PlayerPrefs.GetString("PlayerName", "");
            if (!string.IsNullOrEmpty(savedName))
            {
                inputPlayerName.text = savedName;
            }
        }

        // Putar suara salam pembuka Bu Guru di Menu Utama (jeda sedikit agar AudioListener siap)
        StartCoroutine(PlayIntroDelayed());
    }

    private System.Collections.IEnumerator PlayIntroDelayed()
    {
        yield return new WaitForSeconds(0.4f);
        if (VoiceOverManager.Instance != null)
        {
            VoiceOverManager.Instance.PlayVO_01_Intro();
        }
    }

    /// <summary>
    /// Menjamin semua tombol terhubung dengan listener fungsinya masing-masing tanpa dobel trigger.
    /// </summary>
    public void AutoWireAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button b in buttons)
        {
            if (b.gameObject.name == "Btn_SelectGirl")
            {
                btnSelectGirlImage = b.GetComponent<Image>();
                btnSelectGirlText = b.GetComponentInChildren<Text>(true);
            }
            else if (b.gameObject.name == "Btn_SelectBoy")
            {
                btnSelectBoyImage = b.GetComponent<Image>();
                btnSelectBoyText = b.GetComponentInChildren<Text>(true);
            }

            if (b.onClick.GetPersistentEventCount() > 0) continue;

            switch (b.gameObject.name)
            {
                case "Btn_MulaiSimulasi":
                    b.onClick.RemoveListener(OpenCharacterSelect);
                    b.onClick.AddListener(OpenCharacterSelect);
                    break;
                case "Btn_Pengaturan":
                    b.onClick.RemoveListener(OpenSettings);
                    b.onClick.AddListener(OpenSettings);
                    break;
                case "Btn_Petunjuk":
                    b.onClick.RemoveListener(OpenTutorial);
                    b.onClick.AddListener(OpenTutorial);
                    break;
                case "Btn_Keluar":
                    b.onClick.RemoveListener(QuitGame);
                    b.onClick.AddListener(QuitGame);
                    break;
                case "Btn_Gear_Settings":
                    b.onClick.RemoveListener(OpenSettings);
                    b.onClick.AddListener(OpenSettings);
                    break;
                case "Btn_SelectBoy":
                    b.onClick.RemoveListener(SelectBoy);
                    b.onClick.AddListener(SelectBoy);
                    break;
                case "Btn_SelectGirl":
                    b.onClick.RemoveListener(SelectGirl);
                    b.onClick.AddListener(SelectGirl);
                    break;
                case "Btn_BackToMain":
                    b.onClick.RemoveListener(BackToMainMenu);
                    b.onClick.AddListener(BackToMainMenu);
                    break;
                case "Btn_NextToName":
                    b.onClick.RemoveListener(OpenNameInput);
                    b.onClick.AddListener(OpenNameInput);
                    break;
                case "Btn_BackToChar":
                    b.onClick.RemoveListener(BackToCharacterSelect);
                    b.onClick.AddListener(BackToCharacterSelect);
                    break;
                case "Btn_ConfirmStart":
                    b.onClick.RemoveListener(ConfirmNameAndStartSimulation);
                    b.onClick.AddListener(ConfirmNameAndStartSimulation);
                    break;
                case "Btn_CloseSettings":
                    b.onClick.RemoveListener(CloseSettings);
                    b.onClick.AddListener(CloseSettings);
                    break;
                case "Btn_CloseTut":
                    b.onClick.RemoveListener(CloseTutorial);
                    b.onClick.AddListener(CloseTutorial);
                    break;
            }
        }

        if (sliderBGM != null && sliderBGM.onValueChanged.GetPersistentEventCount() == 0)
        {
            sliderBGM.onValueChanged.RemoveListener(OnBGMChanged);
            sliderBGM.onValueChanged.AddListener(OnBGMChanged);
        }
        if (sliderSFX != null && sliderSFX.onValueChanged.GetPersistentEventCount() == 0)
        {
            sliderSFX.onValueChanged.RemoveListener(OnSFXChanged);
            sliderSFX.onValueChanged.AddListener(OnSFXChanged);
        }
        if (toggleVoiceOver != null)
        {
            if (toggleVoiceOverBg == null) toggleVoiceOverBg = toggleVoiceOver.GetComponent<Image>();
            if (toggleVoiceOverText == null) toggleVoiceOverText = toggleVoiceOver.GetComponentInChildren<Text>(true);
            if (toggleVoiceOverCheckIcon == null)
            {
                Transform tChk = toggleVoiceOver.transform.Find("CheckIcon");
                if (tChk != null) toggleVoiceOverCheckIcon = tChk.gameObject;
            }
            if (toggleVoiceOver.onValueChanged.GetPersistentEventCount() == 0)
            {
                toggleVoiceOver.onValueChanged.RemoveListener(OnVoiceOverChanged);
                toggleVoiceOver.onValueChanged.AddListener(OnVoiceOverChanged);
            }
        }
    }

    public void OnVoiceOverChanged(bool isOn)
    {
        PlayerPrefs.SetInt("VoiceOver_Enabled", isOn ? 1 : 0);
        PlayerPrefs.Save();
        UpdateVoiceOverVisual(isOn);
    }

    private void UpdateVoiceOverVisual(bool isOn)
    {
        Color greenPill = new Color(0.13f, 0.77f, 0.36f);
        Color offPill = new Color(0.88f, 0.90f, 0.93f);
        Color darkText = new Color(0.12f, 0.16f, 0.22f);

        if (toggleVoiceOverBg != null)
        {
            toggleVoiceOverBg.color = isOn ? greenPill : offPill;
        }
        if (toggleVoiceOverText != null)
        {
            toggleVoiceOverText.text = isOn ? "AKTIF" : "MATI";
            toggleVoiceOverText.color = isOn ? Color.white : darkText;
        }
        if (toggleVoiceOverCheckIcon != null)
        {
            toggleVoiceOverCheckIcon.SetActive(isOn);
        }
    }

    #region Panel Navigation

    public void OpenCharacterSelect()
    {
        PlayClick();
        ShowOnlyPanel(panelCharacterSelect);
        UpdateCharacterSelectionVisuals();
    }

    public void BackToMainMenu()
    {
        PlayClick();
        ShowOnlyPanel(panelMainMenu);
    }

    public void OpenNameInput()
    {
        PlayClick();
        ShowOnlyPanel(panelNameInput);

        if (VoiceOverManager.Instance != null && VoiceOverManager.Instance.voNamaKamuSiapa != null)
        {
            VoiceOverManager.Instance.PlayVO(VoiceOverManager.Instance.voNamaKamuSiapa, false);
        }

        if (inputPlayerName != null)
        {
            inputPlayerName.ActivateInputField();
        }
    }

    public void BackToCharacterSelect()
    {
        PlayClick();
        ShowOnlyPanel(panelCharacterSelect);
        UpdateCharacterSelectionVisuals();
    }

    public void OpenSettings()
    {
        PlayClick();
        if (panelSettings != null)
        {
            panelSettings.SetActive(true);
            panelSettings.transform.SetAsLastSibling();
            foreach (Transform child in panelSettings.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
        LoadSettings();
    }

    public void CloseSettings()
    {
        PlayClick();
        SaveSettings();
        if (panelSettings != null) panelSettings.SetActive(false);
    }

    public void OpenTutorial()
    {
        PlayClick();
        if (panelTutorial != null)
        {
            panelTutorial.SetActive(true);
            panelTutorial.transform.SetAsLastSibling();
            foreach (Transform child in panelTutorial.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
    }

    public void CloseTutorial()
    {
        PlayClick();
        if (panelTutorial != null) panelTutorial.SetActive(false);
    }

    public void QuitGame()
    {
        PlayClick();
        Debug.Log("[MainMenu] Keluar dari aplikasi...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowOnlyPanel(GameObject target)
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(panelMainMenu == target);
        if (panelCharacterSelect != null) panelCharacterSelect.SetActive(panelCharacterSelect == target);
        if (panelNameInput != null) panelNameInput.SetActive(panelNameInput == target);
        if (panelTutorial != null) panelTutorial.SetActive(false);
        if (panelSettings != null) panelSettings.SetActive(false);

        if (target != null && (target == panelCharacterSelect || target == panelNameInput))
        {
            foreach (Transform child in target.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
    }

    #endregion

    #region Character Selection

    public void SelectBoy()
    {
        PlayClick();
        selectedGender = "Boy";
        PlayerPrefs.SetString("PlayerGender", "Boy");
        PlayerPrefs.Save();

        UpdateCharacterSelectionVisuals();

        if (textCharacterNotice != null)
        {
            textCharacterNotice.text = "Karakter terpilih: Siswa Putra (Siap Simulasi)";
            textCharacterNotice.color = new Color(0.13f, 0.77f, 0.36f);
        }
    }

    public void SelectGirl()
    {
        PlayClick();
        if (lockGirlCharacter)
        {
            selectedGender = "Boy";
            PlayerPrefs.SetString("PlayerGender", "Boy");
            PlayerPrefs.Save();
            UpdateCharacterSelectionVisuals();
            if (textCharacterNotice != null)
            {
                textCharacterNotice.text = "Karakter Putri dikunci sementara untuk perbaikan model 3D.";
                textCharacterNotice.color = new Color(0.96f, 0.62f, 0.04f);
            }
            return;
        }

        selectedGender = "Girl";
        PlayerPrefs.SetString("PlayerGender", "Girl");
        PlayerPrefs.Save();

        UpdateCharacterSelectionVisuals();

        if (textCharacterNotice != null)
        {
            textCharacterNotice.text = "Karakter terpilih: Siswa Putri (Siap Simulasi)";
            textCharacterNotice.color = new Color(0.13f, 0.77f, 0.36f);
        }
    }

    private void UpdateCharacterSelectionVisuals()
    {
        bool isBoy = lockGirlCharacter || selectedGender == "Boy";
        Color greenColor = new Color(0.13f, 0.77f, 0.36f); // #22C55E
        Color whiteColor = Color.white;
        Color normalOutline = new Color(0.75f, 0.80f, 0.86f);
        Color darkTextColor = new Color(0.12f, 0.16f, 0.22f);
        Color lockedBg = new Color(0.89f, 0.91f, 0.94f);
        Color lockedText = new Color(0.45f, 0.50f, 0.58f);

        if (cardBoyOutline != null)
        {
            cardBoyOutline.effectColor = isBoy ? greenColor : normalOutline;
            cardBoyOutline.effectDistance = isBoy ? new Vector2(4.5f, -4.5f) : new Vector2(3f, -3f);
        }
        if (cardGirlOutline != null)
        {
            cardGirlOutline.effectColor = (!lockGirlCharacter && !isBoy) ? greenColor : normalOutline;
            cardGirlOutline.effectDistance = (!lockGirlCharacter && !isBoy) ? new Vector2(4.5f, -4.5f) : new Vector2(3f, -3f);
        }

        if (cardBoyBorderImage != null)
        {
            cardBoyBorderImage.color = isBoy ? greenColor : normalOutline;
        }
        if (cardGirlBorderImage != null)
        {
            cardGirlBorderImage.color = (!lockGirlCharacter && !isBoy) ? greenColor : normalOutline;
        }

        if (cardBoyBg != null) cardBoyBg.color = Color.white;
        if (cardGirlBg != null) cardGirlBg.color = lockGirlCharacter ? new Color(0.96f, 0.97f, 0.98f) : Color.white;

        if (btnSelectBoyImage != null)
        {
            btnSelectBoyImage.color = isBoy ? greenColor : whiteColor;
        }
        if (btnSelectBoyText != null)
        {
            btnSelectBoyText.color = isBoy ? Color.white : darkTextColor;
            btnSelectBoyText.text = "PILIH SAYA";
        }

        Button girlBtn = btnSelectGirlImage != null ? btnSelectGirlImage.GetComponent<Button>() : null;
        if (girlBtn != null)
        {
            girlBtn.interactable = !lockGirlCharacter;
        }
        if (btnSelectGirlImage != null)
        {
            btnSelectGirlImage.color = lockGirlCharacter ? lockedBg : (!isBoy ? greenColor : whiteColor);
        }
        if (btnSelectGirlText != null)
        {
            btnSelectGirlText.color = lockGirlCharacter ? lockedText : (!isBoy ? Color.white : darkTextColor);
            btnSelectGirlText.text = lockGirlCharacter ? "TERKUNCI" : "PILIH SAYA";
        }

        if (badgeBoySelected != null) badgeBoySelected.SetActive(isBoy);
        if (badgeGirlSelected != null) badgeGirlSelected.SetActive(!lockGirlCharacter && !isBoy);
        if (badgeGirlLocked != null) badgeGirlLocked.SetActive(lockGirlCharacter);
    }

    #endregion

    #region Name Input & Start Simulation

    public void ConfirmNameAndStartSimulation()
    {
        PlayClick();

        string playerName = "Ananda";
        if (inputPlayerName != null && !string.IsNullOrWhiteSpace(inputPlayerName.text))
        {
            playerName = inputPlayerName.text.Trim();
        }

        string effectiveGender = lockGirlCharacter ? "Boy" : selectedGender;

        // Simpan nama player dan gender ke PlayerPrefs
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("PlayerGender", effectiveGender);
        PlayerPrefs.Save();

        Debug.Log($"[MainMenu] Mulai Simulasi untuk Player: '{playerName}' ({effectiveGender})");

        // Muat scene simulasi
        string targetScene = simulationSceneName;
        if (Application.CanStreamedLevelBeLoaded("LECVEL_NewMap"))
        {
            targetScene = "LECVEL_NewMap";
        }
        else if (Application.CanStreamedLevelBeLoaded("LECVEL"))
        {
            targetScene = "LECVEL";
        }

        SceneManager.LoadScene(targetScene);
    }

    #endregion

    #region Settings Logic

    public void OnBGMChanged(float val)
    {
        PlayerPrefs.SetFloat("BGM_Volume", val);
        if (textBGMValue != null) textBGMValue.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    public void OnSFXChanged(float val)
    {
        PlayerPrefs.SetFloat("SFX_Volume", val);
        if (textSFXValue != null) textSFXValue.text = $"{Mathf.RoundToInt(val * 100)}%";
        if (SoundManager.Instance != null) SoundManager.Instance.RefreshVolumes();
    }

    private void SaveSettings()
    {
        if (sliderBGM != null) PlayerPrefs.SetFloat("BGM_Volume", sliderBGM.value);
        if (sliderSFX != null) PlayerPrefs.SetFloat("SFX_Volume", sliderSFX.value);
        if (toggleVoiceOver != null) PlayerPrefs.SetInt("VoiceOver_Enabled", toggleVoiceOver.isOn ? 1 : 0);
        PlayerPrefs.Save();
        if (SoundManager.Instance != null) SoundManager.Instance.RefreshVolumes();
    }

    private void LoadSettings()
    {
        float bgm = PlayerPrefs.GetFloat("BGM_Volume", 0.7f);
        float sfx = PlayerPrefs.GetFloat("SFX_Volume", 0.85f);
        bool vo = PlayerPrefs.GetInt("VoiceOver_Enabled", 1) == 1;

        if (sliderBGM != null) { sliderBGM.SetValueWithoutNotify(bgm); OnBGMChanged(bgm); }
        if (sliderSFX != null) { sliderSFX.SetValueWithoutNotify(sfx); OnSFXChanged(sfx); }
        if (toggleVoiceOver != null) { toggleVoiceOver.SetIsOnWithoutNotify(vo); }
        UpdateVoiceOverVisual(vo);
    }

    private void PlayClick()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    #endregion
}
