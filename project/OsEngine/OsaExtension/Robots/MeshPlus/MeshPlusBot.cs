
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
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
            _tab.BestBidAskChangeEvent += _tab_BestBidAskChangeEvent;// для  работы в рынке 

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;// для  работы в тестере 

            // параметры 

            IsOn = CreateParameter("IsOn", false, "Входные");
            VolumeInBaks = CreateParameter("Объем позиции в $ ", 11, 7, 7, 5, "Входные");
            PartsInput = CreateParameter("Сколько частей на вход", 2, 1, 10, 1, "Входные"); // набирать позицию столькими частями 

        }


        #region Методы =========================================================


        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (StartProgram == StartProgram.IsOsTrader)  return; 

            if (IsOn.ValueBool == false)  return;

            TradeLogic();

        }

        /// <summary>
        /// изменились биды\ аски
        /// </summary>
        private void _tab_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            if (StartProgram == StartProgram.IsTester) return;

            if (IsOn.ValueBool == false) return;

            TradeLogic();

        }
        private void TradeLogic()
        {

        }

        #region сервис ============ 

        private void UserClickOnButtonEvent() // нажал на кнопку в панели параметров 
        {

        }

        private void Start_ParametrsChangeByUser() // событие изменения параметров пользователем
        {

        }
        public override string GetNameStrategyType()
        {
            return nameof(MeshPlusBot);
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        #endregion конец сервис ============ 

        #endregion  конец Методы =========================================================

        #region Свойства ===============================================

        private StrategyParameterBool IsOn; // включение робота
        private StrategyParameterInt VolumeInBaks; // объем позиции в баксах
        private StrategyParameterInt PartsInput; // количество частей на вход в объем позиции 

        #endregion  end Свойства ===============================================

    }
}
