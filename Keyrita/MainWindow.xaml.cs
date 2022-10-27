using Keyrita.Gui.Dialogs;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace Keyrita
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        private static FieldInfo MenuDropAlignmentField;

        static MainWindow()
        {
            MenuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            LogUtils.Assert(MenuDropAlignmentField != null, "_menuDropAlignment item not found");

            EnsureStandardPopupAlignment();
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
        }

        #region Command Bindings

        /// <summary>
        /// Clear the selection if they press escape.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled && e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
            {
                SettingState.KeyboardSettings.SelectedKey.SetSelection(' ');
            }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            mSetCharsMenu.Dialog = Gui.eDlgId.SetCharactersDlg;
            mSettingsMenu.Dialog = Gui.eDlgId.SettingsDlg;

            mKeyboardControl.KeyboardState = SettingState.KeyboardSettings.KeyboardState;
            mKeyboardControl.KeyMappings = SettingState.FingerSettings.KeyMappings;
            mKeyboardControl.ShowFingerUsage = SettingState.KeyboardSettings.ShowFingerUsage;
            mHeatMapSetting.Setting = SettingState.KeyboardSettings.HeatmapType;

            mLoadDatasetProgressBar.Setting = SettingState.MeasurementSettings.CharFrequencyData;

            // Menu item actions.
            mFlipVertMenuItem.Action = SettingState.UserActions.ReflectActions[eKeyboardReflectDirection.Vertical];
            mFlipHorzMenuItem.Action = SettingState.UserActions.ReflectActions[eKeyboardReflectDirection.Horizontal];
        }

        private static void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EnsureStandardPopupAlignment();
        }

        private static void EnsureStandardPopupAlignment()
        {
            if (SystemParameters.MenuDropAlignment && MenuDropAlignmentField != null)
            {
                MenuDropAlignmentField.SetValue(null, false);
            }
        }

        private EnumValueSetting KeyboardDisplaySetting =>
            SettingState.KeyboardSettings.KeyboardDisplay as EnumValueSetting;

        private OnOffSetting ShowAnnotationsSetting =>
            SettingState.MeasurementSettings.ShowAnnotations as OnOffSetting;

        protected void SaveKLC(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "KLC files (*.klc)|*.klc";

            if (saveFileDialog.ShowDialog() == true)
            {
                LogUtils.LogInfo($"Exporting as KLC {saveFileDialog.FileName}");
                Util.KBDTextFile.Serialize(saveFileDialog.FileName, "Set Name", "Write Description", CultureInfo.CurrentCulture, "Write Company", "(c) Copyright",
                    SettingState.KeyboardSettings.KeyboardState.KeyStateCopy);
            }
        }

        /// <summary>
        /// Returns true if the user decided to cancel the load operation.
        /// Or true if there is no dataset currently being loaded.
        /// </summary>
        /// <returns></returns>
        protected bool CancelRunningDatasetLoadOperation()
        {
            if (SettingState.MeasurementSettings.CharFrequencyData.IsRunning)
            {
                MessageBoxResult result = MessageBox.Show("A dataset is currently being loaded. Cancel?",
                    "Confirmation", MessageBoxButton.YesNo);

                if(result == MessageBoxResult.Yes)
                {
                    SettingState.MeasurementSettings.CharFrequencyData.Cancel();
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        protected void ClearDataset(object sender, RoutedEventArgs e)
        {
            if (CancelRunningDatasetLoadOperation())
            {
                SettingState.MeasurementSettings.CharFrequencyData.ClearData();
            }
        }

        protected void LoadDataset(object sender, RoutedEventArgs e)
        {
            if (CancelRunningDatasetLoadOperation())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                if (openFileDialog.ShowDialog() == true)
                {
                    LogUtils.LogInfo("Loading dataset");

                    try
                    {
                        string dataset = File.ReadAllText(openFileDialog.FileName);
                        SettingState.MeasurementSettings.CharFrequencyData.LoadDataset(dataset);
                    }
                    catch (Exception)
                    {
                        LogUtils.Assert(false, "Unable to load dataset");
                    }
                }
            }
        }
    }
}
