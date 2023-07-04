
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

    [Bot("ForBreakdown")]
    public class ForBreakdown : BotPanel
    {
        #region Свойства и Поля ===============================================
        /// <summary>
        /// цена входа расчетная
        /// </summary>
        decimal _pricePointIn = 0;

        private BotTabSimple _tab;

        private StrategyParameterBool IsOn; // включение робота
        private StrategyParameterString StepType; // режим расчета шага сетки 
        private StrategyParameterDecimal StepLevelProfit; // шаг между шагами выхода
        private StrategyParameterDecimal StepLevelInput; // шаг между уровнями входа   
        private StrategyParameterInt VolumeInBaks; // объем позиции в баксах
        private StrategyParameterDecimal VolumeFactor; //  увеличение объема позиции на вход 
        private StrategyParameterInt PartsProfit; // количество частей на вход из позиции 
        private StrategyParameterInt PartsInput; // количество частей на вход в объем позиции 


        // настройки робота публичные

        public decimal ProfitPoint = 0;
        public decimal StartPoint = 0;
        public decimal StopPoint = 0;

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

   

        public ForBreakdown(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.BestBidAskChangeEvent += _tab_BestBidAskChangeEvent;//  логика для  рынке 

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;// для  работы в тестере 

            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent; // для обнуления и выклчения 

            _tab.MyTradeEvent += _tab_MyTradeEvent;// для логики 
            // параметры 

            IsOn = CreateParameter("IsOn", false, "Входные");
            StepType = CreateParameter("Типа шага сетки", "Punkt", new[] { "Punkt", "Percent" }, "Входные");
            StepLevelProfit = CreateParameter("Шаг между выходами частей ", 10m, 10, 100, 10, "Выходные"); // TODO: сделать метод расчета шага профита
            StepLevelInput = CreateParameter("Шаг между входами ", 10m, 10, 100, 10, "Входные");
            VolumeInBaks = CreateParameter("Объем позиции в $ ", 11, 7, 7, 5, "Входные");
            PartsProfit = CreateParameter("Колич. частей на выход", 2, 1, 10, 1, "Выходные");// количество частей профита
            PartsInput = CreateParameter("Сколько частей на вход", 2, 1, 10, 1, "Входные"); // набирать позицию столькими частями 
            VolumeFactor = CreateParameter("Увелич обем входа шага ", 0m, 0, 2, 0.1m, "Входные");

            Load();
        }

        #region Методы =========================================================

        /// <summary>
        /// отработала моя сделка 
        /// </summary>
        private void _tab_MyTradeEvent(MyTrade mytrade)
        {
            CalculateProfitPoint();
            CalculatePointIn();
            Save();
        }

        /// <summary>
        /// после закрытия для обнуления переменных
        /// </summary>
        private void _tab_PositionClosingSuccesEvent(Position position)
        {
            // изменть значения переменных 
            IsOn.ValueBool = false;
            ProfitPoint = 0;
            _pricePointIn = 0;
            // StartPoint = 0;
            StopPoint = 0;

        Save();
        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            _marketPriceSecur = candles[candles.Count - 1].Close;
            if (StartProgram == StartProgram.IsOsTrader) return;

            FollowPrice();

            TradeLogic();
        }

        /// <summary>
        /// изменились биды\ аски
        /// </summary>
        private void _tab_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            MarketPriceSecur = ask;

            if (StartProgram == StartProgram.IsTester) return;

            FollowPrice();

            TradeLogic();
        }

        /// <summary>
        ///  торговая логика 
        /// </summary>
        private void TradeLogic()
        {

            if (IsOn.ValueBool == false) return;
            
            if (StopPoint == 0) CalculateStopPoint();

            if (_pricePointIn == 0) CalculatePointIn();// расчитать точку входа впервые

            if (ProfitPoint == 0)  CalculateProfitPoint();

            // PrinTextDebag("TradeLogic запущен", "отработал ");

            // SendNewLogMessage(" сообщение в лог " , LogMessageType.NoName);

        }

        public void StartCalcPoint()
        {
            CalculateStopPoint();
            CalculateProfitPoint();
            CalculatePointIn();
            Save();
        }

        /// <summary>
        /// расчитать цену профита 
        /// </summary>
        private void CalculateProfitPoint()
        {
            if (_tab.PositionsOpenAll.Count == 0 )
            {
                ProfitPoint = StartPoint + GetStepLevelProfit();
                PrinTextDebag("CalculateProfitPoint Стартовый  ", "ProfitPoint =" + ProfitPoint);
            }
            if (_tab.PositionsOpenAll.Count != 0 )
            {                
                int sumTred = _tab.PositionsLast.MyTrades.Count; // количество трейдов в позиции
                if (sumTred > 1)
                {
                    decimal sellpoint = 0;
                    for (int i = 0; i < sumTred; i++)
                    {                        
                        if (_tab.PositionsLast.MyTrades[i].Side == Side.Sell)
                        {
                            decimal  tridepoint = _tab.PositionsLast.MyTrades[i].Price; // цена трейда позиции
                            if (ProfitPoint < tridepoint)
                            {
                                sellpoint = tridepoint;
                            }                            
                        }                  
                    }
                    if ( ProfitPoint < sellpoint)
                    {
                        ProfitPoint = sellpoint + GetStepLevelProfit();
                        PrinTextDebag("CalculateProfitPoint пересчитали  ", "ProfitPoint =" + ProfitPoint);
                    }
                }
            }            
            Save();
        }

        /// <summary>
        /// расчитать цену стопа 
        /// </summary>
        private void CalculateStopPoint()
        {
            StopPoint = StartPoint - GetStepLevelInput() * (PartsInput.ValueInt +1);
            PrinTextDebag("CalculateStopPoint пересчитали  " + StopPoint , "StopPoint =" + StopPoint);
            Save();
        }

        /// <summary>
        /// следит за ценой 
        /// </summary>
        private void FollowPrice()
        {
            if (MarketPriceSecur <= _pricePointIn)
            {
                RecruitingPosition();
                if (IsOn.ValueBool == false) return;
                //CalculatePointIn();                
            }
            if ( MarketPriceSecur > ProfitPoint )
            {
                ClosePos();                
            }
            if (MarketPriceSecur <= StopPoint &&  StopPoint !=0 )
            {
                if (_tab.PositionsOpenAll.Count == 0) return;
                _tab.CloseAllAtMarket();
                PrinTextDebag(" Закрылись по стопу  ", "StopPoint =" + StopPoint);
            }
        }

        private void ClosePos()
        {
            if (_tab.PositionsOpenAll.Count == 0) return;
            CalculateProfitPoint();
            decimal volPos = _tab.PositionsLast.OpenVolume;
            decimal volInput = Rounding((VolumeInBaks.ValueInt / MarketPriceSecur)
                                / PartsInput.ValueInt, _tab.Securiti.DecimalsVolume);

            int sumTred = _tab.PositionsLast.MyTrades.Count; // количество трейдов в позиции
            if (sumTred > 1 && MarketPriceSecur > ProfitPoint)
            {
                decimal vol = Rounding(volPos / PartsProfit.ValueInt, _tab.Securiti.DecimalsVolume); // считаем объем 
                if (vol <= volInput)
                {
                    //_tab.SellAtMarket(volPos);
                    if (_tab.PositionsLast.OpenActiv == true) return;
                    _tab.CloseAllAtMarket();
                    PrinTextDebag("ClosePos закрыли остаток по профиту  ", "ProfitPoint =" + ProfitPoint);
                }
                else
                {
                    //_tab.SellAtMarketToPosition(_tab.PositionsLast, vol);
                    if (_tab.PositionsLast.OpenActiv == true) return;
                    _tab.CloseAtMarket(_tab.PositionsLast, vol);
                    PrinTextDebag("ClosePos закрыли часть по профиту  ", "ProfitPoint =" + ProfitPoint);
                    CalculateProfitPoint();
                }
            }
        }    

        /// <summary>
        ///  набирает позицию 
        /// </summary>
        private void RecruitingPosition()
        {
            if (IsOn.ValueBool == false) return;

            if (FullVolume()) return;  // проверить набраный обем 
            decimal vol = 0;
            vol = Rounding(VolumeInBaks.ValueInt / PartsInput.ValueInt / MarketPriceSecur
             , _tab.Securiti.DecimalsVolume); // считаем объем 

            if (_tab.PositionsOpenAll.Count == 0 && vol !=0)
            {
                //if (_tab.PositionsOpenAll[0].OpenActiv == true) return;
                _tab.BuyAtMarket(vol);
                PrinTextDebag("RecruitingPosition ОТКРЫЛИ позицию ", "_rpicePointIn = " + _pricePointIn);
            }
            if (_tab.PositionsOpenAll.Count != 0)
            {
                if (_tab.PositionsLast.OpenActiv == true) return;
                _tab.BuyAtMarketToPosition(_tab.PositionsLast, vol);
                PrinTextDebag("RecruitingPosition добрвли позицию ", "_rpicePointIn = " + _pricePointIn);                
            }
            CalculatePointIn();
            // TODO: надо придумать отключение повтороного набора позиции 
        }

        /// <summary>
        /// проверяет набран ли весь  рабочий объем в позиции
        /// </summary>
        private bool FullVolume()
        {
            if (_tab.PositionsOpenAll.Count == 0)
            {
                return false;
            }
            if (_tab.PositionsOpenAll.Count != 0)
            {
                decimal volPos = _tab.PositionsLast.OpenVolume;
                int decimalSecur = GetDecimalsVolumeSecur(_tab); // берем децимал монеты
                decimal desiredVolmonet = Rounding(VolumeInBaks.ValueInt / MarketPriceSecur, decimalSecur); // считаем желаемый  объем в монетах
                if (volPos <= desiredVolmonet)
                {
                    return false;
                }
            }  
            return true;
        }

        /// <summary>
        /// расчет точки набора позиции
        /// </summary>
        private decimal CalculatePointIn()
        {
            // берем цену старта  и вычисляем по шагу 
            if (_tab.PositionsOpenAll.Count == 0)
            {
                _pricePointIn = StartPoint - GetStepLevelInput();
                
            }
            if (_tab.PositionsOpenAll.Count != 0) // берем цену открытия позиции и вычисляем по шагу 
            {
                // _rpicePointIn = _tab.PositionsLast.EntryPrice + GetStepLevel();   // цена открытия позиции
                   
                int sumTred = _tab.PositionsLast.MyTrades.Count; // количество трейдов в позиции
          
                if (sumTred > 1)
                {                   
                    decimal buypoint = 0;
                    for (int i = 0; i < sumTred; i++)
                    {
                        if (_tab.PositionsLast.MyTrades[i].Side == Side.Buy)
                        {
                            decimal tridepoint = _tab.PositionsLast.MyTrades[i].Price; // цена трейда позиции
                            if ( buypoint == 0 && tridepoint !=0)
                            {
                                buypoint = tridepoint;
                            }
                            if ( tridepoint < buypoint)
                            {
                                buypoint = tridepoint;
                            }
                        }
                    }
                    if (_pricePointIn > buypoint)
                    {
                        _pricePointIn = buypoint - GetStepLevelInput();
                    }
                }
            }
            PrinTextDebag("CalculatePointIn пересчитали  ", "_rpicePointIn =" + _pricePointIn);
            return _pricePointIn;
        }

        /// <summary>
        ///  запрос децимал бумаги для расчета объема сделки по монете 
        /// </summary>
        private int GetDecimalsVolumeSecur(BotTabSimple _tabSimp)
        {
            if (_tabSimp.IsConnected && _tabSimp.StartProgram == StartProgram.IsOsTrader)
            {
                return _tabSimp.Securiti.DecimalsVolume;
            }
            if (_tabSimp.StartProgram == StartProgram.IsTester)
            {
                return _tabSimp.Securiti.DecimalsVolume;
            }
            else return 0;
        }

        /// <summary>
        /// расчитывает растояние между уровнями набора
        /// </summary>
        private decimal GetStepLevelInput()
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

        /// <summary>
        /// расчитывает растояние между уровнями профита
        /// </summary>
        private decimal GetStepLevelProfit()
        {
            decimal stepLevel = 0;
            if ("Punkt" == StepType.ValueString)
            {
                stepLevel = StepLevelProfit.ValueDecimal * _tab.Securiti.PriceStep;
            }
            else if ("Percent" == StepType.ValueString)
            {
                stepLevel = StepLevelProfit.ValueDecimal * MarketPriceSecur / 100;
                stepLevel = Decimal.Round(stepLevel, _tab.Securiti.Decimals);
            }
            return stepLevel;
        }

        /// <summary>
        /// округляет децимал до n чисел после запятой
        /// </summary>
        public decimal Rounding(decimal vol, int n) // округляет децимал до n чисел после запятой
        {
            decimal value = vol;
            int N = n;
            decimal chah = decimal.Round(value, N, MidpointRounding.ToEven);
            return chah;
        }

        #region сервис ============ 

        /// <summary>
        /// сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false) )

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

    
    }
}

