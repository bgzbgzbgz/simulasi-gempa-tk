# Catatan Bimbingan Dosen Pembimbing - Tugas Akhir

> **Judul TA:** Game Simulasi Edukasi Evakuasi Gempa Bumi untuk Anak TK  
> **Tanggal Bimbingan:** 17 November 2026  
> **Status:** Dicatat & Dipetakan ke Rencana Aksi Teknis & Akademis

---

## 📋 Poin Catatan Bimbingan & Rencana Aksi

Catatan bimbingan dibagi menjadi 2 Track: **Track Pengembangan Game (Teknis)** dan **Track Penulisan Laporan TA (Akademis)**.

---

### TRACK 1: PENGEMBANGAN GAME & UNITY (TEKNIS)

| No | Poin Catatan Dosen | Status di Project Saat Ini | Rencana Aksi Teknis |
|:--:|---|---|---|
| 1 | **Selesaikan mekanik levelnya** | Mekanik dasar L1, L2, L3 sudah ada scriptnya di `LECVEL_NewMap.unity` | • **L1:** TV visual quiz (animasi rumah) & trigger zona mulai.<br>• **L2:** Drop, Cover & Hold (masuk kolong meja tahan 5 detik) + gempa aktif.<br>• **L3:** Ambil tas sekolah (`BagPickup.cs`) → ikuti garis evakuasi NavMesh → picu gempa susulan di koridor (`AftershockTrigger.cs`) → sampai titik kumpul lapangan. |
| 2 | **Tambahkan karakter cewek** | Model 3D `girl.blend`, `girl_rigged_toon.fbx`, animasi (`Idle_girls`, `Walk_girl`, `Crouched Walking Girls`), dan tekstur muka sudah siap di folder `3d asset/` | • Tambahkan opsi pemilihan karakter (Boy / Girl) di `MainMenu` ATAU jadikan karakter siswi sebagai teman sekelas (NPC) yang ikut evakuasi.<br>• Setup prefab `GirlController` dengan Avatar & Animator Controller yang sudah ada. |
| 3 | **Vibes gempa kurang kerasa kalau cuma kamera getar (opsional)** | Saat ini hanya guncangan kamera (`EarthquakeEffect.cs`) & audio gemuruh | • **VFX/Partikel:** Debu langit-langit / partikel plester jatuh saat gempa.<br>• **Animasi Fisika Objek:** Lampu gantung / kipas kelas bergoyang.<br>• **Haptic/Getar Device:** Tambahkan getar HP (`Handheld.Vibrate()`) untuk build Android.<br>• **Post-Processing URP:** Efek sedikit Motion Blur / Vignette berkedip saat gempa memuncak. |
| 4 | **Tambah nama siswa (opsional)** | Belum ada input nama | • Tambahkan `InputField` sederhana di Main Menu (misal: "Ketik Namamu: [ ... ]").<br>• Tampilkan nama siswa di panel sertifikat/hasil akhir: *"Hebat, [Nama Siswa] Berhasil Evakuasi!"* + Bintang. |

---

### TRACK 2: PENULISAN LAPORAN TA & METODOLOGI (AKADEMIS)

---

#### 5. Kenapa Menggunakan MDLC (Multimedia Development Life Cycle)?
**Pertanyaan Dosen:** *"Knp MDLC? Karena pling..... (cari alasan yang kuat)"*

**Argumen Kuat untuk Laporan TA:**
1. **Spesifik untuk Produk Perangkat Lunak Multimedia Interaktif:**  
   MDLC (versi Luther-Sutopo) dirancang khusus untuk pengembangan aplikasi yang menggabungkan elemen grafis 2D/3D, audio narasi, animasi, dan interaktivitas program. Tidak seperti SDLC tradisional (Waterfall/Agile) yang berorientasi pada database/sistem informasi murni, MDLC memberikan porsi besar pada tahapan seni dan konten digital.
2. **Tahap *Material Collecting* yang Fleksibel & Paralel:**  
   Pengembangan game 3D membutuhkan banyak aset eksternal (3D modeling di Blender, texturing di Substance Painter, pembuatan audio voice over guru, rig animasi). MDLC mengakomodasi tahap *Material Collecting* secara eksplisit sebelum atau bersamaan dengan *Assembly* (penggabungan di Unity Engine).
3. **Iteratif Berbasis Pengujian Pengguna Anak Usia Dini:**  
   Pada tahap *Testing*, MDLC memungkinkan pengujian fungsionalitas (alpha testing oleh pengembang) dan pengujian pengguna (beta testing langsung pada anak TK dan guru). Jika anak kesulitan mengontrol joystick atau memahami instruksi, revisi dapat langsung dilakukan pada tahap *Design* atau *Assembly* tanpa harus merombak fondasi sistem.

---

#### 6. Komparasi: MDLC vs ADDIE (Menjawab Kenapa Terkesan ADDIE)
**Catatan Dosen:** *"Bandingkan MDLC dan EDI (ADDIE), coba perkuat argumen metode MDLC-nya karena kayak keliatan ADDIE dari tujuan yang kamu buat"*

**Analisis & Jawaban:**
* **Mengapa terlihat seperti ADDIE?**  
  Karena tujuan penelitian Anda melibatkan **anak TK (peserta didik)** dan **guru (evaluator)**, sehingga ada unsur analisis kebutuhan belajar dan evaluasi efektivitas media.
* **Perbedaan Mendasar:**

