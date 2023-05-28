using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.Entity;

namespace OsEngine.Robots.MyBots
{


    [Bot("NetSma")]

    internal class NetSma : BotPanel
    {
        public NetSma(string name, StartProgram startProgram) : base(name, startProgram)
        {


        }

        public override string GetNameStrategyType()
        {
            throw new NotImplementedException();
        }

        public override void ShowIndividualSettingsDialog()
        {
            throw new NotImplementedException();
        }
    }
}
