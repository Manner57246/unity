using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    // One reader per drawer. Closing a drawer pauses the current page in place.
    internal sealed class StoryReader
    {
        private sealed class Line
        {
            internal readonly TextElement Element;
            internal readonly string Full;
            internal readonly int[] Letters;
            internal readonly bool Enabled;

            internal Line(TextElement element)
            {
                Element = element;
                Full = element.text;
                Letters = StringInfo.ParseCombiningCharacters(Full);
                Enabled = element.enabledSelf;
            }
        }

        private readonly ScrollView drawer;
        private readonly List<Line> lines = new();
        private readonly IVisualElementScheduledItem timer;
        private Button skip;
        private int line, letter;
        private float nextLetter;
        private bool follow;

        internal StoryReader(ScrollView drawer)
        {
            this.drawer = drawer;
            timer = drawer.schedule.Execute(Tick).Every(16);
            timer.Pause();
            drawer.RegisterCallback<WheelEvent>(_ => follow=false,TrickleDown.TrickleDown);
            drawer.verticalScroller.RegisterCallback<PointerDownEvent>(_ => follow=false,TrickleDown.TrickleDown);
            drawer.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target is Label || e.target == drawer.contentContainer) Finish();
            });
        }

        // Prepare after adding the story choices, before adding navigation controls.
        internal void Prepare(VisualElement illustration)
        {
            Finish();
            bool afterHeading = false;
            foreach (var child in drawer.contentContainer.Children())
            {
                if (child.ClassListContains("title")) { afterHeading=true; continue; }
                if (afterHeading && child is TextElement text && !string.IsNullOrEmpty(text.text))
                    lines.Add(new Line(text));
            }
            if (lines.Count == 0) return;
            foreach (var item in lines)
            {
                item.Element.style.visibility = Visibility.Hidden;
                if (item.Element is Button) item.Element.SetEnabled(false);
            }
            skip = new Button(Finish) { text="显示全文" };
            skip.AddToClassList("story-skip");
            illustration.Add(skip);
            drawer.scrollOffset=Vector2.zero;
            line=letter=0;
            follow=true;
            nextLetter=0;
            timer.Resume();
        }

        private void Tick()
        {
            if (!drawer.enabledInHierarchy || line >= lines.Count) return;
            var item=lines[line];
            float now=Time.unscaledTime;
            if (now < nextLetter) return;
            letter++;
            int end = letter < item.Letters.Length ? item.Letters[letter] : item.Full.Length;
            item.Element.text=item.Full.Substring(0,end);
            item.Element.style.visibility=Visibility.Visible;
            char last=item.Full[end-1];
            float interval=item.Element is Button ? .032f : .048f;
            if ("，、：".IndexOf(last)>=0) interval=.14f;
            if ("。！？……\n".IndexOf(last)>=0) interval=.28f;
            if (follow)
            {
                float overflow=item.Element.worldBound.yMax-drawer.contentViewport.worldBound.yMax+12;
                if (overflow>1) drawer.scrollOffset+=Vector2.up*Mathf.Min(overflow,12);
            }
            if (letter >= item.Letters.Length)
            {
                line++; letter=0; interval+=.16f;
                if (line == lines.Count) { Finish(); return; }
            }
            nextLetter=now+interval;
        }

        internal void Finish()
        {
            timer.Pause();
            foreach (var item in lines)
            {
                item.Element.text=item.Full;
                item.Element.style.visibility=Visibility.Visible;
                if (item.Element is Button) item.Element.SetEnabled(item.Enabled);
            }
            lines.Clear();
            skip?.RemoveFromHierarchy();
            skip=null;
        }
    }
}
