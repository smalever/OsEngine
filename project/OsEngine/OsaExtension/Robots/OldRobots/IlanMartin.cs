/*
 * мартингейл усреднятор
 * 
 * 
 * смотрим тренд по МАшке 
 * 
 * 1. входим в рынок при удалении  цены в нашу сторону от МАшки на N пунктов ( _maOpenDistance )
 * 
 * 2. входить будем на размер ( _lotEntrySize )
 * 
 * 3. при достижении цены тейка выходим ( _takeProfitInPunkt ) и начинаем с начала
 * 
 * 4.если ушли в минус  более чем на столько то пунктов ( _averagingStepInPunkt ) 
 *      то ждем разворот МАшки на ( _maOpenDistance ) и усреднимся на ( _lotEntrySize * _koefAveregingInPercents )
 *      предварительно проверив количество шагов усреднения ( _сountAveregeSteps )
 *      
 *5. если опять дошли в минус до последнего шага ( _сountAveregeSteps ) ставим стоп на ( _averagingStepInPunkt ) 
 *      от цены последнего входа.
 *
 *6. шаги кончились, ждем иди стоп или тейк и потом все заново.
 *
 */


using System.Collections.Generic;
using System.Drawing;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System;

namespace OsEngine.OsaExtension.MyBots.IlanMartin
{
    [Bot("IlanMartin")]

    internal class IlanMartin : BotPanel
    {

        public IlanMartin(string name, StartProgram startProgram) : base(name, startProgram)
        {

            this.TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Mode = this.CreateParameter("Mode", "Edit", new[] { "Edit", "Trade" });

            _smaLenght = CreateParameter("_smaLenght  ", 100, 1, 1000, 10);

            // добавим сма на график 
            _sma = new MovingAverage(name + "Sma", false);
            _sma = (MovingAverage)_tab.CreateCandleIndicator(_sma, "Prime");
            _sma.Lenght = _smaLenght.ValueInt;


            // на график линию средней входа для всех поз
            averegeEntryPriceLevelLine = new LineHorisontal("averegeEntryPriceLevelLine", "Prime", false)
            {
                Color = Color.Yellow,
                Value = 0
            };
            _tab.SetChartElement(averegeEntryPriceLevelLine);
            averegeEntryPriceLevelLine.Value = 0;
            averegeEntryPriceLevelLine.TimeEnd = DateTime.Now;



            // на график линию тейка для всех поз
            takeProfitAllPositionsLevelLine = new LineHorisontal("takeProfitAllPositionsLevelLine", "Prime", false)
            {
                Color = Color.GreenYellow,
                Value = 0
            };
            _tab.SetChartElement(takeProfitAllPositionsLevelLine);
            takeProfitAllPositionsLevelLine.Value = 0;
            takeProfitAllPositionsLevelLine.TimeEnd = DateTime.Now;
            // takeProfitAllPositionsLevelLine



            // _fromMaOpenDistance = CreateParameter("_maOpenDistance ", 10m, 1, 1000, 1);

            _lotEntrySize = CreateParameter("_lotEntrySize  ", 1m, 1, 100, 1);

            _takeProfitInPunkt = CreateParameter("_takeProfitInPunkt  ", 100m, 1, 1000, 1);

            _averagingToMinusStepInPunkts = CreateParameter("_averagingToMinusStepInPunkts  ", 100m, 10, 1000, 10);

            _koefAveregingInPercents = CreateParameter("_koefAveregingInPercents  ", 30m, 1, 100, 1);

            _сountAveregeSteps = CreateParameter("_сountAveregeSteps  ", 5m, 1, 12, 1);

            lotEntrySize = _lotEntrySize.ValueDecimal;



            // событие закрылась свечка
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;


        }


        #region Methods ==============================

        // свеча закрылась
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            // тесты узнать о бумаге
            decimal a = _tab.Securiti.PriceStep;
            decimal b = _tab.Securiti.PriceStepCost;


            // ждем накопления свечек   для корректного расчета cма
            if (candles.Count < _sma.Lenght + 5) { return; }

            lastCandleClose = candles[candles.Count - 1].Close;
            lastSma = _sma.Values[_sma.Values.Count - 1];

            // если закрытие больше чем сма  то открываем  первую позицию
            // если нет позиций делаем первый шаг серии позиций
            if (lastCandleClose > lastSma
                && _tab.PositionOpenLong.Count == 0)
            { OpenFirstStep(); }

            // следующий шаг

            // если количество усреднений превышено больше не открываем
            if (countAveregeSteps >= _сountAveregeSteps.ValueDecimal) { canTrade = false; }

