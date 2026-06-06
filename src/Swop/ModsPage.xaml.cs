using System.IO;
using AdonisUI.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Windows.Media;

namespace Swop;

public partial class ModsPage {
	private readonly string? _gameId;
	private Dictionary<string, string> _mods = new();
	private Dictionary<string, JObject> _profiles = new();

	public ModsPage(string? id) {
		_gameId = id;
		InitializeComponent();
	}

	private int FindProfileIndex(string name) {
		for (int i = 0; i < ProfilesListBox.Items.Count; i++) {
			if (ProfilesListBox.Items[i] is ListBoxItem item && item.Tag.ToString() == name) {
				return i;
			}
		}

		return 0;
	}

	private async Task PopulateMods() {
		ModsListBox.Items.Clear();
		OpenModPageButton.IsEnabled = false;
		string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
		Dictionary<string, string> profileMods = new();
		foreach (var mod in _profiles[selectedProfile]) {
			profileMods[mod.Key] = await Steam.GetModName(mod.Key, true) ?? "Unknown Mod";
		}

		foreach (var mod in _mods) {
			if (!profileMods.Keys.Contains(mod.Key)) {
				profileMods[mod.Key] = mod.Value;
			}
		}

		foreach (KeyValuePair<string, string> mod in profileMods) {
			ListBoxItem listBoxItem = new ListBoxItem();
			listBoxItem.Tag = mod.Key;
			listBoxItem.ToolTip = mod.Value;
			DockPanel dockPanel = new();
			dockPanel.Width = 215;


			TextBlock textBlock = new TextBlock();
			string name = mod.Value;
			textBlock.Text = name;
			textBlock.FontSize = 15;
			textBlock.MaxWidth = 170;
			textBlock.VerticalAlignment = VerticalAlignment.Center;


			ToggleButton toggleSwitch = new ToggleButton();
			toggleSwitch.Style = (Style)Application.Current.TryFindResource("ToggleSwitch");
			toggleSwitch.HorizontalAlignment = HorizontalAlignment.Right;
			toggleSwitch.VerticalAlignment = VerticalAlignment.Center;
			toggleSwitch.BorderBrush = new BrushConverter().ConvertFrom("#7B9BAF") as SolidColorBrush;
			toggleSwitch.BorderThickness = new Thickness(1);
			toggleSwitch.Click += (sender, args) => {
				bool isDirty = HasUnsavedChanges();
   
				SaveProfileButton.IsEnabled = isDirty; 

				if (isDirty) {
					((MainWindow)Application.Current.MainWindow).UnsavedChangesBanner.Visibility = Visibility.Visible;
				} else {
					((MainWindow)Application.Current.MainWindow).UnsavedChangesBanner.Visibility = Visibility.Collapsed;
				}
			};
			object? selectedItem = ProfilesListBox.SelectedItem;
			if (selectedItem != null) {
				if (selectedProfile != null) {
					if (!_mods.ContainsKey(mod.Key)) {
						listBoxItem.IsEnabled = false;
					} else {
						bool enabled = false;
						try {
							enabled = _profiles[selectedProfile][mod.Key].ToObject<bool>();
						} catch {
							// Ignored
						}

						toggleSwitch.IsChecked = enabled;
					}
				}
			}

			dockPanel.Children.Add(textBlock);
			dockPanel.Children.Add(toggleSwitch);
			listBoxItem.Content = dockPanel;

			ModsListBox.Items.Add(listBoxItem);
		}
	}

