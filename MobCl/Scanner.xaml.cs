using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace MobCl;

public partial class Scanner : ContentPage
{
    private string login;
    private static readonly byte[] key = Convert.FromBase64String("LPjR6pHBsx2VvuYNYAaRZfGKsomvqsh3vAODL46dENw=");
    private static readonly byte[] iv = Convert.FromBase64String("nXJhi/OyX83gULxJv1UARQ==");
    public Scanner(string Login)
	{
        login = Login;
		InitializeComponent();
		barcodeReader.Options = new BarcodeReaderOptions
		{
            Formats = ZXing.Net.Maui.BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
		};
	}

    private void barcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
		var result = e.Results?.FirstOrDefault();

		if (result is null)
			return;

        Dispatcher.DispatchAsync(async () =>
        {
            barcodeReader.IsDetecting = false;
            barcodeReader.IsEnabled = false;
            var message = "";
            try
            {
                message = Encoding.UTF8.GetString(Convert.FromBase64String(result.Value));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", "Отсканирован некорректный QR-код.", "OK");
                await Navigation.PopModalAsync();
            }
            if (message.Split(' ')[0] == "startGame")
            {
                var port = 11333;
                var ip = "95.174.93.97";
                using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await sock.ConnectAsync(ip, port);

                var respBytes = new byte[1024];
                var Bytes = await sock.ReceiveAsync(respBytes);
                string resp = Encoding.UTF8.GetString(respBytes, 0, Bytes);

                var msgBytes = Encoding.UTF8.GetBytes(EncryptString("checkSlotName " + message.Split(' ')[1]));
                await sock.SendAsync(msgBytes);

                Bytes = await sock.ReceiveAsync(respBytes);
                resp = Encoding.UTF8.GetString(respBytes, 0, Bytes);
                message += " " + login;
                bool confirmation = await DisplayAlert("Подтвердите действие", "Вы точно хотите начать игру на автомате №" + message.Split(' ')[1] + "?", "Да", "Нет");
                if (confirmation)
                {
                    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(ip, port);

                        var responseBytes = new byte[1024];
                        var bytes = await socket.ReceiveAsync(responseBytes);
                        string response = Encoding.UTF8.GetString(responseBytes, 0, bytes);

                        var messageBytes = Encoding.UTF8.GetBytes(EncryptString(message));
                        await socket.SendAsync(messageBytes);

                        bytes = await socket.ReceiveAsync(responseBytes);
                        response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
                        switch(response)
                        {
                            case "Not enough money":
                                {
                                    await DisplayAlert("Недостаточно средств", "У вас недостаточно средств для игры на данном автомате", "OK");
                                    break;
                                }
                            case "Slot is busy or disabled":
                                {
                                    await DisplayAlert("Автомат недоступен", "В данный момент автомат выключен или используется другим игроком.\nПовторите попытку позднее", "OK");
                                    break;
                                }
                            default:
                                {
                                    await DisplayAlert("Выигрыш", "В ходе игры вы выиграли " + response + " жетонов.", "OK");
                                    break;
                                }
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", "Произошла ошибка, повторите попытку позднее", "ОК");
                    }
                }
            }
            else
            {
                await DisplayAlert("Ошибка", "Отсканирован некорректный QR-код.", "OK");
            }
            await Navigation.PopModalAsync();
        });
    }

    private async void BackButton_Pressed(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
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
}