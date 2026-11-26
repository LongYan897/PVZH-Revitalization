using Godot;
using Pack;
using System.Collections.Generic;

namespace Battle.Texture;

public class DiscoBag : TextureBag
{
    protected override Dictionary<string, Texture2D> Textures => new Dictionary<string, Texture2D>()
    {
        {"Left","res://素材/地图/迪斯科/BGDisco_bldg_left.png".Load<Texture2D>() },
        {"Right","res://素材/地图/迪斯科/BGDisco_bldg_right.png".Load<Texture2D>() },
        {"Base","res://素材/地图/迪斯科/BGDisco_base.png".Load<Texture2D>() },
        {"Cover","res://素材/地图/迪斯科/BGDisco_cover.png".Load<Texture2D>() },
        {"Height","res://素材/地图/迪斯科/BGDisco_high.png".Load<Texture2D>() },
        {"Water","res://素材/地图/迪斯科/BGDisco_low.png".Load<Texture2D>() },
        {"Ground1","res://素材/地图/迪斯科/BGDisco_lane1.png".Load<Texture2D>() },
        {"Ground2","res://素材/地图/迪斯科/BGDisco_lane2.png".Load<Texture2D>() },
        {"MainHole","res://素材/地图/迪斯科/BGDisco_manhole.png".Load<Texture2D>() },
        {"Top","res://素材/地图/迪斯科/BGDisco_top.png".Load<Texture2D>() },
    };
}
