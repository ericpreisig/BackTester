using BackTester.Strategies;
using Engine.Charts;
using Engine.Charts.Plots;
using Engine.Charts.Plots.Candle;
using Engine.Charts.Plots.Line;
using Engine.Charts.Plots.Area;
using Engine.Core;
using Engine.Strategies;
using Engine.Indicators;
using LightweightCharts.Blazor.Charts;
using LightweightCharts.Blazor.Customization.Enums;
using LightweightCharts.Blazor.Customization.Series;
using LightweightCharts.Blazor.Series;
using LightweightCharts.Blazor.Customization.Chart;
using LightweightCharts.Blazor.DataItems;
using LightweightCharts.Blazor.Models;
using LightweightCharts.Blazor.Models.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Reflection;

namespace WebApp.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    #region Fields

    // UI state
    private string _activeTab = "Stats";

    // Chart components
    private ChartComponent _mainChart = default!;
    private int _underChartCount;
    private ChartComponent[] _underCharts = Array.Empty<ChartComponent>();

    // Chart data
    private IEnumerable<IPlotSerie<IPlot, ISerieConfig>>? _chartDatas;
    private bool _chartsInitialized;
    private Chart _chart = default!;
    private IStrategy _liveStrategy = default!;

    // Chart sync state
    private bool _isSyncing;
    private bool _isSyncingCrosshair;
    private LogicalRange? _lastKnownRange;

    // Chart tracking maps
    private Dictionary<string, Dictionary<string, string>> _chartMetrics = new();
    private Dictionary<string, ChartComponent> _underChartMap = new();
    private Dictionary<string, ISeriesApi<long>> _primarySeriesMap = new();
    private List<string> _underChartGroupNames = new();
    private List<(ChartComponent chart, ISeriesApi<long> series)> _allSeries = new();
    private Dictionary<string, EventHandler<MouseEventParams<long>>> _crosshairHandlers = new();

    // Sidebar state
    private List<Type> _availableStrategies = new();
    private List<Type> _availableIndicators = new();
    private Type? _selectedStrategyType;
    private HashSet<Type> _selectedIndicatorTypes = new();

    // Debounce
    private CancellationTokenSource? _debounceCts;

    #endregion

    #region Lifecycle

    protected override async Task OnInitializedAsync()
    {
        DiscoverStrategiesAndIndicators();

        _selectedStrategyType = _availableStrategies.FirstOrDefault(t => t.Name == "SmaCrossingStrategy")
                               ?? _availableStrategies.FirstOrDefault();

        var initialStrategy = CreateStrategyInstance();
        var initialSymbol = (initialStrategy is BaseStrategy bsInit) ? bsInit.SymbolName : "BTC-USD";

        _chart = new Chart
        {
            HistoryFrom = new DateTime(2017, 01, 01),
            DateFrom = new DateTime(2018, 01, 01),
            Strategy = initialStrategy,
            Symbol = new Symbol(initialSymbol)
        };
        _liveStrategy = _chart.Strategy;

        var context = new Context(_chart);
        await context.PrepareAsync();
        _chartDatas = await context.ExecuteOnlyAsync();

        if (_chartDatas != null)
        {
            var groups = GetUnderChartGroups();
            _underChartGroupNames = groups.Select(g => g.Key).ToList();
            _underChartCount = groups.Count;
            _underCharts = new ChartComponent[_underChartCount];
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_chartDatas == null || _chartsInitialized) return;
        if (_underChartCount > 0 && _underCharts.Any(c => c == null)) return;

        _chartsInitialized = true;

        // Remove every series from every chart before re-adding (since components are reused)
        foreach (var (chart, series) in _allSeries)
        {
            try { await chart.RemoveSeries(series); } catch { }
        }
        _allSeries.Clear();
        _primarySeriesMap.Clear();

        var savedRange = _lastKnownRange;

        // Ensure all components are ready
        if (_mainChart != null) await _mainChart.InitializationCompleted;
        foreach (var c in _underCharts)
        {
            if (c != null) await c.InitializationCompleted;
        }

        // Register crosshair handlers
        RegisterCrosshairHandler("Main", _mainChart);

        var underChartGroups = GetUnderChartGroups();
        int currentUnderChartIndex = 0;
        _underChartMap.Clear();

        foreach (var group in underChartGroups)
        {
            var chartName = group.Key;
            var c = _underCharts[currentUnderChartIndex++];
            _underChartMap[chartName] = c;
            RegisterCrosshairHandler(chartName, c);
        }

        // Add all series to their target charts
        await AddAllSeriesToCharts();

        // Restore logical range and hook sync events
        await RestoreAndSyncRange(savedRange);

        // Update metrics for all charts
        UpdateMetrics("Main", null);
        foreach (var group in underChartGroups)
        {
            UpdateMetrics(group.Key, null);
        }
        StateHasChanged();
    }

    #endregion

    #region Sidebar Actions

    private async Task OnSelectStrategy(Type t)
    {
        _selectedStrategyType = t;
        _selectedIndicatorTypes.Clear();
        await FullReload();
    }

    private async Task OnToggleIndicator(Type t)
    {
        if (!_selectedIndicatorTypes.Remove(t))
            _selectedIndicatorTypes.Add(t);

        _selectedStrategyType = null;
        await FullReload();
    }

    #endregion

    #region Chart Reload

    private async Task FullReload()
    {
        _liveStrategy = CreateStrategyInstance();
        await ReloadChart();
    }

    private async Task ReloadChart()
    {
        _chartsInitialized = false;

        if (_mainChart != null)
        {
            try
            {
                var ts = await _mainChart.TimeScale();
                _lastKnownRange = await ts.GetVisibleLogicalRange();
            }
            catch { }
        }

        var symName = (_liveStrategy is BaseStrategy bs) ? bs.SymbolName : "BTC-USD";
        var newStrategy = CreateStrategyInstance();
        if (_liveStrategy != null && newStrategy.GetType() == _liveStrategy.GetType())
            CopyProperties(_liveStrategy, newStrategy);

        var newChart = new Chart
        {
            HistoryFrom = _chart.HistoryFrom,
            DateFrom = _chart.DateFrom,
            Strategy = newStrategy,
            Symbol = new Symbol(symName)
        };

        var context = new Context(newChart);
        await context.PrepareAsync();

        _chartDatas = await context.ExecuteOnlyAsync();
        _chart = newChart;
        _liveStrategy = newChart.Strategy;

        _primarySeriesMap.Clear();
        _underChartMap.Clear();
        _chartMetrics.Clear();

        if (_chartDatas != null)
        {
            var groups = GetUnderChartGroups();
            var newCount = groups.Count;
            _underChartGroupNames = groups.Select(g => g.Key).ToList();

            if (newCount != _underChartCount)
            {
                _underChartCount = newCount;
                _underCharts = new ChartComponent[_underChartCount];
            }
        }
        StateHasChanged();
    }

    #endregion

    #region Input Handling

    private async Task HandleInputChanged(PropertyInfo prop, IIndicator indicator, ChangeEventArgs e)
    {
        try
        {
            object val = prop.PropertyType == typeof(System.Drawing.Color)
                ? System.Drawing.ColorTranslator.FromHtml(e.Value?.ToString() ?? "#FFFFFF")
                : Convert.ChangeType(e.Value, prop.PropertyType);

            prop.SetValue(indicator, val);

            _debounceCts?.Cancel();
            _debounceCts?.Dispose();

            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            await Task.Delay(500, token);

            if (!token.IsCancellationRequested)
                await ReloadChart();
        }
        catch
        {
            // Ignore format parsing errors during typing
        }
    }

    #endregion

    #region Chart Sync

    private async void SyncLogicalRange(object? sender, LogicalRange e)
    {
        if (e != null) _lastKnownRange = e;
        if (_isSyncing || e == null) return;

        _isSyncing = true;
        try
        {
            if (_mainChart != null)
            {
                var ts = await _mainChart.TimeScale();
                await ts.SetVisibleLogicalRange(e);
            }
            foreach (var c in _underCharts)
            {
                if (c != null)
                {
                    var ts = await c.TimeScale();
                    await ts.SetVisibleLogicalRange(e);
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private async void OnCrosshairMoved(string chartName, MouseEventParams<long> e)
    {
        UpdateMetrics(chartName, e.Time);

        if (_isSyncingCrosshair) return;
        _isSyncingCrosshair = true;
        try
        {
            var underGroups = _chartDatas?
                .Where(c => c.Postion == Engine.Enums.PlotPositionEnum.UnderChart)
                .GroupBy(c => c.ChartName)
                .Select(g => g.Key)
                .ToList() ?? new List<string>();

            if (e.Time.HasValue)
            {
                var targetTime = e.Time.Value;

                if (chartName != "Main" && _mainChart != null)
                {
                    UpdateMetrics("Main", targetTime);
                    if (_primarySeriesMap.TryGetValue("Main", out var serieMain))
                        await _mainChart.SetCrosshairPosition(0, targetTime, serieMain);
                }

                foreach (var targetChartName in underGroups)
                {
                    if (targetChartName != chartName && _underChartMap.TryGetValue(targetChartName, out var chartComp))
                    {
                        UpdateMetrics(targetChartName, targetTime);
                        if (_primarySeriesMap.TryGetValue(targetChartName, out var targetSerie))
                            await chartComp.SetCrosshairPosition(0, targetTime, targetSerie);
                    }
                }
            }
            else
            {
                if (chartName != "Main" && _mainChart != null)
                {
                    UpdateMetrics("Main", null);
                    await _mainChart.ClearCrosshairPosition();
                }
                foreach (var targetChartName in underGroups)
                {
                    if (targetChartName != chartName && _underChartMap.TryGetValue(targetChartName, out var chartComp))
                    {
                        UpdateMetrics(targetChartName, null);
                        await chartComp.ClearCrosshairPosition();
                    }
                }
            }
        }
        catch { }
        finally
        {
            _isSyncingCrosshair = false;
        }
    }

    #endregion

    #region Metrics

    private void UpdateMetrics(string chartName, long? unixTime)
    {
        if (_chartDatas == null) return;

        var series = chartName == "Main"
            ? _chartDatas.Where(c => c.Postion == Engine.Enums.PlotPositionEnum.OnChart)
            : _chartDatas.Where(c => c.ChartName == chartName);

        if (!series.Any()) return;

        DateTime targetTime;
        if (unixTime.HasValue)
        {
            targetTime = DateTimeOffset.FromUnixTimeSeconds(unixTime.Value).UtcDateTime;
        }
        else
        {
            var allKeys = series.SelectMany(s => s.Metrics?.Keys ?? Enumerable.Empty<DateTime>()).ToList();
            targetTime = allKeys.Any() ? allKeys.Max() : DateTime.MinValue;
            if (targetTime == DateTime.MinValue) return;
        }

        var combinedMetrics = new Dictionary<string, string>();
        foreach (var s in series)
        {
            if (s.Metrics == null || !s.Metrics.Any()) continue;
            var matches = s.Metrics.Keys.Where(k => k <= targetTime);
            if (matches.Any())
            {
                var closestTime = matches.Max();
                if (s.Metrics.TryGetValue(closestTime, out var dict))
                {
                    foreach (var kvp in dict)
                        combinedMetrics[kvp.Key] = kvp.Value;
                }
            }
        }

        _chartMetrics[chartName] = combinedMetrics;
        InvokeAsync(StateHasChanged);
    }

    private static string GetTooltip(string metricName) => metricName switch
    {
        "Sharpe Ratio" => "Mesure le rendement de la stratégie corrigé de son risque total (Volatilité). Plus c'est élevé, meilleur est le profil rendement/risque.",
        "Sortino Ratio" => "Comme le Sharpe, mais ne pénalise que la volatilité à la baisse (le vrai risque). Un ratio élevé signifie des rendements consistants avec peu de grosses chutes.",
        "Omega Ratio" => "Ratio de la somme des gains sur la somme des pertes. Évalue l'asymétrie totale d'une stratégie de manière probabiliste.",
        "Calmar Ratio" => "Rendement total divisé par le Max Drawdown. Mesure la tolérance à la douleur pour atteindre ce rendement.",
        "Beta (β)" => "Mesure la volatilité systématique (corrélation) de la stratégie par rapport au marché (Buy & Hold). Beta = 1 : Mouvements identiques au marché. Beta < 1 : Plus stable.",
        "Jensen's Alpha (α)" => "La surperformance (ou sous-performance) pure de la stratégie après déduction du risque marché (Beta). C'est le vrai 'edge' de la stratégie.",
        "Information Ratio" => "Rendement excédentaire divisé par le Tracking Error. Mesure la régularité mathématique à battre le marché de référence de façon constante.",
        "Kelly Criterion" => "Algorithme probabiliste suggérant quel pourcentage de capital risquer sur ce signal de trading pour maximiser la croissance à long terme en évitant la ruine.",
        "Recovery Factor" => "Profit net divisé par la pire chute en argent (Net Profit / Max Drawdown $). Affiche la capacité de survie du fond.",
        "Profit Factor" => "Gains bruts divisés par les Pertes brutes. Doit être strictement supérieur à 1 pour garantir une profitabilité structurelle.",
        "Payoff Ratio" => "La taille moyenne d'un trade gagnant divisée par la taille absolue d'un trade perdant.",
        "Expectancy ($)" => "L'espérance mathématique : ce que la stratégie gagne ou perd 'en moyenne' à chaque transaction exécutée.",
        _ => "Analyse en temps réel de l'évolution du portefeuille."
    };

    #endregion

    #region Series Rendering

    private async Task<ISeriesApi<long>> SetLineSerieAsync(ChartComponent chart, IPlotSerie<PlotLine, PlotLineSerieConfig> chartData)
    {
        if (chartData == null) return null!;
        await ApplyDarkModeOptions(chart);

        var series = await chart.AddSeries<LineStyleOptions>(SeriesType.Line, new LineStyleOptions
        {
            Color = System.Drawing.ColorTranslator.FromHtml(GetColorOrDefault(chartData.Config.Color, "#2962FF")),
            Title = GetSeriesTitle(chartData)
        });

        var data = chartData.Plots.Select(a => new LineData<long>
        {
            Time = new DateTimeOffset(a.Time).ToUnixTimeSeconds(),
            Value = (double)a.Value
        });
        await series.SetData(data);
        await ApplyMarkers(series, chartData.Markers);
        return series;
    }

    private async Task<ISeriesApi<long>> SetCandleSticksSerieAsync(ChartComponent chart, IPlotSerie<PlotCandle, PlotCandleSerieConfig> chartData)
    {
        if (chartData == null) return null!;
        await ApplyDarkModeOptions(chart);

        var series = await chart.AddSeries<CandlestickStyleOptions>(SeriesType.Candlestick, new CandlestickStyleOptions
        {
            UpColor = System.Drawing.ColorTranslator.FromHtml(GetColorOrDefault(chartData.Config.UpColor, "#26a69a")),
            DownColor = System.Drawing.ColorTranslator.FromHtml(GetColorOrDefault(chartData.Config.DownColor, "#ef5350")),
            BorderVisible = chartData.Config.BorderVisible,
            WickDownColor = System.Drawing.ColorTranslator.FromHtml(GetColorOrDefault(chartData.Config.WickDownColor, "#ef5350")),
            WickUpColor = System.Drawing.ColorTranslator.FromHtml(GetColorOrDefault(chartData.Config.WickUpColor, "#26a69a")),
            Title = GetSeriesTitle(chartData)
        });

        var data = chartData.Plots.Select(a => new CandlestickData<long>
        {
            Time = new DateTimeOffset(a.Time).ToUnixTimeSeconds(),
            Close = (double)a.Close,
            High = (double)a.High,
            Low = (double)a.Low,
            Open = (double)a.Open,
        });
        await series.SetData(data);
        await ApplyMarkers(series, chartData.Markers);
        return series;
    }

    private async Task<ISeriesApi<long>> SetAreaSerieAsync(ChartComponent chart, IPlotSerie<PlotArea, AreaSerieConfig> chartData)
    {
        if (chartData == null) return null!;
        await ApplyDarkModeOptions(chart);

        var series = await chart.AddSeries<AreaStyleOptions>(SeriesType.Area, new AreaStyleOptions
        {
            BaseLineColor = System.Drawing.ColorTranslator.FromHtml(GetColorOrDefault(chartData.Config.Color, "#2962FF")),
            Title = GetSeriesTitle(chartData)
        });

        var data = chartData.Plots.Select(a => new AreaData<long>
        {
            Time = new DateTimeOffset(a.Time).ToUnixTimeSeconds(),
            Value = (double)a.Value
        });
        await series.SetData(data);
        await ApplyMarkers(series, chartData.Markers);
        return series;
    }

    #endregion

    #region Helpers

    private void DiscoverStrategiesAndIndicators()
    {
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .ToList();

        var strategyBase = typeof(BaseStrategy);
        var indicatorBase = typeof(BaseIndicator);

        _availableStrategies = allTypes
            .Where(t => !t.IsAbstract && !t.IsInterface
                && strategyBase.IsAssignableFrom(t)
                && t.Name != "DefaultStrategy")
            .OrderBy(t => t.Name)
            .ToList();

        _availableIndicators = allTypes
            .Where(t => !t.IsAbstract && !t.IsInterface
                && indicatorBase.IsAssignableFrom(t)
                && !strategyBase.IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToList();
    }

    private IStrategy CreateStrategyInstance()
    {
        if (_selectedStrategyType != null)
            return (IStrategy)CreateDefaultInstance(_selectedStrategyType);

        if (_selectedIndicatorTypes.Any())
        {
            var indicators = _selectedIndicatorTypes
                .Select(t => (IIndicator)CreateDefaultInstance(t))
                .ToArray();
            return new DefaultStrategy(indicators);
        }

        return (IStrategy)CreateDefaultInstance(_availableStrategies.First());
    }

    private static object CreateDefaultInstance(Type type)
    {
        var ctors = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length);
        foreach (var ctor in ctors)
        {
            var args = ctor.GetParameters().Select(p =>
            {
                if (p.ParameterType == typeof(decimal)) return (object)1000m;
                if (p.ParameterType == typeof(int)) return (object)20;
                if (p.ParameterType == typeof(System.Drawing.Color)) return (object)System.Drawing.Color.Blue;
                if (p.ParameterType.IsValueType) return Activator.CreateInstance(p.ParameterType);
                return null;
            }).ToArray();
            try { return ctor.Invoke(args); } catch { }
        }
        return Activator.CreateInstance(type)!;
    }

    private static void CopyProperties(IIndicator source, IIndicator target)
    {
        var props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);
        foreach (var prop in props)
            prop.SetValue(target, prop.GetValue(source));

        for (int i = 0; i < source.Indicators.Count; i++)
        {
            if (i < target.Indicators.Count)
                CopyProperties(source.Indicators[i], target.Indicators[i]);
        }
    }

    /// <summary>Returns the under-chart groups, consistently ordered (Portfolio last).</summary>
    private List<IGrouping<string, IPlotSerie<IPlot, ISerieConfig>>> GetUnderChartGroups()
    {
        return (_chartDatas ?? Enumerable.Empty<IPlotSerie<IPlot, ISerieConfig>>())
            .Where(c => c.Postion == Engine.Enums.PlotPositionEnum.UnderChart)
            .GroupBy(c => c.ChartName)
            .OrderBy(g => g.Key == "Portfolio" ? 1 : 0).ThenBy(g => g.Key)
            .ToList();
    }

    private static string GetColorOrDefault(string? color, string fallback)
        => string.IsNullOrEmpty(color) ? fallback : color;

    private static string GetSeriesTitle(IPlotSerie<IPlot, ISerieConfig> chartData)
        => string.IsNullOrEmpty(chartData.Name) ? chartData.ChartName : chartData.Name;

    private static SeriesMarkerBarPosition MapMarkerPosition(Engine.Charts.Plots.MarkerPosition pos) => pos switch
    {
        Engine.Charts.Plots.MarkerPosition.AboveBar => SeriesMarkerBarPosition.AboveBar,
        Engine.Charts.Plots.MarkerPosition.BelowBar => SeriesMarkerBarPosition.BelowBar,
        Engine.Charts.Plots.MarkerPosition.InBar => SeriesMarkerBarPosition.InBar,
        _ => SeriesMarkerBarPosition.AboveBar
    };

    private static SeriesMarkerShape MapMarkerShape(Engine.Charts.Plots.MarkerShape shape) => shape switch
    {
        Engine.Charts.Plots.MarkerShape.Circle => SeriesMarkerShape.Circle,
        Engine.Charts.Plots.MarkerShape.ArrowUp => SeriesMarkerShape.ArrowUp,
        Engine.Charts.Plots.MarkerShape.ArrowDown => SeriesMarkerShape.ArrowDown,
        Engine.Charts.Plots.MarkerShape.Square => SeriesMarkerShape.Square,
        _ => SeriesMarkerShape.Circle
    };

    private static async Task ApplyMarkers(ISeriesApi<long> series, IEnumerable<Marker>? markers)
    {
        if (markers == null || !markers.Any()) return;

        var mapped = markers.Select(m => new SeriesMarkerBar<long>
        {
            Time = new DateTimeOffset(m.Time).ToUnixTimeSeconds(),
            Position = MapMarkerPosition(m.Position),
            Shape = MapMarkerShape(m.Shape),
            Color = System.Drawing.ColorTranslator.FromHtml(GetColorOrDefault(m.Color, "#26a69a")),
            Text = m.Text ?? "",
            Size = 1
        });
        await series.CreateSeriesMarkers(mapped);
    }

    private async Task ApplyDarkModeOptions(ChartComponent chart)
    {
        if (chart == null) return;

        var options = new ChartOptions
        {
            AutoSize = true,
            Layout = new LayoutOptions
            {
                TextColor = System.Drawing.ColorTranslator.FromHtml("#d1d4dc"),
            },
            Grid = new GridOptions { },
            TimeScale = new TimeScaleOptions
            {
                TimeVisible = true,
                RightOffset = 5
            }
        };

        try { await chart.ApplyOptions(options); } catch { }
    }

    private void RegisterCrosshairHandler(string chartName, ChartComponent? chart)
    {
        if (chart == null) return;

        if (_crosshairHandlers.TryGetValue(chartName, out var oldHandler))
            chart.CrosshairMoved -= oldHandler;

        EventHandler<MouseEventParams<long>> newHandler = (sender, e) => OnCrosshairMoved(chartName, e);
        _crosshairHandlers[chartName] = newHandler;
        chart.CrosshairMoved += newHandler;
    }

    private async Task AddAllSeriesToCharts()
    {
        if (_chartDatas == null) return;

        foreach (var chartData in _chartDatas)
        {
            if (chartData.Postion == Engine.Enums.PlotPositionEnum.Hidden) continue;

            var targetChart = _mainChart;
            var mapKey = "Main";

            if (chartData.Postion == Engine.Enums.PlotPositionEnum.UnderChart)
            {
                if (string.IsNullOrEmpty(chartData.ChartName) || !_underChartMap.TryGetValue(chartData.ChartName, out targetChart))
                    continue;
                mapKey = chartData.ChartName;
            }

            if (targetChart == null) continue;

            ISeriesApi<long>? series = chartData.Type switch
            {
                Engine.Enums.PlotTypeEnum.Line when chartData is IPlotSerie<PlotLine, PlotLineSerieConfig> lineData
                    => await SetLineSerieAsync(targetChart, lineData),
                Engine.Enums.PlotTypeEnum.Candle when chartData is IPlotSerie<PlotCandle, PlotCandleSerieConfig> candleData
                    => await SetCandleSticksSerieAsync(targetChart, candleData),
                Engine.Enums.PlotTypeEnum.Area when chartData is IPlotSerie<PlotArea, AreaSerieConfig> areaData
                    => await SetAreaSerieAsync(targetChart, areaData),
                _ => null
            };

            if (series != null)
            {
                _allSeries.Add((targetChart, series));
                if (!_primarySeriesMap.ContainsKey(mapKey))
                    _primarySeriesMap[mapKey] = series;
            }
        }
    }

    private async Task RestoreAndSyncRange(LogicalRange? savedRange)
    {
        if (_mainChart != null)
        {
            try
            {
                var ts = await _mainChart.TimeScale();
                if (savedRange != null) await ts.SetVisibleLogicalRange(savedRange);
                ts.VisibleLogicalRangeChanged += SyncLogicalRange;
            }
            catch { }
        }

        foreach (var c in _underCharts)
        {
            if (c != null)
            {
                try
                {
                    var ts = await c.TimeScale();
                    if (savedRange != null) await ts.SetVisibleLogicalRange(savedRange);
                    ts.VisibleLogicalRangeChanged += SyncLogicalRange;
                }
                catch { }
            }
        }

        if (savedRange != null)
            _lastKnownRange = savedRange;
    }

    #endregion
}
