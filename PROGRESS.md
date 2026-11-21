# Progress & Handover Lengkap - Game Simulasi Edukasi Gempa TK

> **Single Source of Truth** status pengerjaan, aset Unity, Blender, audio, dan riwayat progress terkini.
> Setiap sesi baru dibuka, file ini dibaca pertama kali agar AI langsung tahu kondisi riil seluruh project.

---

## 📌 Ringkasan Status Project
- **Terakhir Diperbarui:** 21 November 2026
- **Overall Progress Spreadsheet:** 84.3% (10 Selesai, 3 Sedang Dikerjakan, 1 Belum Mulai - Target Sidang 80% Terlampaui!)
- **Lokasi Utama Project Unity:** `D:\KULIAH\TA\PROGGRES\Unity\progress ta`
- **Lokasi Mentahan 3D / Blender:** `D:\KULIAH\TA\PROGGRES\3d asset`
- **Dokumen & Konsep TA:** `D:\KULIAH\TA\PROGGRES`
- **Engine:** Unity 6 (`6000.3.26f1`), URP 17.3, C# (Target Android Landscape)
- **Scene Aktif Terbaru:**
  1. `Assets/Scenes/MainMenu.unity` (Build Index 0 - Menu Utama Playable dengan modal karakter & setting)
  2. `Assets/Scenes/LECVEL_NewMap.unity` (Build Index 1 - Gameplay Utama L1-L3 Playable Sempurna 100/100 Poin)

---

## 📂 Status Aset Blender & 3D Model (`D:\KULIAH\TA\PROGGRES\3d asset`)
1. **Denah Lingkungan TK:**
   - File master: `denah2.blend`, `denah_BACKUP.blend`, `denah2_game_ready.glb`.
   - Ekspor FBX ke Unity: `denah_lengkap.fbx`, `denah_v2.fbx`, `Environment_Buildings_Baru.fbx`.
2. **Karakter & Prop:**
   - `boy.blend` → `boy.fbx`, `boy_substance.fbx` (Karakter siswa laki-laki utama, rigged & textured).
   - `girl.blend` → `girl_rigged_toon.fbx` (Karakter siswi perempuan untuk NPC/opsi).
   - `tas.blend` → `backpack.fbx` (Tas sekolah anak TK untuk mekanik lindungi kepala).
   - `prop.blend`, `rumah.blend`, `#DOR0001_Wood_Window.blend`.
3. **Animasi Karakter:**
   - Siswa: `ANIMASI MENENG.fbx` (Idle), `Walking.fbx`, `Running.fbx`, `Crouched Walking.fbx`.
   - Siswi: `Idle_girls.fbx`, `Walk_girl.fbx`, `Crouched Walking Girls.fbx`, `Running Jump.fbx`.
4. **Animasi Kuis TV (Bencana):**
   - Render simulasi: `rumah gempa.mp4/.gif`, `rumah banjir.mp4/.gif`, `rumah kebakaran.mp4/.gif`.
   - Sudah diekstrak menjadi frame PNG sequence di Unity: `Assets/Sprites/Quiz/Quake/`, `Fire/`, dan `Flood/`.

---

## 🎮 Status Fitur & Script Unity (`Assets/Scripts/`)
1. **Alur Level & GameManager:**
   - `MainMenuManager.cs`: Tombol Mulai Simulasi langsung meload `LECVEL_NewMap`.
   - `GameManager.cs`: Alur Level 1 → Level 2 → Level 3, hitung stopwatch, bintang (1-3), dan skor kuis.
   - Panel kuis Level 1 disembunyikan saat start; dipicu ketika pemain mendekati zona TV (`Mat_ZonaMulai.mat`).
2. **Kuis Visual TV Level 1 (`Level1QuizManager.cs`, `AnimatedImageUI.cs`):**
   - Menggunakan sistem frame looping `AnimatedImageUI` untuk kartu animasi rumah berguncang.
   - Aset kartu: `card_quake.png`, `card_flood.png`, `card_fire.png` + audio card.
3. **Mekanik Level 2 (Drop, Cover, Hold On):**
   - `EarthquakeEffect.cs`: Guncangan kamera, overlay gempa merah/gelap, suara gemuruh.
   - `ShelterZone.cs` & `CheckpointTrigger.cs`: Masuk kolong meja otomatis merunduk, tahan 5 detik.
