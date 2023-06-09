/*
 
 мартингейл на болинжере только лонг


 1. входим в рынок при выходе вверх  цены в нашу сторону от центра болинжера 
 
 2. входить будем на размер ( _lotEntrySize )

 3. тейк ставим на верхнюю линию болинжера
и если цена выше средней то переставляем тейк на верхнюю линию болинжера

 4. если ушли ниже болинжер то усреднимся






 3. при достижении цены тейка выходим ( _takeProfitInPoints ) и начинаем с начала
 

 4.если ушли в минус и пересекает болинжера снизу то на возврате усредняемся 
на ( _lotEntrySize * _koefAveregingInPercents )

 
 
 */



using System.Collections.Generic;
using System.Drawing;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.Indicators;
using System.Windows.Forms;

[Bot("MartinBollinger")]


public class MartinBollinger : BotPanel

{

    public MartinBollinger(string name, StartProgram startProgram) : base(name, startProgram)
    {

        #region  подготовка вкладки ===================================

        // делаем вкладку
        this.TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];

        Mode = this.CreateParameter("Mode", "Edit", new[] { "Edit", "Trade" });

        // добавим болинжер
        BollingerLength = CreateParameter("Bollinger Length", 12, 4, 100, 2);
        BollingerDeviation = CreateParameter("Bollinger Deviation", 2, 0.5m, 4, 0.1m);
        //
        _bol = IndicatorsFactory.CreateIndicatorByName("Bollinger", name + "Bollinger", false);
        _bol = (Aindicator)_tab.CreateCandleIndicator(_bol, "Prime");
        _bol.ParametersDigit[0].Value = BollingerLength.ValueInt;
        _bol.ParametersDigit[1].Value = BollingerDeviation.ValueDecimal;

        // на график линию тейка  для всех поз
        takeProfitPriceLevelLine = new LineHorisontal("takeProfitPriceLevelLine", "Prime", false)
        {
            Color = Color.YellowGreen,
            Value = 0
        };
        _tab.SetChartElement(takeProfitPriceLevelLine);
        // averegeEntryPriceLevelLine.Value = 0;
        // averegeEntryPriceLevelLine.TimeEnd = DateTime.Now;
        // takeProfitPrice

        // на график линию входа для всех поз она же ноль, безубыток
        averegeZerroLeverAllPositionsLine = new LineHorisontal("averegeZerroLeverAllPositionsLine", "Prime", false)
        {
            Color = Color.LightGray,
            Value = 0
        };
        _tab.SetChartElement(averegeZerroLeverAllPositionsLine);
        // takeProfitAllPositionsLevelLine.Value = 0;
        // takeProfitAllPositionsLevelLine.TimeEnd = DateTime.Now;


        #endregion конец подготовка вкладки ===================================


        // лот на вход первым шагом
        _lotEntrySize = CreateParameter("_lotEntrySize  ", 0.01m, 0.0001m, 10, 0.0001m);
        lotEntrySize = _lotEntrySize.ValueDecimal;
        //коэффициент увеличения лота для усреднения в %% на каждый шаг
        _koefAveregingInPercents = CreateParameter("_koefAveregingInPercents  ", 50m, 1, 500, 1);
        //количество шагов на усреднения
        _сountAveregeSteps = CreateParameter("_сountAveregeSteps  ", 4m, 1, 10, 1);
        // тейк в пунктах
        // _takeProfitInPoints = CreateParameter("_takeProfitInPoints  ", 0.03m, 0.00001m, 1000, 0.00001m);


