using JetBrains.Annotations;
using System.Windows;

namespace MarukoLib.Windows
{
    public static class MessageBoxUtils
    {

        public static MessageBoxResult InfoYesNoCancel([NotNull] string msg, [CanBeNull] string title = null)
            => Info(msg, title, MessageBoxButton.YesNoCancel);

        public static MessageBoxResult InfoYesNo([NotNull] string msg, [CanBeNull] string title = null)
            => Info(msg, title, MessageBoxButton.YesNo);

        public static MessageBoxResult InfoOkCancel([NotNull] string msg, [CanBeNull] string title = null)
            => Info(msg, title, MessageBoxButton.OKCancel);

        public static MessageBoxResult InfoOk([NotNull] string msg, [CanBeNull] string title = null)
            => Info(msg, title, MessageBoxButton.OK);

        public static MessageBoxResult Info([NotNull] string msg, [CanBeNull] string title, MessageBoxButton button)
            => MessageBox.Show(msg, title ?? "Information", button, MessageBoxImage.Information);

        public static MessageBoxResult WarningYesNoCancel([NotNull] string msg, [CanBeNull] string title = null)
            => Warning(msg, title, MessageBoxButton.YesNoCancel);

        public static MessageBoxResult WarningYesNo([NotNull] string msg, [CanBeNull] string title = null)
            => Warning(msg, title, MessageBoxButton.YesNo);

        public static MessageBoxResult WarningOkCancel([NotNull] string msg, [CanBeNull] string title = null)
            => Warning(msg, title, MessageBoxButton.OKCancel);

        public static MessageBoxResult WarningOk([NotNull] string msg, [CanBeNull] string title = null)
            => Warning(msg, title, MessageBoxButton.OK);

        public static MessageBoxResult Warning([NotNull] string msg, [CanBeNull] string title, MessageBoxButton button)
            => MessageBox.Show(msg, title ?? "Warning", button, MessageBoxImage.Warning);

        public static MessageBoxResult ErrorYesNoCancel([NotNull] string msg, [CanBeNull] string title = null)
            => Error(msg, title, MessageBoxButton.YesNoCancel);

        public static MessageBoxResult ErrorYesNo([NotNull] string msg, [CanBeNull] string title = null)
            => Error(msg, title, MessageBoxButton.YesNo);

        public static MessageBoxResult ErrorOkCancel([NotNull] string msg, [CanBeNull] string title = null)
            => Error(msg, title, MessageBoxButton.OKCancel);

        public static MessageBoxResult ErrorOk([NotNull] string msg, [CanBeNull] string title = null)
            => Error(msg, title, MessageBoxButton.OK);

        public static MessageBoxResult Error([NotNull] string msg, [CanBeNull] string title, MessageBoxButton button)
            => MessageBox.Show(msg, title ?? "Error", button, MessageBoxImage.Error);

        public static bool IsNone(this MessageBoxResult result) => result == MessageBoxResult.None;

        public static bool IsYes(this MessageBoxResult result) => result == MessageBoxResult.Yes;

        public static bool IsNo(this MessageBoxResult result) => result == MessageBoxResult.No;

        public static bool IsOk(this MessageBoxResult result) => result == MessageBoxResult.OK;

        public static bool IsCancel(this MessageBoxResult result) => result == MessageBoxResult.Cancel;

    }
}
