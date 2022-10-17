using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for KeyboardControl.xaml
    /// </summary>
    public partial class UnusedCharacters : UserControlBase
    {
        private int keySize = 60;
        const int MAX_COLS = 10;

        public UnusedCharacters()
        {
            InitializeComponent();
        }

        #region Drag and Drop

        protected void MoveKey(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                StartDragDropData data = new StartDragDropData((Key)sender,
                                                                e.GetPosition((UIElement)sender),
                                                                e.GetPosition(mKeyGrid));

                ((UIElement)sender).IsHitTestVisible = false;
                DragDrop.DoDragDrop((DependencyObject) sender, 
                    new DataObject(DataFormats.Serializable, data), 
                    DragDropEffects.Move);
            }
        }

        #endregion

        protected void SyncWithKeyboard(SettingBase settingChanged)
        {
            // Get the key size.
            SyncWithAvailableChars();
        }

        protected void SyncWithAvailableChars()
        {
            if(mAvailableChars == null || mKeyboardState == null)
            {
                return;
            }

            // Go through the set of available chars and if the keyboard doesn't include it in the layout, add it to this area.
            var usedKeys = new HashSet<char>();

            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    usedKeys.Add(SettingState.KeyboardSettings.KeyboardState.GetValueAt(i, j));
                }
            }

            List<char> unusedKeys = new List<char>();

            // Now create a sorted list of all unused characters.
            foreach(char c in mAvailableChars.Collection)
            {
                if (!usedKeys.Contains(c) && c != ' ')
                {
                    unusedKeys.Add(c);
                }
            }

            int currentIdx = 0;
            mKeyGrid.Children.Clear();
            foreach(char c in unusedKeys)
            {
                Key k = new Key();
                k.Width = keySize;
                k.Height = keySize;
                k.KeyCharacter = new KeyCharacterWrapper(c, eFinger.None);
                k.MouseMove += MoveKey;
                k.KeyHeatMap = SettingState.KeyboardSettings.HeatmapData;

                Grid.SetRow(k, currentIdx / MAX_COLS);
                Grid.SetColumn(k, currentIdx % MAX_COLS);
                mKeyGrid.Children.Add(k);
                currentIdx += 1;
            }
        }

        private static readonly DependencyProperty KeyboardProperty =
            DependencyProperty.Register(nameof(KeyboardState),
                                        typeof(KeyboardStateSetting),
                                        typeof(UnusedCharacters),
                                        new PropertyMetadata(OnKbSettingChanged));

        private static void OnKbSettingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as UnusedCharacters;
            control.UpdateKeyboard(e.NewValue as KeyboardStateSetting);
        }

        private void UpdateKeyboard(KeyboardStateSetting newValue)
        {
            // Unregister setting change listeners.
            if (mKeyboardState != null)
            {
                mKeyboardState.ValueChangedNotifications.Remove(SyncWithKeyboard);
                mKeyboardState.LimitsChangedNotifications.Remove(SyncWithKeyboard);
            }

            mKeyboardState = newValue;

            if(mKeyboardState != null)
            {
                mKeyboardState.ValueChangedNotifications.AddGui(SyncWithKeyboard);
                mKeyboardState.LimitsChangedNotifications.AddGui(SyncWithKeyboard);
                SyncWithAvailableChars();
            }
        }

        public KeyboardStateSetting KeyboardState
        {
            get
            {
                return mKeyboardState;
            }
            set
            {
                SetValue(KeyboardProperty, value);
            }
        }

        protected KeyboardStateSetting mKeyboardState;

        #region Available Chars Setting

        private static readonly DependencyProperty AvailableCharsProperty =
            DependencyProperty.Register(nameof(AvailableChars),
                                        typeof(ElementSetSetting<char>),
                                        typeof(UnusedCharacters),
                                        new PropertyMetadata(OnAvailableCharsChanged));

        private static void OnAvailableCharsChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as UnusedCharacters;
            control.UpdateAvailableChars(e.NewValue as ElementSetSetting<char>);
        }

        private void UpdateAvailableChars(ElementSetSetting<char> newValue)
        {
            // Unregister setting change listeners.
            if (mAvailableChars != null)
            {
                mAvailableChars.ValueChangedNotifications.Remove(SyncWithKeyboard);
                mAvailableChars.LimitsChangedNotifications.Remove(SyncWithKeyboard);
            }

            mAvailableChars = newValue;

            if(mAvailableChars != null)
            {
                mAvailableChars.ValueChangedNotifications.AddGui(SyncWithKeyboard);
                mAvailableChars.LimitsChangedNotifications.AddGui(SyncWithKeyboard);
                SyncWithAvailableChars();
            }
        }

        public ElementSetSetting<char> AvailableChars
        {
            get
            {
                return mAvailableChars;
            }
            set
            {
                SetValue(AvailableCharsProperty, value);
            }
        }

        protected ElementSetSetting<char> mAvailableChars;

        #endregion

        protected override void OnClose()
        {
            AvailableChars = null;
            KeyboardState = null;
        }
    }
}
