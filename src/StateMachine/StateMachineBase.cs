using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.StateMachine
{
    public abstract class StateMachineBase<AppLifeState, AppLifeTrigger>
    {
        public abstract StateMachine<AppLifeState, AppLifeTrigger> StateMachine { get; protected set; }
        public virtual string GenerateUmlDotGraph() => UmlDotGraph.Format(StateMachine.GetInfo());

    }
}
