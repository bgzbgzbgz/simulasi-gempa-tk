# PROGRESS — Proyek Akhir Simulasi 3D Mitigasi Gempa Bumi untuk Anak TK

> **Single Source of Truth (Memori Tunggal Proyek)**
> Terakhir diperbarui: **26 November 2026**
> File dokumentasi `.md` lama (`New folder`, `simulasi-gempa-tk`, `Roadmap`, `CATATAN_BIMBINGAN_DOSEN`, `GUIDE_UNTUK_AI*`) telah diarsipkan ke `D:\KULIAH\TA\GUDANG_MEMORY\`.
> *(Catatan: tidak ada file `.md` materi PPT yang ditemukan saat konsolidasi.)*

---

## 1. Ringkasan Status Saat Ini (26 Nov 2026)

- **Update 26 Nov 2026 (Perbaikan Alur & Penyamakan UI Word `Tabel 3.7`):**
  - **Fix alur bermain:** `Card_CharacterSelect` di `MainMenu.unity` diaktifkan kembali (`m_IsActive: 1`), objek nyasar (`girl`, `Efek_Gempa`, `Overlay_Gempa`) & `Footer_Copyright` dibersihkan, double-trigger tombol Inspector+runtime diperbaiki di `MainMenuManager` & `Level1QuizManager`, `isAnsweringLocked` dikunci saat VO02 intro & umpan balik salah, `Time.timeScale = 1f` + `StopVO()` dipastikan saat `RestartGame`/`GoToMainMenu`, kontrol BGM/SFX/VO di `Popup_PauseSettings` & `SoundManager` disambungkan ke `PlayerPrefs`, serta `HUD_BuGuru` otomatis muncul saat `ShowBuGuruVoice` / `AftershockTrigger`.
  - **Kunci sementara karakter Putri (demo pakai Putra):** `MainMenuManager.lockGirlCharacter = true` dan `PlayerGenderManager.lockGirlForDemo = true` hingga model 3D `girl` selesai diperbaiki.
  - **UI disamakan dengan `Tabel 3.7` (`progress_buku_pa_update1_deadline.docx`):** `MainMenuBuilderEditor.cs` + `MainMenu.unity` (Menu Utama tanpa footer, Pilih Karakter bersih tanpa subprompt/notice, Input Nama card krem-oranye + input biru + ucapan singkat Bu Guru, Pengaturan 3 baris bersih tanpa emoji & toggle pill `AKTIF`, Panduan 4 langkah aktual) serta `Level23SetupEditor.cs` + `MissionUI.cs` (Kuis buku tulis border biru seleksi, Hold progress `"Bertahan: X / 5.0 dtk"`, toast merah Bu Guru `"Gempa Susulan! Merunduk dulu ya!"`, Rapor Pahlawan Cilik 3 kotak Level 1–3 & bintang sprite tanpa emoji rusak). Slider jarak kamera tidak ditambahkan di Pengaturan MainMenu sesuai rencana implementasi *2-finger pinch-to-zoom* nantinya.
  - **Catatan Editor saat membuka kembali Unity:** Buka `LECVEL_NewMap.unity` sekali di Edit Mode agar `Level23SetupEditor.AutoSyncOnLoad()` meng-apply restyle kartu/sprite lengkap ke scene; jalankan Play Mode untuk uji alur lengkap.
- **Target terdekat**: Sidang Progres (menampilkan progres aplikasi + rencana pengujian), sedangkan pengujian akhir/evaluasi dilakukan pada **Sidang Evaluasi (April)**.
- **Dokumen Buku PA aktif**: `D:\KULIAH\TA\PROGGRES\buku\progress buku pa_update1.docx` (backup: `progress buku pa_update1_BACKUP_1524.docx`).
- **Project Unity aktif**: `D:\KULIAH\TA\PROGGRES\Unity\progress ta` (dan worktree `C:\Users\Lenovo\.gemini\antigravity\worktrees\progress ta\cek_status_urgensi`).
- **Scene Unity aktif**:
  - Build Index `0`: `Assets/Scenes/MainMenu.unity`
  - Build Index `1`: `Assets/Scenes/LECVEL_NewMap.unity`
- **Keputusan Fitur Penting**:
  - **Mekanik `BagPickup` dibatalkan / dihapus permanen** dari desain & simulasi Level 3. File legacy `BagPickup.cs` dan event `OnBagPickedUp` di `GameManager.cs` masih ada di codebase untuk dibersihkan sebelum Sidang Evaluasi.

---

## 2. Arsitektur, Konvensi Kode & Paket Unity

- **Versi / Stack**: Unity 6, Universal Render Pipeline (`universal`), New Input System (`inputsystem`), uGUI (`ugui`), AI Navigation (`ai.navigation`), ProBuilder (`probuilder`), Polybrush (`polybrush`), Timeline (`timeline`), Test Framework (`test-framework`).
- **Pola Arsitektur**:
  - Singleton `GameManager.Instance`, komunikasi berbasis event C#, *defensive null-checks*.
  - Atribut Inspector standar: `[Header]`, `[Tooltip]`, `[SerializeField]`; komentar kode berbahasa Indonesia.
- **Konvensi UI & Audio Khusus**:
  - **Tanpa Emoji di uGUI `Text`**: Font legacy uGUI tidak merender emoji berwarna (`⭐` menjadi kotak/kosong). Wajib gunakan simbol ASCII/Unicode dasar seperti `★` dan `☆`.
  - **Voice Over & Audio Ducking (`VoiceOverManager.cs`)**: Memutar 22 file audio di `Assets/Audio/VoiceOver/`, otomatis mengecilkan volume BGM (*Audio Ducking*) saat Bu Guru berbicara, mendukung *1-click Regular/Slow switcher* di Inspector, serta memicu animasi HUD avatar Bu Guru *slide-in* dari kiri ke kanan via `RectTransform`.

---

## 3. Peta Script Aktual (`Assets/Scripts/`)

### A. Runtime Scripts
- **Inti & Manajemen Game**: `GameManager.cs`, `SoundManager.cs`, `VoiceOverManager.cs`
- **Karakter, Kamera & Kontrol**: `BoyController.cs`, `ThirdPersonCamera.cs`, `VirtualJoystick.cs`, `CrouchButton.cs`, `PlayerGenderManager.cs` (dukungan karakter cowok & cewek `girl_rigged_pbr.fbx`), `PlayerNameBillboard.cs`
- **Menu & UI**: `MainMenuManager.cs` (termasuk pemilihan karakter & input nama siswa), `MissionUI.cs`, `AnimatedImageUI.cs`
- **Mekanik Level 1–3 & Bencana**:
  - **Level 1**: `Level1QuizManager.cs` — saat ini berisi **2 soal** (`questions = new QuizQuestion[2]`): Soal 1 animasi rumah (gempa/kebakaran/banjir via `AnimatedImageUI`), Soal 2 tebak audio/suara bencana.
  - **Level 2**: `EarthquakeEffect.cs`, `CheckpointTrigger.cs` (simulasi saat gempa / berlindung di bawah meja).
  - **Level 3**: `EvacuationGuide.cs`, `EvacuationStar.cs`, `AftershockTrigger.cs`, `ShelterZone.cs` (evakuasi ke titik aman).
- **Legacy / Akan Dihapus**: `BagPickup.cs` *(fitur sudah di-drop; bersihkan sebelum Sidang Evaluasi)*.

### B. Editor Tools (`Assets/Scripts/Editor/`)
- `DenahColliderSetup.cs`, `EvacuationGuideEditor.cs`, `EvacuationSetupEditor.cs`, `GenerateRoundedSprites.cs`, `Level23SetupEditor.cs`, `MainMenuBuilderEditor.cs`, `SelectionTracker.cs`, `SetupGirlCharacterEditor.cs`, `VirtualJoystickEditorSetup.cs`

---

## 4. Progress Buku PA (`progress buku pa_update1.docx`)

- **Hal-hal yang sudah dikerjakan (25 Nov 2026)**:
  - Penyesuaian cover (`PROGRES`) dan perbaikan istilah teori perkembangan kognitif Jean Piaget menjadi tahap **preoperational** (*pra-operasional*).
  - Penyesuaian format Bab 3 mengikuti standar buku penguji: style `paragraf`, `point`, `point angka`, `Caption`, *italic* untuk seluruh istilah asing/Inggris, serta **highlight kuning** pada semua teks yang ditambah/diubah AI agar mudah direview manual.
  - Penambahan argumen perbandingan **MDLC vs ADDIE** serta penguatan urgensi bencana gempa menggunakan konteks **BMKG/BNPB** dan gempa **Cianjur 2022**.
  - Penyusunan `Tabel 3.9 Skenario Black Box Testing`, subbab `3.3 JADWAL PENELITIAN`, dan `Tabel 3.10`.
  - Pembaruan redaksi **3.2.5 Testing**: dibingkai sebagai **rencana pengujian** untuk **Sidang Evaluasi bulan April** (bukan hasil uji yang sudah dieksekusi).
  - Integritas Mendeley terjaga penuh (`87/87` `SDT` citations dan `3/3` `customXml` utuh) dan hasil export PDF/PNG sudah dicek secara visual.
- **Mendeley & Sitasi Baru (Menunggu Sisip Manual di Word)**:
  - 2 referensi baru sudah ditambahkan ke Mendeley Desktop:
    1. **Hidayat (2023)** — DOI `10.46984/sebatik.v27i1.1379` (mitigasi gempa pada anak usia dini).
    2. **Istiqomah (2021)** — DOI `10.30595/jkp.v15i2.10974` (teori perkembangan kognitif Piaget anak usia dini).
  - **Belum disisipkan ke dokumen Word**: sisipkan manual lewat plugin Mendeley Cite di 2 titik target pada `progress buku pa_update1.docx`.

---

## 5. Catatan Bimbingan Dosen & Rencana Evaluasi April

### A. Poin Teknis & Akademik dari Bimbingan
- Pastikan justifikasi pemilihan metode **MDLC** dibanding **ADDIE** jelas dan konsisten.
- Perkuat latar belakang/urgensi dengan data kejadian gempa **BMKG/BNPB** serta dampaknya pada sekolah/anak usia dini (mis. Cianjur 2022).
- Opsi peningkatan teknis menuju Sidang Evaluasi: efek partikel/getaran gempa (*particle debris* / *haptic feedback* opsional) dan pembersihan sisa `BagPickup` di kode.

### B. Draf Kuesioner Evaluasi Guru TK (Untuk Sidang Evaluasi April)
Skala Likert 1–5 (1 = Sangat Tidak Setuju s.d. 5 = Sangat Setuju), mencakup 5 aspek:
- **Aspek A (Kesesuaian Materi & Kurikulum TK)**: ketepatan materi *Drop, Cover, Hold On*, kejelasan urutan Level 1–3, bahasa/instruksi ramah anak usia 4–6 tahun.
- **Aspek B (Kemudahan Penggunaan / Usability)**: kejelasan navigasi menu, responsivitas kontrol virtual joystick/tombol jongkok, keterbacaan visual/UI bagi anak.
- **Aspek C (Keterlibatan & Daya Tarik / Engagement)**: ketertarikan siswa pada karakter 3D (cowok/cewek), animasi, audio Voice Over Bu Guru, dan suasana kelas.
- **Aspek D (Efektivitas Edukasi Mitigasi)**: pemahaman siswa membedakan tanda gempa, cara berlindung saat gempa, dan jalur evakuasi setelah gempa.
- **Aspek E (Saran & Masukan Kualitatif)**: pertanyaan terbuka mengenai kendala saat uji coba dan bagian yang perlu diperbaiki.

---

## 6. Next Steps Bertahap

### Prioritas Dekat (Untuk Sidang Progres)
1. Sisipkan 2 sitasi Mendeley baru (`Hidayat 2023` & `Istiqomah 2021`) secara manual di Word pada `progress buku pa_update1.docx`, lalu refresh daftar pustaka Mendeley.
2. Lengkapi screenshot progres Unity & wireframe/gambar rancangan yang masih kosong pada Bab 3 sesuai template.
3. Review semua teks ber-**highlight kuning** di `progress buku pa_update1.docx` (bersama keputusan minor seperti penamaan/tabel di Bab 2/3 bila perlu), lalu hapus highlight jika sudah disetujui.

### Prioritas Lanjutan (Menuju Sidang Evaluasi April)
4. Bersihkan sisa file & kode legacy `BagPickup` (`BagPickup.cs` & event `OnBagPickedUp` di `GameManager.cs`) setelah dipastikan tidak dipakai scene.
5. Uji coba lengkap alur `MainMenu.unity` → `LECVEL_NewMap.unity` (Level 1–3, karakter cowok/cewek, nama siswa, VO Bu Guru, trigger gempa & evakuasi) dan tambah efek visual/getaran jika diperlukan.
6. Eksekusi skenario **Black Box Testing** (`Tabel 3.9`) dan penyebaran **Kuesioner Guru TK (Aspek A–E)**, lalu masukkan hasilnya ke Bab 4 / dokumen evaluasi.
