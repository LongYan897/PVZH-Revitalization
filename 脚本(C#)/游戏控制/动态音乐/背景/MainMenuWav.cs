using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controller.WavPlay;

public class MainMenuWav : Wav
{
    public override string Path => "res://素材/Audio/Main Menu.mp3";
    public override int InitTone => 0;
    public override Rhythm Kind => Rhythm.Ambient;
    public override Wav DefaultNext => null;
    public override Dictionary<string, Wav> Branches => [];
}