| Parameter | ADDIE (Analysis, Design, Development, Implementation, Evaluation) | MDLC (Concept, Design, Material Collecting, Assembly, Testing, Distribution) |
|---|---|---|
| **Fokus Utama** | Model Desain Instruksional / Kurikulum Pembelajaran | Model Rekayasa Produk Software Multimedia / Game |
| **Hasil Akhir** | Program pelatihan, modul ajar, rencana pembelajaran, atau media instruksional makro | Artefak perangkat lunak interaktif (Game Unity APK) dengan pipeline aset digital |
| **Tahap Produksi Teknis** | Tidak merinci pipeline teknis (hanya satu tahap: *Development*) | Memisahkan secara spesifik antara **Material Collecting** (Blender 3D, Audio, UI) dan **Assembly** (Integrasi Unity C#) |
| **Kesesuaian di TA Ini** | Terlalu luas dan lebih cocok untuk penelitian pendidikan murni / pedagogi | **Paling tepat** untuk Jurusan Teknologi/Komputer karena fokus utamanya adalah proses teknis rekayasa simulasi 3D interaktif. |

**Solusi Redaksi di Laporan TA:**  
Jelaskan bahwa **aspek pedagogi/tujuan edukasi bencana** dimasukkan sebagai landasan pada tahap **Concept** dan **Design** di MDLC, tetapi metodologi pengembangan sistemnya tetap **MDLC** karena output utama penelitian adalah artefak software game 3D Android.

---

#### 7. Penguatan Latar Belakang Gempa Bumi di Indonesia (Data Valid & Sumber Resmi)
**Catatan Dosen:** *"Perkuat latar belakang gempa di Indonesia (cari bukti yang valid)"*

**Poin-Poin Bukti Valid yang Harus Masuk di Bab 1:**
1. **Posisi Geologis Indonesia (Ring of Fire):**  
   Indonesia berada di pertemuan 3 lempeng tektonik aktif dunia: Lempeng Indo-Australia, Lempeng Eurasia, dan Lempeng Pasifik, serta berada di sepanjang Cincin Api Pasifik (*Pacific Ring of Fire*).
2. **Data Statistik BMKG (Badan Meteorologi, Klimatologi, dan Geofisika):**  
   - Rata-rata terjadi **7.000 – 10.000+ kali gempa bumi per tahun** di Indonesia (dengan ratusan gempa berkekuatan di atas M 5.0 yang dirasakan dan berpotensi merusak).
3. **Data BNPB (Badan Nasional Penanggulangan Bencana) & Kerentanan Sekolah:**  
   - Data BNPB menunjukkan ribuan fasilitas pendidikan rusak akibat gempa bumi besar dalam dekade terakhir (misal: Gempa Lombok 2018, Palu 2018, dan Cianjur 2022).
   - Pada Gempa Cianjur 2022 (M 5.6), sebagian besar korban jiwa adalah **anak-anak sekolah**, karena gempa terjadi saat jam belajar siang hari di gedung sekolah.
4. **Kerentanan Spesifik Anak Usia Dini (TK / 5–6 Tahun):**  
   - Teori perkembangan kognitif Piaget: Anak usia 5–6 tahun berada pada tahap pra-operasional konkret; mereka **belum bisa mencerna instruksi teks abstrak** dan mudah mengalami kepanikan (*freeze / panic reflex*).
   - Edukasi konvensional (ceramah / buku bacaan) kurang efektif membentuk memori otot (*muscle memory*). Simulasi visual 3D interaktif berbasis tindakan (*Drop, Cover, Hold On*) memberikan pengalaman kinestetik yang membangun refleks bertahan hidup sejak dini.

---

#### 8. Draft Kuesioner untuk Guru-Guru TK
**Catatan Dosen:** *"Klo survey lagi kasih kuisioner terkait projek buat ditanyain ke guru'guru"*

**Struktur Kuesioner Evaluasi Ahli Pengguna (Guru TK):**
Menggunakan Skala Likert (1: Sangat Tidak Setuju, 2: Tidak Setuju, 3: Cukup, 4: Setuju, 5: Sangat Setuju).

##### A. Aspek Materi Edukasi Kebencanaan
1. Apakah materi pengenalan gempa bumi (Level 1) sesuai dengan daya tangkap anak TK?
2. Apakah tindakan berlindung di bawah meja / Drop, Cover, Hold On (Level 2) mudah dipahami sebagai tindakan penyelamatan mandiri?
3. Apakah alur evakuasi menuju lapangan terbuka (Level 3) sesuai dengan SOP keselamatan di sekolah?

##### B. Aspek Tampilan Visual & Karakter
4. Apakah model 3D dan lingkungan sekolah TK menarik dan ramah bagi anak-anak?
5. Apakah animasi karakter dan visualisasi gempa bumi terlihat jelas tanpa menimbulkan rasa takut/trauma berlebihan bagi anak?
6. Apakah penggunaan karakter siswa dan siswi membantu keterlibatan anak dalam bermain?

##### C. Aspek Audio & Voice Over
7. Apakah instruksi suara Bu Guru terdengar jelas, ramah, dan mudah diikuti oleh anak usia TK?
8. Apakah efek suara (SFX gemuruh gempa, alarm) cukup memberikan suasana tanpa membuat anak panik?

##### D. Aspek Kemudahan Penggunaan (Usability untuk Anak TK)
9. Apakah kontrol joystick layar sentuh dan tombol aksi mudah dioperasikan oleh anak?
10. Apakah durasi dan tempo permainan sesuai dengan rentang fokus anak usia 5–6 tahun?

##### E. Saran Terbuka
- *"Apa saran atau masukan Bapak/Ibu guru agar game simulasi ini lebih efektif diterapkan sebagai media pembelajaran mitigasi bencana di TK?"*

---

*File ini tersimpan di: `CATATAN_BIMBINGAN_DOSEN.md` dan ditautkan ke `PROGRESS.md`.*
