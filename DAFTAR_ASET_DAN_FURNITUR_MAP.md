# Daftar Kebutuhan Objek, Properti, dan Furnitur Map Lingkungan TK

> Dokumen acuan aset lingkungan 3D untuk **Game Simulasi Edukasi Evakuasi Gempa Bumi untuk Anak TK**.  
> Disusun untuk memastikan lingkungan sekolah TK tampak hidup, ramah anak, dan realistis, sekaligus menjaga performa stabil (60 FPS) pada build **Android APK**.

---

## 1. Ruang Kelas TK (Area Level 1 & Level 2)

Fokus area: Kuis visual identifikasi bencana gempa (Level 1) dan perlindungan diri *Drop, Cover & Hold On* di kolong meja (Level 2).

| Kategori | Nama Objek / Properti | Fungsi / Peran | Catatan Setup & Collider |
|---|---|---|---|
| **Wajib** | **Meja Siswa (Model Kolong Terbuka)** | Tempat sembunyi / Shelter Zone saat gempa | Box Collider di papan atas & kaki meja (kolong kosong agar bisa dimasuki). |
| **Wajib** | **Kursi Siswa Kecil** | Duduk murid (warna-warni plastik/kayu) | Box Collider sederhana, diselipkan di dekat meja. |
| **Wajib** | **Meja & Kursi Guru** | Area depan kelas dekat papan tulis | Box Collider. |
| **Wajib** | **Papan Tulis (Whiteboard / Greenboard)** | Media ajar di dinding depan | Menempel di dinding, tanpa collider atau thin Box Collider. |
| **Wajib** | **Smart TV / Layar Monitor** | Media visual kuis kenali bencana (Level 1) | Menempel di dinding sekat. |
| **Dekorasi** | **Loker / Rak Tas & Sepatu Siswa** | Loker kayu kotak-kotak khas TK di dinding samping/belakang | Box Collider di badan luar rak. |
| **Dekorasi** | **Karpet Bermain (Playmat Busa)** | Sudut baca / bermain anak dengan warna-warni ceria | Bidang datar / Plane di atas lantai (No Collider). |
| **Dekorasi** | **Poster Edukasi & Hiasan Dinding** | Poster Abjad A–Z, Angka, Peta Indonesia, Gambar Anak, Rambu Tanggap Gempa | Quad tipis menempel di dinding (No Collider). |
| **Dekorasi** | **Rak Buku Cerita & Kotak Balok/Mainan** | Kotak plastik transparan berisi balok susun di pojok kelas | Box Collider sederhana. |
| **Dekorasi** | **Jam Dinding** | Jam bulat di dinding kelas | No Collider (bisa digoyang saat gempa). |
| **Dekorasi** | **Lampu Gantung / Kipas Angin Langit-Langit** | Menggantung di plafon | No Collider (efek ayun saat EarthquakeEffect aktif). |

---

## 2. Koridor / Selasar Kelas

Fokus area: Jalur keluar pertama murid dari dalam kelas menuju area terbuka.

| Kategori | Nama Objek / Properti | Fungsi / Peran | Catatan Setup & Collider |
|---|---|---|---|
| **Wajib** | **Papan Rambu "Jalur Evakuasi" + Panah Hijau** | Edukasi SOP evakuasi anak TK | Menempel di atas kusen pintu keluar & dinding koridor (No Collider). |
| **Wajib** | **Pintu Kelas Terbuka Keluar** | Pintu terbuka ke arah koridor (SOP mitigasi gempa) | Box Collider pada daun pintu yang terbuka. |
| **Dekorasi** | **Rak Sepatu Susun** | Tempat sepatu anak sebelum masuk kelas | Diletakkan merapat ke dinding luar (Box Collider). |
| **Dekorasi** | **Wastafel Cuci Tangan Rendah** | Wastafel ramah anak di selasar | Diletakkan di pinggir selasar (Box Collider). |
| **Dekorasi** | **Mading (Majalah Dinding)** | Papan pengumuman & pajangan karya anak | Menempel di tembok selasar (No Collider). |
| **Dekorasi** | **Pot Tanaman Gantung / Pot Bunga** | Mempercantik tepian selasar | Di pinggir pagar pembatas (No Collider). |
| **Dekorasi** | **Tempat Sampah 3 Warna** | Organik, anorganik, B3 (kuning, hijau, merah) | Di dekat pintu keluar koridor (Box Collider). |

