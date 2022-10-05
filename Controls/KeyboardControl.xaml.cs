using Keyrita.Settings;
using Keyrita.Util;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace Keyrita
{
    /// <summary>
    /// Interaction logic for KeyboardControl.xaml
    /// </summary>
    public partial class KeyboardControl : UserControl
    {
        private const int KEY_SIZE = 80;

        public KeyboardControl()
        {
            InitializeComponent();

            // Create the key objects.
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var nextKey = new Key();
                    nextKey.Width = KEY_SIZE;
                    nextKey.Height = KEY_SIZE;

                    mKeyCanvas.Children.Add(nextKey);
                }
            }

            // Listen to changes in the keyboard offset.
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                RowOffsets[i] = SettingState.MeasurementSettings.RowOffsets[i];
                RowOffsets[i].LimitsChangedNotifications.AddGui(SyncWithShape);
                RowOffsets[i].LimitsChangedNotifications.AddGui(SyncWithShape);
            }
        }

        #region Keyboard Sync

        protected void SyncWithKeyboard(SettingBase settingChanged)
        {
            SyncWithKeyboard();
        }

        protected void SyncWithKeyboard()
        {
            // Go through each of the children and update with the correct key from the kb setting.
            int index = 0;
            foreach (var key in mKeyCanvas.Children)
            {
                int row = index / KeyboardStateSetting.COLS;
                int col = index % KeyboardStateSetting.COLS;
                var k = (Key)key;
                var character = mKeyboardState.GetCharacterAt(row, col);

                k.KeyCharacter = new KeyCharacterWrapper(character);
                index++;
            }

            SyncWithShape();
        }

        private static readonly DependencyProperty KeyboardProperty =
            DependencyProperty.Register(nameof(KeyboardState),
                                        typeof(KeyboardStateSetting),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnKbSettingChanged));

        private static void OnKbSettingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
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
                SyncWithKeyboard();
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

        #endregion

        #region Layout Sync

        protected void SyncWithShape(SettingBase settingChanged)
        {
            SyncWithShape();
        }

        protected void SyncWithShape()
        {
            // Add offsets to each key depending on their layer.
            int keyIndex = 0;
            var minOffset = 1.0;
            var maxOffset = -1.0;

            foreach(var offset in RowOffsets)
            {
                if((double)offset.Value < minOffset)
                {
                    minOffset = (double)offset.Value;
                }
                else if((double)offset.Value > maxOffset)
                {
                    maxOffset = (double)offset.Value;
                }
            }

            var offsetDifference = maxOffset - minOffset;

            mKeyCanvas.Width = (KEY_SIZE + 4) * KeyboardStateSetting.COLS + (offsetDifference * KEY_SIZE) + 4;
            mKeyCanvas.Height = (KEY_SIZE + 4) * KeyboardStateSetting.ROWS;

            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var nextKey = mKeyCanvas.Children[keyIndex++];

                    Canvas.SetTop(nextKey, i * (KEY_SIZE + 4));
                    Canvas.SetLeft(nextKey, j * (KEY_SIZE + 4) + (double)RowOffsets[i].Value * (double)KEY_SIZE - (minOffset * KEY_SIZE) + 4);
                }
            }
        }

        /// <summary>
        /// Stores the offsets per row.
        /// </summary>
        protected ConcreteValueSetting<double>[] RowOffsets { get; } = 
            new ConcreteValueSetting<double>[KeyboardStateSetting.ROWS];

        #endregion
    }
}
