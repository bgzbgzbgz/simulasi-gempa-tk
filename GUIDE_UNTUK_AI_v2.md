# Guide v2: Game Simulasi Edukasi Evakuasi Gempa Bumi untuk Anak TK

> Konteks project Tugas Akhir untuk AI (Cursor / Claude / ChatGPT / Gemini). Versi ini menggantikan
> `GUIDE_UNTUK_AI.md`. File lama tidak diubah.
>
> Bedanya dengan v1: guide ini **tidak menyalin source code**. Kode selalu dibaca dari file aslinya,
> supaya AI tidak bekerja dari salinan yang sudah basi.

---

## Cara Pakai

- **Di Cursor / AI yang bisa membaca repo:** minta AI membaca file ini dulu, lalu file script yang
  disebut di bagian "Peta File" sesuai tugas.
- **Di AI chat (ChatGPT / Gemini / Claude chat):** paste isi file ini, lalu paste juga **isi script asli**
  yang akan diubah atau dipanggil (misal `GameManager.cs` + `CheckpointTrigger.cs`). Jangan minta AI
  menebak isi script yang tidak di-paste.

**Prioritas aturan:** repo ini juga punya `AGENTS.md` (gaya "kode sesedikit mungkin"). Prinsip itu tetap
dipakai: perubahan sekecil mungkin dan jangan menambah fitur yang tidak diminta. Kalau ada yang
bertentangan, **konvensi di file ini yang dipakai** (Header/Tooltip, komentar Bahasa Indonesia,
penjelasan setup Inspector).

---

## Deskripsi Project

**Judul TA:** Game Simulasi Edukasi Evakuasi Gempa Bumi untuk Anak TK
**Engine:** Unity **6000.3.26f1** (Unity 6), URP 17.3, C#, target build **Android APK** (landscape)
**Target user:** anak usia Taman Kanak-Kanak

Pemain mengendalikan karakter anak laki-laki (third-person 3D) melalui 3 level berurutan:

| Level | Nama | Objektif | Selesai lewat |
|-------|------|----------|---------------|
| 1 | Kenali Bencana | Kuis visual 3 soal pilihan gambar | `Level1QuizManager` → `GameManager.OnLevel1QuizFinished()` |
| 2 | Drop & Cover | Cari kolong meja → runduk → tahan posisi | `CheckpointTrigger` (targetLevel 2, requireCrouch, hold time) |
| 3 | Evakuasi | Ikuti jalur evakuasi ke titik kumpul di lapangan | `CheckpointTrigger` (targetLevel 3) di titik kumpul |

**Penilaian:** total waktu → bintang (≤ 60 detik = 3, ≤ 120 detik = 2, lainnya = 1; di Inspector
`goldTimeTarget` / `silverTimeTarget`), plus **+10 poin** per jawaban kuis yang benar.

**Alur di `GameManager`:**
`StartGame()` → kuis Level 1 (gerak pemain dimatikan) → `CompleteLevel(1)` → panel "Level Selesai" →
tombol Lanjut → `NextLevel()` (gerak aktif lagi) → `CompleteLevel(2)` → … → `CompleteLevel(3)` → skor akhir.
`CompleteLevel(n)` diabaikan kalau `n` bukan level yang sedang aktif.

---

## Konvensi Kode

- **Singleton:** `GameManager.Instance` mengatur seluruh alur level
- **Event-driven:** komponen memanggil method GameManager saat objektif selesai
- **Null-check defensif:** semua reference dicek null sebelum dipakai
- **Inspector-friendly:** `[Header]`, `[Tooltip]`, `[SerializeField]`
- **Komentar Bahasa Indonesia** di `<summary>` dan inline
- **UI:** uGUI (`UnityEngine.UI`, komponen `Text` legacy), bukan UI Toolkit
- **Input:** New Input System (`UnityEngine.InputSystem`), keyboard + `VirtualJoystick` + `CrouchButton`
- **Teks UI tanpa emoji.** `Text` legacy umumnya tidak bisa menampilkan emoji seperti ⭐. Untuk bintang
  pakai simbol `★` / `☆` seperti di `MissionUI.cs`.

### Package
Pakai package yang **sudah ada** di `Packages/manifest.json`; jangan menambah package baru.
Yang relevan:

