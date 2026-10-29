# ClipClick

**ClipClick** adalah aplikasi desktop Windows modern, bersih, minimalis, dan sangat ringan yang menggabungkan **Clipboard Manager canggih**, **Pemutar Musik Lokal (NAudio 2.x)**, **Kontrol Media Eksternal (Windows GSMTC)**, dan **Floating Dynamic Island** interaktif.

Dirancang dengan gaya visual modern gelap (*Dark Aesthetic*), aksen neon hijau stabilo (`#B6FF00`), ikon vektor geometris resolusi independen, serta kontrol UI yang halus dan tanpa kesan generik buatan AI.

---

## 🚀 Fitur Utama

### 1. 📋 Clipboard Manager
- **Pemantauan Otomatis**: Memanfaatkan Win32 `AddClipboardFormatListener` & pesan sistem `WM_CLIPBOARDUPDATE` (event-driven, tanpa polling CPU yang berat).
- **Penyimpanan SQLite**: Database lokal terenkripsi di `%LOCALAPPDATA%\ClipClick\clipclick.db` atau portable `Data\clipclick.db`.
- **Batas 500 Entri Otomatis**: Secara cerdas mempertahankan maksimal 500 riwayat terbaru. Entri yang disematkan (*Pinned*) tidak akan terhapus saat pemangkasan otomatis.
- **Pencarian Real-Time**: Ketik langsung untuk memfilter riwayat teks secara instan.
- **Sematkan (*Pin*)**: Tandai teks penting agar selalu berada di posisi paling atas.
- **Salin Cepat**: Klik tombol salin atau klik ganda (*double-click*) pada kartu untuk menyalin kembali ke clipboard Windows (dengan indikator visual centang hijau neon).
- **Hapus & Bersihkan**: Hapus item individual atau bersihkan semua riwayat teks yang tidak disematkan.

### 2. 🎵 Local Music Player (NAudio 2.x)
- Didukung oleh mesin audio latensi rendah **NAudio 2.2.1** (`WaveOutEvent` & `AudioFileReader`).
- Mendukung format audio populer: `.mp3`, `.wav`, `.m4a`, `.aac`, `.flac`, `.wma`.
- **Manajemen Playlist**: Tambah file individual, impor seluruh folder sekaligus, hapus track, dan bersihkan playlist.
- **Kontrol Lengkap**: Previous, Play/Pause, Next, Stop, seek bar interaktif, kontrol volume (0 - 100%), dan tombol mute.
- **Auto Next**: Otomatis memutar lagu berikutnya ketika lagu selesai atau mengulang playlist (*loop*).

### 3. 🌐 Kontrol Media Eksternal (Windows GSMTC API)
- Terintegrasi langsung dengan **Windows 10 Global System Media Transport Controls (GSMTC)** melalui Windows Runtime API (`Windows.Media.Control`).
- Membaca dan menampilkan status lagu yang sedang diputar di aplikasi lain seperti Spotify, Google Chrome, Microsoft Edge, Firefox, dan pemutar media Windows lainnya.
- Kontrol Previous, Play/Pause, dan Next untuk media eksternal.
- **Graceful Fallback**: Jika tidak ada aplikasi media yang aktif, aplikasi menampilkan status informatif tanpa menimbulkan error atau freeze.

### 4. 🏝️ Dynamic Island Floating Widget
- Widget kapsul mengambang (*floating pill*) yang selalu berada di atas jendela lain (*Topmost*).
- **Dua Mode Tampilan**:
  - **Compact Mode** (380x48 px): Menampilkan ikon equalizer beranimasi, judul lagu & artis bergulir rapi, tombol mini Previous, Play/Pause, Next, serta tombol ekspansi.
  - **Expanded Mode** (400x115 px): Membuka progress bar waktu, nama aplikasi sumber, dan kontrol lengkap.
- **Draggable**: Dapat digeser ke posisi mana pun di layar desktop dan posisinya akan disimpan otomatis.
- **Sinkronisasi Cerdas**: Otomatis menampilkan lagu yang sedang aktif, baik dari pemutar lokal ClipClick maupun media eksternal (Spotify/Browser).
- Dapat dinyalakan/dimatikan melalui tombol di Sidebar atau menu System Tray.

### 5. ⚙️ System Tray & Pengaturan
- Berjalan di latar belakang dengan ikon modern di system tray Windows.
- **Menu Klik Kanan Tray**: Buka ClipClick, Toggle Dynamic Island, Bersihkan Riwayat Clipboard, dan Keluar.
- **Minimize on Close**: Menutup jendela utama akan meminimalkannya ke tray tanpa menghentikan pemutaran musik atau pemantauan clipboard.
- **Start with Windows**: Opsi untuk otomatis berjalan saat Windows booting (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).

