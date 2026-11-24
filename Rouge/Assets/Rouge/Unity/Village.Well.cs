using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    public sealed partial class Village
    {
        private static readonly (string image, float hold)[] WellShots = {
            ("Riders",1.12f), ("Blade",.19f), ("Glint",.08f), ("Cut",.105f),
            ("Stagger",.13f), ("Kneel",.16f), ("Fall",.55f),
            ("Flinch",.13f), ("Rush",.23f), ("Scatter",.50f)
        };

        private IEnumerator StrikeAtWell()
        {
            busy=true;
            reader.Finish();
            menu.Clear();
            menu.Add(plate);
            drawer.scrollOffset=Vector2.zero;
            Text("村口井台","title");
            Text("村长动了动嘴。","note");
            reader.Prepare(plate);
            var root=GetComponent<UIDocument>().rootVisualElement;
            var resting=root.style.translate;
            var frames=new Texture2D[WellShots.Length];
            for (int i=0;i<frames.Length;i++)
                frames[i]=Resources.Load<Texture2D>("Village/"+WellShots[i].image);
            var draw=Resources.Load<AudioClip>("BladeFX/bladeDraw");
            var cut=Resources.Load<AudioClip>("BladeFX/backhandImpact");
            var crowd=Resources.Load<AudioClip>("Village/CrowdCry");
            float shakeStart=0, shakeLength=0, amplitude=0;
            try
            {
                for (int i=0;i<frames.Length;i++)
                {
                    var shot=WellShots[i];
                    plate.style.backgroundImage=frames[i];
                    switch (shot.image)
                    {
                        case "Blade":
                            foley.PlayCue(draw,.44f);
                            break;
                        case "Cut":
                            foley.PlayCue(cut,.70f);
                            shakeStart=Time.unscaledTime; shakeLength=.20f; amplitude=7;
                            break;
                        case "Flinch":
                            foley.PlayCue(crowd,.83f);
                            shakeStart=Time.unscaledTime; shakeLength=.65f; amplitude=4;
                            break;
                    }
                    float end=Time.unscaledTime+shot.hold;
                    do
                    {
                        float t=Time.unscaledTime-shakeStart;
                        if (t<shakeLength)
                        {
                            float strength=amplitude*(1-t/shakeLength);
                            root.style.translate=new Translate(
                                Mathf.Round(Mathf.Cos(t*85)*strength),Mathf.Round(Mathf.Sin(t*67)*strength*.64f));
                        }
                        else root.style.translate=resting;
                        yield return null;
                    } while (Time.unscaledTime<end);
                }
            }
            finally { root.style.translate=resting; }
            raid=Raid.Flight;
            busy=false;
            Refresh();
        }
    }
}
