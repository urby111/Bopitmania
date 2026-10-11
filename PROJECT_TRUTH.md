# Bop Beat — project truth

Last updated: 2026-10-10. Working code name: **Bop Beat**. The reference main menu displays **boopitmania**; the other title spelling is retained as an art alternative.

## Purpose and confirmed design

Unity hackathon rhythm game for an eventual Bop It controller, inspired by Beatmania. The title/menu milestone and countdown proof are complete. The user has now authorized an **actual falling-note prototype** with a simple layout based on their screen reference. Physical controller integration remains separate.

Confirmed requirements:

- Five lanes/controls, IDs `0–4`, with taps, holds, and releases.
- A customizable level contains a song, JSON beatmap, and optional background.
- Notes use absolute seconds from the audio's start. Their position is calculated from song time.
- A chart-level `speed` controls travel time: at `1x`, a note takes one second from the top to its target.
- Latest confirmed judgments: **Perfect = 0 s (exact timestamp)**, **Good > 0 and ≤ 0.15 s**, **OK > 0.15 and ≤ 0.4 s**. Symmetric early/late windows. Zero Perfect is compared exactly without numeric tolerance; nonzero boundaries retain the tiny decimal-rounding tolerance.
- Score per completed note: `hitval + hitval * (combo / 25)`.
- Combo increments only for Perfect. Good and OK reset it.
- Sportypo font requested for score and combo. Five colored targets and a simple replaceable layout. Gameplay uses the menu's pixelated background by default; holds are solid, uniform-width rectangles. Judgment feedback sits in the middle of the lanes and fades out over 0.6 seconds. The corner Boopit circle pulses to each song's BPM.

Prototype choices, not additional user-confirmed rules:

- Speed is treated as a multiplier: `travelSeconds = 1 / speed`, so `2x` is 0.5 seconds. The alternative interpretation was asked but has not been selected by the user.
- Base points default to Perfect **1000**, Good **500**, OK **100**, Miss **0**. The user supplied the score formula but has not selected the base point values. Editable in the component's Scoring fields.
- Score uses the combo **after** the judgment: the first Perfect makes combo 1 and awards 1040 with the default base value. Good/OK reset before their points are calculated. Miss also resets combo. Fractional division and score are preserved; display uses up to two decimals.
- A hold counts once at release/expiry; its final grade is the worse endpoint. Hold-start feedback is provisional and does not yet add points/combo.
- Keyboard D, F, Space, J, K; three-second pre-roll; unmatched inputs have no extra penalty. Standalone releases require holding the key beforehand.
- Tap thickness represents 40 ms of travel distance, visually clamped to 8–96 pixels. Hold body length represents its duration multiplied by pixels per second. Lane width is fixed by layout.

## Current implementation

Unity **6000.5.8f1**, C#, uGUI, existing Input System. No new packages.