            // нужен признак что мы были ниже сма после открытия 
            // если предыдущие Х свечек были ниже сма
            if (
                   candles[candles.Count - 2].Close < lastSma
                && candles[candles.Count - 3].Close < lastSma
                && candles[candles.Count - 4].Close < lastSma
                && candles[candles.Count - 5].Close < lastSma
                && candles[candles.Count - 6].Close < lastSma

                )
            { wereBelowSma = true; }
            else
            { wereBelowSma = false; }


            // если цена стала выше сма 
            if (
                _tab.PriceCenterMarketDepth > lastSma
                )
            { canTrade = true; }
            else
            { canTrade = false; }


            // если цена выше чем средняя цена всех открытых позиций и количество пунктов для усреднения 
            //
            if (
                _tab.PriceCenterMarketDepth < lastOpenPositionPrice - _averagingToMinusStepInPunkts.ValueDecimal
                && _tab.PositionOpenLong.Count != 0
                && wereBelowSma
                && canTrade
                )
            {
                decimal aaa = averegePriceAllPositionOpen;
                OpenNextStep();
                canTrade = false;
                wereBelowSma = false;

                // посчитаем и нарисуем линию
                CalculateTakeProfitAllPositions();
            }


            // если цена больше чем цена средняя  позиций + тейк И есть открытые позиции то закроем
            if (_tab.PriceCenterMarketDepth > averegePriceAllPositionOpen + takeProfitInPoints
                && _tab.PositionOpenLong.Count != 0)
            {
                CloseAllposition();
            }

        }

        void OpenFirstStep()
        {

            lastOpenPositionPrice = _tab.PriceCenterMarketDepth;
            _tab.BuyAtLimit(lotEntrySize, lastOpenPositionPrice);
            countAveregeSteps++; // и увеличиваем значение шагов усреднения

            averegePriceAllPositionOpen = lastOpenPositionPrice;

            // считаем шаг уменьшения пропорционально кол-ву шагов
            takeProfitInPoints = _takeProfitInPunkt.ValueDecimal;
            stepDecrementTake = takeProfitInPoints / _сountAveregeSteps.ValueDecimal;

            // нарисуем среднюю входа
            averegeEntryPriceLevelLine.Value = averegePriceAllPositionOpen;
            averegeEntryPriceLevelLine.Refresh();

            CalculateTakeProfitAllPositions();

            //if (_koefAveregingInPercents.ValueDecimal > 100) { _koefAveregingInPercents.ValueDecimal = 100; }
        }

        void OpenNextStep()
        {
            lastOpenPositionPrice = _tab.PriceCenterMarketDepth;

            // посчитаем увеличение лота для сделки
            lotEntrySize = lotEntrySize + (lotEntrySize * _koefAveregingInPercents.ValueDecimal / 100);
            _tab.BuyAtLimit(lotEntrySize, lastOpenPositionPrice);
            // и увеличиваем значение шагов усреднения
            countAveregeSteps++;

            // уменьшим тейк на коэффициент увеличения лотности. чтоб приблизить выход из увеличивающейся позы
            takeProfitInPoints = takeProfitInPoints - stepDecrementTake;
            if (takeProfitInPoints < 10) { takeProfitInPoints = 10; }

            // расчет безубыточности по всем позам
            averegePriceAllPositionOpen = 0;
            List<Position> positions = _tab.PositionOpenLong;
            //int count = _tab.PositionOpenLong.Count;
            decimal lots = 0;
            decimal entryPrice = 0;
            decimal lotAccum = 0;

            for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
            {
                lots = positions[i].Lots;
                entryPrice = positions[i].EntryPrice;
                averegePriceAllPositionOpen = averegePriceAllPositionOpen + (lots * entryPrice);
                lotAccum = lotAccum + positions[i].Lots;
            }

            averegePriceAllPositionOpen = averegePriceAllPositionOpen / lotAccum;

            // нарисуем среднюю входа
            averegeEntryPriceLevelLine.Value = averegePriceAllPositionOpen;
            averegeEntryPriceLevelLine.Refresh();
        }

        void CloseAllposition()
        {
            //Task task1 = new Task(_tab.CloseAllAtMarket);
            //task1.Start();
            //task1.Wait();

            _tab.CloseAllAtMarket();

            lotEntrySize = _lotEntrySize.ValueDecimal;
            countAveregeSteps = 0;
            lastOpenPositionPrice = 0;
            averegePriceAllPositionOpen = 0;

            //Task task2 = new Task(_tab.PositionOpenLong.Clear);
            //task2.Start();
            //task2.Wait();

            _tab.PositionOpenLong.Clear();

            averegeEntryPriceLevelLine.Delete();
            takeProfitAllPositionsLevelLine.Delete();
        }


