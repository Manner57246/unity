using UnityEngine;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    internal static class StoryPlate
    {
        internal static void Fit(VisualElement plate)
        {
            plate.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.width > 0 && !Mathf.Approximately(evt.oldRect.width,evt.newRect.width))
                    plate.style.height = Mathf.Round(evt.newRect.width * .66f);
            });
        }
    }
}
