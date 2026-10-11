# Bop Beat — understanding and presenting the project

Updated: 2026-10-10. Current milestone: playable falling-note prototype.

This guide explains the actual source. `PROJECT_TRUTH.md` records requirements, implementation, and open decisions. Start with the short explanation, then try the examples with Unity open. The learning log records your real experience, not assumptions about what you learned.

## The explanation to learn first

> We're making a rhythm game for a Bop It controller. Each level bundles a song, background, and beatmap: a list of which controls to use and when in the song to use them. Our Unity prototype currently uses five keyboard lanes. Blocks fall according to the audio clock, and presses/releases are compared with their target timestamps. Timing produces Perfect, Good, OK, or Miss, and Perfects build a combo that boosts score. The timing engine is separate from rendering, so we can connect hardware without making the falling blocks decide the rules.

Current status: **falling blocks, keyboard play, scoring, and menus work in code; physical Bop It input is not connected here.** Describe any teammate's separate hardware work with its own evidence.

Five facts to remember:

1. Notes use **absolute seconds from the audio's start**, not frame counts or image collisions.
2. Lanes **0–4** use **D, F, Space, J, K**.
3. Perfect requires **exactly 0 s error**, Good allows nonzero error through **0.15 s**, OK through **0.4 s**, equally early/late.
4. Speed **1x** means **one second of travel**. The current multiplier convention makes 2x half a second.
5. Only completed Perfect notes build combo. A hold completes once at its release/expiry.

## What exists today

| Feature | Current state |
| --- | --- |
| Menu flow | Reference title → pixelated song select → gameplay → score screen → song select |
| Demo level | 28 seconds, 32 notes, 120 BPM beat pulse |
| Featured song | Cruel Angel Thesis, 94.903 seconds, 96 generated taps; 80 BPM until 16 seconds, then 128 BPM |
| Level data | AudioClip + JSON TextAsset + optional background Sprite + BPM/beat offset/ordered tempo changes in a ScriptableObject |
| Falling notes | Five colored lanes; rectangles positioned from song time; no keyboard labels under targets |
| Actions | Taps, holds with tails, and R-marked standalone releases |
| Judgments | Perfect / Good / OK / Miss and timing error, centered over lanes with 0.6-second fade |
| Score and combo | Requested formula, Sportypo labels with readable italic numeric fallback, Perfect-only streak |
| Score screen | Pixelated background, Sportypo labels, final score, best combo, judgment counts; button/Enter/Space/Escape returns to song select |
| Replay / back | Clears session/score/combo, restarts audio; back stops playback |
| Judgment art | Graffonti PNGs now used; Billo alternatives retained |
| Background | Pixelated menu background by default; custom level Sprite overrides it |
| Main menu | Hypik logo, black Sportypo menu sprites with white backdrops, slowly rotating user-supplied background |
| Hardware, calibration, pause/seek | Not implemented |
| Settings / Quit | Settings is an intentional no-op; Quit exits a build or editor Play Mode |
| Multi-song menu | Not implemented |

Unity **6000.5.8f1**, **C#**, **uGUI**, and Unity's **Input System** are used. The original countdown proof has been replaced by falling notes; the saved runner's class name `CountdownPrototype` is retained for scene compatibility.

## Follow the first note through the game

```json
{ "time": 1.5, "lane": 0, "type": "tap" }
```

This means press **D at 1.5 seconds into the audio**.

1. Main-menu Start opens song select; clicking the song row or pressing Enter/Space passes the selected level to `CountdownPrototype.BeginLevel`.
2. Unity's JSON parser populates `BeatmapData`. The validator copies/sorts notes and checks their times, lanes, actions, holds, and speed.
3. A fresh `BeatmapSession` stores the timing/score settings and starts at score/combo zero.
4. Audio is loaded and scheduled three seconds ahead on the DSP clock.
5. At 1x, this note enters at song time **0.5**, one second before its target.
6. Each frame, its position is recalculated from current song time. Pressing D sends lane 0 and a timestamp to the session.
7. The closest eligible unfinished tap/hold on that lane is judged. Completion updates counts, combo, and score once.
8. A feedback event tells the view to show the appropriate Graffonti sprite and error. The completed block is hidden.

The image helps the player aim. **Its pixel position does not determine the judgment**; target and input timestamps do.

## Chart data and customization

A ScriptableObject is a saved Unity data asset. Our `SongDefinition` groups the correct AudioClip, chart TextAsset, optional background Sprite/tint, BPM, first-beat offset, and menu metadata. Different levels can reuse the same gameplay scripts.

JSON is a readable data format. A chart looks like:

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

| Field | Meaning |
| --- | --- |
| `speed` | Whole-chart visual travel multiplier; default 1x for older files |
| `time` | Target time in song seconds; a hold's starting time |
| `lane` | Control ID, 0 through 4 |
| `type` | tap / hold / release; action is separate from lane |
| `endTime` | When to release a hold; must follow its start |

