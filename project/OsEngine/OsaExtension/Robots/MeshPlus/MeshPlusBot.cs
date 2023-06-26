
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.OsaExtension.MVVM.Models;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Controls.Primitives;
using System.Windows.Forms.DataVisualization.Charting;
using static OsEngine.OsaExtension.MVVM.ViewModels.MainWindowRobWpfVM;

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
            StepType = CreateParameter("Типа шага сетки", "Punkt", new[] { "Punkt", "Percent" }, "Входные");
            StepLevel = CreateParameter("Шаг между уровнями ", 10m, 10, 100, 10, "Входные");
            VolumeInBaks = CreateParameter("Объем позиции в $ ", 11, 7, 7, 5, "Входные");
            PartsInput = CreateParameter("Сколько частей на вход", 2, 1, 10, 1, "Входные"); // набирать позицию столькими частями 

        }


        #region Методы =========================================================


        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            _marketPriceSecur = candles[candles.Count-1].Close;
            if (StartProgram == StartProgram.IsOsTrader)  return; 

            if (IsOn.ValueBool == false)  return;

            TradeLogic();

        }

        /// <summary>
        /// изменились биды\ аски
        /// </summary>
        private void _tab_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            _marketPriceSecur = ask;

            if (StartProgram == StartProgram.IsTester) return;

            if (IsOn.ValueBool == false) return;

            TradeLogic();

        }
        private void TradeLogic()
        {
            PrinTextDebag("TradeLogic запущен", "отработал ");
            // SendNewLogMessage(" сообщение в лог " , LogMessageType.NoName);
            GetStepLevel();

        }
        private decimal GetStepLevel()
        {
            decimal stepLevel = 0;
            if ("Punkt" == StepType.ValueString)
            {
                stepLevel = StepLevel.ValueDecimal * _tab.Securiti.PriceStep;
            }
            else if ("Percent" == StepType.ValueString)
            {
                stepLevel = StepLevel.ValueDecimal * MarketPriceSecur / 100;
                stepLevel = Decimal.Round(stepLevel, _tab.Securiti.Decimals);
            }
            return stepLevel;
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
        /// <summary>
        /// вывод в дебаг текста 
        /// </summary>
        public static void PrinTextDebag(string text, string secondLine = "")
        {
            string Time = DateTime.Now.ToString("hh:mm:ss:ffff");
            string str = text + " \n "
                    + secondLine + " " + Time + "\n";
            Debug.WriteLine(str);
        }

        #endregion конец сервис ============ 

        #endregion  конец Методы =========================================================

        #region Свойства ===============================================


        private StrategyParameterBool IsOn; // включение робота
        private StrategyParameterString StepType; // режим расчета шага сетки 
        private StrategyParameterDecimal StepLevel; // шаг между уровнями 
        private StrategyParameterInt VolumeInBaks; // объем позиции в баксах
        private StrategyParameterInt PartsInput; // количество частей на вход в объем позиции 

        /// <summary>
        /// Рыночная цена бумаги 
        /// </summary>
        public decimal MarketPriceSecur
        {
            get => _marketPriceSecur;
            set
            {
                _marketPriceSecur = value;                
            }
        }
        private decimal _marketPriceSecur;

        #endregion  end Свойства ===============================================

    }
}
