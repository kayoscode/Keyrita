using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;

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
        public KeyCharacterWrapper(char character, eFinger finger, double heatmapValue)
        {
            Character = character;
            Finger = finger;
            HeatmapValue = heatmapValue;
        }

        public char Character { get; private set; }
        public eFinger Finger { get; private set; }
        public double HeatmapValue { get; private set; }
    }

    /// <summary>
    /// Interaction logic for Key.xaml
    /// </summary>
    public partial class Key : UserControl
    {
        private Color LowestFreq = Color.FromRgb(42, 43, 52);

        private static readonly IReadOnlyDictionary<eFinger, SolidColorBrush> FingerToColor = new Dictionary<eFinger, SolidColorBrush>()
        {
            { eFinger.None,         Brushes.OrangeRed },
            { eFinger.LeftPinkie,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#978de2")) },
            { eFinger.LeftRing,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33b5e5")) },
            { eFinger.LeftMiddle,     new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00c851")) },
            { eFinger.LeftIndex,    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff4444")) },

            { eFinger.LeftThumb,    Brushes.Magenta },
            { eFinger.RightThumb,   Brushes.Maroon },

            { eFinger.RightIndex,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffbb33")) },
            { eFinger.RightMiddle,     new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00c851")) },
            { eFinger.RightRing,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33b5e5")) },
            { eFinger.RightPinkie,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#978de2")) },
        };


        public Key()
        {
            InitializeComponent();

            mSelectedKey = SettingState.KeyboardSettings.SelectedKey;
            mSelectedKey.ValueChangedNotifications.AddGui(SyncWithSelectedKey);
            SyncWithSelectedKey(null);
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
            mBorder.BorderBrush = FingerToColor[newValue.Finger];

            Color keyHighlightColor = GetGradientColor((float)mKeyCharacter.HeatmapValue, 
                FingerToColor[newValue.Finger].Color);
            mBorder.Background = new SolidColorBrush(keyHighlightColor);

            if(mKeyCharacter != null)
            {
                mChar.Text = "_" + mKeyCharacter.Character;
            }

            SyncWithSelectedKey(null);
        }

        protected Color GetGradientColor(float heatmapValue, Color highestFreqColor)
        {
            Color startColor = LowestFreq;
            Color endColor = highestFreqColor;
            float weight = heatmapValue;

            Color keyHighlight = Color.FromRgb(
                (byte)Math.Round(startColor.R * (1 - weight) + endColor.R * weight),
                (byte)Math.Round(startColor.G * (1 - weight) + endColor.G * weight),
                (byte)Math.Round(startColor.B * (1 - weight) + endColor.B * weight));

            return keyHighlight;
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

        #region Key Selection

        protected void SyncWithSelectedKey(SettingBase changedSetting)
        {
            if (SettingState.KeyboardSettings.SelectedKey.HasValue)
            {
                if (mKeyCharacter != null && SettingState.KeyboardSettings.SelectedKey.Value == mKeyCharacter.Character)
                {
                    SetSelected();
                }
                else
                {
                    SetUnselected();
                }
            }
        }

        public void SetSelected()
        {
            mSelectionBorder.BorderBrush = Brushes.White;
        }

        public void SetUnselected()
        {
            mSelectionBorder.BorderBrush = Brushes.Transparent;
        }

        protected SelectedKeySetting mSelectedKey;

        #endregion
    }
}
