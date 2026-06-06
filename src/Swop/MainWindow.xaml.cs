using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Swop;

public partial class MainWindow {
	public MainWindow() {
		InitializeComponent();
	}
	
	private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
		VersionTextBlock.Text = "Version: " + Utils.VERSION;
		await Task.Yield();
		await Task.Run(Utils.CheckUpdate);

		string[] appPaths = Directory.Exists(Workshop.WorkshopPath)
			? Directory.GetDirectories(Workshop.WorkshopPath, "*", SearchOption.TopDirectoryOnly)
			: Array.Empty<string>();

		LoadingContentPage loadingPage = new LoadingContentPage(appPaths.Length);
		OverlayFrame.Navigate(loadingPage);

		Dictionary<string, string> apps = new();
		int i = 0;
		await Task.Run(async () => {
			foreach (string path in appPaths) {
				i++;
				string id = path.Replace(Workshop.WorkshopPath, "");
				string? name = Steam.GetGameName(id);
				if (name != null) apps[id] = name;

				Application.Current.Dispatcher.Invoke(() =>
					loadingPage.UpdateProgress(i, $"Loading game: {name ?? id}"));
			}
		});

		OverlayFrame.Content = null;

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