---

## 3. Jalan Gang Berkelok (Menuju Lapangan)

Fokus area: Rute navigasi luar ruangan (Level 3) dari sekolah menuju titik kumpul lapangan terbuka.

| Kategori | Nama Objek / Properti | Fungsi / Peran | Catatan Setup & Collider |
|---|---|---|---|
| **Wajib** | **Plang Penunjuk Titik Kumpul** | Papan panah petunjuk arah di belokan gang | Menempel di dinding/pagar gang (No Collider). |
| **Wajib** | **Jalur Evakuasi Bersih** | Area jalan utama untuk lintasan NavMesh | Bebas dari rintangan collider agar tidak menghambat karakter. |
| **Dekorasi** | **Tiang Listrik & Kabel Menggantung** | Nuansa gang perkampungan / sekolah Indonesia | Tiang di sudut gang (Capsule Collider), kabel di atas (No Collider). |
| **Dekorasi** | **Pagar & Gerbang Rumah Warga** | Variasi dinding semen, bata merah, atau pagar besi | Box Collider statis. |
| **Dekorasi** | **Pot Tanaman Warga** | Berjejer di sepanjang kaki tembok gang | No Collider atau Box Collider kecil. |
| **Dekorasi** | **Plang Nama Sekolah TK** | Plang nama "TK 'Aisyiyah Bustanul Athfal" di dekat gerbang luar | Box Collider. |
| **Dekorasi** | **Sepeda Anak / Gerobak di Tepi** | Mengisi sudut mati gang agar tidak kosong | Diletakkan rapat ke pinggir (Box Collider). |

---

## 4. Lapangan Titik Kumpul (Area Terbuka Level 3)

Fokus area: Titik akhir evakuasi yang aman dari reruntuhan bangunan atau pohon roboh.

| Kategori | Nama Objek / Properti | Fungsi / Peran | Catatan Setup & Collider |
|---|---|---|---|
| **Wajib** | **Plang Rambu Titik Kumpul (Assembly Point)** | Rambu hijau resmi berlogo 4 panah mengarah ke tengah | Terpasang tegak di area titik kumpul (Box Collider). |
| **Wajib** | **Garis Marka Kotak di Lapangan** | Batas area kumpul murid di atas paving/rumput | Decal / Quad pipih di atas tanah (No Collider). |
| **Wajib** | **Karakter Bu Guru (NPC Penanda)** | Pemandu barisan murid di titik kumpul | Posisi di dalam zona titik kumpul. |
| **Dekorasi** | **Tiang Bendera Merah Putih** | Khas halaman sekolah TK Indonesia | Terletak di sisi lapangan (Cylinder Collider kecil). |
| **Dekorasi** | **Playground Outdoor TK** | Ayunan, perosotan, jungkat-jungkit | Di tepi lapangan, jauh dari titik kumpul (Box Collider). |
| **Dekorasi** | **Pohon Rindang di Tepi Pagar** | Peneduh di sisi luar lapangan | Di tepi luar agar tidak mengganggu zona aman kumpul. |
| **Dekorasi** | **Bangku Taman Panjang** | Tempat duduk di pinggir lapangan | Diletakkan rapat di sisi pagar (Box Collider). |

---

## 5. Panduan Teknis & Optimasi Mobile Android

1. **Collider Hygiene:**
   - Semua benda dekoratif kecil (poster dinding, hiasan gantung, buku, pot kecil, tanaman) **wajib tanpa collider (No Collider)**.
   - Furnitur besar (meja, loker, dinding, pagar) gunakan **Box Collider** sederhana. Hindari *Mesh Collider* kompleks.
2. **Kamera Third-Person:**
   - Jaga koridor dan gang tetap lapang dengan tinggi plafon/kabel yang aman agar kamera `ThirdPersonCamera` tidak terbentur dan mendadak *zoom-in*.
3. **NavMesh & Line Renderer:**
   - Jalur evakuasi (`EvacuationGuide`) memerlukan lintasan lantai yang rata dan bebas collider statis yang menghalangi pergerakan AI NavMesh.
4. **Draw Call & Atlas Material:**
   - Gabungkan palet warna cerah mainan, poster, dan furnitur TK ke dalam **Texture Atlas** bersama agar dapat di-*batching* optimal pada URP Mobile.
