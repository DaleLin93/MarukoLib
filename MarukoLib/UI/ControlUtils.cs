using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarukoLib.Lang;

namespace MarukoLib.UI
{

    public static class ControlUtils
    {

        public static void SetupPressEnterToMoveFocusForAllTextBox() => EventManager.RegisterClassHandler(typeof(TextBox), UIElement.KeyDownEvent, new KeyEventHandler(TextBox_KeyDown));

        public static IDisposable SetupPressEnterToMoveFocusForTextBox(this TextBox textBox)
        {
            textBox.KeyDown += TextBox_KeyDown;
            return Disposables.For(() => textBox.KeyDown -= TextBox_KeyDown);
        }

        public static bool MoveFocus(this UIElement focusedElement, FocusNavigationDirection direction) => focusedElement.MoveFocus(new TraversalRequest(direction));

        private static void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return; // Check pressed key
            if (!(sender is TextBox textBox)) return; // Check sender type
            if (textBox.AcceptsReturn) return; // Check text box properties
            if (sender != Keyboard.FocusedElement) return; // Check keyboard focus
            if (MoveFocus(textBox, FocusNavigationDirection.Next)) e.Handled = true; // Handle event
        }

    }

}