---

## 🛠️ Spesifikasi Teknologi

| Komponen | Spesifikasi |
|---|---|
| Bahasa & Framework | C# WPF (.NET Framework 4.8) |
| Target OS | Windows 10 Versi 1809 ke atas (Build 17763+) & Windows 11 |
| Database | SQLite 3 (`System.Data.SQLite.Core` 1.0.118) |
| Audio Engine | NAudio 2.2.1 (`NAudio.Core`, `NAudio.WinMM`, `NAudio.Wasapi`) |
| Media API Eksternal | Windows Runtime GSMTC (`Windows.Media.Control`) |
| Arsitektur Target | AnyCPU / x64 |
| Palet Warna | Background `#0A0A0A`, Aksen Neon `#B6FF00`, Panel `#141414`, Teks `#FFFFFF` |

---

## 📂 Struktur Direktori Proyek

```
ClipClick/
├── Models/
│   ├── AudioTrack.cs               # Model data track audio
│   └── ClipboardItem.cs            # Model data riwayat clipboard
├── Services/
│   ├── ClipboardMonitorService.cs  # Win32 clipboard hook listener
│   ├── DatabaseService.cs          # SQLite DB layer (limit 500, pin, search)
│   ├── ExternalMediaManagerService.cs # GSMTC Windows Runtime Media Controller
│   ├── LocalAudioPlayerService.cs  # NAudio playback & playlist engine
│   └── SettingsService.cs          # Registry startup & app configuration
├── ViewModels/
│   ├── ClipboardViewModel.cs       # Logika Clipboard Manager UI
│   ├── IslandViewModel.cs          # Logika Dynamic Island Floating UI
│   ├── MainViewModel.cs            # Navigasi utama
│   ├── PlayerViewModel.cs          # Logika Music Player & Media Hub
│   ├── RelayCommand.cs             # ICommand implementation
│   ├── SettingsViewModel.cs        # Pengaturan & preferensi
│   └── ViewModelBase.cs            # INotifyPropertyChanged base
├── Views/
│   ├── ClipboardView.xaml          # Tampilan Clipboard Manager
│   ├── DynamicIslandWindow.xaml    # Jendela Dynamic Island Floating Widget
│   ├── PlayerView.xaml             # Tampilan Music Player & Media Hub
│   └── SettingsView.xaml           # Tampilan Pengaturan
├── Themes/
│   ├── Colors.xaml                 # Palet warna gelap & neon stabilo green
│   ├── Icons.xaml                  # Path geometries vektor ikon resolusi bebas
│   └── Styles.xaml                 # Gaya tombol, scrollbar, slider, textbox
├── App.xaml / App.xaml.cs          # Titik masuk aplikasi & Dependency Injection
├── MainWindow.xaml / .cs           # Jendela utama frameless & system tray
├── ClipClick.csproj                # File proyek MSBuild
├── build.bat                       # Script kompilasi instan
├── run.bat                         # Script menjalankan aplikasi
├── Test-ClipClickBackend.ps1       # Script pengujian backend otomatis
└── README.md
```

---

## 🔨 Cara Membangun (Build) & Menjalankan

### Persyaratan:
- Windows 10 Versi 1809 ke atas atau Windows 11
- .NET Framework 4.8 (sudah terpasang bawaan di Windows 10/11)

### Opsi 1: Menggunakan Script Batch Otomatis
1. **Membangun Aplikasi**:
   Cukup klik ganda pada `build.bat` atau jalankan di Command Prompt / PowerShell:
   ```cmd
   build.bat
   ```
2. **Menjalankan Aplikasi**:
   Klik ganda pada `run.bat` atau jalankan:
   ```cmd
   run.bat
   ```

### Opsi 2: Menggunakan MSBuild Langsung
Buka PowerShell / Command Prompt dan jalankan:
```cmd
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" ClipClick.csproj /v:minimal /p:Configuration=Release /p:PlatformTarget=x64
```
Hasil kompilasi akan berada di folder `bin\Release\ClipClick.exe`.

### Menjalankan Pengujian (Testing)
Untuk menjalankan uji integrasi backend (Database SQLite 500-limit, NAudio engine, dan Settings):
```powershell
powershell -ExecutionPolicy Bypass -File .\Test-ClipClickBackend.ps1
```
Semua modul backend akan diuji secara otomatis dan menampilkan status keberhasilan di konsol.
