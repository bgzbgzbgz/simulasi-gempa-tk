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
    public Image cardBoyBg;
    public Image cardGirlBg;
    public Image btnSelectBoyImage;
    public Text btnSelectBoyText;
    public Image btnSelectGirlImage;
    public Text btnSelectGirlText;
    public GameObject badgeBoySelected;
    public GameObject badgeGirlSelected;
    public Text textCharacterNotice;

    [Header("Pengaturan Audio UI")]
    public Slider sliderBGM;
    public Slider sliderSFX;
    public Toggle toggleVoiceOver;
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

        // Auto-wire semua tombol di scene agar selalu aktif & berfungsi saat Play mode
        AutoWireAllButtons();

        // Load setting tersimpan
        LoadSettings();
    }

    private void Start()
    {
        ShowOnlyPanel(panelMainMenu);

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
    }

    /// <summary>
    /// Menjamin semua tombol terhubung dengan listener fungsinya masing-masing.
    /// </summary>
    public void AutoWireAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button b in buttons)
        {
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

        if (sliderBGM != null)
        {
            sliderBGM.onValueChanged.RemoveListener(OnBGMChanged);
            sliderBGM.onValueChanged.AddListener(OnBGMChanged);
        }
        if (sliderSFX != null)
        {
            sliderSFX.onValueChanged.RemoveListener(OnSFXChanged);
            sliderSFX.onValueChanged.AddListener(OnSFXChanged);
        }
        if (toggleVoiceOver != null)
        {
            toggleVoiceOver.onValueChanged.RemoveListener(OnVoiceOverChanged);
            toggleVoiceOver.onValueChanged.AddListener(OnVoiceOverChanged);
        }

        // Auto-assign elemen kartu karakter jika belum terhubung
        if (btnSelectBoyImage == null || btnSelectGirlImage == null)
        {
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns)
            {
                if (b.gameObject.name == "Btn_SelectBoy")
                {
                    btnSelectBoyImage = b.GetComponent<Image>();
                    btnSelectBoyText = b.GetComponentInChildren<Text>();
                }
                else if (b.gameObject.name == "Btn_SelectGirl")
                {
                    btnSelectGirlImage = b.GetComponent<Image>();
                    btnSelectGirlText = b.GetComponentInChildren<Text>();
                }
            }
        }
    }

    public void OnVoiceOverChanged(bool isOn)
    {
        PlayerPrefs.SetInt("VoiceOver_Enabled", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    #region Panel Navigation

    public void OpenCharacterSelect()
    {
        PlayClick();
        ShowOnlyPanel(panelCharacterSelect);
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

        if (inputPlayerName != null)
        {
            inputPlayerName.ActivateInputField();
        }
    }

    public void BackToCharacterSelect()
    {
        PlayClick();
        ShowOnlyPanel(panelCharacterSelect);
    }

    public void OpenSettings()
    {
        PlayClick();
        if (panelSettings != null) panelSettings.SetActive(true);
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
        if (panelTutorial != null) panelTutorial.SetActive(true);
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
        bool isBoy = selectedGender == "Boy";
        Color greenColor = new Color(0.13f, 0.77f, 0.36f); // #22C55E
        Color whiteColor = Color.white;
        Color normalOutline = new Color(0.12f, 0.16f, 0.22f); // #1F2937
        Color darkTextColor = new Color(0.12f, 0.16f, 0.22f);

        // Update Outline Kartu (Hijau jika terpilih, Netral jika tidak)
        if (cardBoyOutline != null)
        {
            cardBoyOutline.effectColor = isBoy ? greenColor : normalOutline;
            cardBoyOutline.effectDistance = isBoy ? new Vector2(4.5f, -4.5f) : new Vector2(3f, -3f);
        }
        if (cardGirlOutline != null)
        {
            // Karakter cewe ketika terpilih juga BERWARNA HIJAU (bukan merah/pink)
            cardGirlOutline.effectColor = !isBoy ? greenColor : normalOutline;
            cardGirlOutline.effectDistance = !isBoy ? new Vector2(4.5f, -4.5f) : new Vector2(3f, -3f);
        }

        // Background kartu tetap putih bersih
        if (cardBoyBg != null) cardBoyBg.color = Color.white;
        if (cardGirlBg != null) cardGirlBg.color = Color.white;

        // Tombol Pilih Boy
        if (btnSelectBoyImage != null)
        {
            btnSelectBoyImage.color = isBoy ? greenColor : whiteColor;
        }
        if (btnSelectBoyText != null)
        {
            btnSelectBoyText.color = isBoy ? Color.white : darkTextColor;
            btnSelectBoyText.text = isBoy ? "PILIH SAYA ✓" : "PILIH SAYA";
        }

        // Tombol Pilih Girl (Hijau saat dipilih, putih saat tidak)
        if (btnSelectGirlImage != null)
        {
            btnSelectGirlImage.color = !isBoy ? greenColor : whiteColor;
        }
        if (btnSelectGirlText != null)
        {
            btnSelectGirlText.color = !isBoy ? Color.white : darkTextColor;
            btnSelectGirlText.text = !isBoy ? "PILIH SAYA ✓" : "PILIH SAYA";
        }

        if (badgeBoySelected != null) badgeBoySelected.SetActive(isBoy);
        if (badgeGirlSelected != null) badgeGirlSelected.SetActive(!isBoy);
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

        // Simpan nama player dan gender ke PlayerPrefs
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("PlayerGender", selectedGender);
        PlayerPrefs.Save();

        Debug.Log($"[MainMenu] Mulai Simulasi untuk Player: '{playerName}' ({selectedGender})");

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
        if (textBGMValue != null) textBGMValue.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    public void OnSFXChanged(float val)
    {
        if (textSFXValue != null) textSFXValue.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    private void SaveSettings()
    {
        if (sliderBGM != null) PlayerPrefs.SetFloat("BGM_Volume", sliderBGM.value);
        if (sliderSFX != null) PlayerPrefs.SetFloat("SFX_Volume", sliderSFX.value);
        if (toggleVoiceOver != null) PlayerPrefs.SetInt("VoiceOver_Enabled", toggleVoiceOver.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        float bgm = PlayerPrefs.GetFloat("BGM_Volume", 0.7f);
        float sfx = PlayerPrefs.GetFloat("SFX_Volume", 0.85f);
        bool vo = PlayerPrefs.GetInt("VoiceOver_Enabled", 1) == 1;

        if (sliderBGM != null) { sliderBGM.value = bgm; OnBGMChanged(bgm); }
        if (sliderSFX != null) { sliderSFX.value = sfx; OnSFXChanged(sfx); }
        if (toggleVoiceOver != null) toggleVoiceOver.isOn = vo;
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
