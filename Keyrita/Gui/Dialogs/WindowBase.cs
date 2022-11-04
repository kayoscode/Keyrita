using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;
using Microsoft.Win32;
using System;
using System.Text;
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
            openFileDialog.Filter = "XML Files (*.xml)|*.xml";

            if (openFileDialog.ShowDialog() == true)
            {
                LogUtils.LogInfo("Loading settings");

                try
                {
                    XmlDocument xmlReader = new XmlDocument();
                    xmlReader.Load(openFileDialog.FileName);
                    SettingsSystem.LoadSettings(xmlReader, false);
                }
                catch (Exception)
                {
                    LogUtils.Assert(false, "Failed to load file");
                }
            }
        }

        protected void SaveSettings(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML Files (*.xml)|*.xml";

            if (saveFileDialog.ShowDialog() == true)
            {
                LogUtils.LogInfo($"Saving settings to file {saveFileDialog.FileName}");

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

        public void CreateCopyPasteCommand()
        {
            // Copy
            CommandBinding copyBinding = new CommandBinding(
                ApplicationCommands.Copy,
                CopyLayoutToClipboard);

            this.CommandBindings.Add(copyBinding);

            // Paste
            CommandBinding pasteBinding = new CommandBinding(
                ApplicationCommands.Paste,
                LoadLayoutFromClipboard);

            this.CommandBindings.Add(pasteBinding);
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
            CreateCopyPasteCommand();
        }

        protected override void OnClosed(EventArgs e)
        {
            LogUtils.LogInfo($"Closing window {this.Title}");
            base.OnClosed(e);
        }

        protected void LoadLayoutFromClipboard(object sender, RoutedEventArgs e)
        {
            // We accept two layout formats. 
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                string clipboardText = Clipboard.GetText(TextDataFormat.Text);

                clipboardText = clipboardText.Trim();

                // Clear newlines -> we just want an array of 30 characters.
                clipboardText = clipboardText.Replace("\r\n", " ");
                clipboardText = clipboardText.Replace("\n", " ");
                clipboardText = clipboardText.Replace("\t", " ");

                string[] layout = clipboardText.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                if(layout.Length == 30)
                {
                    char[,] loadedLayout = new char[KeyboardStateSetting.ROWS, KeyboardStateSetting.COLS];

                    int index = 0;
                    for(int i = 0; i < loadedLayout.GetLength(0); i++)
                    {
                        for(int j = 0; j < loadedLayout.GetLength(1); j++)
                        {
                            loadedLayout[i, j] = layout[index][0];
                            index++;
                        }
                    }

                    SettingState.KeyboardSettings.KeyboardState.SetKeyboardState(loadedLayout);
                }
                else
                {
                    MessageBox.Show("Layout in wrong format", "Message", MessageBoxButton.OK);
                }
            }
            else
            {
                MessageBox.Show("A layout is not in the clipboard", "Message", MessageBoxButton.OK);
            }
        }

        protected void CopyLayoutToClipboard(object sender, RoutedEventArgs e)
        {
            // Create a string to represent the layout in the correct format
            StringBuilder layout = new StringBuilder();
            var kbState = SettingState.KeyboardSettings.KeyboardState.KeyStateCopy;

            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    layout.Append(kbState[i, j]);
                    layout.Append(" ");
                }

                layout.Append("\n");
            }

            Clipboard.SetText(layout.ToString());
        }
    }
}