- `Assets/Scenes/SampleScene.unity`: build entry, three saved panels under Menu Flow > Menu Canvas, plus a results panel generated once at runtime. Original camera/lighting retained. Gameplay component has saved Sportypo and Graffonti sprite references, timing, and scoring defaults.
- `Assets/Scripts/Menus/MenuFlow.cs`: title → song select → falling-note game → results → song select. A private UI submit action supports Enter, numpad Enter, Space, and existing controller confirmation on all menus. Arrow navigation and mouse work together; lost menu focus is restored. UI move/submit events are disabled during gameplay so Space remains a lane input and cannot activate HUD buttons. Title Start opens song selection; Settings is an intentional no-op; Quit calls Application.Quit in builds and exits Play Mode in the editor. Escape returns/stops audio, and Escape again returns to title.
- `Assets/Scripts/Menus/TitleMenuController.cs`: responsive oversized rotating background at 2°/second clockwise. The short side exceeds the viewport diagonal by 5% so every angle covers the display. Rotation uses unscaled time; menu confirmation is centralized in MenuFlow.
- `Assets/Scripts/Menus/MenuItemHighlight.cs`: smooth 6% enlargement on pointer hover or keyboard selection, with a stable click area. Mouse hover also selects the item; explicit Button navigation wraps Up/Down and Left/Right through Start, Settings, Quit.
- `Assets/Editor/TitleMenuSetup.cs`: explicit title-only rebuild based on the user-supplied Assets/Art/menu/Untitled design.pdf. Assigns background/logo/menu assets and actions, and replaces MenuFlow playButton reference. Tools > Bop Beat > Apply Reference Title Menu rebuilds only the title and should be used only when a rebuild is intended.
- `Assets/Editor/SongSelectMenuSetup.cs`: explicit song-select-only rebuild from the latest image reference. Static pixelated background, white Sportypo Back/heading, and one rounded teal row with a white border. SongSelectLayout anchors the reference layout to the top and uniformly scales it to the screen width, including taller Game views. The row is the start button and shows the selected asset's title using exported Sportypo glyph sprites. Artist/details/cover UI are omitted; MenuFlow safely handles those optional references. Tools > Bop Beat > Apply Reference Song Select replaces this screen only. After this revision imports (or Play Mode stops), an older open SampleScene lacking SongSelectLayout is upgraded once with the same builder; an already upgraded screen is left alone. Background UVs trim the source JPEG's thin white edge; the cropped image's aspect ratio is preserved.
- `Assets/Scripts/Menus/SongSelectLayout.cs`: top-aligned 1280 x 720 design-space menu, scaled uniformly to viewport width in Edit and Play Mode. Prevents the fixed center layout from shifting down in tall Game views. Back/header/song-row artwork and actions remain the same.
- `Assets/Scripts/Menus/SpriteLettering.cs`: reusable one-line sprite lettering for the dynamic row title. Glyph advance/bearing metrics come from the font export manifest; letters are fitted to the label without wrapping or distorting glyphs. Uppercase letters use matching lowercase sprites from the requested a-z kit. Image objects are reused when names/layout change. Unsupported punctuation/non-Latin characters fall back to the original font rather than being silently omitted.
- `Assets/Scripts/Menus/SongDefinition.cs`: level ScriptableObject with AudioClip, JSON TextAsset, optional background Sprite/tint, BPM, first-beat offset, optional ordered tempo changes, and menu metadata. Tempo drives the decorative circle pulse. Beat Offset Seconds defaults to 0 and aligns a song with intro/silence. Tempo changes start a new pulse beat at the supplied song time. Hit times remain explicit chart seconds. Create via Assets > Create > Bop Beat > Level.
- `Assets/Data/Songs/SampleSong.asset`: Five Beat Demo. Its Background is empty to use the default pixelated backdrop; the previous grid asset is retained and can be reassigned.
- `Assets/Audio/FiveBeatDemo.wav`: generated 28-second synth/percussion audio, 120 BPM metadata.
- `Assets/Data/Beatmaps/FiveBeatDemo.json`: `speed: 1.0`, 32 notes across all five lanes, taps/holds/releases/chords.
- `Assets/Scripts/Beatmaps/BeatmapData.cs`: chart model and validation/sorting; speed must be finite and positive. Older charts without speed default to 1x when loaded with FromJsonOverwrite.
- `Assets/Scripts/Beatmaps/JudgmentSettings.cs`: editable timing and base score settings. Checked and copied when a session starts.
- `Assets/Scripts/Beatmaps/BeatmapSession.cs`: pure C# judgment/state/scoring logic. Nearest eligible matching lane/action wins; ties choose earlier note. Tracks Perfect/Good/OK/Miss, Score, Combo, MaxCombo. Completed notes cannot score twice.
- `Assets/Scripts/Gameplay/CountdownPrototype.cs`: retained component name/GUID for scene compatibility; now runs the falling-note game. Loads/validates charts, schedules audio with DSP time, routes timestamped presses/releases, manages replay/back/results. Its name is historical; there is no countdown-card view now.
- `Assets/Scripts/Gameplay/NoteTravel.cs`: pure song-time position/size calculations.
- `Assets/Scripts/Gameplay/BeatPulse.cs`: circle scale from absolute audio time, BPM, and first-beat offset. Each beat expands it by 8%, then eases to normal over up to 0.18 seconds (at most half a beat). It remains still before the first beat and after the song finishes; replay resets it. No beat detection or scoring changes.
- `Assets/Scripts/Gameplay/FallingNoteView.cs`: creates simple lanes, target blocks, notes, hold bodies/tails, release markers, Sportypo score/combo, centered Graffonti feedback with a 0.6-second fade, and a corner Boopit circle. Holds have identical opaque lane color and width throughout their body/head/tail. Empty level backgrounds use the supplied pixelated texture, scaled to cover without stretching and lightly darkened; custom background sprites/tints still override it. Reuses note objects on replay. Bounds are masked to the playfield.
- `Assets/Editor/MenuSceneSetup.cs`: one-time setup; existing menu UI is not overwritten. New setup assigns the supplied font/art and updated song-menu instructions.
- `Assets/Tests/EditMode` and `Assets/Tests/PlayMode`: timing/chart/score/travel tests and Unity menu/audio/keyboard/render/replay flow checks.

