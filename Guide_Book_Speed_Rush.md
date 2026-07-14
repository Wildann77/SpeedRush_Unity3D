# Manual Book
## Game Racing Speed Rush 3D

---

### DAFTAR ISI

1. [Pendahuluan](#1-pendahuluan)
2. [Langkah Penginstalan dan Menjalankan Game](#2-langkah-penginstalan-dan-menjalankan-game)
3. [Pengenalan Game](#3-pengenalan-game)
   - 3.1 [Logo Game](#31-logo-game)
   - 3.2 [Menu Utama (MainMenu)](#32-menu-utama-mainmenu)
   - 3.3 [Menu Pilihan Level (Level Select)](#33-menu-pilihan-level-level-select)
   - 3.4 [Tampilan HUD & Level Game](#34-tampilan-hud--level-game)
   - 3.5 [Fitur Anti-Stuck & Auto-Flip](#35-fitur-anti-stuck--auto-flip)
4. [Cara Main Game](#4-cara-main-game)
5. [Dokumen Teknis](#5-dokumen-teknis)
   - 5.1 [High Concept Statement](#51-high-concept-statement)
   - 5.2 [Latar Belakang Game](#52-latar-belakang-game)
   - 5.3 [Mekanik dan Peran Pemain](#53-mekanik-dan-peran-pemain)
   - 5.4 [Genre & Mode](#54-genre--mode)
   - 5.5 [Penggalan Kode Sumber (Source Code Snippet)](#55-penggalan-kode-sumber-source-code-snippet)

---

### DAFTAR GAMBAR

* Gambar 3. 1 Logo Speed Rush 3D
* Gambar 3. 2 Tampilan Kamera dan Mobil Level 2
* Gambar 3. 3 Tampilan HUD Utama Level 1
* Gambar 3. 4 Tampilan Menu Pause
* Gambar 4. 1 Status Lintasan & Koleksi Koin
* Gambar 5. 1 Struktur Kode SR_GameManager.cs

---

### 1. Pendahuluan

![Logo Speed Rush 3D](file:///mnt/windows/Users/boyblanco/Documents/SpeedRush1/Assets/logo_speed_rush_320x180.png)
*Gambar 3. 1 Logo Speed Rush 3D*

**Speed Rush 3D** adalah permainan balap mobil 3D bertema arcade time-attack dengan grafis poli-rendah (low-poly) yang mengharuskan pemain mengendarai mobil sport (seperti model Mustang PolyStang) melewati serangkaian checkpoint sebelum waktu hitung mundur habis.

Setiap checkpoint memberikan bonus tambahan waktu untuk membantu pemain menyelesaikan seluruh putaran (laps) lintasan. Di sepanjang trek, terdapat berbagai item koleksi (Collectibles) seperti Koin untuk mengumpulkan skor, Speed Boost (Nitro) untuk dorongan kecepatan instan, dan Extra Time untuk menambah waktu. Game ini dikembangkan menggunakan Unity Engine dengan sistem fisika kendaraan yang stabil (downforce, anti-roll, dan grip yang disempurnakan) agar memberikan sensasi berkendara yang responsif sekaligus menantang.

---

### 2. Langkah Penginstalan dan Menjalankan Game

Saat ini game sedang dikembangkan dan dapat dijalankan langsung di dalam Unity Editor atau dibangun (Build) ke platform PC (Windows/Linux) dan Android (Mobile).

#### Cara Menjalankan di Unity Editor (Development):
1. Buka Unity Hub dan pilih proyek **SpeedRush_Unity3D** (atau arahkan ke direktori `/mnt/windows/Users/boyblanco/Documents/SpeedRush1`).
2. Gunakan Unity Editor versi yang sesuai (disarankan Unity 2022 LTS / Unity 6).
3. Di dalam panel *Project*, masuk ke folder `Assets/Scenes` lalu klik dua kali pada `MainMenu.unity`.
4. Tekan tombol **Play** di bagian atas Unity Editor untuk memulai permainan.

#### Cara Menginstal File APK di Android:
1. Lakukan Build melalui Unity Editor (`File -> Build Settings`, pilih platform **Android**, lalu klik **Build** untuk menghasilkan file `.apk`).
2. Transfer file `.apk` yang dihasilkan ke perangkat Android Anda.
3. Di perangkat Android, masuk ke **Pengaturan (Settings)** -> **Keamanan & Privasi (Security & Privacy)**.
4. Aktifkan opsi **Instal aplikasi dari sumber tidak dikenal (Install from unknown apps)** pada browser atau file manager yang Anda gunakan.
5. Buka file `.apk` tersebut di file manager Android, klik **Install**, dan tunggu hingga selesai.
6. Jalankan aplikasi **Speed Rush** dari laci aplikasi (App Drawer) Anda.

---

### 3. Pengenalan Game

#### 3.1 Logo Game
Logo resmi game dapat dilihat pada **Gambar 3.1** yang merepresentasikan tema balap cepat bernuansa arcade modern.

#### 3.2 Menu Utama (MainMenu)
Ketika game pertama kali dijalankan, pemain akan disambut oleh Menu Utama yang memiliki 3 panel navigasi dinamis:
1. **Mulai (Play / Mulai)**: Mengarahkan pemain ke panel pemilihan level (Level Select).
2. **Pengaturan (Settings)**: Menyediakan slider volume musik untuk mengatur BGM secara real-time.
3. **Keluar (Quit)**: Menutup aplikasi game (pada build runtime) atau menghentikan mode simulasi (di Unity Editor).

#### 3.3 Menu Pilihan Level (Level Select)
Pada menu ini, pemain dapat memilih level yang ingin dimainkan:
* **Level 1**: Lintasan awal. Terbuka secara default untuk melatih kontrol berkendara dasar pemain.
* **Level 2**: Lintasan tingkat lanjut dengan tikungan tajam. Level ini terkunci secara default dan baru akan terbuka setelah pemain berhasil menyelesaikan Level 1 (status tersimpan secara persisten menggunakan `PlayerPrefs`).

#### 3.4 Tampilan HUD & Level Game

![Tampilan HUD Level 1](file:///mnt/windows/Users/boyblanco/Documents/SpeedRush1/Assets/Screenshots/L1_UI_only.png)
*Gambar 3. 3 Tampilan HUD Utama Level 1*

Di dalam game, pemain disuguhkan antarmuka HUD (Heads-Up Display) yang informatif sebagai berikut:
1. **Timer (Waktu)**: Menampilkan sisa waktu dalam format `MM:SS`. Waktu awal adalah 40 detik. Sebelum mobil digerakkan pertama kali (setelah fase hitung mundur selesai), timer berada dalam status **(READY)** dan tidak berkurang. Setelah ada input gas/kemudi dari pemain, timer akan mulai menghitung mundur secara aktif.
2. **Score (Skor)**: Menunjukkan akumulasi poin dari koin yang dikumpulkan selama balapan.
3. **Lap Info (Putaran)**: Menampilkan putaran saat ini dan total putaran yang harus diselesaikan (misalnya `Lap: 1/1`).
4. **Checkpoint Info (CP)**: Menampilkan nama checkpoint terakhir yang berhasil dilewati (misalnya `CP: 1` atau `CP: Finish Line`).
5. **Speedometer (Indikator Kecepatan)**: Terintegrasi pada bagian visual bawah untuk menunjukkan kecepatan mobil saat ini secara real-time.

Pemain juga dapat menghentikan game sementara dengan menekan tombol **Pause** merah di kiri bawah layar (atau menekan tombol **Escape** pada keyboard).

![Tampilan Menu Pause](file:///mnt/windows/Users/boyblanco/Documents/SpeedRush1/Assets/Screenshots/L1_pausepanel.png)
*Gambar 3. 4 Tampilan Menu Pause*

Di dalam panel Pause, terdapat opsi:
* **Lanjutkan (Resume)**: Melanjutkan kembali balapan.
* **Ulangi (Restart)**: Mengulang lintasan dari awal.
* **Menu Utama (Main Menu)**: Kembali ke layar utama game.
* **Music Slider**: Mengatur keras/lemah volume musik latar balapan.

#### 3.5 Fitur Anti-Stuck & Auto-Flip
Game balapan 3D rentan terhadap mobil terbalik atau tersangkut di dinding pembatas lintasan. Oleh karena itu, Speed Rush 3D memiliki fitur kenyamanan pemain:
1. **Warning UI Reset**: Jika mobil terbalik (kemiringan ekstrim) atau tersangkut di rintangan selama lebih dari **2.5 detik**, teks peringatan **"TEKAN 'R' UNTUK RESET MOBIL"** akan berkedip di layar.
2. **Reset Manual**: Pemain dapat menekan tombol **'R'** kapan saja untuk mereset mobil kembali ke posisi tegak di lintasan aman dekat checkpoint terakhir.
3. **Auto-Flip Otomatis**: Jika mobil tersangkut selama **5.0 detik** tanpa tindakan dari pemain, sistem akan otomatis menegakkan kembali rotasi mobil dan menempatkannya sedikit di atas permukaan aspal untuk menghindari ban terselip di bawah mesh jalan.

---

### 4. Cara Main Game

![Lintasan Level 2](file:///mnt/windows/Users/boyblanco/Documents/SpeedRush1/Assets/Screenshots/level2_position_check.png)
*Gambar 4. 1 Status Lintasan & Koleksi Koin*

1. Buka game, klik **Mulai** pada Menu Utama, lalu pilih **Level 1**.
2. Sebelum balapan dimulai, sistem akan menjalankan fase **Start Countdown (3... 2... 1... GO!)** secara visual di layar sementara mobil dan kontrol dikunci agar pemain bersiap.
3. Setelah hitung mundur selesai, game berada pada status **(READY)**. Tekan kontrol gas untuk mulai berkendara; timer level akan otomatis mulai menghitung mundur secara aktif begitu ada input pertama dari pemain.
4. **Kontrol Karakter Mobil (PC / Keyboard)**:
   * **W** / **Panah Atas**: Gas (Akselerasi Maju).
   * **S** / **Panah Bawah**: Rem / Mundur.
   * **A** / **Panah Kiri**: Belok Kiri.
   * **D** / **Panah Kanan**: Belok Kanan.
   * **Left Shift**: Rem Tangan (Handbrake / Drift).
   * **R**: Reset posisi mobil ke checkpoint terakhir jika tersangkut.
5. **Kontrol Karakter Mobil (Mobile / Android)**:
   * Menggunakan tombol virtual kemudi (kiri & kanan) dan slider/tombol gas di layar.
6. **Aturan Checkpoint**:
   * Pemain harus melewati checkpoint berurutan (misal: Checkpoint_1, Checkpoint_2, dst) sebelum bisa melewati garis Finish untuk menyelesaikan putaran.
   * Pemain **tidak boleh melakukan kecurangan (shortcut)**; minimal **70%** dari total checkpoint normal di lintasan harus dilewati agar putaran lap dianggap sah saat menyentuh garis Finish.
7. **Mengambil Item (Collectibles)**:
   * **Koin Kuning**: Memberikan bonus **+10 Skor**.
   * **Extra Time (Ikon Jam)**: Memberikan tambahan **+5 detik** waktu langsung ke timer.
   * **Speed Boost (Nitro)**: Memberikan akselerasi instan berkali-lipat selama **3 detik** dengan efek dorongan fisik ke depan.
8. **Kondisi Menang**:
   * Menyelesaikan jumlah putaran lintasan (totalLaps) sebelum waktu habis. Kemenangan akan membuka akses ke level berikutnya.
9. **Kondisi Kalah**:
   * Jika waktu di bagian timer habis (mencapai `00:00`), maka game over dan pemain harus mengulangi level tersebut.

---

### 5. Dokumen Teknis

#### 5.1 High Concept Statement
**Speed Rush 3D** adalah game balap arcade time-attack 3D bersudut pandang orang ketiga (third-person) yang memadukan kecepatan berkendara presisi dengan tantangan manajemen waktu. Menggunakan mobil sport dengan fisika suspensi realistis, pemain dituntut mengumpulkan koin dan item penambah waktu di sepanjang sirkuit jalan raya untuk memecahkan rekor putaran tercepat.

#### 5.2 Latar Belakang Game
Balapan di sirkuit perkotaan modern yang penuh tantangan tikungan dinamis. Pemain berperan sebagai pembalap profesional yang berpacu melawan waktu. Kecepatan dan ketepatan memilih jalur balap (racing line) sangat menentukan kelulusan level untuk membuka sirkuit baru yang lebih menantang.

#### 5.3 Mekanik dan Peran Pemain
* **Fisika Suspensi & Ban**: Kestabilan mobil dioptimalkan menggunakan gaya tekan ke bawah (*downforce*), kekakuan ban (*sideways/forward stiffness*), dan *anti-roll bar* untuk mencegah mobil melayang atau melintir saat belok kencang.
* **Sistem Kamera Dinamis**: Kamera mengikuti mobil secara otomatis dari belakang menggunakan komponen *Cinemachine Transposer & Composer* dengan peredaman (damping) gerakan halus.
* **Manajemen Checkpoint & Lap**: Pemantauan checkpoint yang ketat dengan penghitungan persentase minimal kelulusan lintasan (70% checkpoint terlewati) guna memastikan integritas jalur balap.

#### 5.4 Genre & Mode
* **Genre**: 3D Arcade Racing / Time-Attack.
* **Mode**: Single Player.

#### 5.5 Penggalan Kode Sumber (Source Code Snippet)
Berikut adalah struktur dasar pengelolaan alur permainan dalam script `SR_GameManager.cs`:

```csharp
// Source Code: SR_GameManager.cs
namespace SpeedRush
{
    public class SR_GameManager : MonoBehaviour
    {
        public static SR_GameManager Instance { get; private set; }

        [Header("Game Settings")]
        public float initialTime = 40f;
        public int totalLaps = 1;
        public string nextLevelName = "MainMenu";

        private float timeRemaining;
        private int score;
        private int currentLap = 1;
        private bool isGameActive = false;
        
        // ... inisialisasi dan logika update waktu ...
        private void Update()
        {
            if (!isGameActive) return;

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCarToLastCheckpoint();
            }

            if (hasStartedDriving)
            {
                if (timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;
                    if (timeRemaining <= 0)
                    {
                        timeRemaining = 0;
                        GameOver(false);
                    }
                }
            }
            UpdateUI();
        }
    }
}
```

Script lengkap dan terstruktur dapat diakses di repositori proyek Unity pada folder `Assets/Scripts/`.
