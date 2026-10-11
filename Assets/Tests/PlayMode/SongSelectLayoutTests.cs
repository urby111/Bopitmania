using System.Collections;
using BopBeat.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BopBeat.Tests
{
    public sealed class SongSelectLayoutTests
    {
        [UnityTest]
        public IEnumerator ResizingFitsTheWidthAndKeepsTheReferenceAtTheTop()
        {
            var screen = new GameObject("Test Viewport", typeof(RectTransform));
            try
            {
                var viewport = screen.GetComponent<RectTransform>();
                var layout = new GameObject("Song Layout", typeof(RectTransform));
                layout.transform.SetParent(screen.transform, false);
                layout.AddComponent<SongSelectLayout>();
                var frame = layout.GetComponent<RectTransform>();
                foreach (var size in new[] { new Vector2(1280, 720), new Vector2(1024, 768), new Vector2(1600, 900), new Vector2(720, 1280) })
                {
                    viewport.sizeDelta = size;
                    yield return null;
                    Assert.AreEqual(new Vector2(0.5f, 1), frame.anchorMin);
                    Assert.AreEqual(new Vector2(0.5f, 1), frame.anchorMax);
                    Assert.AreEqual(new Vector2(0.5f, 1), frame.pivot);
                    Assert.AreEqual(Vector2.zero, frame.anchoredPosition);
                    Assert.AreEqual(size.x, frame.rect.width * frame.localScale.x, 0.01);
                    Assert.AreEqual(frame.localScale.x, frame.localScale.y, "Keep the lettering proportions.");
                    var outer = new Vector3[4]; var inner = new Vector3[4];
                    viewport.GetWorldCorners(outer); frame.GetWorldCorners(inner);
                    Assert.AreEqual(outer[1].y, inner[1].y, 0.01, "Height changes must not push the menu down.");
                }
            }
            finally { Object.Destroy(screen); }
        }
    }
}