The supplied Sportypo demo renders digits as promotional symbols. Score/combo labels use Sportypo; their numeric values use readable bold italic LegacyRuntime glyphs. This fallback was discovered in the Unity render and explained to the user.

The 1280 × 720 Canvas scales to the display. Lane target colors, left to right: yellow `#F4FF33`, blue `#3D4DFF`, white, green `#86FF52`, lavender `#DC85FF`. The target center is the hit line. The keyboard labels beneath the targets are removed; lane bindings remain D, F, Space, J, K. Holds have a connected body and tail; release notes carry **R**. The circle is a BPM-driven decoration, not a physical controller connection or clickable lane input.

## Art and presentation guide

- Original Billo transparent judgments retained in `Assets/Art/Judgments`: blue perfect `#8BDCFB`, green good `#A9EFAB`, yellow ok `#FFF1A1`.
- `Assets/Art/ClipArt/Judgments_Graffonti`: 3D Drop alternatives, perfect `#F51E69`, good `#1EF65F`, ok `#FFFB80`; now used by gameplay feedback. Miss uses plain text.
- `Assets/Art/ClipArt/Logos_Hypik/BoopitBlack.png`: black transparent boopit lettering in the same Hypik font as the logo, used inside the gameplay circle. Regenerate with Tools/GenerateBoopitArt.py (Python/Pillow); retains the sprite GUID.
- `Assets/Art/ClipArt/Logos_Hypik`: original purple `#A700CC` boopitmania and bopitmania alternatives retained. The revised title uses two copies of Assets/Art/menu/BoopitmaniaWhiteMask.png (rendered directly from Hypik): front `#D650FF`, sampled from the latest reference, and rear `#A700CC` shifted left 8/down 6 design pixels. Both are identical glyph shapes with flat color; the old white outline is removed. The white source mask allows either layer to be tinted accurately.
- `Assets/Art/ClipArt/Menu_Sportypo`: original white start/settings/quit/back sprites retained. Start/settings/quit are tinted black; each has one identical white copy shifted left 5/down 5 design pixels beneath it, using uGUI Shadow. This replaces the surrounding Outline effect. Back art remains available. Settings remains a no-op; Quit behavior is implemented separately in MenuFlow.
- `Assets/Art/ClipArt/Sportypo_Characters`: 36 white transparent glyph PNGs, a-z and 0-9, directly rendered from the supplied demo font. The user explicitly chose its original digit glyphs after being informed that they are promotional logos, not readable numbers. No invented or fallback numerals were substituted. `manifest.json` records the source and dimensions; README explains usage. Gameplay's existing readable numeric fallback remains unchanged.
- `Assets/Art/ClipArt/SongSelect_Sportypo`: white SongSelect heading sprite and a sliced rounded song-card sprite. Back uses the existing white Sportypo Back.png. `Tools/GenerateSongSelectArt.py` regenerates these assets and the character kit/metrics with Pillow while retaining Unity GUIDs. Dynamic song titles assemble the exported character sprites, so changing the level name does not require new artwork.
- `Assets/Art/menu/pixelated-image.jpg`: user-selected pixelated title background, enlarged and rotating slowly. Point filtering preserves the pixelated edges. The high-resolution source and reference PDF are preserved; the high-resolution image is unused. The new title keeps the reference logo/list arrangement; song selection and gameplay are retained.
- `Assets/Art/PrototypeBackground.png`: former grid background, retained as an optional Sprite.
- `Assets/Art/ClipArt/README.md`: exact font sources/colors. Clip art comes directly from user-supplied fonts, not AI approximation.
- `HACKATHON_GUIDE.md`: maintained technical-understanding/presentation guide. Update it with this truth document when implementation changes. `AGENTS.md` records that requirement. Never invent personal learning or team contributions.

