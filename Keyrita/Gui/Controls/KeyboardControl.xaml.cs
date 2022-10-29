using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Gui.Controls
{
    public class StartDragDropData
    {
        public StartDragDropData(Key clickedKey, Point clickOffset, Point startPosition)
        {
            ClickedKey = clickedKey;
            ClickedOffset = clickOffset;
            StartPosition = startPosition;
        }

        public Key ClickedKey;
        public Point ClickedOffset;
        public Point StartPosition;
    }

    /// <summary>
    /// Interaction logic for KeyboardControl.xaml
    /// </summary>
    public partial class KeyboardControl : UserControlBase
    {
        private int keySize = 80;

        public KeyboardControl()
        {
            InitializeComponent();

            // Create the key objects.
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var nextKey = new Key();
                    nextKey.Width = keySize;
                    nextKey.Height = keySize;
                    nextKey.MouseDown += MoveKey;
                    nextKey.MouseDoubleClick += SelectKey;
                    nextKey.KeyHeatMap = SettingState.KeyboardSettings.HeatmapData;
                    AccessKeyManager.AddAccessKeyPressedHandler(nextKey, SelectKey);

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

        #region Key selection

        protected void SelectKey(object sender, EventArgs args)
        {
            Key key = sender as Key;

            if(key != null)
            {
                SettingState.KeyboardSettings.SelectedKey.SetSelection(key.KeyCharacter.Character);
            }
            else
            {
                LogUtils.Assert(false, "Select key event triggered without a key.");
            }
        }

        #endregion

        #region Drag and Drop

        protected void MoveKey(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                StartDragDropData data = new StartDragDropData((Key)sender,
                                                                e.GetPosition((UIElement)sender),
                                                                e.GetPosition(mKeyCanvas));
                Panel.SetZIndex((UIElement)sender, 10000);
                ((UIElement)sender).IsHitTestVisible = false;
                DragDrop.DoDragDrop((DependencyObject) sender, 
                    new DataObject(DataFormats.Serializable, data), 
                    DragDropEffects.Move);
            }
        }

        protected void RepositionKey(object sender, DragEventArgs e)
        {
            var position = e.GetPosition((Canvas)sender);

            StartDragDropData data = (StartDragDropData)e.Data.GetData(DataFormats.Serializable);
            Point offset = data.ClickedOffset;
            Key key = data.ClickedKey;

            Canvas.SetLeft(key, position.X - offset.X);
            Canvas.SetTop(key, position.Y - offset.Y);
        }

        /// <summary>
        /// Just put the key back in the original position, it wasn't dropped over another key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DropKey(object sender, DragEventArgs e)
        {
            StartDragDropData data = (StartDragDropData)e.Data.GetData(DataFormats.Serializable);
            Key key = data.ClickedKey;
            Panel.SetZIndex(key, 0);
            key.IsHitTestVisible = true;

            // Reset key to original position.
            Point offset = data.ClickedOffset;
            Canvas.SetLeft(key, data.StartPosition.X - offset.X);
            Canvas.SetTop(key, data.StartPosition.Y - offset.Y);
        }

        #endregion

        #region Keyboard Sync

        protected void CanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We want to fill the canvas and we know that there are 10.75 keys across.
            // Make sure we don't take too much space vertically as well.
            keySize = (int)(mKeyCanvas.ActualWidth / 10.75);
            double fontSize = (((mKeyCanvas.ActualWidth / 12) / 3 * 2) / 5) * 5.5;

            foreach (var k in mKeyCanvas.Children)
            {
                var key = k as Key;
                if(key != null)
                {
                    key.Width = keySize;
                    key.Height = keySize;
                    key.mChar.FontSize = fontSize;
                }
            }

            SyncWithKeyboard();
        }

        protected void SyncWithKeyboard(object settingChanged)
        {
            // Get the key size.
            SyncWithKeyboard();
        }

        protected void SyncWithKeyboard()
        {
            // Go through each of the children and update with the correct key from the kb setting.
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var k = (Key)mKeyCanvas.Children[i * KeyboardStateSetting.COLS + j];
                    var character = '*';
                    var finger = eFinger.None;

                    if (mKeyboardState != null)
                    {
                        character = mKeyboardState.GetValueAt(i, j);
                    }

                    if (mKeyMappings != null && ShowFingerUsage != null && ShowFingerUsage.Value.Equals(eOnOff.On))
                    {
                        finger = mKeyMappings.GetValueAt(i, j);
                    }

                    k.KeyCharacter = new KeyCharacterWrapper(character, finger);
                }
            }

            SyncWithShape();
        }

        protected void SyncWithEditMode(object sender)
        {
            SyncWithScissorMap();
        }

        protected void SyncWithScissorMap()
        {
            int lineStartIndex = 0;
            foreach(UIElement l in mKeyCanvas.Children)
            {
                if(l as Line != null)
                {
                    break;
                }

                lineStartIndex++;
            }

            mKeyCanvas.Children.RemoveRange(lineStartIndex, mKeyCanvas.Children.Count - lineStartIndex);

            // Start by clearing it. And if we are in scissor edit mode, add the components back as expected.
            if (mEditMode != null && mEditMode.Value.Equals(eKeyboardEditMode.ScissorMap) && mScissorMap != null)
            {
                for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
                {
                    for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                    {
                        var indices = mScissorMap.GetScissorsAt(i, j);

                        for(int k = 0; k < indices.Count; k++)
                        {
                            int startChild = i * KeyboardStateSetting.COLS + j;
                            int endChild = indices[k].Item1 * KeyboardStateSetting.COLS + indices[k].Item2;

                            CreateArrowBetween((Key)mKeyCanvas.Children[startChild], (Key)mKeyCanvas.Children[endChild], mKeyCanvas);
                        }
                    }
                }
            }
        }

        // The arrow will point from start to end
        private void CreateArrowBetween(Key startElement, Key endElemend, Panel parentContainer)
        {
            // Center the line horizontally and vertically.
            // Get the positions of the controls that should be connected by a line.
            Point centeredArrowStartPosition = startElement.TransformToAncestor(parentContainer)
              .Transform(new Point(startElement.ActualWidth / 2, startElement.ActualHeight / 2));

            Point centeredArrowEndPosition = endElemend.TransformToAncestor(parentContainer)
              .Transform(new Point(endElemend.ActualWidth / 2, endElemend.ActualHeight / 2));

            // Draw the line between two controls
            var arrowLine = new Line()
            {
                Stroke = Brushes.Red,
                StrokeThickness = 3,
                X1 = centeredArrowStartPosition.X,
                Y2 = centeredArrowEndPosition.Y,
                X2 = centeredArrowEndPosition.X,
                Y1 = centeredArrowStartPosition.Y
            };

            parentContainer.Children.Add(
              arrowLine);
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

            if (mKeyboardState != null)
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

        protected void SyncWithShape(object settingChanged)
        {
            SyncWithShape();
        }

        protected void SyncWithShape()
        {
            // Add offsets to each key depending on their layer.
            int keyIndex = 0;
            var minOffset = 1.0;
            var maxOffset = -1.0;

            foreach (var offset in RowOffsets)
            {
                if ((double)offset.Value < minOffset)
                {
                    minOffset = (double)offset.Value;
                }
                else if ((double)offset.Value > maxOffset)
                {
                    maxOffset = (double)offset.Value;
                }
            }

            mKeyCanvas.Height = (keySize) * KeyboardStateSetting.ROWS;

            var offsetDifference = maxOffset - minOffset;

            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var nextKey = mKeyCanvas.Children[keyIndex++];

                    Canvas.SetTop(nextKey, i * (keySize));
                    Canvas.SetLeft(nextKey, j * (keySize) + (double)RowOffsets[i].Value * (double)keySize - (minOffset * keySize));
                }
            }
        }

        /// <summary>
        /// Stores the offsets per row.
        /// </summary>
        protected ConcreteValueSetting<double>[] RowOffsets { get; } =
            new ConcreteValueSetting<double>[KeyboardStateSetting.ROWS];

        #endregion

        #region Sync With Keys Per Finger

        private static readonly DependencyProperty KeyMappingsProperty =
            DependencyProperty.Register(nameof(KeyMappings),
                                        typeof(KeyMappingSetting),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnKeyMappingChanged));

        private static void OnKeyMappingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
            control.UpdateKeyMappings(e.NewValue as KeyMappingSetting);
        }

        private void UpdateKeyMappings(KeyMappingSetting newValue)
        {
            // Unregister setting change listeners.
            if (mKeyMappings != null)
            {
                mKeyMappings.ValueChangedNotifications.Remove(SyncWithKeyboard);
                mKeyMappings.LimitsChangedNotifications.Remove(SyncWithKeyboard);
            }

            mKeyMappings = newValue;

            if (mKeyMappings != null)
            {
                mKeyMappings.ValueChangedNotifications.AddGui(SyncWithKeyboard);
                mKeyMappings.LimitsChangedNotifications.AddGui(SyncWithKeyboard);
                SyncWithKeyboard();
            }
        }

        public KeyMappingSetting KeyMappings
        {
            get
            {
                return mKeyMappings;
            }
            set
            {
                SetValue(KeyMappingsProperty, value);
            }
        }

        protected KeyMappingSetting mKeyMappings;

        #endregion

        #region Show Finger Usage

        private static readonly DependencyProperty ShowFingerUsageProperty =
            DependencyProperty.Register(nameof(ShowFingerUsage),
                                        typeof(OnOffSetting),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnShowFingerUsageChanged));

        private static void OnShowFingerUsageChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
            control.UpdateFingerUsage(e.NewValue as OnOffSetting);
        }

        private void UpdateFingerUsage(OnOffSetting newValue)
        {
            // Unregister setting change listeners.
            if (mShowFingerUsage != null)
            {
                mShowFingerUsage.ValueChangedNotifications.Remove(SyncWithKeyboard);
                mShowFingerUsage.LimitsChangedNotifications.Remove(SyncWithKeyboard);
            }

            mShowFingerUsage = newValue;

            if (mShowFingerUsage != null)
            {
                mShowFingerUsage.ValueChangedNotifications.AddGui(SyncWithKeyboard);
                mShowFingerUsage.LimitsChangedNotifications.AddGui(SyncWithKeyboard);
                SyncWithKeyboard();
            }
        }

        public OnOffSetting ShowFingerUsage
        {
            get
            {
                return mShowFingerUsage;
            }
            set
            {
                SetValue(ShowFingerUsageProperty, value);
            }
        }

        protected OnOffSetting mShowFingerUsage;

        #endregion

        #region Edit Mode

        private static readonly DependencyProperty EditModeProperty =
            DependencyProperty.Register(nameof(EditMode),
                                        typeof(EnumValueSetting<eKeyboardEditMode>),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnKeyboardEditModeChanged));

        private static void OnKeyboardEditModeChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
            control.UpdateEditMode(e.NewValue as EnumValueSetting<eKeyboardEditMode>);
        }

        private void UpdateEditMode(EnumValueSetting<eKeyboardEditMode> newValue)
        {
            // Unregister setting change listeners.
            if (mEditMode != null)
            {
                mEditMode.ValueChangedNotifications.Remove(SyncWithEditMode);
                mEditMode.LimitsChangedNotifications.Remove(SyncWithEditMode);
            }

            mEditMode = newValue;

            if (mEditMode != null)
            {
                mEditMode.ValueChangedNotifications.AddGui(SyncWithEditMode);
                mEditMode.LimitsChangedNotifications.AddGui(SyncWithEditMode);
                SyncWithKeyboard();
            }
        }

        public EnumValueSetting<eKeyboardEditMode> EditMode
        {
            get
            {
                return mEditMode;
            }
            set
            {
                SetValue(EditModeProperty, value);
            }
        }

        protected EnumValueSetting<eKeyboardEditMode> mEditMode;

        #endregion

        #region Scissor map

        private static readonly DependencyProperty ScissorMapProperty =
            DependencyProperty.Register(nameof(ScissorMap),
                                        typeof(ScissorMapSetting),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnScissorMapSettingChanged));

        private static void OnScissorMapSettingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
            control.UpdateScissorMap(e.NewValue as ScissorMapSetting);
        }

        private void UpdateScissorMap(ScissorMapSetting newValue)
        {
            // Unregister setting change listeners.
            if (mScissorMap != null)
            {
                mScissorMap.ValueChangedNotifications.Remove(SyncWithEditMode);
                mScissorMap.LimitsChangedNotifications.Remove(SyncWithEditMode);
            }

            mScissorMap = newValue;

            if (mScissorMap != null)
            {
                mScissorMap.ValueChangedNotifications.AddGui(SyncWithEditMode);
                mScissorMap.LimitsChangedNotifications.AddGui(SyncWithEditMode);
                SyncWithKeyboard();
            }
        }

        public ScissorMapSetting ScissorMap
        {
            get
            {
                return mScissorMap;
            }
            set
            {
                SetValue(ScissorMapProperty, value);
            }
        }

        protected ScissorMapSetting mScissorMap;

        #endregion

        protected override void OnClose()
        {
            // Remove row listeners.
            for (int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                RowOffsets[i] = SettingState.MeasurementSettings.RowOffsets[i];
                RowOffsets[i].LimitsChangedNotifications.Remove(SyncWithShape);
                RowOffsets[i].LimitsChangedNotifications.Remove(SyncWithShape);
            }

            KeyboardState = null;
            KeyMappings = null;
            ShowFingerUsage = null;
            EditMode = null;
            ScissorMap = null;
        }
    }
}
