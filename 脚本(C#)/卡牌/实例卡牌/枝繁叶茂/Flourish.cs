using Card.Cmd;
using System.Threading.Tasks;
using Target;

namespace Card.Flower;

public class Flourish : CardModel
{
    public override TargetType TargetType => TargetType.Lines;
    public override async Task Play(ITarget target)
    {
        await PlayInstantAnimation("effect", target);
        await CardCmd.Draw(null, this, 2);
    }
}
