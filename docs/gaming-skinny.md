# Gaming skinny (reversible session posture)

UnboundOS makes Windows quieter **for the session**, then restores the
desktop. It is not a permanent gut.

## Profiles (`profiles.json`)

Created on first launch under LocalAppData. Tweak the JSON — flags are
plain booleans on each `SessionProfile`.

| Profile | Game Mode | Game DVR | Visual effects | Power | Overlays |
|---------|-----------|----------|----------------|-------|----------|
| **Competitive Edge** | on | **off** | Performance | Ultimate Performance while live | Rainmeter/Discord stripped |
| **Streamer Split** | on | **left on** | Performance | Ultimate Performance while live | Rainmeter/OBS/Discord protected |
| **Living Room Shell** | on | left on | left pretty | Ultimate Performance while live | Rainmeter/Playnite protected |

Shared: `ApplyGameMode` (default true), `EnableHighPerformancePowerHint`
(default true). Competitive sets `DisableGameDvr` and
`VisualEffectsPerformance`. Living Room leaves effects pretty.

`SkinnySummary` on the profile is the one-line readout on Profiles.

## What Enter Session does

1. Snapshot NIC metrics; apply game/stream split if two adapters exist
2. Switch to Ultimate Performance (saved GUID restored on exit)
3. Terminate the profile denylist; never touch HardProtect / Explorer /
   anticheat / Vortex
4. Apply HKCU Game Mode / Game DVR / visual-effects **if the flags say so**
5. **Do not** write Defender, Windows Update, VBS, or BCDEdit.
   Quality/LCU stays on Microsoft’s channel. Feature deferral is
   Options → Updates ([update-guard.md](update-guard.md)), never
   session enter.

Exit / Restore writes the snapshot back.

## HAGS (not a session default)

Options → **Session skinny** → Hardware-accelerated GPU scheduling.

- Preference is saved in `settings.json` (`hardwareGpuScheduling`)
- Apply needs an elevated HKLM write; reboot may be required
- Copy tells you to **test frametimes** — some GPUs regress
- Session enter does **not** flip HAGS

## Motion-off

Settings → Interface motion Off freezes Home label enlarge/push and
list ease. No WebGL on Home. Three.js stays vendored and unused.

## Startup / debloat

Settings → Startup audit. Pin what you want. The image can register
`docs/startup-audit-task.xml` for a daily pass. AppX trim stays
Store-reinstallable — this slice does not silently remove inboxes.

## NIC interrupt tip (Competitive, optional)

If a Competitive match still feels mushy, some Intel/Realtek adapters
allow interrupt moderation Off **per adapter**. That is a Network
Director / vendor-driver choice, not a global default.

## Restore guidance

1. Session page → **Exit / Restore**
2. If a session faulted, run Exit again; power plan and HKCU values
   come back from `SessionSnapshot`
3. HAGS: toggle Off in Session skinny (elevated) or reset
   `HwSchMode` in GPU driver UI
4. Defender / Update / VBS were never turned off — if someone else
   disabled them, use Windows Security, not UnboundOS
5. Update Guard: Options → Updates → Restore (elevated). See
   [update-guard.md](update-guard.md)

See [competitive-landscape.md](competitive-landscape.md).
