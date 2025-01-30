using System.Windows;
using System.Windows.Controls;

namespace Swop;

public partial class ContentErrorPage : Page{
    private string errorMessage;
    public ContentErrorPage(string message){
        errorMessage = message;
        InitializeComponent();
    }

    private void ContentErrorPage_OnLoaded(object sender, RoutedEventArgs e){
        ErrorMessageTextBlock.Text = errorMessage;
    }
}