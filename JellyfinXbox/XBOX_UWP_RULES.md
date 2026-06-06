# UWP Xbox Development Rules
1. Navigation: All navigation must be keyboard/gamepad accessible. Use `XYFocus` properties (`XYFocusUp`, `XYFocusDown`, etc.) on UI elements.
2. Events: Do not use `Click` events as the primary interaction. Use `KeyDown` (checking for `VirtualKey.GamepadA`) or `GotFocus` combined with `KeyDown`.
3. Visuals: Always include a `FocusVisualPrimaryBrush` and `FocusVisualSecondaryBrush` in styles so the user can see what is selected on a TV screen from 10 feet away.
4. Scaling: Assume the app is viewed on a TV. Use larger fonts (minimum 15-20px) and high contrast.
5. Manifest: Remember this is a UWP app. Use `Windows.UI.Xaml`. `Microsoft.UI.Xaml` v2.x (WinUI 2) is allowed for styling/resources. Do NOT use WinUI 3.