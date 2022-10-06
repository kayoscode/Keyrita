using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Xml;

namespace Keyrita
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            mKeyboardDisplay.Setting = KeyboardDisplaySetting;
            mKeyboardShape.Setting = KeyboardShapeSetting;
            mLanguage.Setting = LanguageSetting;
            mShowAnnotations.Setting = ShowAnnotationsSetting;

            mKeyboardControl.KeyboardState = SettingState.KeyboardSettings.KeyboardState;
        }

        private EnumValueSetting KeyboardDisplaySetting =>
            SettingState.KeyboardSettings.KeyboardDisplay as EnumValueSetting;

        private EnumValueSetting KeyboardShapeSetting =>
            SettingState.KeyboardSettings.KeyboardShape as EnumValueSetting;

        private EnumValueSetting LanguageSetting =>
            SettingState.KeyboardSettings.KeyboardLanguage as EnumValueSetting;
        
        private OnOffSetting ShowAnnotationsSetting =>
            SettingState.MeasurementSettings.ShowAnnotations as OnOffSetting;

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        protected void LoadSettings(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if(openFileDialog.ShowDialog() == true)
            {
                LTrace.LogInfo("Loading settings");

                try
                {
                    XmlDocument xmlReader = new XmlDocument();
                    xmlReader.Load(openFileDialog.FileName);
                    SettingsSystem.LoadSettings(xmlReader);
                }
                catch(Exception)
                {
                    LTrace.Assert(false, "Failed to load file");
                }
            }
        }

        protected void SaveSettings(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

			if(saveFileDialog.ShowDialog() == true)
            {
                LTrace.LogInfo($"Saving settings to file {saveFileDialog.FileName}");

                using (XmlWriter fileWriter = XmlWriter.Create(saveFileDialog.FileName, 
                        new XmlWriterSettings { Indent = true }))
                {
                    SettingsSystem.SaveSettings(fileWriter);
                }
            }
        }
    }
}
