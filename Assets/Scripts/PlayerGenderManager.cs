using UnityEngine;

/// <summary>
/// Mengatur pemilihan karakter Putra (Boy) atau Putri (Girl) di scene simulasi
/// berdasarkan PlayerPrefs ("PlayerGender") yang dipilih pemain di Main Menu.
/// </summary>
public class PlayerGenderManager : MonoBehaviour
{
    public static PlayerGenderManager Instance { get; private set; }

    [Header("Player References")]
    public GameObject boyCharacter;
    public GameObject girlCharacter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        ApplyGenderSelection();
    }

    public void ApplyGenderSelection()
    {
        string gender = PlayerPrefs.GetString("PlayerGender", "Boy");
        bool isGirl = gender.Equals("Girl", System.StringComparison.OrdinalIgnoreCase);

        if (boyCharacter == null)
        {
            boyCharacter = GameObject.Find("boy");
        }
        if (girlCharacter == null)
        {
            girlCharacter = GameObject.Find("girl");
        }

        if (boyCharacter != null)
        {
            boyCharacter.SetActive(!isGirl);
        }

        if (girlCharacter != null)
        {
            girlCharacter.SetActive(isGirl);
        }

        GameObject activePlayer = isGirl ? girlCharacter : boyCharacter;
        if (activePlayer != null)
        {
            if (ThirdPersonCamera.Instance != null)
            {
                ThirdPersonCamera.Instance.SetTarget(activePlayer.transform);
            }
        }

        Debug.Log($"[PlayerGenderManager] Karakter aktif: {(isGirl ? "Putri (Girl)" : "Putra (Boy)")}");
    }
}
