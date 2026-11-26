using System.Collections.Generic;
using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeCrowd
    {
        readonly Dictionary<Color,Material> supporterMaterials = new();

        Material SupporterDye(Color colour)
        {
            if (supporterMaterials.TryGetValue(colour, out var cloth)) return cloth;
            cloth = new Material(game.popMaterial) { name = "Grandstand dye", enableInstancing = true };
            cloth.SetColor("_BaseColor", colour);
            cloth.SetFloat("_Smoothness", .15f);
            supporterMaterials.Add(colour, cloth);
            return cloth;
        }

        void DressSupporters()
        {
            var faces = Resources.Load<Texture2D>("Court/Supporters");
            for (int i = 0; i < seats.Length; i++)
            {
                var seat = seats[i];
                if (!seat.body) continue;
                int side = seat.body.position.x < 0 ? 0 : 1;
                int row = i / DodgeBleachers.Columns;
                int variant = (i * 7 + row * 2) % 5;
                Color skin = DodgeColleges.Skin(variant);
                Color team = DodgeColleges.Colour(side);
                Color sweater = ((i + row) % 6) switch
                {
                    0 => DodgeColleges.Navy, 1 => new Color32(196,172,127,255),
                    2 => new Color32(220,208,178,255), _ => team
                };
                foreach (var mesh in seat.body.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Color dye;
                    switch (mesh.name)
                    {
                        case "Head":
                            if (faces)
                            {
                                var face = new MaterialPropertyBlock();
                                mesh.sharedMaterial = SupporterDye(Color.white);
                                face.SetColor("_BaseColor", Color.white);
                                face.SetTexture("_BaseMap", faces);
                                face.SetVector("_BaseMap_ST", new Vector4(1f, .125f, 0f, (7-variant)*.125f));
                                mesh.SetPropertyBlock(face);
                                continue;
                            }
                            dye = skin; break;
                        case "Jelly shirt": dye = sweater; break;
                        case "Jelly neck": case "Arm": case "Other arm": dye = skin; break;
                        case "Seated thigh": case "Seated shin": dye = DodgeColleges.Navy; break;
                        case "Marshmallow hat": dye = (i % 2 == 0) ? team : DodgeColleges.Gold; break;
                        default: continue;
                    }
                    mesh.sharedMaterial = SupporterDye(dye);
                    mesh.SetPropertyBlock(null);
                }
            }
        }
    }
}