## How to play and customize

1. Let Unity import changes. Reopen `Assets/Scenes/SampleScene.unity` to load the saved component references/settings if the scene was already open.
2. Unity Play and focus Game view. On the title, song-select, and score screens, arrow keys choose and Enter or Space confirms; mouse hover/click also works. Start opens song select. Confirm the featured song row to begin gameplay; arrows switch between the row and Back. Back/Escape returns to the title. Settings does nothing; Quit closes a build or stops editor Play Mode.
3. After pre-roll, press when the block's center reaches its colored target: lanes 0–4 are D, F, Space, J, K.
4. Long block: press its head, hold, and release when its tail reaches the target. R block: hold early, then release at the target.
5. Watch judgment art, score, and combo. R/Replay restarts everything. Escape/Back returns to song select and stops playback. Finishing the song opens a separate score screen; click Song Select, press Enter/Space, or Escape to return. Menu confirmation is disabled during a song so Space can play lane 2 safely.

Edit the beatmap:

```json
{
  "speed": 1.0,
  "notes": [
    { "time": 1.5, "lane": 0, "type": "tap" },
    { "time": 3.0, "lane": 2, "type": "hold", "endTime": 4.25 },
    { "time": 6.0, "lane": 4, "type": "release" }
  ]
}
```

At 1x, a time-1.5 note starts entering at song time 0.5 and reaches its target at 1.5. At 2x it starts at 1.0 and still reaches at 1.5. Speed does not alter music pitch, chart times, BPM, or judgment windows. All chart times include silence in the audio. BPM drives the circle pulse only; charts are authored, not automatically analyzed from music. A 120 BPM song pulses every 0.5 seconds. Beat Offset Seconds shifts the first pulse to the song's actual first beat without moving chart notes. Optional Tempo Changes use ordered song times and positive BPM; each change anchors a new pulse beat. Already saved JSON notes do not move when metadata changes.

Create a new Bop Beat > Level asset, assign audio/JSON and optionally a Sprite background/tint, then assign it to Menu Flow > Sample Song. The menu has one slot; new assets do not automatically create a multi-song list. To replace the background, assign the level's Background field. Empty uses the pixelated menu background. Set BPM and Beat Offset Seconds on each level for its circle pulse.

Select Gameplay Prototype Screen > Countdown Prototype to edit Timing, Scoring, keys, volume, score font, feedback sprites, Default Background texture, or Boopit Art sprite outside Play Mode. Timing/scoring are copied on the next launch/replay. Persistent lane/layout edits belong in FallingNoteView; runtime Hierarchy changes are temporary.

Validation rejects malformed/empty charts, invalid lanes/types/times, invalid speed, invalid holds, duplicate same-lane timestamps, and notes overlapping a hold on the same lane. Chords across lanes are allowed. Invalid charts display an error before audio starts. Closely spaced same-lane notes can have overlapping hit windows; nearest matching note decides the result.

