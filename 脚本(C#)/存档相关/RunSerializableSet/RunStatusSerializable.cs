using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunSerializableSet;

public class RunStatusSerializable(params (string Key,IRunSaveProp SaveProp)[] props) : DictionarySerializable("Status",props.ToList())
{ 
}
