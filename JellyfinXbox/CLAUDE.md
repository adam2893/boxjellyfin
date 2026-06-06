# Project Context
This is a UWP application targeting Windows and Xbox. It is a client for the Jellyfin media server.

# STRICT ARCHITECTURE RULES
1. UWP ONLY: Use `Windows.UI.Xaml`. `Microsoft.UI.Xaml` v2.x (WinUI 2) is allowed for styling/resources — it is the standard UWP modern styling library. NEVER use WinUI 3 or Windows App SDK.
2. Target Framework: The main app is UWP (uap10.0). The API client is .NET Standard 2.0.
3. API CLIENT: We have an NSwag-generated Jellyfin API client in the `JellyfinApiClient` project (namespace `Jellyfin.Api`).
   - NEVER guess Jellyfin REST endpoints or write raw HttpClient calls.
   - The generated client is in `JellyfinApiClient/JellyfinApiClient.cs` (single file, 80K+ lines).
   - The app-facing wrapper (state, logging, convenience methods) is in `JellyfinApiClient/Services/JellyfinClientWrapper.cs` and implements `IJellyfinClient`.
4. JSON: Both the generated and hand-written clients use `System.Text.Json`. Use `System.Text.Json` for any custom serialization.

# XBOX UI/UX RULES
1. Navigation: The app is navigated via Xbox Controller (D-pad). 
2. Focus States: Every clickable UI element MUST have clear `FocusVisualPrimaryBrush` and `FocusVisualSecondaryBrush` properties.
3. XYFocus: Use `XYFocusUp`, `XYFocusDown`, `XYFocusLeft`, `XYFocusRight` on GridViews/Lists to control D-pad navigation flow.
4. Scaling: Assume a 10-foot viewing distance. Minimum font size is 18px. Buttons must be at least 60x60 pixels.
5. Events: Do not rely on `PointerEntered` (mouse hover). Use `GotFocus` and `LostFocus` for visual state changes.