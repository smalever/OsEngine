using OsEngine.Alerts;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Logging;
using OsEngine.OsaExtension.MVVM.ViewModels;
using OsEngine.OsaExtension.MyBots.meshOnSMA;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.OsaExtension.Robots.MeshPlus
{

    [Bot("MeshPlusBot")]
    public class MeshPlusBot : BotPanel
    {
        public MeshPlusBot(string name, StartProgram startProgram) : base(name, startProgram)
        {

        }

        public override string GetNameStrategyType()
        {
            return nameof(MeshPlusBot);
        }

        public override void ShowIndividualSettingsDialog()
        {
           
        }
    }
}
