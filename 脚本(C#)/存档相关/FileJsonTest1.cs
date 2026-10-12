using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FileExtension
{
	public partial class FileJsonTest1 : Node2D
	{
		public override void _Ready()
		{
			FileJson fj = new();
			fj.Open("res://数据(C#)/playerAsset.json")
				.OpenObject("CardCounts");
			fj.SetVariable<int>("Peashooter", 3);
			fj.SetVariable<int>("SunFlower", 2);
			GD.Print(fj.GetVariable<int>("Peashooter"));
			fj.Save();
		}
	}
}
