using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        protected void QueryResetKeyPosition(object sender, QueryContinueDragEventArgs e)
        {
            if (e.KeyStates == DragDropKeyStates.LeftMouseButton)
            {
                e.Action = DragAction.Continue;
            }
            else
            {
                // Reset key position
                ResetKeyPosition();
                e.Action = DragAction.Cancel;
            }
        }

        protected void ResetKeyPosition()
        {
            Panel.SetZIndex(this, 0);
            this.IsHitTestVisible = true;

            // Reset key to original position.
            Canvas.SetLeft(this, this.StartPosition.X); 
            Canvas.SetTop(this, this.StartPosition.Y);
        }

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
                var swapKeyPos = SettingState.KeyboardSettings.KeyboardState.GetKeyForCharacter(draggedOverKey.KeyCharacter.Character);

                if(!SettingState.KeyboardSettings.LockedKeys.IsKeyLocked(swapKeyPos.Item1, swapKeyPos.Item2))
                {
                    SettingState.KeyboardSettings.KeyboardState.SwapKeys(key.KeyCharacter.Character, draggedOverKey.KeyCharacter.Character);
                }
            }
        }

        protected void LockUnlockKey()
        {
            if(this.KeyCharacter != null)
            {
                var character = SettingState.KeyboardSettings.KeyboardState.GetKeyForCharacter(this.KeyCharacter.Character);
                SettingState.KeyboardSettings.LockedKeys.ToggleKeyLock(character.Item1, character.Item2);
            }
        }

        #endregion

        public Point StartPosition;

        public Key()
        {
            InitializeComponent();
            StartPosition = new Point(0, 0);

            mSelectedKey = SettingState.KeyboardSettings.SelectedKey;
            mLockedKeys = SettingState.KeyboardSettings.LockedKeys;

            mSelectedKey.ValueChangedNotifications.AddGui(SyncWithSelectedKey);
            mLockedKeys.ValueChangedNotifications.AddGui(SyncWithLockedKeys);

            this.AllowDrop = true;
            this.Drop += DropKey;
            this.QueryContinueDrag += QueryResetKeyPosition;
            this.MouseLeftButtonDown += (sender, e) =>
            {
                if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
                {
                    LockUnlockKey();
                }
            };
            this.mLockIcon.Visibility = Visibility.Collapsed;

            SyncWithLockedKeys(null);
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

            if (mKeyCharacter != null)
            {
                mChar.Text = "_" + mKeyCharacter.Character;
            }

            SyncWithSelectedKey(null);
            SyncWithHeatmap(null);
            SyncWithLockedKeys(null);
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

        #region Lock keys

        protected void SyncWithLockedKeys(object changedSetting)
        {
            if (this.KeyCharacter != null)
            {
                var characterPos = SettingState.KeyboardSettings.KeyboardState.GetKeyForCharacter(this.KeyCharacter.Character);
                if(SettingState.KeyboardSettings.LockedKeys.IsKeyLocked(characterPos.Item1, characterPos.Item2))
                {
                    SetLocked();
                }
                else
                {
                    SetUnlocked();
                }
            }
            else
            {
                SetUnlocked();
            }
        }

        public void SetLocked()
        {
            this.mLockIcon.Visibility = Visibility.Visible;
        }

        public void SetUnlocked()
        {
            this.mLockIcon.Visibility = Visibility.Collapsed;
        }

        protected LockedKeysSetting mLockedKeys;

        #endregion

        #region Heatmap properties

        protected void SyncWithHeatmap(object changedSetting)
        {
            if (KeyCharacter != null && mHeatmap != null)
            {
                if (mHeatmap.HeatMapData.ContainsKey(KeyCharacter.Character))
                {
                    double heatmapValue = mHeatmap.HeatMapData[KeyCharacter.Character];

                    Color keyHighlightColor = GetGradientColor((float)heatmapValue,
                        FingerToColor[KeyCharacter.Finger].Color);
                    mBorder.Background = new SolidColorBrush(keyHighlightColor);
                }
                else
                {
                    mBorder.Background = new SolidColorBrush(LowestFreq);
                }
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

            if (mHeatmap != null)
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
            mLockedKeys.ValueChangedNotifications.Remove(SyncWithLockedKeys);
            KeyHeatMap = null;
        }
    }
}