`FromJsonOverwrite` fills a newly initialized data object so a missing speed preserves its 1x default. Validation rejects nonpositive/nonfinite speed, bad actions/lanes, invalid song times, duplicate same-lane timestamps, and notes inside a prior same-lane hold. Different lanes at the same time form a chord.

Times include silence in the audio file. `1.5000` and `1.5` mean the same thing. BPM drives the corner circle pulse; it does not shift chart times. At 120 BPM, quarter-note beats are 0.5 seconds apart, but we author their absolute times ourselves. There is no automatic beat detection or chart editor.

Create a level via **Assets → Create → Bop Beat → Level**, assign music/chart/background, then assign it to **Menu Flow → Sample Song**. The song menu has one slot. Empty Background uses the pixelated menu backdrop. Set BPM and Beat Offset Seconds to align the circle pulse. The optional background can be replaced independently of the lanes.

## Audio time and input time

DSP means digital signal processing. `AudioSettings.dspTime` is Unity's audio clock. We schedule playback with `AudioSource.PlayScheduled`, then use the same start reference:

```text
scheduledStart = audioClockNow + 3
songTime = audioClockNow - scheduledStart
```

If audio starts at DSP time 103 and the clock reads 104.5, song time is 1.5. Before playback, song time is negative: the preparation period. It does not change note times and permits early hits for a time-zero note.

Reading this clock avoids accumulating position or timing drift by adding a small movement each frame. The UI can refresh less often while target timestamps keep their meaning.

Input events have timestamps too. To reduce the error of an event waiting for a frame:

```text
eventAge = max(0, realtimeNow - inputEventTimestamp)
inputSongTime = audioClockNow - scheduledStart - eventAge
```

This does not remove physical audio/display/controller latency or guarantee correctness through severe stalls. Real hardware and calibration are future work. Times use `double` for precision. Nonzero grade boundaries use a tiny `1e-9`-second tolerance for decimal rounding; the requested zero Perfect window is checked exactly with no tolerance.

## How falling positions and sizes work

We use screen coordinates increasing downward. The current field starts at y=0 and target centers are at y=510 in the 1280 × 720 design.

```text
travelSeconds = 1 / speed
pixelsPerSecond = (targetY - topY) * speed
noteCenterY = targetY - (noteTargetTime - songTime) * pixelsPerSecond
```

For a target 100 pixels down and a 1x note due at song time 10:

| Song time | Time since entrance at 9 | Center y |
| --- | --- | --- |
| 9.0 | 0.0 s | 0 px |
| 9.2 | 0.2 s | 20 px: 20% of the journey |
| 9.5 | 0.5 s | 50 px |
| 10.0 | 1.0 s | 100 px: target |

At 2x it enters at 9.5 and still reaches the target at 10.0. **Speed changes appearance/travel time, not music pitch, hit windows, or chart timestamps.** The convention above 1x is a prototype choice; only the one-second 1x example is user-confirmed.

A tap's thickness is `pixelsPerSecond * 0.04`, visually clamped to 8–96 pixels. Its width fits its fixed lane. A hold's duration becomes length: `(endTime - time) * pixelsPerSecond`. Its tail uses the same position formula with endTime. Once held, the head stays at the target while the tail approaches. The long body is clipped to the field. Release notes have an R marker.

Blocks are created once per chart slot and reused on replay. The view hides completed and offscreen notes and clips drawing with RectMask2D. It currently scans the small chart, so large charts need profiling before we claim performance guarantees.

## Timing grades and note states

```text
signedError = inputSongTime - targetTime
abs(error) == 0 → Perfect
otherwise <= 0.15 → Good
otherwise <= 0.4 → OK
otherwise → outside the hit window
```

Negative error is early, positive is late. 0.15 s is 150 milliseconds. Boundaries are inclusive and editable under **Gameplay Prototype Screen → Countdown Prototype → Timing** outside Play Mode. A zero Perfect window requires the input timestamp to equal the note timestamp; it is extremely difficult to achieve with ordinary live keyboard timing.

For a note at 1.5: 1.5 is Perfect (0), 1.45 and 1.65 are Good (−0.05 and +0.15), 1.8 is OK (+0.3), and 1.95 is too late (+0.45). An unmatched press adds no separate Miss; an unplayed note expires after its latest allowed time. A wrong lane may hit a different note there, but cannot hit the intended lane.

Nearest unfinished matching lane/action wins; ties choose the earlier note. Closely spaced notes can have overlapping windows. Each note moves among **Waiting**, **Holding**, and **Complete** states. Complete excludes it from further scoring.

