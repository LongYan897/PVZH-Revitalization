using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controller;
public static partial class SceneManager
{
    private partial class FadeRect : CanvasLayer
    {
        private ColorRect ColorRectangle;
        private Color ToColor;
        private float Time;
        public FadeRect(Color fromColor,Color toColor,float time)
        {
            Layer = 99999;
            ColorRectangle = new ColorRect();
            ColorRectangle.Color = fromColor;
            ColorRectangle.Size = new Vector2(720,1280);
            AddChild(ColorRectangle);
            ToColor = toColor;
            Time = time;
        }
        public async Task Call(bool needQueueFree)
        {
            Tween tween = ColorRectangle.CreateTween();
            tween.TweenProperty(ColorRectangle, "color", ToColor, Time);
            await ToSignal(tween, "finished");
            if (needQueueFree)
            {
                QueueFree();
            }
        }
    }
    public static async Task ChangeScene(SceneTree tree,string scenePath)
    {
        var scene = Godot.ResourceLoader.Load<Godot.PackedScene>(scenePath);
        if (scene != null)
        {
            FadeRect fadeRect = new FadeRect(new Color(Colors.Black,0), Colors.Black, 0.5f);
            tree.Root.AddChild(fadeRect);
            await fadeRect.Call(true);
            tree.ChangeSceneToFile(scenePath);
            fadeRect = new FadeRect(Colors.Black, new Color(Colors.Black,0), 0.5f);
            tree.Root.AddChild(fadeRect);
            await fadeRect.Call(true);
            GD.Print($"Scene changed to: {scenePath}");
        }
        else
        {
            Godot.GD.PrintErr($"Failed to load scene: {scenePath}");
        }
    }
}
