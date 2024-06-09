using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace MobCl
{
    public partial class MainPage : ContentPage
    {
        private string login;
        private static readonly byte[] key = Convert.FromBase64String("LPjR6pHBsx2VvuYNYAaRZfGKsomvqsh3vAODL46dENw=");
        private static readonly byte[] iv = Convert.FromBase64String("nXJhi/OyX83gULxJv1UARQ==");

        public MainPage(string Login)
        {
            login = Login;
            InitializeComponent();
        }

        private async void Scanner_Pressed(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new Scanner(login));
        }

        private async void SpendTokens_Pressed(object sender, EventArgs e)
        {
            await DisplayAlert("Данная функция находится в разработке", "Данная функция находится в разработке", "OK");
        }
        private static string EncryptString(string Text)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(Text);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        private async void CheckBalance_Pressed(object sender, EventArgs e)
        {
            var port = 11333;
            var ip = "95.174.93.97";
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(ip, port);

                var responseBytes = new byte[1024];
                var bytes = await socket.ReceiveAsync(responseBytes);
                var response = Encoding.UTF8.GetString(responseBytes, 0, bytes);

                var message = "checkBalance " + login;
                var messageBytes = Encoding.UTF8.GetBytes(EncryptString(message));
                await socket.SendAsync(messageBytes);


                bytes = await socket.ReceiveAsync(responseBytes);
                var answer = Encoding.UTF8.GetString(responseBytes, 0, bytes).Split(" ");
                await DisplayAlert("Ваш баланс", "На вашем балансе:\n" + answer[0] + " рублей\n" + answer[1] + " жетонов.", "OK");

            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", "Произошла ошибка, повторите попытку позднее", "ОК");
            }
        }
    }

}
