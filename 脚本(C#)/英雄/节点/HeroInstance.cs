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
}
