using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Keyrita.Util;

namespace Keyrita.Gui.Controls
{
    public class UserControlBase : UserControl
    {
        protected Window mWindow;

        public UserControlBase()
            :base()
        {
            this.Loaded += ControlLoaded;
        }

        private void ControlLoaded(object sender, RoutedEventArgs e)
        {
            // Figure out which window were in.
            mWindow = Window.GetWindow(this);

            if(mWindow != null)
            {
                mWindow.Closing += NotifyClosing;
            }
            else
            {
                LogUtils.Assert(false, "Unable to find window for control");
            }
        }

        private void NotifyClosing(object sender, CancelEventArgs args)
        {
            OnClose();
            
            // Unregister event handler.
            if(mWindow != null)
            {
                mWindow.Closing -= NotifyClosing;
            }
        }

        protected virtual void OnClose() { }
    }
}
