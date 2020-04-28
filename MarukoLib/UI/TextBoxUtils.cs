using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JetBrains.Annotations;
using MarukoLib.Lang;

namespace MarukoLib.UI
{

    public static class TextBoxUtils
    {

        public static void SetupPressEnterToMoveFocusForAllTextBox()
            => EventManager.RegisterClassHandler(typeof(TextBox), UIElement.KeyDownEvent, new KeyEventHandler(TextBox_OnKeyDown));

        public static IDisposable SetupPressEnterToMoveFocus([NotNull] this TextBox textBox)
        {
            textBox.KeyDown += TextBox_OnKeyDown;
            return Disposables.For(() => textBox.KeyDown -= TextBox_OnKeyDown);
        }

        public static void SetupInputValidation([NotNull] this TextBox textBox, [NotNull] IEnumerable<char> chars)
        {
            var set = new HashSet<char>(chars);
            textBox.PreviewTextInput += (sender, e) => e.Handled = !e.Text.All(set.Contains);
        }

        public static void SetupInputValidation([NotNull] this TextBox textBox, [NotNull] Regex regex) 
            => textBox.PreviewTextInput += (sender, e) => e.Handled = !regex.IsMatch(e.Text);

        private static void TextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return; // Check pressed key
            if (!(sender is TextBox textBox)) return; // Check sender type
            if (textBox.AcceptsReturn) return; // Check text box properties
            if (sender != Keyboard.FocusedElement) return; // Check keyboard focus
            if (textBox.MoveFocus(FocusNavigationDirection.Next)) e.Handled = true; // Handle event
        }

    }

}
