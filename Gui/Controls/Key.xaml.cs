using System;
using System.Windows;
using System.Windows.Controls;

namespace Keyrita.Gui.Controls
{ 
    /// <summary>
    /// Wraps a character into a non-nullable.
    /// </summary>
    public class KeyCharacterWrapper
    {
        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="character"></param>
        public KeyCharacterWrapper(char character)
        {
            Character = character;
        }

        public char Character { get; private set ; }
    }

    /// <summary>
    /// Interaction logic for Key.xaml
    /// </summary>
    public partial class Key : UserControl
    {
        public Key()
        {
            InitializeComponent();
        }

        private static readonly DependencyProperty KeyCharacterProperty =
            DependencyProperty.Register(nameof(KeyCharacter),
                                        typeof(KeyCharacterWrapper),
                                        typeof(Key),
                                        new PropertyMetadata(OnCharacterChanged));

        private static void OnCharacterChanged(DependencyObject source,
                                               DependencyPropertyChangedEventArgs e)
        {
            var control = source as Key;
            control.UpdateSetting(e.NewValue as KeyCharacterWrapper);
        }

        private void UpdateSetting(KeyCharacterWrapper newValue)
        {
            // Unregister setting change listeners.
            mKeyCharacter = newValue;

            if(mKeyCharacter != null)
            {
                mChar.Text = "" + mKeyCharacter.Character;
            }
        }

        public KeyCharacterWrapper KeyCharacter
        {
            get
            {
                return mKeyCharacter;
            }
            set
            {
                SetValue(KeyCharacterProperty, value);
            }
        }

        protected KeyCharacterWrapper mKeyCharacter;
    }
}
