using System.Windows;
using JetBrains.Annotations;

namespace MarukoLib.Windows
{
    public static class MsgBoxUtils
    {

        #region INFO

        public static MessageBoxResult InfoYesNoCancel([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Info(msg, title, MessageBoxButton.YesNoCancel, defaultResult);

        public static MessageBoxResult InfoYesNo([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Info(msg, title, MessageBoxButton.YesNo, defaultResult);

        public static MessageBoxResult InfoOkCancel([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Info(msg, title, MessageBoxButton.OKCancel, defaultResult);

        public static MessageBoxResult InfoOk([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Info(msg, title, MessageBoxButton.OK, defaultResult);

        public static MessageBoxResult Info([NotNull] string msg, [CanBeNull] string title, MessageBoxButton button, MessageBoxResult defaultResult = MessageBoxResult.None)
            => MessageBox.Show(msg, title ?? "Information", button, MessageBoxImage.Information);

        #endregion

        #region QUESTION

        public static MessageBoxResult QuestionYesNoCancel([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Question(msg, title, MessageBoxButton.YesNoCancel, defaultResult);

        public static MessageBoxResult QuestionYesNo([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Question(msg, title, MessageBoxButton.YesNo, defaultResult);

        public static MessageBoxResult QuestionOkCancel([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Question(msg, title, MessageBoxButton.OKCancel, defaultResult);

        public static MessageBoxResult QuestionOk([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Question(msg, title, MessageBoxButton.OK, defaultResult);

        public static MessageBoxResult Question([NotNull] string msg, [CanBeNull] string title, MessageBoxButton button, MessageBoxResult defaultResult = MessageBoxResult.None)
            => MessageBox.Show(msg, title ?? "Question", button, MessageBoxImage.Question, defaultResult);

        #endregion

        #region WARNING

        public static MessageBoxResult WarningYesNoCancel([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Warning(msg, title, MessageBoxButton.YesNoCancel, defaultResult);

        public static MessageBoxResult WarningYesNo([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Warning(msg, title, MessageBoxButton.YesNo, defaultResult);

        public static MessageBoxResult WarningOkCancel([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Warning(msg, title, MessageBoxButton.OKCancel, defaultResult);

        public static MessageBoxResult WarningOk([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Warning(msg, title, MessageBoxButton.OK, defaultResult);

        public static MessageBoxResult Warning([NotNull] string msg, [CanBeNull] string title, MessageBoxButton button, MessageBoxResult defaultResult = MessageBoxResult.None)
            => MessageBox.Show(msg, title ?? "Warning", button, MessageBoxImage.Warning, defaultResult);

        #endregion

        #region ERROR

        public static MessageBoxResult ErrorYesNoCancel([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Error(msg, title, MessageBoxButton.YesNoCancel, defaultResult);

        public static MessageBoxResult ErrorYesNo([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Error(msg, title, MessageBoxButton.YesNo, defaultResult);

        public static MessageBoxResult ErrorOkCancel([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Error(msg, title, MessageBoxButton.OKCancel, defaultResult);

        public static MessageBoxResult ErrorOk([NotNull] string msg, [CanBeNull] string title = null, MessageBoxResult defaultResult = MessageBoxResult.None)
            => Error(msg, title, MessageBoxButton.OK, defaultResult);

        public static MessageBoxResult Error([NotNull] string msg, [CanBeNull] string title, MessageBoxButton button, MessageBoxResult defaultResult = MessageBoxResult.None)
            => MessageBox.Show(msg, title ?? "Error", button, MessageBoxImage.Error, defaultResult);

        #endregion

        public static bool IsNone(this MessageBoxResult result) => result == MessageBoxResult.None;

        public static bool IsYes(this MessageBoxResult result) => result == MessageBoxResult.Yes;

        public static bool IsNo(this MessageBoxResult result) => result == MessageBoxResult.No;

        public static bool IsOk(this MessageBoxResult result) => result == MessageBoxResult.OK;

        public static bool IsCancel(this MessageBoxResult result) => result == MessageBoxResult.Cancel;

    }
}