## Still open

Physical Bop It mapping/protocol, real device latency/calibration, pause/seek, multi-song selection, final naming/background/music, and further balancing remain open. Point values, multiplier interpretation above 1x, score/combo ordering, Miss combo reset, and visual thickness are documented prototype choices. Rendering and note lookups currently scan the small chart and reuse one visual per note; large charts need profiling before optimization.

## Validation

Unity 6000.5.8f1 compiled the final source and passed **35 Edit Mode cases and 1 Play Mode scenario** on 2026-10-10. Rules/geometry tests cover inclusive early/late Perfect/Good/OK boundaries, time-zero inputs, repeated/wrong presses, holds/releases, chords, bad charts/speed/settings, exact song-time positions, speed/size changes, fractional combo scoring, resets, one-result holds, and a full 32-Perfect sample run (53,120 points). The Unity scenario checks menu flow, scheduled audio, real generated block positions, font references, displayed score/combo, judgment sprites, simulated Perfect/Good/OK/Miss input, replay resets/UI reuse, and audio stop on return. The final source, tests, scene, and level data matched the tested copy; the rendered screen was inspected and the unusable Sportypo digit glyphs were corrected with a readable numeric fallback.

Results: `Logs/FallingNoteEditModeResults.xml`, `Logs/FallingNotePlayModeResults.xml`; preview: `Logs/FallingNotePrototype.png`. Tests ran in a temporary copy to avoid disturbing the open editor; that copy was removed afterward. Previous countdown results remain as historical evidence. Physical controller timing, device latency, and severe stalls remain untested.

## Main menu verification

Before the later offset-copy art revision, Unity compiled the title redesign on 2026-10-10 and passed **both Play Mode scenarios**: the new title controls/rotation check and the existing gameplay flow. Tests cover the pixelated texture and Point filtering, actual Up/Down wrapping, Space/no-op Settings, mouse hover/click and enlargement, clockwise rotation/overscan, and the saved Quit callback. Quit is not executed inside tests because it deliberately terminates the player/play session. The preview was visually inspected. All 92 existing song-select serialized objects were preserved exactly with normalized newlines; source, tests, data, final scene, and new menu assets matched the tested temporary project. The temporary project was removed afterward.

Evidence: `Logs/TitleMenuSetup.log`, `Logs/TitleMenuPlayModeResults.xml`, `Logs/TitleMenuVerification.txt`; preview: `Logs/ReferenceTitleMenu.png`. Reopen SampleScene after Unity imports the saved scene, then press Play. The background's speed and overscan are editable on Title Screen > Title Menu Controller. Its texture is assigned on Rotating Background > Raw Image.

The subsequent offset-copy art revision on 2026-10-10 passed the existing menu Play Mode scenario again (**1 passed, 0 failed**). The revised preview was visually inspected: `Logs/OffsetTitleMenu.png`. Results: `Logs/OffsetMenuPlayModeResults.xml`; scope/source verification: `Logs/OffsetMenuArtVerification.txt`. Seven serialized art blocks changed and one logo Outline was removed; all 167 other scene blocks remained unchanged. The tested scene, title builder, and menu test match the final files. The art uses duplicate glyph layers in Unity, so original PNG files are preserved. The temporary validation copy was removed afterward.

## Song-select and glyph-kit verification

On 2026-10-10 the final song-select revision compiled and passed **both Play Mode scenarios**. The menu scenario checks the updated row/title, keyboard Back, actual row mouse-click launch, and existing main-menu controls. The other scenario verifies audio, judgments, score/combo, replay, and return from gameplay. The rendered screen was inspected after replacing the wrapping/soft native-font row with sharp, one-line sprite lettering. All 81 existing title/gameplay/shared serialized blocks and their sample-level/main-menu references stayed unchanged. The final scene/source/data/art match the tested copy. All 36 glyph PNGs contain white visible pixels and transparent backgrounds; original demo numeral-logo glyphs are intentionally preserved. The temporary validation copy was removed afterward.

