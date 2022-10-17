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

        #region Sync with linked operation

        protected void SyncWithMeasurement(object changedSetting)
        {
            mMeasurementNameTextBlock.Text = mMeasurement.UIText();

            mMeasurementGrid.Children.Clear();
            // Work out how the measurement wants to be output, and output it there.

            // Add all of the necessary columns and put vertical separators between.
            mMeasurementGrid.ColumnDefinitions.Clear();

            for(uint i = 0; i < mMeasurementOp.NumUICols; i++)
            {
                mMeasurementGrid.ColumnDefinitions.Add(new ColumnDefinition());
                TextBlock colName = new TextBlock();
                colName.VerticalAlignment = VerticalAlignment.Center;

                TextBlock colResult = new TextBlock();
                colResult.VerticalAlignment = VerticalAlignment.Center;

                colName.Text = mMeasurementOp.UIRowName(i);
                colName.FontSize = 15;
                mMeasurementGrid.Children.Add(colName);
                Grid.SetColumn(colName, (int)(i));
                Grid.SetRow(colName, 0);


                colResult.Text = String.Format("{0:0.##}", mMeasurementOp.UIRowValue(i));
                colResult.FontSize = 15;
                colResult.Foreground = mMeasurementOp.UIRowColor(i);
                mMeasurementGrid.Children.Add(colResult);
                Grid.SetColumn(colResult, (int)(i));
                Grid.SetRow(colResult, 1);
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
