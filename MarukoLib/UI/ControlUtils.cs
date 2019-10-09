using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MarukoLib.UI
{

    public static class ControlUtils
    {

        public static void SetupPressEnterToMoveFocusForAllTextbox() => EventManager.RegisterClassHandler(typeof(TextBox), UIElement.KeyDownEvent, new KeyEventHandler(TextBox_KeyDown));

        public static void SetupPressEnterToMoveFocusForTextbox(this TextBox textBox) => textBox.KeyDown += TextBox_KeyDown;

        public static void UnsetupPressEnterToMoveFocusForTextbox(this TextBox textBox) => textBox.KeyDown -= TextBox_KeyDown;

        public static bool MoveFocus(this UIElement focusedElement, FocusNavigationDirection direction) => focusedElement.MoveFocus(new TraversalRequest(direction));

        private static void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return; // Check pressed key
            if (!(sender is TextBox textBox)) return; // Check sender type
            if (textBox.AcceptsReturn) return; // Check textbox properties
            if (sender != Keyboard.FocusedElement) return; // Check keyboard focus
            if (MoveFocus(textBox, FocusNavigationDirection.Next)) e.Handled = true; // Handle event
        }

    }

}
