using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.Store;
using Windows.Globalization;
using Windows.Globalization.NumberFormatting;
using Windows.UI;
using Windows.UI.Text;

namespace Client_System_C_
{
    public sealed partial class MainPage : Page
    {
        public static MainPage? Current { get; private set; }
        public Frame ContentFrame { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            ContentFrame = contentFrame;
            contentFrame.Navigate(typeof(HomePage));
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(SettingsPage));
            }

            if (args.SelectedItemContainer != null)
            {
                string? tag = args.SelectedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "home":
                        contentFrame.Navigate(typeof(HomePage));
                        break;
                    case "add":
                        contentFrame.Navigate(typeof(AddUserPage));
                        break;
                    case "find":
                        contentFrame.Navigate(typeof(FindUserPage));
                        break;
                    case "remove":
                        contentFrame.Navigate(typeof(RemoveUserPage));
                        break;
                    case "list":
                        contentFrame.Navigate(typeof(ListUsersPage));
                        break;
                    case "addMachine":
                        contentFrame.Navigate(typeof(AddMachinePage));
                        break;
                    case "findMachine":
                        contentFrame.Navigate(typeof(FindMachinePage));
                        break;
                    case "removeMachine":
                        contentFrame.Navigate(typeof(RemoveMachinePage));
                        break;
                    case "listMachines":
                        contentFrame.Navigate(typeof(ListMachinesPage));
                        break;
                }
            }
        }

    }
}