        // событие закрылась свечка
        _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;


    }


    #region TradeLogic ==============================

    // открылась новая свечка
    private void _tab_CandleFinishedEvent(List<Candle> candles)
    {

        // если не разрешено торговать то возврат
        if (Mode.ValueString != "Trade")
        {
            return;
        }

        // ждем накопления свечек   для корректного расчета болинжера
        if (_bol.DataSeries[0].Values == null || candles.Count < _bol.ParametersDigit[0].Value + 2)
        { return; }


        // тесты узнать о бумаге
        //decimal a = _tab.Securiti.PriceStep;
        //decimal b = _tab.Securiti.PriceStepCost;

        // значения переменных на сейчас
        lastCandleLow = candles[candles.Count - 1].Low;
        bolLastUp = _bol.DataSeries[0].Last;
        bolLastDown = _bol.DataSeries[1].Last;
        bolCenterLine = _bol.DataSeries[2].Last;
        bolSpread = bolLastUp - bolLastDown;

        // если превысили количество шагов то кроем нафиг все и не торгуем
        if (countAveregeSteps >= _сountAveregeSteps.ValueDecimal)
        {

            CloseAllposition();
            Mode.ValueString = "Edit";
            MessageBox.Show(
                "рынок вас натянул на шишку. превышено кол-во шагов усреднений",
                "еще хотите ???",
                MessageBoxButtons.OK,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly);
            return;
        }




        // если закрытие больше чем центр болинжера то открываем  первую позицию
        // и если нет позиций делаем первый шаг серии позиций
        // if (lastCandleLow < bolLastDown
        // if (lastCandleLow > bolCenterLine
        // 
        if (lastCandleLow < bolLastDown
            && _tab.PositionOpenLong.Count == 0)
        {
            OpenFirstStep();
            // CalculateTakeProfitPrice();
            // CalculateAveregeZerroLeverAllPositions();

        }


        // если не превышено кол во шагов
        // если ушли ниже болинжер то усредним
        // и есть открытые позы
        // если ушли от последней открытой позы не менее чем размах болинжера
        if (countAveregeSteps < _сountAveregeSteps.ValueDecimal
            && _tab.PriceCenterMarketDepth < bolLastDown
            && _tab.PositionOpenLong.Count != 0
            && _tab.PriceCenterMarketDepth < lastOpenPositionPrice - bolSpread
            )
        {

            OpenNextStep();
            CalculateTakeProfitPrice();
            CalculateAveregeZerroLeverAllPositions();
        }

        // если поза подвисла надо тейк пересчитывать
        //  если расстояние от последней отрытой позы до  средней всех поз больше размаха болинжера то пересчитываем  
        if (averegeZerroLeverAllPositions - lastOpenPositionPrice > bolSpread)
        {

            CalculateTakeProfitPrice();
        }




        // если цена больше чем цена  тейк И есть открытые позиции то закроем
        if (_tab.PriceCenterMarketDepth > takeProfitPrice
            && _tab.PositionOpenLong.Count != 0)
        {
            CloseAllposition();
        }


    }

    #endregion end TradeLogic ==============================


    #region Methods ==============================

    /// <summary>
    /// метод расчет профит цены входа  по всем позам
    /// </summary>
    void CalculateTakeProfitPrice()
    {

        // поставим тейк на безубыток плюс размах болинжера
        takeProfitPrice = averegeZerroLeverAllPositions + bolSpread;

        // если он выше предыдущего то приравняем к предыдущему
        if (takeProfitPrice > takeProfitPricePrevious)
        {
            takeProfitPrice = takeProfitPricePrevious;
        }
        else { takeProfitPricePrevious = takeProfitPrice; }

        // поставим тейк на верхнюю линию болинжера
        // takeProfitPrice = bolLastUp;

        // нарисуем линию тейка 
        takeProfitPriceLevelLine.Value = takeProfitPrice;
        takeProfitPriceLevelLine.Refresh();
    }

    /// <summary>
    /// метод открываем первую сделку
    /// </summary>
    void OpenFirstStep()
    {

        lastOpenPositionPrice = _tab.PriceCenterMarketDepth;
        _tab.BuyAtLimit(lotEntrySize, lastOpenPositionPrice);
        // и увеличиваем значение шагов усреднения
        countAveregeSteps++;
        // средняя цена равна открытию
        averegeZerroLeverAllPositions = lastOpenPositionPrice;
        // ставим тейк на размах болинжера
        takeProfitPrice = lastOpenPositionPrice + bolSpread / 2;
        takeProfitPricePrevious = takeProfitPrice;
        // нарисуем линию тейка 
        takeProfitPriceLevelLine.Value = takeProfitPrice;
        takeProfitPriceLevelLine.Refresh();
        // нарисуем среднюю входа она же безубытка
        averegeZerroLeverAllPositionsLine.Value = averegeZerroLeverAllPositions;
        averegeZerroLeverAllPositionsLine.Refresh();


        // считаем шаг уменьшения пропорционально кол-ву шагов
        // takeProfitInPoints = _takeProfitInPoints.ValueDecimal;
        // stepDecrementTake = takeProfitInPoints / _сountAveregeSteps.ValueDecimal;;

        //if (_koefAveregingInPercents.ValueDecimal > 100) { _koefAveregingInPercents.ValueDecimal = 100; }
    }

    /// <summary>
    /// метод открываем следующий шаг
    /// </summary>
    void OpenNextStep()
    {
        lastOpenPositionPrice = _tab.PriceCenterMarketDepth;

        // посчитаем увеличение лота для сделки
        lotEntrySize = lotEntrySize + (lotEntrySize * _koefAveregingInPercents.ValueDecimal / 100);
        _tab.BuyAtLimit(lotEntrySize, lastOpenPositionPrice);
        // и увеличиваем значение шагов усреднения
        countAveregeSteps++;

        // уменьшим тейк на коэффициент увеличения лотности. чтоб приблизить выход из увеличивающейся позы
        // takeProfitInPoints = takeProfitInPoints - stepDecrementTake;
        // if (takeProfitInPoints < 0) { takeProfitInPoints = 0; }
    }

    /// <summary>
    /// метод закроем все позы
    /// </summary>
    void CloseAllposition()
    {
        //Task task1 = new Task(_tab.CloseAllAtMarket);
        //task1.Start();
        //task1.Wait();

        lastClosePositionPrice = _tab.PriceCenterMarketDepth;
        _tab.CloseAllAtMarket();

        lotEntrySize = _lotEntrySize.ValueDecimal;
        countAveregeSteps = 0;
        lastOpenPositionPrice = 0;
        averegeZerroLeverAllPositions = 0;
        takeProfitPrice = 0;
        takeProfitPricePrevious = 0;

        //Task task2 = new Task(_tab.PositionOpenLong.Clear);
        //task2.Start();
        //task2.Wait();

        _tab.PositionOpenLong.Clear();

        takeProfitPriceLevelLine.Delete();
        averegeZerroLeverAllPositionsLine.Delete();

    }

    /// <summary>
    /// метод расчет средней цены входа  по всем позам
    /// </summary>
    void CalculateAveregeZerroLeverAllPositions()
    {
        // расчет безубыточности по всем позам
        averegeZerroLeverAllPositions = 0;
        List<Position> positions = _tab.PositionOpenLong;
        //int count = _tab.PositionOpenLong.Count;
        decimal lots = 0;
        decimal entryPrice = 0;
        decimal lotAccum = 0;

        for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
        {
            lots = positions[i].Lots;
            entryPrice = positions[i].EntryPrice;
            averegeZerroLeverAllPositions = averegeZerroLeverAllPositions + (lots * entryPrice);
            lotAccum = lotAccum + positions[i].Lots;
        }
        averegeZerroLeverAllPositions = averegeZerroLeverAllPositions / lotAccum;

        // нарисуем среднюю входа она же безубытка
        averegeZerroLeverAllPositionsLine.Value = averegeZerroLeverAllPositions;
        averegeZerroLeverAllPositionsLine.Refresh();



    }


    // вернуть имя
    public override string GetNameStrategyType()
    {
        return nameof(MartinBollinger);
    }
    // для настроек
    public override void ShowIndividualSettingsDialog()
    {

    }

    #endregion end Methods ==============================



    #region Fields ==============================

    /// <summary>
    /// цена тейкпрофит
    /// </summary>
    private decimal takeProfitPrice;

    /// <summary>
    /// цена тейкпрофит предыдущий
    /// </summary>
    private decimal takeProfitPricePrevious;


    /// <summary>
    /// цена закрытия последней свечи
    /// </summary>
    private decimal lastCandleLow;
    /// <summary>
    /// цена верхнего болинжера
    /// </summary>
    private decimal bolLastUp;
    /// <summary>
    /// цена нижнего болинжера
    /// </summary>
    private decimal bolLastDown;
    /// <summary>
    /// цена центра болинжера
    /// </summary>
    private decimal bolCenterLine;
    /// <summary>
    /// размах болинжера (от низа до верха, типа волатильность)
    /// </summary>
    private decimal bolSpread;

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
    public decimal averegeZerroLeverAllPositions = 0m;

    /// <summary>
    /// цена открытия последней позиции
    /// </summary>
    public decimal lastOpenPositionPrice = 0m;

    /// <summary>
    /// цена открытия последней позиции
    /// </summary>
    public decimal lastClosePositionPrice = 0m;

    /// <summary>
    /// счетчик для шагов усреднения набора позиции
    /// </summary>
    public decimal countAveregeSteps = 0;

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
    private StrategyParameterDecimal _takeProfitInPoints;

    /// <summary>
    /// режим работы
    /// </summary>
    public StrategyParameterString Regime;

    /// <summary>
    /// вкладка для торговли
    /// </summary>
    private BotTabSimple _tab;

    /// <summary>
    /// режим торгуем - редактируем
    /// </summary>
    private StrategyParameterString Mode;

    /// <summary>
    /// болинжер 
    /// </summary>
    private Aindicator _bol;
    public StrategyParameterDecimal BollingerDeviation;
    public StrategyParameterInt BollingerLength;

    /// <summary>
    ///на график линию входа для всех поз она же ноль, безубыток
    /// </summary>
    public LineHorisontal averegeZerroLeverAllPositionsLine;

    /// <summary>
    /// линия средней цены входа на график линию тейка  для всех поз
    /// </summary>
    public LineHorisontal takeProfitPriceLevelLine;


    /// <summary>
    ///коэффициент увеличения лота для усреднения в %% на каждый шаг
    /// </summary>
    private StrategyParameterDecimal _koefAveregingInPercents;


    /// <summary>
    /// количество шагов на усреднения
    /// </summary>
    private StrategyParameterDecimal _сountAveregeSteps;



    #endregion end Fields ==============================

}



