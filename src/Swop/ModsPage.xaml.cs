using System.IO;
using AdonisUI.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using Brushes = AdonisUI.Brushes;
using MessageBox = System.Windows.MessageBox;

namespace Swop
{
    /// <summary>
    /// Interaction logic for ModsPage.xaml
    /// </summary>
    public partial class ModsPage{
        private string? GameID;
        public Dictionary<string, string> Mods = new();
        public Dictionary<string, JObject> Profiles = new();
        public ModsPage(string? id){
            GameID = id;
            InitializeComponent();
        }

        private int FindProfileIndex(string name){
            for (int i = 0; i < ProfilesListBox.Items.Count; i++) {
                if (ProfilesListBox.Items[i] is ListBoxItem item && item.Tag.ToString() == name){
                    return i;
                }
            }
            return 0;
        }
        
        private async Task PopulateMods() {
            ModsListBox.Items.Clear();
            string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
            Dictionary<string, string> profileMods = new();
            foreach (var mod in Profiles[selectedProfile]){
                profileMods[mod.Key] = await Steam.GetModName(mod.Key, true) ?? "Unknown Mod";
            }

            foreach (var mod in Mods){
                if (!profileMods.Keys.Contains(mod.Key)){
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
                    string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
                    if (selectedProfile == null) return;
                    string id = listBoxItem.Tag?.ToString() ?? String.Empty;
                    Profiles[selectedProfile][id] = toggleSwitch.IsChecked;
                };
                object? selectedItem = ProfilesListBox.SelectedItem;
                if (selectedItem != null){
                    if (selectedProfile != null){
                        if (!Mods.ContainsKey(mod.Key)){
                            listBoxItem.IsEnabled = false;
                        } else{
                            bool enabled = false;
                            try {
                                enabled = Profiles[selectedProfile][mod.Key].ToObject<bool>();
                            } catch{
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

        private async Task PopulateProfiles(){
            Profiles = await Utils.GetProfiles(GameID);
            if (Profiles.Count < 1){
                Dictionary<string, bool> profile = new();
                foreach (KeyValuePair<string,string> mod in Mods){
                    profile[mod.Key] = true;
                }
                await Utils.WriteProfileJson(GameID, "default", profile);
                Profiles = await Utils.GetProfiles(GameID);
            }
            
            ProfilesListBox.Items.Clear();
            foreach (KeyValuePair<string, JObject> profile in Profiles){
                ListBoxItem listBoxItem = new ListBoxItem();
                listBoxItem.Tag = profile.Key;
                
                DockPanel dockPanel = new DockPanel();
                dockPanel.Width = 315;
                
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
                exportButton.Click += async (o, args) => {
                    string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
                    SaveFileDialog saveDialog = new SaveFileDialog();
                    saveDialog.Filter = "Json Profile|*.json";
                    saveDialog.Title = "Export the profile";
                    saveDialog.ShowDialog();

                    if (saveDialog.FileName != ""){
                        string profilePath = $"{Utils.Paths.ProfilesDir}\\{GameID}\\{selectedProfile}.json";
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
                    await Utils.DeleteProfile(GameID, name);
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
            if (GameID == null) {
                ContentErrorPage errorPage = new ContentErrorPage("The selected game could not be parsed.");
                NavigationService?.Navigate(errorPage);
            }
            Utils.ClearLogFile();
            await Task.Run(() => Mods = Workshop.GetMods(GameID).Result);
            await PopulateProfiles();
            
            string disabledModsPath = Utils.Paths.DisabledModsDir + GameID + "\\";
            string[] disabledMods = Directory.GetDirectories(disabledModsPath, "*", SearchOption.TopDirectoryOnly);
            foreach (var path in disabledMods){
                string folderName = Path.GetFileName(path);
                string modPath = Workshop.WorkshopPath + GameID + "\\" + folderName;
                if (Directory.Exists(modPath)) break;
                Directory.Move(path, modPath);
            }
        }

        private async void SaveProfileButton_Click(object sender, RoutedEventArgs e){
            string newProfileName = NewProfileTextBox.Text;
            bool cancel = Utils.EnsureProfileOverwrite(newProfileName, ProfilesListBox.Items);
            if (!cancel && newProfileName.Replace(" ", "") != ""){
                string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
                await Utils.WriteProfileJson(GameID, newProfileName, Profiles[selectedProfile]);
                await PopulateProfiles();
                int index = FindProfileIndex(newProfileName);
                ProfilesListBox.SelectedIndex = index;
            }
        }

        private async void ImportProfileButton_Click(object sender, RoutedEventArgs e){
            OpenFileDialog dialog = new OpenFileDialog{
                FileName = "default",
                DefaultExt = ".json",
                Filter = "Profile Files (.json)|*.json"
            };
            bool? result = dialog.ShowDialog();
            if (result == true) {
                string path = dialog.FileName;
                bool isValid = Utils.IsValidProfile(path);
                if (!isValid){
                    AdonisUI.Controls.MessageBox.Show("Failed to import profile, ensure that the file is the correct format", "Import Failed");
                    return;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                bool cancel = Utils.EnsureProfileOverwrite(fileName, ProfilesListBox.Items);
                if (cancel) return;
                string profilePath = $"{Utils.Paths.ProfilesDir}\\{GameID}\\{fileName}.json";
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
            }
        }

        private async void LaunchGameButton_OnClick(object sender, RoutedEventArgs e){
            if (ModsListBox.Items.Count < 1) return;
            LaunchGameButton.IsEnabled = false;
            string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
            await Utils.SetConfigProperty("active_profile", selectedProfile);
            await Workshop.ApplyProfile(GameID, Profiles[selectedProfile]);
            Steam.OpenSteamLink("steam://rungameid/" + GameID);
            LaunchGameButton.IsEnabled = true;
        }

        private void OpenModPageButton_OnClick(object sender, RoutedEventArgs e){
            if (ModsListBox.SelectedItem == null) return;
            string? selectedMod = ((ListBoxItem)ModsListBox.SelectedItem).Tag?.ToString();
            Steam.OpenSteamLink("steam://openurl/" + "https://steamcommunity.com/sharedfiles/filedetails/?id="+ selectedMod);
        }
    }
}
