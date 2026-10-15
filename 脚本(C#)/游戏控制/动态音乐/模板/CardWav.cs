using System.Collections.Generic;

namespace Controller.WavPlay;

public abstract class CardWav : Wav
{
    public sealed override int InitTone => 0;
    public sealed override Rhythm Kind => Rhythm.SFX;
    public override Wav DefaultNext => null;
    public override Dictionary<string, Wav> Branches => [];
}