| Package | Dipakai untuk |
|---------|---------------|
| `com.unity.inputsystem` | Input keyboard, mouse, touch |
| `com.unity.ugui` | Semua UI |
| `com.unity.ai.navigation` | **NavMesh**: pilihan utama untuk jalur/pathfinding Level 3 (sudah terpasang) |
| `com.unity.render-pipelines.universal` | Rendering (URP) |
| `com.unity.probuilder`, `com.unity.polybrush` | Membuat/merapikan geometri level di Editor |
| `com.unity.timeline` | Cutscene sederhana bila perlu |
| `com.unity.test-framework` | Unit test (belum ada test) |

---

## Peta File

### Script runtime (`Assets/Scripts/`)

| File | Peran | Method publik yang dipanggil script lain |
|------|-------|-------------------------------------------|
| `GameManager.cs` | Alur level, stopwatch, poin, skor akhir | `StartGame`, `AddScore`, `OnLevel1QuizFinished`, `CompleteLevel`, `NextLevel`, `RestartGame`, `GoToMainMenu`; field `currentLevel`, `isGameActive`, `missionUI` |
| `Level1QuizManager.cs` | Kuis 3 soal Level 1 (soal default dibuat otomatis bila kosong) | `StartQuiz`, `OnOptionClicked`, `SetupDefaultQuestions` |
| `CheckpointTrigger.cs` | Trigger objektif per level; opsi `requireCrouch` + `requiredHoldTime` | (tidak ada; atur di Inspector) |
| `ShelterZone.cs` | Trigger kolong meja; **otomatis membuat pemain merunduk** saat masuk dan berdiri saat keluar | properti `IsPlayerInside`; event `onEnterShelter` / `onExitShelter` |
| `MissionUI.cs` | HUD instruksi, timer, hold progress, panel level selesai, panel skor akhir | `SetInstruction`, `UpdateTimerDisplay`, `UpdateHoldProgress`, `ResetHoldProgress`, `ShowLevelComplete`, `HideLevelComplete`, `ShowFinalScore` |
| `BoyController.cs` | Gerak karakter (CharacterController), crouch, animasi | `SetCrouching`, `ToggleCrouch`; properti `IsCrouching` |
| `ThirdPersonCamera.cs` | Kamera orbit third-person dengan collision | (tidak ada) |
| `VirtualJoystick.cs` | Joystick layar sentuh; juga mengunci layar ke landscape | properti `InputDirection` |
| `CrouchButton.cs` | Tombol runduk di layar | `OnClickButton` |
| `EarthquakeEffect.cs` | Efek gempa selama Level 2: guncangan kamera (berjalan setelah `ThirdPersonCamera`), overlay berkedip, suara gemuruh, benda jatuh | properti `IsShaking` |
| `EvacuationGuide.cs` | Garis jalur evakuasi Level 3 dari pemain ke titik kumpul, mengikuti NavMesh (garis lurus bila NavMesh belum di-bake) | (tidak ada) |

### Tool Editor (`Assets/Scripts/Editor/`, menu **Tools**)

| Menu | File | Hasil |
|------|------|-------|
| Tools/Setup Game Manager & UI Evakuasi | `EvacuationSetupEditor.cs` | GameManager, Canvas HUD misi, panel kuis Level 1, panel level selesai & skor akhir, EventSystem |
| Tools/Generate Skenario Evakuasi Lengkap (Real TK) | `RealScenarioGenerator.cs` | Blockout ruang kelas (3 meja + ShelterZone), koridor, gang, lapangan + area titik kumpul (trigger BoxCollider **tanpa** CheckpointTrigger) |
| Tools/Export Denah ke OBJ (Untuk Edit di Blender) | `RealScenarioGenerator.cs` | Ekspor denah ke OBJ |
| Tools/Buat Blockout Kelas TK (1-Klik) | `ClassroomBlockoutSetup.cs` | Blockout kelas + meja + `ShelterZone_KolongMeja` |
| Tools/Pasang Denah Referensi di Scene | `DenahReferenceSetup.cs` | Gambar denah sebagai alas referensi |
| Tools/Buat Virtual Joystick UI (Mobile) | `VirtualJoystickEditorSetup.cs` | Canvas + VirtualJoystick |
| Tools/Tambah Tombol Merunduk (Crouch Button) | `VirtualJoystickEditorSetup.cs` | Tombol runduk |
| Tools/Level 2: Pasang Objektif Berlindung di Meja Terpilih | `Level23SetupEditor.cs` | Zona `CheckpointTrigger` (targetLevel 2, wajib runduk, tahan 5 detik) di setiap meja yang dipilih |
| Tools/Level 2: Pasang Efek Gempa | `Level23SetupEditor.cs` | Objek `Efek_Gempa` (EarthquakeEffect + AudioSource) dan `Overlay_Gempa` di Canvas |
| Tools/Level 3: Pasang Titik Kumpul di Posisi Terpilih | `Level23SetupEditor.cs` | `Titik_Kumpul_Level3` (CheckpointTrigger targetLevel 3 + penanda hijau) dan `Jalur_Evakuasi` (EvacuationGuide) |