4. **Mekanik Level 3 (Evakuasi & Aftershock):**
   - `BagPickup.cs`: Mengambil tas sekolah untuk ditaruh di atas kepala saat lari keluar.
   - `AftershockTrigger.cs`: Trigger gempa susulan ringan di area koridor menuju lapangan.
   - `EvacuationGuide.cs`: Garis pemandu NavMesh dari posisi pemain ke titik kumpul lapangan.
5. **Sistem Suara & Audio (`SoundManager.cs`, `Assets/Audio/`):**
   - SFX siap pakai: `sfx_alarm_siren.wav`, `sfx_earthquake_rumble.wav`, `sfx_bag_pickup.wav`, `sfx_level_complete.wav`, `sfx_game_win.wav`, `sfx_water_flood.wav`, `sfx_button_click.wav`.
   - Dokumen Voice Over Bu Guru: `NASKAH_VOICE_OVER_GURU_TK.docx` (11 script VO dari Intro sampai Ending).

---

## 📝 Catatan Bimbingan Dosen Pembimbing (17 November 2026)
*Detail lengkap, argumen metodologi, data gempa, dan kuesioner guru tersimpan di: [CATATAN_BIMBINGAN_DOSEN.md](file:///C:/Users/Lenovo/.gemini/antigravity/worktrees/progress%20ta/tes/CATATAN_BIMBINGAN_DOSEN.md)*

### Track 1: Teknis Game & Unity
1. **Selesaikan Mekanik Level:** Finalisasi alur L1 (TV Quiz), L2 (Drop-Cover di meja), L3 (Tas + Aftershock + Titik Kumpul).
2. **Karakter Cewek:** Model `girl_rigged_toon.fbx` & animasi sudah siap di `3d asset/`. Tinggal pasang sebagai opsi karakter atau NPC kelas.
3. **Vibes Gempa Lebih Terasa (Opsional):** Tambah getar HP (`Handheld.Vibrate()`), partikel debu/reruntuhan langit-langit, dan benda goyang/jatuh.
4. **Input Nama Siswa (Opsional):** Field nama siswa di Main Menu / panel sertifikat bintang akhir.

### Track 2: Akademis & Laporan TA
5. **Alasan Kuat Pemilihan MDLC:** Software multimedia interaktif 3D; fleksibilitas tahap *Material Collecting* (Blender, VO, UI) dan *Assembly* (Unity); iteratif pada pengujian anak TK.
6. **MDLC vs ADDIE:** ADDIE adalah desain instruksional kurikulum umum, sedangkan MDLC adalah rekayasa produk multimedia. Tujuan edukatif diserap di tahap *Concept/Design* MDLC.
7. **Latar Belakang Gempa Valid:** Ring of Fire, data tahunan gempa BMKG (7.000-10.000 kejadian/tahun), data BNPB korban anak sekolah pada gempa Cianjur 2022, dan urgensi refleks bertahan hidup anak TK.
8. **Kuesioner Guru TK:** Kuesioner evaluasi aspek materi, visual/audio, usability anak, dan efektivitas pembelajaran mitigasi.

---

## 🎯 Target Pengerjaan Aktif (Next Steps)
1. **[Teknis - Prioritas 1] Selesaikan Mekanik Level di `LECVEL_NewMap`:**
   - Sambungkan `BagPickup.cs` (ambil tas di Level 3) & `AftershockTrigger.cs` (gempa susulan di koridor).
   - Pastikan NavMesh ter-bake dan `EvacuationGuide` mengarah ke titik kumpul.
2. **[Teknis - Prioritas 2] Tambah Karakter Cewek:**
   - Buat prefab karakter siswi menggunakan `girl_rigged_toon.fbx` & setup Animator Controller.
3. **[Teknis - Prioritas 3] Audio VO Guru & Vibes Gempa:**
   - Pasang file Voice Over Bu Guru (`VO_01` s/d `VO_11`) dan tambahkan efek getar HP / partikel gempa.
4. **[Akademis] Integrasi Argumen Dosen ke Naskah Laporan TA:**
   - Masukkan argumen MDLC vs ADDIE, data BMKG/BNPB, dan kuesioner guru ke Bab 1, 2, dan 3.
