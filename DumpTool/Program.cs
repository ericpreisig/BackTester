using System;
using System.Reflection;
using System.Linq;

class Program {
    static void Main() {
        var t = typeof(LightweightCharts.Blazor.Charts.ChartComponent).Assembly
            .GetTypes()
            .FirstOrDefault(x => x.Name.Contains("TimeScaleApi") || x.Name.Contains("ITimeScaleApi"));
        
        var evt = t.GetMethod("add_VisibleLogicalRangeChanged");
        var arg = evt.GetParameters()[0].ParameterType.GenericTypeArguments[0];
        Console.WriteLine(arg.FullName);
    }
}
