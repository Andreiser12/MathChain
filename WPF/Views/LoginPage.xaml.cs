using MathChain.WPF.Services;
using MathChain.WPF.ViewModels;
using QRCoder;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MathChain.WPF.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
            Loaded += LoginPage_Loaded;
        }

        private async void LoginPage_Loaded(object sender,
                          System.Windows.RoutedEventArgs e)
        {
            await ConnectWalletAsync();
        }

        private async Task ConnectWalletAsync()
        {
            try
            {
                StatusText.Text = "Generating QR Code...";

                var wcUri = await WalletConnectHelper.GenerateUriAsync();

                if (string.IsNullOrEmpty(wcUri))
                {
                    StatusText.Text = "Bridge unavailable. Try again.";
                    return;
                }

                GenerateQRCode(wcUri);

                StatusText.Text = "Waiting for MetaMask...";

                var walletAddress = await WalletConnectHelper.WaitForConnectionAsync();

                StatusText.Text = $"Connected: {walletAddress[..8]}...";

                AppSession.WalletAddress = walletAddress;

                await Task.Delay(1000);
                NavigationService.Navigate(new DashboardPage());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Eroare: {ex.Message}");
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void GenerateQRCode(string uri)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(uri,
                         QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(10);

            var bitmap = new BitmapImage();
            using var ms = new System.IO.MemoryStream(qrBytes);
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            QRCodeImage.Source = bitmap;
        }

        private void SecretBypass_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.Navigate(new DashboardPage());
        }
    }
}