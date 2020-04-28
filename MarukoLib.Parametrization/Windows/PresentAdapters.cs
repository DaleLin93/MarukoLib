using System.Collections.Generic;
using JetBrains.Annotations;
using MarukoLib.Lang;

namespace MarukoLib.Parametrization.Windows
{

    public interface IPresentAdapter
    {

        double DesiredWidth { get; }

    }

    public interface IParameterPresentAdapter : IPresentAdapter
    {

        bool CanCollapse([NotNull] IGroupDescriptor group, int depth);

        bool IsEnabled([NotNull] IReadonlyContext context, [NotNull] IParameterDescriptor parameter);

        bool IsVisible([NotNull] IReadonlyContext context, [NotNull] IDescriptor descriptor);

    }
    
    public static class PresentAdapterExt
    {

        public static double GetPreferredMinWidth([NotNull] this IEnumerable<IPresentAdapter> presentAdapters, double defaultValue = double.NaN)
        {
            double? max = null;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var adapter in presentAdapters)
            {
                var w = adapter.DesiredWidth;
                if (!double.IsNaN(w) && (max == null || max.Value < w))
                    max = w;
            }
            return max ?? defaultValue;
        }

    }

}
