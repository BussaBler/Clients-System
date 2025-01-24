using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Client_System_C_
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public static Window? Instance { get; private set; }
        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTittleBar);
            

            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                MicaBackdrop micaBackdrop = new()
                {
                    Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                };
                this.SystemBackdrop = micaBackdrop;
            }
        }
    }
}
