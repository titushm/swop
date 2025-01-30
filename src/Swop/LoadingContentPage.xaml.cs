using System.Windows.Controls;

namespace Swop;

public partial class LoadingContentPage : Page{
    public LoadingContentPage(int max){
        InitializeComponent();
        LoadingBar.Maximum = max;
    }

    public void UpdateProgress(int value, string message){
        LoadingBar.Value = value;
        LoadingBar.SetValue(AdonisUI.Extensions.ProgressBarExtension.ContentProperty, message);
    }
}