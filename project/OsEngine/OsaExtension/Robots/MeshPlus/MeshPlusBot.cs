
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.OsaExtension.Robots.MeshPlus
{

    [Bot("MeshPlusBot")]
    public class MeshPlusBot : BotPanel
    {
        private BotTabSimple _tab;

        public MeshPlusBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            // параметры 

            IsOn = CreateParameter("IsOn", false, "Входные");
            VolumeInBaks = CreateParameter("Объем позиции в $ ", 11, 7, 7, 5, "Входные");
            PartsInput = CreateParameter("Сколько частей на вход", 2, 1, 10, 1, "Входные"); // набирать позицию столькими частями 

        }


        public override string GetNameStrategyType()
        {
            return nameof(MeshPlusBot);
        }

        public override void ShowIndividualSettingsDialog()
        {
           
        }
        #region Свойства ===============================================

        private StrategyParameterBool IsOn; // включение робота
        private StrategyParameterInt VolumeInBaks; // объем позиции в баксах
        private StrategyParameterInt PartsInput; // количество частей на вход в объем позиции 

        #endregion  end Свойства ===============================================

    }
}
