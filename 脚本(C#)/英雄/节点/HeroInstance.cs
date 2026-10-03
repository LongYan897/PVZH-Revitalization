using Card;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;

namespace Hero;

[GlobalClass]
public partial class HeroInstance : Control, ITarget
{
    public TargetType TargetType => TargetType;

    public Func<CardModel, Func<ITarget, bool>, bool> CanBeTarget => (t,f) =>
    {
        return false;
    };

    public bool Calling => throw new NotImplementedException();

    public void OnCallTargeted(TargetType type)
    {
        throw new NotImplementedException();
    }

    public void OnDeleteTargeted(TargetType type)
    {
        throw new NotImplementedException();
    }

    public void OnDistargeted(TargetContext ctx)
    {
        throw new NotImplementedException();
    }

    public void OnTargeted(TargetContext ctx)
    {
        throw new NotImplementedException();
    }
}
