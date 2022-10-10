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
        private Vector3 LowFreq = new Vector3(255, 255, 255);
        private Vector3 HighFreq = new Vector3(255, 0, 0);

        private static readonly IReadOnlyDictionary<eFinger, SolidColorBrush> FingerToColor = new Dictionary<eFinger, SolidColorBrush>()
        {
            { eFinger.None,         Brushes.Gray },
            { eFinger.LeftPinkie,   new SolidColorBrush(Color.FromRgb(181, 192, 0)) },
            { eFinger.LeftRing,     new SolidColorBrush(Color.FromRgb(117, 4, 108)) },
            { eFinger.LeftMiddle,   new SolidColorBrush(Color.FromRgb(0, 192, 43)) },
            { eFinger.LeftIndex,    new SolidColorBrush(Color.FromRgb(232, 37, 14)) },

            { eFinger.LeftThumb,    Brushes.Magenta },
            { eFinger.RightThumb,   Brushes.Maroon },

            { eFinger.RightIndex,   new SolidColorBrush(Color.FromRgb(0, 192, 191)) },
            { eFinger.RightMiddle,   new SolidColorBrush(Color.FromRgb(0, 192, 43)) },
            { eFinger.RightRing,     new SolidColorBrush(Color.FromRgb(117, 4, 108)) },
            { eFinger.RightPinkie,   new SolidColorBrush(Color.FromRgb(181, 192, 0)) },
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

            Vector3 color = Vector3.Lerp(LowFreq, HighFreq, (float)newValue.HeatmapValue);
            mBorder.Background = new SolidColorBrush(Color.FromArgb((byte)(150 + (105 * newValue.HeatmapValue)), (byte)color.X, (byte)color.Y, (byte)color.Z));

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
