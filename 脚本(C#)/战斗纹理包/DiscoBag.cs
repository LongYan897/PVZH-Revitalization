using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battle.Texture;

public class DiscoBag : TextureBag
{
    protected override Dictionary<string, Texture2D> Textures => new Dictionary<string, Texture2D>()
    {
        {"Left",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_bldg_left.png") },
        {"Right",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_bldg_right.png") },
        {"Base",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_base.png") },
        {"Cover",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_cover.png") },
        {"Height",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_high.png") },
        {"Water",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_low.png") },
        {"Ground1",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_lane1.png") },
        {"Ground2",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_lane2.png") },
        {"MainHole",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_manhole.png") },
        {"Top",GD.Load<Texture2D>("res://素材/地图/迪斯科/BGDisco_top.png") },
    };
}
