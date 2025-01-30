using System.IO;
using AdonisUI.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace Swop
{
    /// <summary>
    /// Interaction logic for ModsPage.xaml
    /// </summary>
    public partial class ModsPage{
        private string? GameID;
        public Dictionary<string, bool> Mods = new();
        public Dictionary<string, string> CurrentMods = new();
        public Dictionary<string, JObject> Profiles = new();
        public ModsPage(string? id){
            GameID = id;
            InitializeComponent();
        }

        private void PopulateMods() {
            ModsListBox.Items.Clear();
            
            foreach (KeyValuePair<string, string> mod in CurrentMods) {
                ListBoxItem listBoxItem = new ListBoxItem();
                listBoxItem.Tag = mod.Key;
                listBoxItem.ToolTip = mod.Value;
                DockPanel dockPanel = new DockPanel();
                dockPanel.Width = 215;

                TextBlock textBlock = new TextBlock();
                string name = mod.Value;
                textBlock.Text = name;
                textBlock.FontSize = 15;
                textBlock.MaxWidth = 170;
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                
                ToggleButton toggleSwitch = new ToggleButton();
                toggleSwitch.Template = (ControlTemplate)Application.Current.TryFindResource("ToggleSwitch");
                toggleSwitch.HorizontalAlignment = HorizontalAlignment.Right;
                toggleSwitch.VerticalAlignment = VerticalAlignment.Center;
                string? selectedProfile = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString();
                if (selectedProfile != null){
                    bool enabled = Profiles[selectedProfile][mod.Key].ToObject<bool>();
                    toggleSwitch.IsChecked = enabled;
                }
                dockPanel.Children.Add(textBlock);
                dockPanel.Children.Add(toggleSwitch);
                listBoxItem.Content = dockPanel;

                ModsListBox.Items.Add(listBoxItem);
            }
        }

        private void PopulateProfiles(){
            Profiles = Utils.GetProfiles();
            if (Profiles.Count < 1){
                Utils.WriteProfileJson("default", Mods);
                Profiles = Utils.GetProfiles();
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
                textBlock.FontSize = 15;
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                
                Button button = new Button();
                button.Content = "Delete";
                button.HorizontalAlignment = HorizontalAlignment.Right;
                button.VerticalAlignment = VerticalAlignment.Center;
                button.Click += (o, args) => {
                    Utils.DeleteProfile(name);
                    PopulateProfiles();
                };
                if (profile.Key.ToLower() == "default") button.IsEnabled = false;
                dockPanel.Children.Add(textBlock);
                dockPanel.Children.Add(button);
                listBoxItem.Content = dockPanel;
                
                ProfilesListBox.Items.Add(listBoxItem);
            }
        }
        
        private async void ModsPage_OnLoaded(object sender, RoutedEventArgs e) {
            if (GameID == null) {
                ContentErrorPage errorPage = new ContentErrorPage("The selected game could not be parsed.");
                NavigationService?.Navigate(errorPage);
            }

            Dictionary<string, string> CurrentMods = new();
            await Task.Run(() => CurrentMods = Workshop.GetMods(GameID));
            foreach (KeyValuePair<string, string> item in CurrentMods){
                Mods[item.Key] = true;
            }
            PopulateProfiles();
            PopulateMods();
        }

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e){
            string newProfileName = NewProfileTextBox.Text;
            bool cancel = Utils.EnsureProfileOverwrite(newProfileName, ProfilesListBox.Items);
            if (!cancel && newProfileName.Replace(" ", "") != ""){
                Utils.WriteProfileJson(newProfileName, Mods);
                PopulateProfiles();
            }
        }

        private void ImportProfileButton_Click(object sender, RoutedEventArgs e){
            OpenFileDialog dialog = new OpenFileDialog{
                FileName = "default",
                DefaultExt = ".json",
                Filter = "Profile Files (.json)|*.json"
            };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string path = dialog.FileName;
                bool isValid = Utils.EnsureProfile(path);
                if (!isValid){
                    AdonisUI.Controls.MessageBox.Show("Failed to import profile, ensure that the file is the correct format", "Import Failed");
                    return;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                bool cancel = Utils.EnsureProfileOverwrite(fileName, ProfilesListBox.Items);
                if (cancel) return;
                string profilePath = Utils.Paths.ProfilesDir + fileName + ".json";
                File.Delete(profilePath);
                File.Copy(path, profilePath);
                PopulateProfiles();
            }
        }

        private void NewProfileTextBox_GotFocus(object sender, RoutedEventArgs e) {
            Dispatcher.BeginInvoke((Action)(() => NewProfileTextBox.SelectAll()), DispatcherPriority.Input);
        }

        private void ProfilesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e){
            if (ProfilesListBox?.SelectedItem != null){
                NewProfileTextBox.Text = ((ListBoxItem)ProfilesListBox.SelectedItem).Tag?.ToString() ?? string.Empty;
            }

        }
    }
}
