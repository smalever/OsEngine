
using OsEngine.Charts.CandleChart.Indicators;
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
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms.DataVisualization.Charting;
using static OsEngine.OsaExtension.MVVM.ViewModels.MainWindowRobWpfVM;

namespace OsEngine.OsaExtension.Robots.Forbreakdown
{

    [Bot("Forbreakdown")]
    public class ForBreakdown : BotPanel
    {
        private BotTabSimple _tab;

        public ForBreakdown(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.BestBidAskChangeEvent += _tab_BestBidAskChangeEvent;//  логика для  рынке 

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;// для  работы в тестере 


            // параметры 

            IsOn = CreateParameter("IsOn", false, "Входные");
            StepType = CreateParameter("Типа шага сетки", "Punkt", new[] { "Punkt", "Percent" }, "Входные");
            StepLevelProfit = CreateParameter("Шаг между выходами частей ", 10m, 10, 100, 10, "Выходные");
            StepLevelInput = CreateParameter("Шаг между входами ", 10m, 10, 100, 10, "Входные");
            VolumeInBaks = CreateParameter("Объем позиции в $ ", 11, 7, 7, 5, "Входные");
            PartsProfit = CreateParameter("Колич. частей на выход", 2, 1, 10, 1, "Выходные");// количество частей профита
            PartsInput = CreateParameter("Сколько частей на вход", 2, 1, 10, 1, "Входные"); // набирать позицию столькими частями 
            VolumeFactor = CreateParameter("Увелич обем входа шага ", 0m, 0, 2, 0.1m, "Входные");

            Load();
        }


        #region Методы =========================================================


        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            _marketPriceSecur = candles[candles.Count - 1].Close;
            if (StartProgram == StartProgram.IsOsTrader) return;

            if (IsOn.ValueBool == false) return;

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

        /// <summary>
        /// закрытие всех позиций по маркету 
        /// </summary>
        public void ClosePosicion() 
        {
            if (_tab.PositionsOpenAll.Count != 0)
            {
                _tab.CloseAllAtMarket();
            }
        }
        /// <summary>
        /// расчитывает растояние между уровнями 
        /// </summary>
        private decimal GetStepLevel()
        {
            decimal stepLevel = 0;
            if ("Punkt" == StepType.ValueString)
            {
                stepLevel = StepLevelInput.ValueDecimal * _tab.Securiti.PriceStep;
            }
            else if ("Percent" == StepType.ValueString)
            {
                stepLevel = StepLevelInput.ValueDecimal * MarketPriceSecur / 100;
                stepLevel = Decimal.Round(stepLevel, _tab.Securiti.Decimals);
            }
            return stepLevel;
        }

        #region сервис ============ 

        /// <summary>
        /// сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false)
                    )
                {
                    writer.WriteLine(ProfitPoint);
                    writer.WriteLine(StartPoint);
                    writer.WriteLine(StopPoint);
         
                    writer.Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Операция прервана, т.к. в одном из полей недопустимое значение.");
                return;
            }
        }

        /// <summary>
        /// загрузить настройки из файла
        /// </summary>
        private void Load()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    ProfitPoint = Convert.ToDecimal(reader.ReadLine());
                    StartPoint = Convert.ToDecimal(reader.ReadLine());
                    StopPoint = Convert.ToDecimal(reader.ReadLine());
          
                    reader.Close();
                }
            }
            catch (Exception)
            {
                // отправить в лог
                SendNewLogMessage(" Настройки робота из файла не загружены  ", LogMessageType.NoName);
            }
        }

        private void UserClickOnButtonEvent() // нажал на кнопку в панели параметров 
        {

        }

        private void Start_ParametrsChangeByUser() // событие изменения параметров пользователем
        {

        }
        public override string GetNameStrategyType()
        {
            return nameof(ForBreakdown);
        }

        public override void ShowIndividualSettingsDialog()
        {
            ForBreakdownUI ui = new ForBreakdownUI(this);
            ui.Show();
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
        private StrategyParameterDecimal StepLevelProfit; // шаг между шагами выхода
        private StrategyParameterDecimal StepLevelInput; // шаг между уровнями входа   
        private StrategyParameterInt VolumeInBaks; // объем позиции в баксах
        private StrategyParameterDecimal VolumeFactor; //  увеличение объема позиции на вход 
        private StrategyParameterInt PartsProfit; // количество частей на вход из позиции 
        private StrategyParameterInt PartsInput; // количество частей на вход в объем позиции 
       

        // настройки робота публичные

        public decimal ProfitPoint;
        public decimal StartPoint;
        public decimal StopPoint;

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

