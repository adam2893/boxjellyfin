# Jellyfin for Xbox Series

A beautiful, fast Jellyfin client built natively for Xbox Series X/S with **hardware AV1 decoding** and **native OPUS audio** support.

## ✨ Features

- **Native AV1 Hardware Decode** — Xbox Series X/S AV1 hardware acceleration via Media Foundation
- **Native OPUS Audio** — Direct play without transcoding
- **Audio & Subtitle Track Selection** — Switch audio languages and toggle subtitles during playback
- **Quick Connect** — Link your Xbox to Jellyfin from your phone — no keyboard needed
- **Video Quality Control** — Lower bitrate (120 Mbps → 2 Mbps) to stop buffering on slow connections
- **Settings Page** — Streaming quality, subtitle mode/size, playback options, server info
- **10-Foot UI** — Designed for couch viewing with gamepad-first navigation
- **Cinematic Player** — Gradient overlays, auto-hiding transport controls, codec info display
- **Fast & Lightweight** — Custom API client, no heavy SDK, lazy loading
- **Beautiful Dark Theme** — Deep blacks with purple accent, glass morphism, smooth animations
- **Direct Play Priority** — AV1 > HEVC > H264 codec preference, falls back to transcoding
- **HDR Detection** — Automatic HDR badge display
- **BlurHash Placeholders** — Instant visual placeholders while images load
- **Continue Watching** — Resume where you left off
- **Next Up** — Episode queue for TV shows
- **Search** — Real-time search across your library

## 🎮 Controls

| Input | Action |
|-------|--------|
| **A** | Select / Play |
| **B** | Back |
| **X** | Favorite |
| **Y** | Info |
| **D-Pad** | Navigate |
| **Left Stick** | Navigate |
| **Right Trigger** | Seek forward |
| **Left Trigger** | Seek backward |
| **Menu** (☰) | Settings |
| **View** (⊞) | Audio/Subtitle selection |

## 🏗 Architecture

```
JellyfinXbox/
├── JellyfinXbox.sln
├── JellyfinXbox/                  # UWP App
│   ├── App.xaml                   # DI setup, theme resources
│   ├── ShellPage.xaml             # Navigation shell
│   ├── Views/
│   │   ├── LoginPage.xaml         # Server connect + auth + Quick Connect
│   │   ├── HomePage.xaml          # Library hub
│   │   ├── MediaDetailPage.xaml   # Hero backdrop + info
│   │   ├── PlayerPage.xaml        # Cinematic player + track selection
│   │   ├── SearchPage.xaml        # Search
│   │   ├── SettingsPage.xaml      # Quality, subtitles, playback, server
│   │   ├── QuickConnectPage.xaml  # Phone-to-Xbox linking
│   │   └── LibraryPage.xaml       # Library browsing
│   ├── ViewModels/                # MVVM with CommunityToolkit
│   ├── Services/
│   │   ├── NavigationService.cs
│   │   └── GamepadService.cs      # Xbox controller input
│   ├── Converters/
│   └── Styles/
├── JellyfinClient/                # API Library (portable)
│   ├── Models/Models.cs           # Jellyfin API DTOs
│   └── Services/JellyfinApiClient.cs
└── README.md
```

## 🔧 Tech Stack

- **UWP C# with WinUI 2.x** (Windows App SDK)
- **MVVM** with CommunityToolkit.Mvvm 8.4
- **Windows.Media.Playback** for native media pipeline
- **Media Foundation** for codec support (AV1, HEVC, H264, OPUS, FLAC, etc.)
- **Jellyfin REST API** — lightweight custom client
- **DI** via hand-rolled service container

## 📋 Prerequisites

1. **Windows 10/11** dev machine
2. **Visual Studio 2022** (17.0+)
3. **UWP workload** installed via Visual Studio Installer
4. **Windows SDK** 10.0.19041.0 or later
5. **.NET 6.0** SDK
6. **Xbox One/Xbox Series X/S** (for deployment)
   - Enable **Dev Mode** on the console (free, no dev account needed for sideloading)

## 🚀 Build & Run

### Desktop (Development)
```bash
# Restore dependencies
dotnet restore JellyfinXbox.sln

# Build
dotnet build JellyfinXbox/JellyfinXbox/JellyfinXbox.csproj -c Debug -r win10-x64

# Run (deploy to local machine)
# Open in Visual Studio and press F5
```

### Visual Studio (Recommended)
1. Open `JellyfinXbox.sln` in Visual Studio 2022
2. Set **JellyfinXbox** as the startup project
3. Set target to **x64** (or ARM64 for native Xbox build)
4. Press **F5** to build and deploy

### Deploy to Xbox
1. On your Xbox, go to **Settings > Dev Home > Switch to Dev Mode**
2. Install the Dev Mode app from the Store
3. In the Dev Mode app, find your Xbox's IP address
4. In Visual Studio: **Project > Properties > Debug > Target Device** → Remote Machine
5. Enter the Xbox IP address
6. Set authentication mode to **Universal (Unencrypted Protocol)**
7. Press **F5**

### Sideloading (no Visual Studio)
```bash
# Build the .appx package
dotnet publish JellyfinXbox/JellyfinXbox/JellyfinXbox.csproj -c Release -r win10-x64

# Use WinAppDeployCmd tool to deploy
WinAppDeployCmd install -file JellyfinXbox/JellyfinXbox/bin/x64/Release/JellyfinXbox_1.0.6.44_x64.msix -ip <XBOX_IP>
```

## 🎬 Media Codec Support

The Xbox device profile is tuned for maximum direct play:

| Codec | Support | Notes |
|-------|---------|-------|
| **AV1** | ✅ Native HW | Xbox Series X/S hardware decoder |
| **HEVC/H.265** | ✅ Native HW | Xbox hardware decoder |
| **H.264/AVC** | ✅ Native HW | Universal support |
| **VP9** | ✅ Native SW | Software decode |
| **OPUS** | ✅ Native | Direct play, no transcoding |
| **AAC** | ✅ Native | Direct play |
| **FLAC** | ✅ Native | Lossless audio direct play |
| **TrueHD** | ✅ Pass-through | |
| **DTS/X** | ✅ Pass-through | |

### Transcoding (fallback)
When direct play isn't possible, the server transcodes to:
- **Video**: AV1 or H.264 via HLS
- **Audio**: OPUS or AAC

## 🎨 Design Language

- **Color**: Deep black surfaces (#0B0B0F) with purple accent (#6C63FF)
- **Typography**: Segoe UI Variable, bold titles, generous spacing
- **Cards**: 12px rounded corners, subtle elevation via theme shadows
- **Navigation**: Slim 80px side rail with icon buttons
- **Player**: Full-screen with gradient overlays, auto-hide transport, codec badges
- **Loading**: Acrylic blur + progress ring, no jarring transitions

## 📝 Roadmap

- [x] Audio/subtitle track selection UI
- [x] Settings page
- [x] Quick Connect
- [x] Video quality control
- [ ] Live TV support
- [ ] Music library view with now playing bar
- [ ] Photo slideshow mode
- [ ] User profile switching
- [ ] Offline downloads (USB storage)
- [ ] Custom aspect ratio / zoom controls
- [ ] HDR tone mapping toggle

## 📄 License

MIT
