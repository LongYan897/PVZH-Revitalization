using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Controller.WavPlay;

public class CustomWav : Wav
{
    public override string Path => throw new NotImplementedException();

    public override int InitTone => throw new NotImplementedException();

    public override Rhythm Kind => throw new NotImplementedException();

    public override Wav DefaultNext => throw new NotImplementedException();

    public override Dictionary<string, Wav> Branches => throw new NotImplementedException();
}
