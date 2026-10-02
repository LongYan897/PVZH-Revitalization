
using Card;
using Play;
using System.Threading.Tasks;

namespace Target;

public interface ITarget
{
    TargetType TargetType { get; }
}
