"""Render the supplied Sportypo font directly; requires Python and Pillow.

Run from any directory. Existing Unity GUIDs are retained on regeneration.
The demo's numeral glyphs are deliberately exported unchanged at the user's request.
"""
from pathlib import Path
import json
import re
import string
import uuid
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/Art/ClipArt"
FONT_PATH = ROOT / "Assets/Art/sportypo/Sportypo-Reguler-Demo.ttf"
FONT = ImageFont.truetype(str(FONT_PATH), 256)
TEMPLATE = (ART / "Menu_Sportypo/Back.png.meta").read_text(encoding="utf-8")


def folder(path):
    path.mkdir(parents=True, exist_ok=True)
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n", encoding="utf-8")


def sprite_meta(path, border=None):
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        data = re.sub(r"(?m)^guid: .*", f"guid: {uuid.uuid4().hex}", TEMPLATE)
        data = re.sub(r"(?m)^    spriteID: .*", f"    spriteID: {uuid.uuid4().hex}", data)
        if border:
            data = data.replace("  spriteMode: 1", f"  spriteBorder: {{x: {border}, y: {border}, z: {border}, w: {border}}}\n  spriteMode: 1")
        meta.write_text(data, encoding="utf-8")


def word(text, path):
    box = FONT.getbbox(text)
    padding = 16
    image = Image.new("RGBA", (box[2] - box[0] + padding * 2, box[3] - box[1] + padding * 2))
    ImageDraw.Draw(image).text((padding - box[0], padding - box[1]), text, font=FONT, fill="white")
    image.save(path)
    sprite_meta(path)
    return image


def main():
    characters = ART / "Sportypo_Characters"
    song_select = ART / "SongSelect_Sportypo"
    folder(characters)
    folder(song_select)
    manifest = {"font": "Assets/Art/sportypo/Sportypo-Reguler-Demo.ttf", "color": "#FFFFFF", "fontSize": 256, "spaceAdvance": FONT.getlength(" "),
                "digits": "Original demo promotional-logo glyphs; not readable numerals.", "characters": []}
    sheet = Image.new("RGB", (1320, 1320), "#163947")
    labels = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 20)
    for i, character in enumerate(string.ascii_lowercase + string.digits):
        image = word(character, characters / f"{character}.png")
        bounds = FONT.getbbox(character)
        manifest["characters"].append({"character": character, "file": f"{character}.png", "width": image.width, "height": image.height,
                                      "advance": FONT.getlength(character), "offsetX": bounds[0] - 16, "offsetY": bounds[1] - 16})
        thumb = image.copy()
        thumb.thumbnail((180, 175), Image.Resampling.LANCZOS)
        x, y = (i % 6) * 220, (i // 6) * 220
        sheet.paste(thumb, (x + (220 - thumb.width) // 2, y + (185 - thumb.height) // 2), thumb)
        ImageDraw.Draw(sheet).text((x + 98, y + 192), character, font=labels, fill="#A9CBD5")
    (characters / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    (characters / "README.md").write_text(
        "# White Sportypo character clip art\n\n36 transparent PNGs: a-z and 0-9, rendered directly from the supplied Sportypo-Reguler-Demo.ttf at 256 pixels.\n\n"
        "The demo font does not provide readable numbers: its 0-9 glyphs are promotional logos. The user explicitly chose to export those original glyphs unchanged. These files are not replacement numeric artwork.\n\n"
        "Each PNG imports as a single Unity Sprite. Use a UI Image with white Color and Preserve Aspect. Regenerate with Tools/GenerateSongSelectArt.py (Python/Pillow); existing GUIDs are preserved.\n", encoding="utf-8")
    word("song select", song_select / "SongSelect.png")

    # New procedural UI graphic: rounded white border around a restrained teal gradient.
    scale = 4
    size = (512 * scale, 128 * scale)
    outer = Image.new("L", size)
    ImageDraw.Draw(outer).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius=32 * scale, fill=255)
    inner = Image.new("L", size)
    ImageDraw.Draw(inner).rounded_rectangle((8 * scale, 8 * scale, size[0] - 1 - 8 * scale, size[1] - 1 - 8 * scale), radius=24 * scale, fill=255)
    gradient = Image.new("RGBA", size)
    draw = ImageDraw.Draw(gradient)
    for x in range(size[0]):
        t = x / (size[0] - 1)
        color = tuple(round(a + (b - a) * t) for a, b in zip((13, 100, 117), (6, 52, 68))) + (255,)
        draw.line((x, 0, x, size[1]), fill=color)
    card = Image.composite(gradient, Image.new("RGBA", size, "white"), inner)
    card.putalpha(outer)
    path = song_select / "RoundedSongCard.png"
    card.resize((512, 128), Image.Resampling.LANCZOS).save(path)
    sprite_meta(path, border=32)
    logs = ROOT / "Logs"
    logs.mkdir(exist_ok=True)
    sheet.save(logs / "SportypoGlyphSheet.png")
    print("Exported 36 original white Sportypo glyphs, the heading, and rounded song-card art.")


if __name__ == "__main__":
    main()
