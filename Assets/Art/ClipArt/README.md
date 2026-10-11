# Text clip art

Rendered directly from the supplied fonts at 256 px, with transparent backgrounds and antialiased edges. Original Billo judgments are retained.

| Asset | Exact text | Color | Font source |
| --- | --- | --- | --- |
| `Judgments_Graffonti/Perfect.png` | perfect | `#F51E69` | `Assets/Art/graffonti/graffonti.3d.drop.[fontvir.us].ttf` |
| `Judgments_Graffonti/Good.png` | good | `#1EF65F` | `Assets/Art/graffonti/graffonti.3d.drop.[fontvir.us].ttf` |
| `Judgments_Graffonti/Ok.png` | ok | `#FFFB80` | `Assets/Art/graffonti/graffonti.3d.drop.[fontvir.us].ttf` |
| `Logos_Hypik/Boopitmania.png` | boopitmania | `#A700CC` | `Assets/Art/hypik/hypik.otf` |
| `Logos_Hypik/Bopitmania.png` | bopitmania | `#A700CC` | `Assets/Art/hypik/hypik.otf` |
| `Menu_Sportypo/Start.png` | start | `#FFFFFF` | `Assets/Art/sportypo/Sportypo-Reguler-Demo.ttf` |
| `Menu_Sportypo/Settings.png` | settings | `#FFFFFF` | `Assets/Art/sportypo/Sportypo-Reguler-Demo.ttf` |
| `Menu_Sportypo/Quit.png` | quit | `#FFFFFF` | `Assets/Art/sportypo/Sportypo-Reguler-Demo.ttf` |
| `Menu_Sportypo/Back.png` | back | `#FFFFFF` | `Assets/Art/sportypo/Sportypo-Reguler-Demo.ttf` |

The font defines the letter shapes and case appearance; the input strings are lowercase as requested. Graffonti uses the supplied 3D Drop variant.

Menu labels default to white because no menu color was specified. White sprites can be tinted through the Unity UI Image Color field.

All PNGs have individual Sprite (2D and UI) import metadata. Use Source Image and Set Native Size, or Preserve Aspect, to avoid stretching. Keep Image Color white to display the judgment/title hex colors exactly.

`Sportypo_Characters` contains white transparent a-z and 0-9 character sprites. The demo's original 0-9 glyphs are promotional logos, deliberately preserved at the user's request; they are not readable numerals. `SongSelect_Sportypo` contains the white heading and rounded teal/white-border card. Song select uses the existing Back sprite and SpriteLettering to compose the dynamic level title from the glyph kit. Regenerate with `Tools/GenerateSongSelectArt.py` (Python/Pillow); existing Unity GUIDs are retained. The manifest includes glyph advance/offset metrics so the sprite title stays sharp and fits one line.

Graffonti perfect/good/ok sprites are now connected to falling-note gameplay feedback. The title logo uses two copies of the white Hypik Boopitmania glyph mask: #D650FF in front, #A700CC behind at (-8, -6) design pixels. Sportypo start/settings/quit are black with a single white copy underneath at (-5, -5), rendered by uGUI Shadow. Original PNGs and other logo/back alternatives are retained. Settings is a no-op; Quit exits through MenuFlow, rather than through the artwork itself. OK timing and scoring are implemented separately in BeatmapSession, not by the image itself.

Gameplay circle: `Logos_Hypik/BoopitBlack.png` is black `boopit` lettering directly rendered from `Assets/Art/hypik/hypik.otf`, with transparency. Regenerate with `Tools/GenerateBoopitArt.py`; existing sprite GUIDs are retained.
