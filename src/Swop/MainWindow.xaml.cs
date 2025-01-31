using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace Swop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
    }

    
    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e){
        VersionTextBlock.Text = Utils.VERSION.ToString();
        Utils.CheckUpdate();
        Dictionary<string, string> apps = Workshop.GetWorkshopAppIDs();
        foreach (KeyValuePair<string, string> item in apps) {
            TabItem tabItem = new TabItem();
            tabItem.Header = item.Value;
            tabItem.Tag = item.Key;
            AppsTabControl.Items.Add(tabItem);
        }
    }

    private void AppsTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e){
        TabItem selected = (TabItem)AppsTabControl.SelectedItem;
        ModsPage modsPage = new ModsPage(selected.Tag?.ToString());
        ContentFrame.NavigationService.Navigate(modsPage);
    }
}