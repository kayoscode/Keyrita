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
        public KeyCharacterWrapper(char character, eFinger finger)
        {
            Character = character;
            Finger = finger;
        }

        public char Character { get; private set; }
        public eFinger Finger { get; private set; }
    }

    /// <summary>
    /// Interaction logic for Key.xaml
    /// </summary>
    public partial class Key : UserControlBase
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

        #region

        protected void DropKey(object sender, DragEventArgs e)
        {
            // Swap the keys.
            StartDragDropData data = (StartDragDropData)e.Data.GetData(DataFormats.Serializable);
            Key key = data.ClickedKey;
            Key draggedOverKey = e.Source as Key;
            Panel.SetZIndex(key, 0);
            key.IsHitTestVisible = true;

            if (draggedOverKey != null && draggedOverKey != key)
            {
                SettingState.KeyboardSettings.KeyboardState.SwapKeys(key.KeyCharacter.Character, draggedOverKey.KeyCharacter.Character);
            }
        }

        #endregion

        public Key()
        {
            InitializeComponent();

            mSelectedKey = SettingState.KeyboardSettings.SelectedKey;
            mSelectedKey.ValueChangedNotifications.AddGui(SyncWithSelectedKey);

            this.AllowDrop = true;
            this.Drop += DropKey;

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

            if(mKeyCharacter != null)
            {
                mChar.Text = "_" + mKeyCharacter.Character;
            }

            SyncWithSelectedKey(null);
            SyncWithHeatmap(null);
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

        protected void SyncWithSelectedKey(object changedSetting)
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

        #region Heatmap properties

        protected void SyncWithHeatmap(object changedSetting)
        {
            double heatmapValue = 0;  

            if(KeyCharacter != null && mHeatmap != null && mHeatmap.HeatMapData.ContainsKey(KeyCharacter.Character))
            {
                heatmapValue = mHeatmap.HeatMapData[KeyCharacter.Character];

                Color keyHighlightColor = GetGradientColor((float)heatmapValue, 
                    FingerToColor[KeyCharacter.Finger].Color);
                mBorder.Background = new SolidColorBrush(keyHighlightColor);
            }
        }

        private static readonly DependencyProperty HeatmapDataProperty =
            DependencyProperty.Register(nameof(KeyHeatMap),
                                        typeof(HeatmapDataSetting),
                                        typeof(Key),
                                        new PropertyMetadata(OnHeatmapDataChanged));

        private static void OnHeatmapDataChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as Key;
            control.UpdateCharFrequency(e.NewValue as HeatmapDataSetting);
        }

        private void UpdateCharFrequency(HeatmapDataSetting newValue)
        {
            // Unregister setting change listeners.
            if (mHeatmap != null)
            {
                mHeatmap.ValueChangedNotifications.Remove(SyncWithHeatmap);
                mHeatmap.LimitsChangedNotifications.Remove(SyncWithHeatmap);
            }

            mHeatmap = newValue;

            if(mHeatmap != null)
            {
                mHeatmap.ValueChangedNotifications.AddGui(SyncWithHeatmap);
                mHeatmap.LimitsChangedNotifications.AddGui(SyncWithHeatmap);
                SyncWithHeatmap(null);
            }
        }

        public HeatmapDataSetting KeyHeatMap
        {
            get
            {
                return mHeatmap;
            }
            set
            {
                SetValue(HeatmapDataProperty, value);
            }
        }

        protected HeatmapDataSetting mHeatmap;

        #endregion

        protected override void OnClose()
        {
            // Remove all event handlers.
            mSelectedKey.ValueChangedNotifications.Remove(SyncWithSelectedKey);
            KeyHeatMap = null;
        }
    }
}