Evidence: `Logs/SongSelectSetup.log`, `Logs/SongSelectPlayModeResults.xml`, `Logs/SongSelectVerification.txt`; previews: `Logs/ReferenceSongSelect.png` and `Logs/SportypoGlyphSheet.png`. Reopen SampleScene after importing, then press Play. Click the song row or press Enter to launch; Back/Escape returns. Timing/scoring and physical controller limitations remain as documented above.

## Gameplay polish verification

On 2026-10-10 Unity 6000.5.8f1 compiled the final gameplay-polish source and passed **37 Edit Mode cases and 3 Play Mode scenarios**. Added checks verify pulse timing/tempo/offset/no drift, normal scale during preparation/finish, equal hold-body/head/tail width and opaque color, default pixelated texture/Point filtering, custom background override, centered non-interactive feedback, fade/retrigger/Miss/replay behavior, and saved black Boopit sprite. Existing rules, score, input/audio/menu/replay checks still pass. A real sample hold-start screenshot was inspected with the centered Perfect art visible; preview rendering was warmed up so the brief feedback did not expire during first-use rendering.

Only two gameplay art references were added to the saved scene; all 157 other serialized blocks were unchanged. The final scene, art, and 26 source/test/data files matched the tested copy. Black clip-art visible pixels and transparency were checked. Results: `Logs/GameplayVisualEditModeResults.xml`, `Logs/GameplayVisualPlayModeResults.xml`; verification: `Logs/GameplayVisualVerification.txt`; previews: `Logs/GameplayVisualPrototype.png`, `Logs/BoopitBlackPreview.png`. Testing used a temporary copy to preserve the user's open editor.

Reopen SampleScene after importing. Set BPM and Beat Offset Seconds on the level asset, not the beatmap. The sample is 120 BPM with offset 0. Pulse shape/size and fade duration are presentation defaults; variable-tempo music needs a future tempo map. Physical controller/latency limitations remain unchanged.

## Song-select screenshot correction

The user reconfirmed the compact white Sportypo Back/heading and rounded Five Beat Demo row on the pixelated background. The previous saved 16:9 preview already matched that artwork, but its fixed centered layout could move down when the Game view was taller. The correction anchors it to the top and scales with viewport width. A one-time editor upgrade also replaces an older loaded song-select Hierarchy after importing this revision, without rebuilding title/gameplay. If importing during Play Mode, the upgrade waits until returning to Edit Mode. Original art is preserved. The older-menu upgrade also retains existing gameplay art references and restores the recently added pixelated/default-Boopit references if an older loaded scene has them empty, so saving the menu cannot discard that prior polish.

The final correction compiled in Unity 6000.5.8f1 and passed **4 Play Mode scenarios** on 2026-10-10. These cover menu controls/launch, existing gameplay/audio/scoring/replay, previous visual polish, and top alignment/proportional width fitting across wide, 4:3, and portrait viewport dimensions. Rendered 1280x720 and 1024x768 previews were inspected. All 156 other scene blocks were preserved; only the song-select layout object/transform changed, and one component was added. The saved scene and 28 source/test/data files matched the tested copy. Evidence: `Logs/SongSelectFixPlayModeResults.xml`, `Logs/SongSelectFixVerification.txt`; previews: `Logs/FixedSongSelect.png`, `Logs/FixedSongSelectTall.png`. Tests used a temporary project. The live computer-use connection was unavailable, so the older-loaded-menu upgrade was not visually inspected in the user's open editor; it runs after import/return to Edit Mode. Stop Play Mode and let Unity import, then Play > Start. The explicit Apply Reference Song Select tool remains available if a manual rebuild is desired.

## Space cleanup — 2026-10-10