- **Tap:** a fresh press completes it. Holding a key or repeating the same press cannot earn extra taps.
- **Hold:** press at the head, release at the tail. The final grade is the worse endpoint. Perfect start + OK release gives one OK. Start feedback is provisional; no points/combo until completion. Early/late failure gives one Miss.
- **Standalone release:** hold beforehand, then release at its target. Letting go without first holding cannot score it.

`Advance(songTime)` expires unplayed notes/holds without needing player input. The song completes after both audio duration and the last note's allowed late window; results remain visible.

## Score and combo: explain the actual arithmetic

The user confirmed the formula and Perfect-only combo. Current base-point defaults are **Perfect 1000 / Good 500 / OK 100 / Miss 0**; these are provisional, editable **Scoring** values, not confirmed balancing.

We update combo first: Perfect adds one; Good/OK/Miss reset to zero. Then:

```text
pointsForNote = hitval + hitval * (combo / 25.0)
score += pointsForNote
```

| Result sequence | Combo after result | Points for this note | Total score |
| --- | --- | --- | --- |
| First Perfect | 1 | 1000 + 1000 × 1/25 = 1040 | 1040 |
| Second Perfect | 2 | 1080 | 2120 |
| Good | 0 | 500 | 2620 |
| Perfect | 1 | 1040 | 3660 |
| OK | 0 | 100 | 3760 |
| Miss | 0 | 0 | 3760 |

The 25th consecutive Perfect receives twice its base value. The formula has no cap. Division uses `25.0` so it is fractional rather than truncated integer division. Score preserves fractions and displays up to two decimal places. A hold scores once when complete, and chords score one note per lane. Applying the new combo before scoring and resetting on Miss are documented prototype choices.

## Code map

| File | Responsibility / useful methods |
| --- | --- |
| `Assets/Scripts/Menus/MenuFlow.cs` | Panel flow: StartSelectedSong, SetScreen |
| `Assets/Scripts/Menus/SongDefinition.cs` | Saved music/chart/background references |
| `Assets/Scripts/Beatmaps/BeatmapData.cs` | Data and ValidateAndSort |
| `Assets/Scripts/Beatmaps/JudgmentSettings.cs` | Editable timing and base points |
| `Assets/Scripts/Beatmaps/BeatmapSession.cs` | Grade, Press, Release, Advance, Finish; score/state |
| `Assets/Scripts/Gameplay/CountdownPrototype.cs` | Audio scheduling, timestamped input, replay/back |
| `Assets/Scripts/Gameplay/NoteTravel.cs` | Position, HoldLength, BlockHeight |
| `Assets/Scripts/Gameplay/BeatPulse.cs` | Decorative circle scale from song time/BPM/beat offset |
| `Assets/Scripts/Gameplay/FallingNoteView.cs` | Generated UI, rectangles, art, score/combo display |
| `Assets/Data/Beatmaps/FiveBeatDemo.json` | Actual chart |
| `Assets/Tests/EditMode` / `PlayMode` | Rule/geometry tests and running Unity flow |

The session is plain C#: it does not depend on Unity audio or images. The runner supplies inputs/time, and an event notifies the view of judgments. This separation makes rules testable and visuals replaceable. The scene retains its existing component references. Generated gameplay Hierarchy edits in Play Mode are temporary; change FallingNoteView for persistent layout edits.

The supplied Sportypo demo font renders digits as symbol graphics, discovered in the actual Unity screen check. Score/combo labels use Sportypo, while numbers use bold italic LegacyRuntime glyphs for readability. This is a font-asset limitation, not a scoring bug. Graffonti judgment sprites preserve the supplied colors; Hypik/Sportypo title/menu art is now used by the main menu.

## Gameplay visual polish and BPM

Gameplay uses the supplied pixelated menu texture whenever the level has no custom background. It covers the display without stretching, trims the thin source-image edge using UVs, and has a light dark shade for readability. A level-specific Sprite/tint still overrides this default. The scene runner holds the default texture and black Boopit art references; its layout is generated by FallingNoteView.

Holds are now one solid-looking rectangle: the body, head, and tail share the same width, x position, and opaque lane color. Their lengths and positions still come from head/end timestamps; this is a visual change to the existing press/hold/release rules. The renderer clips very long bodies to the field.

Judgments appear over the middle of the lane field. A non-interactive CanvasGroup fades both art and timing detail together, from alpha 1 to 0 over 0.6 real-time seconds. A new judgment restarts the fade; replay clears it. Miss uses the same fading text fallback. Feedback continues fading when the song finishes.

Every level already had BPM; it now drives the circle. At 120 BPM, a beat is 60/120 = 0.5 seconds. We subtract Beat Offset Seconds from absolute song time and take the remainder modulo the beat duration to find how far into the current beat we are. At each beat the circle reaches scale 1.08 and eases back over up to 0.18 seconds. Computing from the same song clock avoids accumulating frame-time drift. The offset lets the first beat begin after intro silence. Negative preparation time and finished playback keep normal scale, and replay restarts at the scheduled audio origin.

