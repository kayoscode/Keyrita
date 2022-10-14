using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using Microsoft.Win32;
using System;
using System.ComponentModel;
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
    public partial class MainWindow : Window
    {
        private static FieldInfo MenuDropAlignmentField;

        static MainWindow()
        {
            MenuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            LTrace.Assert(MenuDropAlignmentField != null, "_menuDropAlignment item not found");

            EnsureStandardPopupAlignment();
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
        }

        #region Command Bindings

        public void CreateUndoRedoCommand()
        {
            CommandBinding undoCmdBinding = new CommandBinding(
                ApplicationCommands.Undo,
                TriggerUndo);

            this.CommandBindings.Add(undoCmdBinding);

            // Redo
            KeyGesture newRedoKeyBinding = new KeyGesture(Key.Z, ModifierKeys.Shift | ModifierKeys.Control);
            ApplicationCommands.Redo.InputGestures.Add(newRedoKeyBinding);

            CommandBinding redoCmdBinding = new CommandBinding(
                ApplicationCommands.Redo,
                TriggerRedo);

            this.CommandBindings.Add(redoCmdBinding);
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            CreateUndoRedoCommand();

            mKeyboardControl.KeyboardState = SettingState.KeyboardSettings.KeyboardState;
            mKeyboardControl.KeyMappings = SettingState.FingerSettings.KeyMappings;
            mKeyboardControl.KeyHeatMap = SettingState.KeyboardSettings.HeatmapData;
            mKeyboardControl.ShowFingerUsage = SettingState.KeyboardSettings.ShowFingerUsage;

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

        protected void TriggerUndo(object sender, RoutedEventArgs e)
        {
            SettingsSystem.Undo();
        }

        protected void TriggerRedo(object sender, RoutedEventArgs e)
        {
            SettingsSystem.Redo();
        }

        protected void SetToDefaults(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This action cannot be undone, continue? (Dataset will remain loaded)",
                                                      "Confirmation", MessageBoxButton.YesNo);

            if(result == MessageBoxResult.Yes)
            {
                SettingsSystem.DefaultSettings();
            }
        }

        protected void LoadSettings(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                LTrace.LogInfo("Loading settings");

                try
                {
                    XmlDocument xmlReader = new XmlDocument();
                    xmlReader.Load(openFileDialog.FileName);
                    SettingsSystem.LoadSettings(xmlReader, false);
                }
                catch (Exception)
                {
                    LTrace.Assert(false, "Failed to load file");
                }
            }
        }

        protected void SaveSettings(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            if (saveFileDialog.ShowDialog() == true)
            {
                LTrace.LogInfo($"Saving settings to file {saveFileDialog.FileName}");

                using (XmlWriter fileWriter = XmlWriter.Create(saveFileDialog.FileName,
                        new XmlWriterSettings { Indent = true }))
                {
                    SettingsSystem.SaveSettings(fileWriter, false);
                }
            }
        }

        protected void LoadDataset(object sender, RoutedEventArgs e)
        {
            if (SettingState.MeasurementSettings.CharFrequencyData.IsRunning)
            {
                MessageBoxResult result = MessageBox.Show("A dataset is currently being loaded. Cancel?",
                    "Confirmation", MessageBoxButton.YesNo);

                if(result == MessageBoxResult.Yes)
                {
                    SettingState.MeasurementSettings.CharFrequencyData.Cancel();
                }
                else
                {
                    return;
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                LTrace.LogInfo("Loading dataset");

                try
                {
                    string dataset = File.ReadAllText(openFileDialog.FileName);
                    SettingState.MeasurementSettings.CharFrequencyData.LoadDataset(dataset);
                }
                catch (Exception)
                {
                    LTrace.Assert(false, "Unable to load dataset");
                }
            }
        }
    }
}