At the user's request, removed old test/import logs, superseded preview PNGs, Python bytecode cache, and the IDE upgrade report. Removed about 30 MB (29.37 MiB); kept current title/song-select/gameplay/glyph previews, all test result XMLs and compact verification receipts. Some historical preview/raw-log paths earlier in this document now describe removed artifacts. Source assets, original fonts/license/reference files, requested clip art (including original demo digits), scripts, tests, scene, beatmaps/audio, generators, package manifests, settings, and project documentation were preserved.

All 286 protected source/asset/metadata/settings/generator files matched pre-cleanup SHA256 hashes. Unity and Visual Studio remained open, so Library (~3.2 GiB), active Temp/current logs, .vs, and generated .csproj/.sln files were left intact. No new gameplay tests were run for this file cleanup. Details: Logs/CleanupReceipt.txt.

The user subsequently saved and closed Unity and authorized removing its generated cache. After verifying the project editor was closed and the exact Library path contained no linked directories, removed Library (3,444,054,389 bytes, 3.21 GiB). All 286 protected source/asset/package/settings/generator files still matched SHA256 hashes. Drive C had 26.61 GiB free afterward. Unity was not relaunched or retested because that would recreate the cache. The next launch rebuilds Library, takes longer, and uses disk space again; Assets, Packages, ProjectSettings, and game behavior are preserved.

## Cruel Angel chart and score screen — 2026-10-10

The user requested a generated chart for the supplied Cruel Angel's Thesis audio: 80 BPM for the first 16 seconds, then 128 BPM, with one random note every two beats. `Assets/Editor/CruelAngelSetup.cs` uses the imported AudioClip duration and a fixed random seed (80128), generating tap lanes 0–4 in `Assets/Data/Beatmaps/CruelAngelThesis.json`, with speed 1. The chosen section alignment starts at song time 0 and resets to a downbeat at 16 seconds: slow-section times 0, 1.5, 3, …, 15; fast-section times 16, 16.9375, 17.875, … before the audio ends. This is supplied-tempo chart generation, not audio analysis. Exact alignment to the recording may need manual adjustment. Tools > Bop Beat > Regenerate Cruel Angel Chart overwrites this chart and its generated song metadata; save manual edits elsewhere before regenerating.

`Assets/Data/Songs/CruelAngelThesis.asset` links the original MP3/chart, with BPM 80 and Tempo Changes `{time: 16, bpm: 128}`. The featured single song-select slot now uses this level. The original Five Beat Demo audio/chart/level remain available; assign SampleSong.asset to Menu Flow > Sample Song to restore the old timing/hold demonstration. The song-select layout/art and gameplay scoring are retained.

`ScoreScreenView.cs` creates a reusable runtime panel under Menu Canvas with the pixelated background, white Sportypo labels/title, final score, max combo, and Perfect/Good/OK/Miss totals. Numeric values use the existing readable LegacyRuntime fallback because Sportypo demo digits are logos. CountdownPrototype invokes its completion callback only after the audio duration and last note's late window have passed and all notes have results. MenuFlow stops/hides gameplay, displays results, and focuses Song Select. Click/Enter on that button or Escape/gamepad Back returns to song select. Aborting gameplay still returns directly without showing results. Results are per-run and are not saved as high scores. The new panel is generated when a song first finishes, so it appears in the runtime Hierarchy rather than the saved scene.

Tempo changes validate as finite ascending song times after the first-beat offset, before audio ends, with positive BPM. TempoAt selects the active tempo/beat anchor for the circle; JSON note positions and timing/score rules remain unchanged. The previous constant-tempo-only limitation in historical verification sections is superseded by this implementation.