	private async Task PopulateProfiles() {
		_profiles = await Utils.GetProfiles(_gameId);
		if (_profiles.Count < 1) {
			Dictionary<string, bool> profile = new();
			foreach (KeyValuePair<string, string> mod in _mods) {
				profile[mod.Key] = true;
			}

			await Utils.WriteProfileJson(_gameId, "default", profile);
			_profiles = await Utils.GetProfiles(_gameId);
		}

		ProfilesListBox.Items.Clear();
		foreach (KeyValuePair<string, JObject> profile in _profiles) {
			ListBoxItem listBoxItem = new ListBoxItem();
			listBoxItem.Tag = profile.Key;

			DockPanel dockPanel = new DockPanel();
			dockPanel.Width = 300;

			TextBlock textBlock = new TextBlock();
			string name = profile.Key;
			textBlock.Text = name;
			textBlock.ToolTip = name;
			textBlock.FontSize = 15;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			textBlock.MaxWidth = 180;

			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = Orientation.Horizontal;
			stackPanel.HorizontalAlignment = HorizontalAlignment.Right;

			Button exportButton = new Button();
			exportButton.Content = "Export";
			exportButton.HorizontalAlignment = HorizontalAlignment.Right;
			exportButton.VerticalAlignment = VerticalAlignment.Center;
			exportButton.Margin = new Thickness(0, 0, 5, 0);
			exportButton.Click += (o, args) => {
				string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
				SaveFileDialog saveDialog = new SaveFileDialog();
				saveDialog.Filter = "Profile File|*.json";
				saveDialog.Title = "Export the profile";
				saveDialog.ShowDialog();

				if (saveDialog.FileName != "") {
					string profilePath = $"{Utils.Paths.ProfilesDir}\\{_gameId}\\{selectedProfile}.json";
					if (File.Exists(saveDialog.FileName)) File.Delete(saveDialog.FileName);
					File.Copy(profilePath, saveDialog.FileName);
					AdonisUI.Controls.MessageBox.Show("The profile has been successfully exported", "Profile Exported");
				}
			};

			Button deleteButton = new Button();
			deleteButton.Content = "Delete";
			deleteButton.HorizontalAlignment = HorizontalAlignment.Right;
			deleteButton.VerticalAlignment = VerticalAlignment.Center;
			deleteButton.Click += async (o, args) => {
				await Utils.DeleteProfile(_gameId, name);
				await PopulateProfiles();
			};
			if (profile.Key.ToLower() == "default") deleteButton.IsEnabled = false;

			stackPanel.Children.Add(exportButton);
			stackPanel.Children.Add(deleteButton);

			dockPanel.Children.Add(textBlock);
			dockPanel.Children.Add(stackPanel);
			listBoxItem.Content = dockPanel;

			ProfilesListBox.Items.Add(listBoxItem);
			string? activeProfile = await Utils.GetConfigProperty<string>("active_profile");
			int index = FindProfileIndex(activeProfile);
			ProfilesListBox.SelectedIndex = index;
		}
	}

	private async void ModsPage_OnLoaded(object sender, RoutedEventArgs e) {
		if (_gameId == null) {
			ContentErrorPage errorPage = new ContentErrorPage("The selected game could not be parsed.");
			NavigationService?.Navigate(errorPage);
		}

		Utils.ClearLogFile();
		await Task.Run(() => _mods = Workshop.GetMods(_gameId).Result);
		await PopulateProfiles();

		string disabledModsPath = Utils.Paths.DisabledModsDir + _gameId + "\\";
		if (!Directory.Exists(disabledModsPath)) Directory.CreateDirectory(disabledModsPath);
		string[] disabledMods = Directory.GetDirectories(disabledModsPath, "*", SearchOption.TopDirectoryOnly);
		foreach (var path in disabledMods) {
			string folderName = Path.GetFileName(path);
			string modPath = Workshop.WorkshopPath + _gameId + "\\" + folderName;
			if (Directory.Exists(modPath)) break;
			Directory.Move(path, modPath);
		}
	}

	private async void SaveProfileButton_Click(object sender, RoutedEventArgs e){
		string newProfileName = NewProfileTextBox.Text;
		bool cancel = Utils.EnsureProfileOverwrite(newProfileName, ProfilesListBox.Items);
   
		if (!cancel && newProfileName.Replace(" ", "") != ""){
			string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
			if (selectedProfile == null) return;

			JObject profileDataToSave = (JObject)_profiles[selectedProfile].DeepClone();

			foreach (var item in ModsListBox.Items) {
				if (item is ListBoxItem listBoxItem && listBoxItem.Content is DockPanel dockPanel) {
					var toggle = dockPanel.Children.OfType<ToggleButton>().FirstOrDefault();
					if (toggle != null) {
						string id = listBoxItem.Tag?.ToString() ?? string.Empty;
						profileDataToSave[id] = toggle.IsChecked ?? false;
					}
				}
			}

			await Utils.WriteProfileJson(_gameId, newProfileName, profileDataToSave);
			await PopulateProfiles();
      
			int index = FindProfileIndex(newProfileName);
			ProfilesListBox.SelectedIndex = index;

			SaveProfileButton.IsEnabled = false;
			((MainWindow)Application.Current.MainWindow).UnsavedChangesBanner.Visibility = Visibility.Collapsed;
		}
	}