        void CalculateTakeProfitAllPositions()
        {

            // нарисуем среднюю входа
            takeProfitAllPositionsLevelLine.Value = averegePriceAllPositionOpen + takeProfitInPoints;
            takeProfitAllPositionsLevelLine.Refresh();


            // takeProfitAllPositionsLevelLine

        }

        /*
        /// <summary>
        /// save settings
        /// сохранить настройки
        /// </summary>
        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false)
                    )
                {
                    writer.WriteLine(_maOpenDistance);
                    writer.WriteLine(_lotEntrySize);
                    writer.WriteLine(_takeProfitInPunkt);
                    writer.WriteLine(_averagingStepInPunkt);
                    writer.WriteLine(_koefAveregingInPercents);
                    writer.WriteLine(_сountAveregeSteps);

                    writer.Close();

                }
            }
            catch (Exception)
            {
                // ignore
            }
        }


        
        /// <summary>
        /// load settings
        /// загрузить настройки
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

                    // decimal test = reader.ReadLine();

                    _maOpenDistance = Convert.ToDecimal(reader.ReadLine());
                    _lotEntrySize = Convert.ToDecimal(reader.ReadLine());
                    _takeProfitInPunkt = Convert.ToDecimal(reader.ReadLine());

                    // 
                    // Enum.TryParse(reader.ReadLine(), true, out Regime);
                    _averagingStepInPunkt = Convert.ToDecimal(reader.ReadLine());
                    _koefAveregingInPercents = Convert.ToDecimal(reader.ReadLine());
                    _сountAveregeSteps = Convert.ToDecimal(reader.ReadLine());

                    reader.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
*/


        public override string GetNameStrategyType()
        {
            return nameof(IlanMartin);
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        #endregion end Methods ==============================



        #region Fields ==============================

        private BotTabSimple _tab;

        private StrategyParameterString Mode;

        private MovingAverage _sma;


        /// <summary>
        /// линия тейка для всех поз
        /// </summary>
        public LineHorisontal takeProfitAllPositionsLevelLine;

        /// <summary>
        /// линия средней цены входа
        /// </summary>
        public LineHorisontal averegeEntryPriceLevelLine;

        /// <summary>
        /// признак что были ниже сма
        /// </summary>
        bool wereBelowSma = false;

        /// <summary>
        /// шаг уменьшения тейка при наборе шагов
        /// </summary>
        public decimal stepDecrementTake = 0;

        /// <summary>
        /// уменьшаемый тейк при наборе шагов
        /// </summary>
        public decimal takeProfitInPoints = 0;


        /// <summary>
        /// средняя цена открытия всех позиций (без убыток)
        /// </summary>
        public decimal averegePriceAllPositionOpen = 0m;

        /// <summary>
        /// цена открытия последней позиции
        /// </summary>
        public decimal lastOpenPositionPrice = 0m;

        /// <summary>
        /// цена закрытия последней свечки
        /// </summary>
        public decimal lastCandleClose = 0;

        /// <summary>
        /// цена МАшки на последней свечки
        /// </summary>
        decimal lastSma = 0;


        /// <summary>
        /// счетчик для шагов усреднения набора позиции
        /// </summary>
        public decimal countAveregeSteps = 0;


        /// <summary>
        /// можно ли торговать ??
        /// </summary>
        public bool canTrade = false;


        /// <summary>
        /// расстояние от МА для входа в позицию
        /// </summary>
        //private StrategyParameterDecimal _fromMaOpenDistance;

        /// <summary>
        /// размер лота для входа
        /// </summary>
        private StrategyParameterDecimal _lotEntrySize;

        /// <summary>
        /// аккумулятор размера лота для входа
        /// </summary>
        public decimal lotEntrySize = 0;

        /// <summary>
        /// количество пунктов на тейк профит
        /// </summary>
        private StrategyParameterDecimal _takeProfitInPunkt;

        /// <summary>
        /// количество пунктов ухода цены в минус для начала усреднения
        /// </summary>
        private StrategyParameterDecimal _averagingToMinusStepInPunkts;

        /// <summary>
        /// коэффициент увеличения лота для усреднения в %%
        /// </summary>
        private StrategyParameterDecimal _koefAveregingInPercents;

        /// <summary>
        /// количество шагов на усреднения
        /// </summary>
        private StrategyParameterDecimal _сountAveregeSteps;

        /// <summary>
        /// длинна МАшки
        /// </summary>
        private StrategyParameterInt _smaLenght;


        #endregion end Fields ==============================






    }
}