Sebelum membuat setup manual, cek dulu apakah salah satu tool di atas sudah melakukannya.

### Scene & aset
- `Assets/Scenes/LECVEL.unity`: **scene utama**. Berisi GameManager, MissionUI, Level1QuizManager,
  karakter, kamera, joystick, tombol runduk, dan denah `denah_tes.fbx`.
- `Assets/Scenes/SampleScene.unity`: scene percobaan (karakter + kontrol + 3 ShelterZone, tanpa GameManager).
- `Assets/References/`: gambar denah, peta lokasi TK, referensi kelas.
- `Assets/Animation/`: animasi karakter (idle, walk, run, crouch).

---

## Status Saat Ini (dicek 2026-11-04)

### ✅ Sudah jadi
1. Karakter 3D (`boy.fbx`) dengan animasi Idle, Walk, Run, Crouch
2. Kontrol PC + mobile (keyboard, virtual joystick, tombol runduk)
3. Kamera third-person orbit dengan collision
4. Environment kelas TK dari denah FBX (`denah_tes.fbx`) di `LECVEL`
5. Level 1: kuis visual 3 soal, terpasang di `LECVEL`
6. GameManager (alur level + bintang) dan MissionUI (HUD, timer, hold progress, panel)
7. Script `ShelterZone` dan `CheckpointTrigger` (hold timer + syarat runduk)
8. Tool Editor untuk generate scene, UI, dan blockout (lihat tabel di atas)

9. Script Level 2 & 3: `EarthquakeEffect`, `EvacuationGuide`, dan tool `Level23SetupEditor`
10. Build Settings: `LECVEL` scene pertama (`SampleScene` dinonaktifkan)

### ⚠️ Perlu dipasang di Editor (`LECVEL` belum berisi objektif Level 2 & 3)
1. Pilih `Meja Siswa 1` → **Tools/Level 2: Pasang Objektif Berlindung di Meja Terpilih**
2. **Tools/Level 2: Pasang Efek Gempa** (opsional: isi AudioClip gemuruh, seret benda jatuh)
3. Pilih lantai lokasi titik kumpul (misal `paving`) → **Tools/Level 3: Pasang Titik Kumpul di Posisi Terpilih**
4. Pilih `denah_tes` → Add Component → **NavMesh Surface** → **Bake**
5. Simpan scene, lalu Play dari Level 1 sampai skor akhir

Catatan: teks instruksi Level 3 di GameManager menyebut "di belakang Guru", tetapi belum ada karakter guru.

### ❌ Belum jadi
1. **Polish UI skor akhir:** animasi bintang, panel hasil yang menarik untuk anak TK
2. **Audio:** narasi, SFX gempa (belum ada file suara di repo), feedback
3. **Karakter guru** untuk Level 3 (atau ubah teks instruksinya)
4. **Android build & performa**
5. **Unit test** (`com.unity.test-framework` sudah terpasang, belum ada test)

---

## Contoh Permintaan

- "Buat animasi bintang muncul satu per satu di panel skor akhir (`MissionUI.ShowFinalScore`)."
- "Tambahkan getar HP (vibrate) singkat saat gempa mulai di `EarthquakeEffect`."
- "Buat karakter guru yang berjalan ke titik kumpul di Level 3 memakai NavMeshAgent."
- "Ubah teks instruksi Level 3 di `GameManager` supaya tidak menyebut guru."

## Aturan Saat Minta Script Baru

- Baca script asli yang terkait dulu; jangan menulis ulang logika yang sudah ada
  (contoh: hold timer + syarat runduk sudah ada di `CheckpointTrigger`)
- Ikuti pola singleton GameManager dan null-check yang sudah ada
- Hanya package yang sudah ada di `Packages/manifest.json`
- `[Header]` dan `[Tooltip]` untuk Inspector, komentar Bahasa Indonesia
- Jelaskan cara setup di Unity Editor (apa yang di-assign di Inspector, menu Tools mana yang dipakai)