The imported audio is 94.903 seconds long. The saved chart has 96 taps, starting at 0 and ending at 94.75. With the default OK window, results open after 95.15 seconds to preserve the last late hit. Unity 6000.5.8f1 compiled this revision and passed **39 Edit Mode cases and 5 Play Mode scenarios**. The chart check verifies section spacing, duration bounds, all lanes, an all-Perfect chart run, and the tempo switch. The results scenario uses a short audio/chart fixture to exercise natural completion, final score/count/max-combo totals, Enter/button return, reuse with fresh values, and abort behavior; existing menu/audio/input/render/replay/layout checks also pass. All 79 source/metadata/test/data/scene/core audio-font-background files matched the tested isolated copy by SHA256. The two 1280x720 previews were visually inspected; the score preview shows test-fixture values, not a full-song score. Evidence: Logs/CruelAngelEditModeResults.xml, Logs/CruelAngelPlayModeResults.xml, Logs/CruelAngelVerification.txt, Logs/CruelAngelSongSelect.png, Logs/ScoreScreenPreview.png. The temporary test project and redundant raw logs were removed. Reopen SampleScene after importing to load the featured-song assignment; the live editor's loaded scene was not changed. Musical alignment and physical hardware remain unverified.

## Latest timing revision — 2026-10-10

The user replaced the previous 0.1/0.2/0.4 windows with Perfect exactly 0, Good through ±0.15 seconds, and OK through ±0.4 seconds. Updated TimingSettings defaults, BeatmapSession public constants/comparison, saved SampleScene timing fields, and the base menu-builder notice. Zero Perfect uses strict equality, including for taps and both hold endpoints; the numeric epsilon does not expand it. Positive configured Perfect windows and Good/OK keep their 1e-9-second decimal-boundary tolerance. Combo/scoring rules, charts, art, tempo, and results flow remain unchanged. Exact timestamp equality is extremely difficult with ordinary live keyboard input; this is the requested behavior rather than an added minimum allowance.

The guide's current explanations/examples and AGENTS guidance were updated. Earlier dated validation entries describe the thresholds in effect for those earlier revisions. Reopen SampleScene after import to load the changed serialized values in an already open editor; start/replay copies its current settings into the next session. Rebuild an exported executable to include this change.

Unity 6000.5.8f1 passed **39 Edit Mode cases and 1 targeted Play Mode scenario** for this revision. Tests cover strict-zero Perfect (including tiny nonzero errors grading Good), symmetric inclusive Good/OK boundaries, chart/hold/release/scoring regressions, and runtime saved timing values plus actual keyboard Good/OK/Miss, displayed score/combo, replay, and audio stop on return. All 79 checked final files matched the tested isolated copy by SHA256. Evidence: Logs/TimingEditModeResults.xml, Logs/TimingPlayModeResults.xml, Logs/TimingVerification.txt. The initial temporary copy was missing Assets at launch; restoring it outside Unity Temp allowed successful testing. The validation copy and redundant raw logs were removed afterward. Main-editor/physical hardware timing was not tested.

## Menu controls and lane labels — 2026-10-10

Removed the generated keyboard text beneath the lane targets while retaining D/F/Space/J/K gameplay input. Title, song select, and results now use arrows and Enter/Space consistently. MenuFlow owns one private UI submit action, replacing the old title-only Space handler; it supports numpad Enter and existing controller confirmation too. Title Left/Right navigation mirrors Up/Down, song select switches between Back and the row with all four arrows, and results keeps its one return button focused. Lost menu focus is recovered. Menu move/submit is disabled during gameplay so Space cannot activate a selected HUD button, and restored on returning to a menu. Mouse controls remain available. Settings is still a no-op; no settings screen was added. Scene, artwork, charts, and timing were preserved.

Unity 6000.5.8f1 compiled this revision and passed **all 5 Play Mode scenarios**, covering arrows/Enter/Space, single activation and no transition click-through, focus recovery, results return, mouse controls, removed lane labels, and gameplay Space with Replay selected. Existing audio/scoring/replay/visual/layout checks pass. All 79 checked source/metadata/test/data/scene/core media files matched the tested isolated copy. Evidence: Logs/MenuNavigationPlayModeResults.xml and Logs/MenuNavigationVerification.txt. Quit wiring is checked without executing it. Restart Play after import to load the runtime changes; rebuild exported executables. Temporary validation files are removed after checking to reclaim space. The user's live editor and physical hardware were not tested.