	private async void ImportProfileButton_Click(object sender, RoutedEventArgs e) {
		OpenFileDialog dialog = new OpenFileDialog {
			FileName = "default",
			DefaultExt = ".json",
			Filter = "Profile Files|*.json"
		};
		bool? result = dialog.ShowDialog();
		if (result == true) {
			string path = dialog.FileName;
			bool isValid = Utils.IsValidProfile(path);
			if (!isValid) {
				AdonisUI.Controls.MessageBox.Show("Failed to import profile, ensure that the file is the correct format", "Import Failed");
				return;
			}

			string fileName = Path.GetFileNameWithoutExtension(path);
			bool cancel = Utils.EnsureProfileOverwrite(fileName, ProfilesListBox.Items);
			if (cancel) return;
			string profilePath = $"{Utils.Paths.ProfilesDir}\\{_gameId}\\{fileName}.json";
			File.Delete(profilePath);
			File.Copy(path, profilePath);
			await PopulateProfiles();
		}
	}

	private void NewProfileTextBox_GotFocus(object sender, RoutedEventArgs e) {
		Dispatcher.BeginInvoke((Action)(() => NewProfileTextBox.SelectAll()), DispatcherPriority.Input);
	}

	private async void ProfilesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e){
		if (ProfilesListBox?.SelectedItem != null){
			NewProfileTextBox.Text = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString() ?? string.Empty;
			await PopulateMods();
      
			SaveProfileButton.IsEnabled = false; 
			((MainWindow)Application.Current.MainWindow).UnsavedChangesBanner.Visibility = Visibility.Collapsed;

		}
	}

	private async void LaunchGameButton_OnClick(object sender, RoutedEventArgs e){
		if (ModsListBox.Items.Count < 1) return;
		LaunchGameButton.IsEnabled = false;
		string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
		if (selectedProfile == null) return;

		JObject activeLayout = (JObject)_profiles[selectedProfile].DeepClone();
		foreach (var item in ModsListBox.Items) {
			if (item is ListBoxItem listBoxItem && listBoxItem.Content is DockPanel dockPanel) {
				var toggle = dockPanel.Children.OfType<ToggleButton>().FirstOrDefault();
				if (toggle != null) {
					string id = listBoxItem.Tag?.ToString() ?? string.Empty;
					activeLayout[id] = toggle.IsChecked ?? false;
				}
			}
		}

		await Utils.SetConfigProperty("active_profile", selectedProfile);
		await Workshop.ApplyProfile(_gameId, activeLayout);
		Steam.OpenSteamLink("steam://rungameid/" + _gameId);
		LaunchGameButton.IsEnabled = true;
	}

	private void OpenModPageButton_OnClick(object sender, RoutedEventArgs e) {
		if (ModsListBox.SelectedItem == null) return;
		string? selectedMod = ((ListBoxItem)ModsListBox.SelectedItem).Tag?.ToString();
		Steam.OpenSteamLink("steam://openurl/" + "https://steamcommunity.com/sharedfiles/filedetails/?id=" + selectedMod);
	}

	private bool HasUnsavedChanges() {
		if (ProfilesListBox.SelectedItem is not ListBoxItem selectedItem) return false;
		string? selectedProfile = selectedItem.Tag?.ToString();
		if (selectedProfile == null || !_profiles.ContainsKey(selectedProfile)) return false;

		JObject originalProfile = _profiles[selectedProfile];

		foreach (var item in ModsListBox.Items) {
			if (item is ListBoxItem listBoxItem && listBoxItem.Content is DockPanel dockPanel) {
				var toggle = dockPanel.Children.OfType<ToggleButton>().FirstOrDefault();
				if (toggle != null) {
					string id = listBoxItem.Tag?.ToString() ?? string.Empty;
					bool currentToggleState = toggle.IsChecked ?? false;

					bool originalState = originalProfile[id]?.ToObject<bool>() ?? false;

					if (currentToggleState != originalState) {
						return true;
					}
				}
			}
		}

		return false;
	}

	private void NewProfileTextBox_OnTextChanged(object sender, TextChangedEventArgs e) {
		SaveProfileButton.IsEnabled = !string.IsNullOrWhiteSpace(NewProfileTextBox.Text);
	}

	private void ModsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
		OpenModPageButton.IsEnabled = ModsListBox.SelectedItem != null;
	}
}