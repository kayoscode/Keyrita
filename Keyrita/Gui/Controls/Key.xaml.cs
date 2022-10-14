using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Keyrita.Settings;

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
        private Color LowestFreq = Color.FromRgb(125, 125, 125);
        private Color HighestFreq = (Color)ColorConverter.ConvertFromString("#4c3549");

        private static readonly IReadOnlyDictionary<eFinger, SolidColorBrush> FingerToColor = new Dictionary<eFinger, SolidColorBrush>()
        {
            { eFinger.None,         Brushes.Gray },
            { eFinger.LeftPinkie,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffbb33")) },
            { eFinger.LeftRing,     new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00c851")) },
            { eFinger.LeftMiddle,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33b5e5")) },
            { eFinger.LeftIndex,    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#978de2")) },

            { eFinger.LeftThumb,    Brushes.Magenta },
            { eFinger.RightThumb,   Brushes.Maroon },

            { eFinger.RightIndex,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d38b5d")) },
            { eFinger.RightMiddle,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33b5e5")) },
            { eFinger.RightRing,     new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00c851")) },
            { eFinger.RightPinkie,   new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffbb33")) },
        };


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
            mBorder.BorderBrush = FingerToColor[newValue.Finger];

            Color keyHighlightColor = GetGradientColor((float)mKeyCharacter.HeatmapValue);
            mBorder.Background = new SolidColorBrush(keyHighlightColor);

            if(mKeyCharacter != null)
            {
                mChar.Text = "" + mKeyCharacter.Character;
            }
        }

        protected Color GetGradientColor(float heatmapValue)
        {
            Color startColor = LowestFreq;
            Color endColor = HighestFreq;
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
    }
}
