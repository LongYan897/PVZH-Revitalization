using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet;

public interface IRunInitializable
{
    void Init(bool isLoadSave);
}