This is a constant-tempo visual pulse, not automatic beat detection. Set BPM and Beat Offset Seconds yourself on the level asset; neither changes chart hit times, note speed, grades, or scoring. Songs with tempo changes would need a tempo map later. Black boopit PNG artwork is rendered directly from the supplied Hypik logo font by Tools/GenerateBoopitArt.py and used inside the corner circle.

Likely question: **Why does changing BPM not move notes?** Notes use explicit seconds in JSON. BPM drives the decorative pulse independently. **Why use the audio clock for animation?** It stays tied to the song across variable frame rates and replay.

## How the main menu works

The title follows the reference in Assets/Art/menu: a large Hypik boopitmania logo, left-aligned Start/Settings/Quit, and the supplied pixelated-image.jpg background. The user selected the pixelated version instead of the high-resolution source. Point filtering keeps its pixelated edges crisp while enlarged. The latest art reference uses offset copies: the logo has a light-purple (#D650FF) front over an identical dark-purple (#A700CC) copy shifted left 8/down 6 design pixels. Both use a white Hypik glyph sprite tinted in Unity. Each black menu sprite uses uGUI Shadow to draw one identical white copy shifted left 5/down 5 pixels beneath it. This gives a flat layered edge, replacing the surrounding outlines. Original source art is preserved. The revision changes only the displayed art; layout, controls, rotation, and game logic stay the same.

`TitleMenuController` rotates only the background, clockwise at 2 degrees per second using unscaled time. Text and buttons remain upright. Its short side is at least the screen diagonal times 1.05, so a full rotation cannot uncover the viewport corners. It recalculates size when the Canvas size changes; this is deliberate overscan/cropping, not distortion of the image's aspect ratio.

Unity's EventSystem and explicit Button navigation handle all four arrows: Up/Left select the previous title item and Down/Right select the next, wrapping through the three items. Song select uses arrows to switch between its row and Back; the score screen has one focused return button. MenuFlow assigns one private Input System UI submit action for Enter, numpad Enter, Space, and controller confirmation. One press invokes one button, even when it opens another screen. Lost focus is restored automatically. `MenuItemHighlight` enlarges the main-menu visual child smoothly to 1.06 times its size for hover/selection; the parent click area remains stable. Hover also focuses the button, so Enter/Space activates the item you are pointing at.

During gameplay, MenuFlow disables the EventSystem's navigation events, which include move and submit. Mouse HUD controls still work, and Escape/R remain gameplay shortcuts. This prevents Space from activating Back or Replay while it is also used by lane 2. The keyboard labels below the targets have been removed, but D/F/Space/J/K still play the five lanes. Menu navigation is re-enabled on return or results.

Start invokes MenuFlow.ShowSongSelect. Settings invokes an empty placeholder method, as requested. Quit calls Application.Quit in a built game; Unity ignores that call in the editor, so the editor branch stops Play Mode instead. Tests inspect Quit's saved callback without activating it and ending their own run.

`TitleMenuSetup.ApplyReferenceMenu` is an explicit editor rebuild of the title alone. The saved scene contains editable UI and references; routine playback does not rebuild the title. Song selection and falling-note gameplay keep their existing objects. Avoid rerunning the rebuild command after customizing the title unless you intend to replace those edits.

Likely questions: **Why enlarge the background?** To cover corners at any rotation. **Does one Space press also start the song?** A press on title Start opens only song selection; a fresh Enter/Space press on its row starts the song. **Does Settings work?** It is deliberately a no-op until settings are specified. **Why not test clicking Quit?** It would close the player or stop the test Play session, so we verify its wiring separately.

## Demo walkthrough

SongSelectLayout keeps the menu at the top and uniformly scales the 1280-wide reference layout to the actual viewport width, so taller Game views do not shift the controls down. It runs in Edit and Play Mode. The editor builder upgrades an older open SampleScene once if the layout component is missing, after import or exiting Play Mode; upgraded layouts remain editable. When upgrading an older loaded scene, the builder preserves custom gameplay art references and fills the two recent default-art fields only if they are empty.

The revised song select follows the latest reference: a static pixelated background scaled to cover the screen without stretching, Back at the upper left, a centered white Sportypo heading, and one rounded teal row with a white border. The whole row launches the demo. Back and the heading are font-rendered PNG sprites. The row title uses SpriteLettering to assemble the exported character sprites, so changing the level asset's Song Title updates it automatically. The renderer uses the font's glyph spacing/offset metrics, fits one line inside the label, and reuses Image objects. Uppercase text uses the kit's matching lowercase sprites. Punctuation/non-Latin titles fall back to the source font if they are outside the kit. The sprite approach replaced a softer native-font preview that wrapped the demo title onto two lines. Artist, difficulty, BPM, and cover still exist in level data but are omitted from this simpler screen. MenuFlow checks those optional UI references before updating them. The title menu retains its existing design; gameplay uses the same pixelated background with its five-lane layout. The background's thin white source edge is trimmed through UV coordinates, preserving the original JPG.

The requested white character kit contains a-z and 0-9 under Assets/Art/ClipArt/Sportypo_Characters. The supplied demo's digit glyphs are promotional logos; the user explicitly chose to export the original glyphs rather than substitute numbers. This is separate from gameplay's readable score/combo fallback. Tools/GenerateSongSelectArt.py renders the supplied font with Pillow and creates the rounded card graphic; it preserves Unity GUIDs when rerun.

1. Open/reopen SampleScene, Unity Play, focus Game view. Use arrows and Enter/Space (or mouse) to choose Start, then confirm the Five Beat Demo row. In song select, arrows switch between the row and Back; Escape returns to the title.
2. Explain the three-second preparation time and five target colors/keys.
3. D at exactly **1.5** would earn Perfect; with the new zero window, demonstrate an attainable Good near **1.55** instead. F near **2.075** demonstrates Good for its 2.0 note. Skip Space at **2.5** to show Miss.
4. K at **3.8** demonstrates OK for its 3.5 note. Read the actual displayed error if your timing differs.
5. F hold **6.5–8.0** shows the long body and release tail.
6. D + K at **10.0** demonstrates a chord.
7. Hold J early, release at **13.5** for the R note.
8. R clears score/combo and restarts audio. Escape stops audio and returns to song select; again returns to title.

To show customization: change chart speed from 1 to 2, save/import, replay. Notes enter later and travel faster, but hit times stay unchanged. Restore 1. Changing the first time from 1.5 to 1.75 changes when to hit it without rewriting gameplay. Restore the sample afterward.

## Likely judge questions

**Does the game listen to music and find beats?** No. The chart explicitly supplies times. The Cruel Angel generator calculates times from supplied tempo sections and assigns random lanes; it does not analyze the audio. Automatic music analysis remains future work.

**How does it stay synchronized?** Scheduled audio and note math share the DSP start time. Input queue age is accounted for. Physical latency still needs measurement/calibration.

**Why not use collisions?** Timestamp error gives a direct, testable timing grade and keeps the rules independent of pixel size and rendering speed.

**What does speed change?** Travel time, position, and duration-based block dimensions. It does not change target times or audio speed.

**Why JSON and level assets?** JSON is readable and easy to edit; the asset keeps its matching audio/chart/background together. Another level can reuse the same code.

**How do holds work?** Track their press and release, take the worse grade, and score once at completion. A missing release expires as Miss.

**Can I mash or keep holding?** Held-state tracking prevents duplicate presses from scoring the same note or subsequent taps. Unmatched inputs carry no additional penalty currently, so do not claim a complete anti-mashing system.

**What builds combo?** Completed Perfects only. Other judgments reset it. The score uses the resulting combo with fractional division by 25.

**Why those point values?** They are changeable prototype defaults. The requested scoring formula is implemented, but final balancing has not been selected.

**Can notes happen together?** Across lanes, yes. Duplicates at the same timestamp in one lane are rejected.

**What happens to bad data?** Checks reject invalid chart structure/times/speed/holds before playback, and the UI displays an error.

**Is the Bop It connected?** Not in this Unity implementation. An adapter must translate hardware actions into lane press/release events and useful timestamps; we still need to validate physical hold/release support.

**What about low FPS?** Positions are recomputed from audio time, not accumulated frame movement. Input timestamps reduce queue delay. Severe stalls and device latency have not been fully characterized.

**Can it pause?** Not yet. Pause/resume needs to preserve audio position and rebase the clock; Time.timeScale alone would not handle this audio-clock design correctly.

**Can it support huge maps?** The current demo is small and scans notes. Per-lane indexes and pooling only visible notes are potential optimizations after profiling.

**What was challenging?** Concrete decisions you can inspect are audio/input synchronization, decimal boundaries, holds, preventing duplicate results, and keeping scoring separate from rendering. Describe your personal challenge only after you actually worked through it.

**How did you test it?** See the dated evidence section below. Tests cover rules and geometry separately, plus a Unity flow with scheduled audio, simulated input, rendering references, and replay/back. They do not prove physical Bop It accuracy.

## Assistance and honest attribution

Codex implemented the menus, chart model, timing engine, first countdown proof, falling-note renderer, scoring, and automated tests in this chat. It generated the placeholder background/demo audio and used Python/Pillow to render the supplied Billo/Graffonti/Hypik/Sportypo fonts into clip art. It also implemented the reference-menu setup, keyboard/mouse interaction, background rotation, responsive song-select layout, and gameplay polish (uniform holds, centered fading feedback, pixelated background, and a BPM-driven circle pulse). It wrote this guide by inspecting the code and test evidence; the PDF skill was used to inspect the supplied menu reference. The fonts, color swatches, screen reference, concept, desired chart behavior, speed example, timing windows, and score formula came from the user. Add other teammates' actual contributions to the log.

A disclosure to adapt after studying:

> We used Codex to help write and test the Unity code. We supplied the gameplay requirements, reference layout, timing windows, and scoring formula. I've worked through how a chart entry becomes a moving block and a judged input, and can demonstrate that by changing speed or calculating a score. Hardware integration is separate work.

If you cannot explain a part: **“Codex helped with that, and I haven't fully worked through it yet. I can explain [what you understand] and show where this part is implemented.”** Do not claim learning or authorship you cannot demonstrate.

## Practice without reading the answers

| Question / experiment | Expected answer |
| --- | --- |
| Target 2.0, press 1.85? | −0.15: early Good |
| Exactly 0 / 0.15 / 0.4 error? | Perfect / Good / OK |
| A tiny nonzero error, such as 0.000001? | Good; zero Perfect has no allowance |
| 100 px journey, 1x, 0.2 s after entrance? | 20 px, 20% down |
| A note at 10.0 at 2x enters when? | 9.5; it still hits at 10.0 |
| First two default Perfect scores? | 1040 then 1080; total 2120 |
| A Good after them? | Combo 0, +500, total 2620 |
| Hold begins Perfect, releases OK? | One OK; score only when it completes |
| Press a tap too early and keep holding? | No new press at target; it misses unless released/re-pressed in range |
| Change BPM only? | Circle pulse tempo changes; chart timestamps stay unchanged |
| Make speed zero or hold end before start? | Validation blocks launch; restore afterward |

Open NoteTravel.Position, BeatmapSession.Grade, and Finish. Trace one movement, one judgment, and one score yourself. Then replay a modified chart and explain the observation without memorizing all the code.

## Team learning and challenge log


Fill this with true experiences as the weekend progresses. Personal learning cannot be inferred from tests passing.

| Date / person | What we tried or changed | Challenge / observation | What we learned and can now explain | Tool or person that helped | Evidence |
| --- | --- | --- | --- | --- | --- |
| To fill in | | | | | |

Useful prompts: What did you believe before trying it? What surprised you? Which test or demo changed your understanding? Which part could you now change without AI assistance? What still needs help?

Recorded implementation evidence you can study: the grade code uses a tiny numeric tolerance at nonzero decimal boundaries and strict equality for zero Perfect; note state prevents counting a result twice; the Play Mode test explicitly configures simulated input for a headless test environment and restores those settings afterward. These are specific engineering decisions, not claims about any person's learning.

## Evidence and maintenance

- Source of truth: PROJECT_TRUTH.md.
- Previous countdown checks passed 20 Edit Mode cases and one Play Mode scenario; those saved results describe the older milestone.
- On 2026-10-10 the falling-note implementation passed **35 Edit Mode cases and one Play Mode scenario**, compiled by Unity 6000.5.8f1. Results: `Logs/FallingNoteEditModeResults.xml` and `Logs/FallingNotePlayModeResults.xml`. Rendered, visually inspected preview: `Logs/FallingNotePrototype.png`. Tests include movement/speed geometry, boundary judgments, score/combo resets and fractional math, holds/releases, a full perfect chart (53,120 points), and actual Unity menu/audio/input/render/replay/back behavior. The source and scene matched the tested copy. Test runs used a temporary copy; physical hardware/latency remains untested.
- Before the later offset-copy art revision, the title redesign passed **both Play Mode scenarios** on 2026-10-10, including the existing gameplay regression check. `Logs/TitleMenuPlayModeResults.xml` records keyboard wrapping/Space, Settings no-op, mouse hover/click, enlargement, pixelated texture/Point filtering, clockwise rotation/overscan, and saved Quit wiring. `Logs/ReferenceTitleMenu.png` was visually inspected. All 92 existing song-select serialized objects remained unchanged (normalized newlines), and the final scene/source/assets matched the tested copy; see `Logs/TitleMenuVerification.txt`. The temporary test project was removed afterward. Quit's callback is checked without executing it, because it intentionally ends the player session.
- Historical test result XMLs and compact verification receipts remain for reference; dates/milestones matter when citing them. Superseded previews and raw logs were removed during the user-requested cleanup below.
- The later offset-copy art revision passed the existing menu scenario again on 2026-10-10 (**1 passed, 0 failed**): `Logs/OffsetMenuPlayModeResults.xml`. `Logs/OffsetTitleMenu.png` was visually inspected. `Logs/OffsetMenuArtVerification.txt` confirms only seven art blocks and the old logo Outline changed; 167 other scene blocks stayed unchanged, including buttons, controls, background settings, song selection, and gameplay. The scene/builder/test matched the tested copy, which was removed afterward. Original clip-art PNGs remain preserved; the colored copies are composed in Unity.
- The final song-select/glyph-kit revision passed **both Play Mode scenarios** on 2026-10-10, including keyboard Back and an actual mouse-click song-row launch, plus the existing gameplay/audio/scoring/replay checks. Results: `Logs/SongSelectPlayModeResults.xml`; scope/source/art verification: `Logs/SongSelectVerification.txt`. The final inspected screen is `Logs/ReferenceSongSelect.png`; the 36 original white glyphs are previewed in `Logs/SportypoGlyphSheet.png`. All 81 existing title/gameplay/shared serialized blocks remained unchanged. The final files matched the tested copy, which was removed afterward. The screenshot exposed a soft, wrapped native-font title; replacing it with sprite lettering fixed the presentation while preserving automatic title updates from the level asset.

Maintain this guide alongside code and PROJECT_TRUTH.md. Update status, explanation, demo, Q&A, tool attribution, and evidence when behavior changes. Preserve the distinction between confirmed rules and adjustable defaults. Never invent personal experiences or claim tests passed before running them.

Latest gameplay-polish evidence (2026-10-10): **37 Edit Mode cases and 3 Play Mode scenarios passed** with Unity 6000.5.8f1. Results are in `Logs/GameplayVisualEditModeResults.xml` and `Logs/GameplayVisualPlayModeResults.xml`. Added cases exercise beat pulse timing/offset/replay/finish, opaque rectangular holds, pixelated/background overrides, and centered feedback fading/retriggering/Miss/reset. Existing timing/scoring/audio/input/menu flow checks still pass. The real sample hold-start preview `Logs/GameplayVisualPrototype.png` was inspected with Perfect art visible. `Logs/GameplayVisualVerification.txt` records final source/scene/art agreement and 157 preserved scene blocks; only two gameplay art references were added. Testing used a separate temporary Unity project to avoid disturbing the open editor. Constant BPM is supported; automatic beat detection, tempo changes, and physical hardware timing remain separate work.

The subsequent song-select screenshot correction (2026-10-10) passed **4 Play Mode scenarios**, including the previous menu/gameplay/visual scenarios and resizing checks for top alignment and uniform width fitting across wide, 4:3, and portrait dimensions. `Logs/FixedSongSelect.png` (1280x720) and `Logs/FixedSongSelectTall.png` (1024x768) were visually inspected. Results: `Logs/SongSelectFixPlayModeResults.xml`; final source/scene verification: `Logs/SongSelectFixVerification.txt`. The correction preserves 156 existing scene blocks and changes only the layout object/transform plus one added component. A separate temporary test project preserved the user's open editor. The live UI connection was unavailable; the one-time older-open-menu refresh is implemented, but its live execution was not visually verified. Stop Play Mode and allow import before checking the refreshed screen.

## Menu controls and lane labels — 2026-10-10

Latest menu-control verification (2026-10-10): Unity 6000.5.8f1 compiled and passed **5 Play Mode scenarios**. Checks cover all menu arrows, Enter/Space confirmation, one activation per press without confirming the next screen, focus recovery, results return, mouse controls, and absence of keyboard labels beneath targets. During gameplay, a Space press still reaches lane 2 with Replay selected and does not restart the session. Existing audio/scoring/replay/visual/layout checks also pass. All 79 checked source/metadata/test/data/scene/core media files matched the tested copy. Evidence: Logs/MenuNavigationPlayModeResults.xml and Logs/MenuNavigationVerification.txt. Quit is checked by wiring rather than executed. Restart Play after import and rebuild exported executables. The isolated project and raw log are removed after verification; the live editor and physical hardware were not tested. Earlier dated entries retain the controls in effect at their time.

## Space cleanup — 2026-10-10

At the user's request, removed old test/import logs, superseded preview PNGs, Python bytecode cache, and the IDE upgrade report. Removed about 30 MB (29.37 MiB); kept current title/song-select/gameplay/glyph previews, all test result XMLs and compact verification receipts. Some historical preview/raw-log paths earlier in this document now describe removed artifacts. Source assets, original fonts/license/reference files, requested clip art (including original demo digits), scripts, tests, scene, beatmaps/audio, generators, package manifests, settings, and project documentation were preserved.

All 286 protected source/asset/metadata/settings/generator files matched pre-cleanup SHA256 hashes. Unity and Visual Studio remained open, so Library (~3.2 GiB), active Temp/current logs, .vs, and generated .csproj/.sln files were left intact. No new gameplay tests were run for this file cleanup. Details: Logs/CleanupReceipt.txt.

After the user saved and closed Unity, the authorized Library cache removal freed 3.21 GiB. All 286 protected source/asset/package/settings/generator files matched SHA256 hashes afterward. Library contains Unity's generated imported assets, compiled code, and package cache; the original project files remain in Assets, Packages, and ProjectSettings. Reopening Unity rebuilds Library, takes longer, and uses that disk space again. No gameplay changed, and Unity was not relaunched just for cleanup verification. Details remain in Logs/CleanupReceipt.txt.

## Cruel Angel chart and finished-song results — 2026-10-10

The menu's single row now features Cruel Angel Thesis, linked to the supplied TV-size MP3. The original Five Beat Demo level remains in Assets/Data/Songs/SampleSong.asset; assign it to Menu Flow > Sample Song when demonstrating the specific hold/release examples earlier in this guide. The new chart contains taps only and has speed 1.

The user supplied the tempo sections. One beat lasts `60 / BPM` seconds, so two beats last `120 / BPM`: 1.5 seconds at 80 BPM and 0.9375 seconds at 128 BPM. The generator starts at time 0, then treats 16 seconds as the new section's first beat. It chooses each lane with System.Random seeded at 80128, so randomness is baked into the saved JSON and replay is consistent. It covers the imported audio duration. Tools > Bop Beat > Regenerate Cruel Angel Chart replaces the chart/metadata; it does not detect beats from sound, and regeneration overwrites manual chart edits. Actual recording alignment may need adjustment.

The level's BPM starts at 80. Its optional Tempo Changes array contains time 16, BPM 128. SongDefinition.TempoAt selects the current tempo and section anchor; FallingNoteView passes them to BeatPulse. This changes the circle decoration. Gameplay still judges explicit JSON seconds. Changing tempo metadata alone never rewrites notes. Ordered tempo changes now work; older constant-tempo limitation statements describe the earlier milestone.

When the song finishes and every note is judged, CountdownPrototype stops audio/input and calls MenuFlow with the completed session. ScoreScreenView copies the score, best combo, and four judgment totals into a separate pixelated screen. Labels/title use Sportypo, while numbers use the same readable fallback as gameplay. The screen is created once at runtime and reused; a later run replaces its values. Click Song Select or press Enter/Space/Escape to return. Quitting a song early skips results. These are final run statistics, not persistent high scores.

Codex implemented this chart generator, tempo-change support, results flow, and verification tests from the user's requirements. This records tool assistance, not a claim about any teammate's personal learning.

Verification: Unity 6000.5.8f1 passed **39 Edit Mode cases and 5 Play Mode scenarios** for this revision. The imported song is 94.903 seconds; the 96-note chart ends at 94.75 and keeps the final 0.4-second late window before results. Chart tests check every-two-beat spacing, all five lanes, bounds, all-Perfect playthrough, and tempo validation/switching. A short 4-note audio fixture tests automatic result display (1640 points, max combo 1, one of each grade), Enter/button return, fresh result values on the next run, and abort behavior. The inspected score preview uses that fixture; it is not a full-song performance. Existing menu/input/audio/scoring/visual/resizing tests still pass. All 79 checked final files match the tested copy by SHA256. XML results and the receipt are Logs/CruelAngelEditModeResults.xml, Logs/CruelAngelPlayModeResults.xml, and Logs/CruelAngelVerification.txt; inspected previews are Logs/CruelAngelSongSelect.png and Logs/ScoreScreenPreview.png. The temporary test project and redundant raw logs were removed to reclaim space. Reopen SampleScene after Unity imports, then Play > Start > Cruel Angel Thesis. Alignment to the recording and physical controller timing remain unverified.

## Latest timing revision — 2026-10-10

The user now requests Perfect at exactly zero error, Good through ±0.15 seconds, and OK through ±0.4 seconds. TimingSettings, the judgment engine, saved SampleScene values, builder text, and the current explanation/examples above reflect that choice. Historical test receipts describe their own earlier revisions. Zero Perfect has no numeric tolerance, so even a tiny nonzero error earns Good instead. Good/OK decimal boundaries keep the existing 1e-9-second tolerance. Holds still take the worse endpoint grade, so both endpoints must be exactly timed for a Perfect hold. Scoring and Perfect-only combo rules remain the same; with this exact window, live Perfects/combo are extremely difficult. Codex implemented this change at the user's explicit request.

Reopen SampleScene after Unity imports so an older loaded scene receives the changed serialized timing values, then start/replay the song. Rebuild the Windows executable if you already exported it; previous builds contain their previous settings.

Verification: Unity 6000.5.8f1 passed **39 Edit Mode cases and 1 targeted Play Mode scenario**. Updated boundary cases prove zero Perfect and tiny nonzero Good, plus inclusive early/late ±0.15 Good and ±0.4 OK. The runtime test verifies saved scene settings, actual keyboard grading and displayed score/combo, replay reset, and audio stop on return. All 79 checked final files matched the tested isolated copy. Results/receipt: Logs/TimingEditModeResults.xml, Logs/TimingPlayModeResults.xml, Logs/TimingVerification.txt. The isolated test copy and redundant raw logs were removed; the open main editor was preserved. Existing musical-alignment and physical-hardware limitations remain.
