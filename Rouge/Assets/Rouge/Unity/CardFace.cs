using System;
using Rouge.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    internal sealed class CardFace : VisualElement
    {
        internal CardFace(CardRule card, bool compact, Reaction reaction=null)
        {
            name = "card-" + card.id;
            AddToClassList("folio-card");
            AddToClassList(card.rarity.ToString());
            if (compact) AddToClassList("compact");
            var title = new Label(reaction==null ? card.title : card.title+" · "+reaction.Name);
            title.AddToClassList("card-title");
            if(reaction!=null)title.AddToClassList("reactive-title");
            Add(title);
            var art = new VisualElement();
            art.AddToClassList("illuminated");
            art.style.backgroundImage = Resources.Load<Texture2D>("Folio/Cards/" + card.id);
            Add(art);
            var kind = new Label((card.IsSlash ? "斩击" : card.Category) + "  ·  " + card.Grade);
            kind.AddToClassList("card-type");
            Add(kind);
            if (!compact)
            {
                var text = new Label(card.Text+(reaction!=null ? "\n"+reaction.Description : ""));
                text.AddToClassList("card-rules");
                Add(text);
            }
            tooltip = card.title + " · " + card.Text;
            foreach (var part in Children()) part.pickingMode = PickingMode.Ignore;
        }

        internal void Bind(VisualElement root, CardRule card, Action tap, Action<Vector2> drop)
        {
            focusable = true;
            Vector2 start = default;
            CardFace ghost = null;
            int pointer = -1;
            RegisterCallback<PointerDownEvent>(evt => {
                if (evt.button != 0 || pointer >= 0) return;
                pointer = evt.pointerId; start = evt.position;
                this.CapturePointer(pointer); evt.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(evt => {
                if (evt.pointerId != pointer || Vector2.Distance(start,evt.position)<5 && ghost==null) return;
                if (ghost==null)
                {
                    ghost = new CardFace(card,false) { pickingMode = PickingMode.Ignore };
                    ghost.style.position = Position.Absolute;
                    root.Add(ghost); style.opacity=.45f;
                }
                var p=root.WorldToLocal(evt.position);
                ghost.style.left=p.x-70; ghost.style.top=p.y-48;
            });
            RegisterCallback<PointerUpEvent>(evt => {
                if (evt.pointerId != pointer) return;
                bool dragged = ghost!=null;
                this.ReleasePointer(pointer);pointer=-1;
                ghost?.RemoveFromHierarchy();ghost=null; style.opacity=1;
                if (dragged) drop(evt.position); else tap();
                evt.StopPropagation();
            });
            RegisterCallback<PointerCaptureOutEvent>(evt => {
                if (evt.pointerId != pointer) return;
                ghost?.RemoveFromHierarchy();ghost=null;pointer=-1;style.opacity=1;
            });
            RegisterCallback<NavigationSubmitEvent>(evt=>{ tap();evt.StopPropagation(); });
        }
    }
}
