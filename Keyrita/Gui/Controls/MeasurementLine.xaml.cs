using System;
using System.Windows;
using System.Windows.Controls;
using Keyrita.Measurements;
using Keyrita.Operations.OperationUtil;
using Keyrita.Util;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for MeasurementLine.xaml
    /// </summary>
    public partial class MeasurementLine : UserControlBase
    {
        public MeasurementLine()
        {
            InitializeComponent();
        }

        public Grid ParentGrid
        {
            get
            {
                return mParentGrid;
            }
            set
            {
                mMeasurementGrid.ColumnDefinitions.Clear();

                foreach(var colDef in value.ColumnDefinitions)
                {
                    var newCol = new ColumnDefinition();
                    newCol.Width = new GridLength(colDef.Width.Value, colDef.Width.GridUnitType);
                    mMeasurementGrid.ColumnDefinitions.Add(newCol);
                }

                this.mParentGrid = value;
            }
        }
        protected Grid mParentGrid;

        #region Sync with linked operation

        protected void SyncWithMeasurement(object changedSetting)
        {
            mMeasurementGrid.Children.Clear();

            TextBlock measName = new TextBlock();
            measName.VerticalAlignment = VerticalAlignment.Center;
            measName.Text = mMeasurement.UIText();
            measName.FontSize = 15;
            measName.Padding = new Thickness(5, 0, 0, 0);
            Grid.SetColumn(measName, 0);
            mMeasurementGrid.Children.Add(measName);

            for(uint i = 0; i < mMeasurementOp.NumUICols; i++)
            {
                TextBlock colResult = new TextBlock();
                colResult.VerticalAlignment = VerticalAlignment.Center;
                colResult.Text = String.Format("{0:0.##}", mMeasurementOp.UIRowValue(i));
                colResult.FontSize = 15;
                colResult.Foreground = mMeasurementOp.UIRowColor(i);
                colResult.Padding = new Thickness(5, 0, 0, 0);

                mMeasurementGrid.Children.Add(colResult);
                Grid.SetColumn(colResult, (int)(i + 1));
            }
        }

        private static readonly DependencyProperty MeasurementProperty =
            DependencyProperty.Register(nameof(Measurement),
                                        typeof(Enum),
                                        typeof(MeasurementLine),
                                        new PropertyMetadata(OnLinkedMeasurementChanged));

        private static void OnLinkedMeasurementChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as MeasurementLine;
            control.UpdateMeasurementOp(e.NewValue as Enum);
        }

        private void UpdateMeasurementOp(Enum newValue)
        {
            // Unregister setting change listeners.
            if (mMeasurementOp != null)
            {
                mMeasurementOp.ValueChangedNotifications.Remove(SyncWithMeasurement);
            }

            mMeasurement = newValue;
            mMeasurementOp = OperationSystem.GetInstalledOperation((Enum)mMeasurement) as MeasurementOp;

            if(mMeasurementOp != null)
            {
                this.Visibility = Visibility.Visible;
                this.ToolTip = mMeasurement.UIToolTip();
                this.mMeasurementOp.ValueChangedNotifications.AddGui(SyncWithMeasurement);
                SyncWithMeasurement(null);
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }
        }

        public Enum Measurement
        {
            get
            {
                return mMeasurement;
            }
            set
            {
                SetValue(MeasurementProperty, value);
            }
        }

        protected Enum mMeasurement;
        protected MeasurementOp mMeasurementOp;

        #endregion

        protected override void OnClose()
        {
            Measurement = null;
        }
    }
}
