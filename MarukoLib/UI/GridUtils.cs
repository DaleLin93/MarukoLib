using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;

namespace MarukoLib.UI
{

    public static class GridUtils
    {

        public static void AddWithColumn([NotNull] this Grid grid, [NotNull] UIElement element, int column, int columnSpan = 1)
        {
            if (column != 0) Grid.SetColumn(element, column);
            if (columnSpan != 1) Grid.SetColumnSpan(element, columnSpan);
            grid.Children.Add(element);
        }

        public static void AddWithRow([NotNull] this Grid grid, [NotNull] UIElement element, int row, int rowSpan = 1)
        {
            if (row != 0) Grid.SetRow(element, row);
            if (rowSpan != 1) Grid.SetRowSpan(element, rowSpan);
            grid.Children.Add(element);
        }

        public static void AddWithPosition([NotNull] this Grid grid, [NotNull] UIElement element, int row, int column, int rowSpan = 1, int columnSpan = 1)
        {
            if (row != 0) Grid.SetRow(element, row);
            if (column != 0) Grid.SetColumn(element, column);
            if (rowSpan != 1) Grid.SetRowSpan(element, rowSpan);
            if (columnSpan != 1) Grid.SetColumnSpan(element, columnSpan);
            grid.Children.Add(element);
        }

    }

}
