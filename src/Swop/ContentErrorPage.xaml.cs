using System.Windows;

namespace Swop;

public partial class ContentErrorPage {
	private readonly string _errorMessage;
	public ContentErrorPage(string message){
		_errorMessage = message;
		InitializeComponent();
	}

	private void ContentErrorPage_OnLoaded(object sender, RoutedEventArgs e){
		ErrorMessageTextBlock.Text = _errorMessage;
	}
}