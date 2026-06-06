# Jellyfin Xbox Finish — Design Spec

**Version:** 1.0.6.45
**Date:** 2026-06-04

## Goal
Stabilize BoxJellyfin for Xbox and Windows: playback seek/resume, XBOX_UWP_RULES navigation, library browsing.

## Out of scope
Live TV, music, photos, offline, profile switching.

## Playback
- Unified SeekTo via stream URL + startTimeTicks
- Persist DeviceId in LocalSettings
- AV1 in device profile; progressive stream over HLS when transcoding

## Navigation
- FocusVisual brushes, XboxFocusHelper, shell XYFocus, GamepadA on GridView

## Library
- Image Primary/Thumb fallback, ErrorMessage + Retry, limit 50 items

## Verification
docs/DEBUG.md
