
using Card;
using Play;
using System;
using System.Threading.Tasks;

namespace Target;

public interface ITarget
{
    TargetType TargetType { get; }
    static virtual bool CanTargetedBy(CardModel card) { return false; }
}
