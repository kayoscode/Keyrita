using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace Keyrita.Gui.Dialogs
{
    public partial class WindowBase : Window
    {

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

        public void CreateUndoRedoCommand()
        {
            // Undo
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

        public void CreateNewSaveOpenCommands()
        {
            CommandBinding saveCmdBinding = new CommandBinding(
                ApplicationCommands.Save,
                SaveSettings);
            this.CommandBindings.Add(saveCmdBinding);

            CommandBinding openCmdBinding = new CommandBinding(
                ApplicationCommands.Open,
                LoadSettings);
            this.CommandBindings.Add(openCmdBinding);

            CommandBinding newCmdBinding = new CommandBinding(
                ApplicationCommands.New,
                SetToDefaults);
            this.CommandBindings.Add(newCmdBinding);
        }

        public WindowBase()
            :base()
        {
            CreateUndoRedoCommand();
            CreateNewSaveOpenCommands();
        }

        protected override void OnClosed(EventArgs e)
        {
            LTrace.LogInfo($"Closing window {this.Title}");
            base.OnClosed(e);
        }
    }
}
