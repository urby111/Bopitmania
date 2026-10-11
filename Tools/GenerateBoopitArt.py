"""Export black boopit lettering directly from the project's Hypik logo font."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from GenerateSongSelectArt import sprite_meta

root = Path(__file__).resolve().parents[1]
font = ImageFont.truetype(str(root / "Assets/Art/hypik/hypik.otf"), 256)
box = font.getbbox("boopit")
padding = 16
image = Image.new("RGBA", (box[2] - box[0] + padding * 2, box[3] - box[1] + padding * 2))
ImageDraw.Draw(image).text((padding - box[0], padding - box[1]), "boopit", font=font, fill="black")
path = root / "Assets/Art/ClipArt/Logos_Hypik/BoopitBlack.png"
image.save(path)
sprite_meta(path)
print(f"Exported black Hypik boopit: {image.width} x {image.height}, transparent PNG.")
