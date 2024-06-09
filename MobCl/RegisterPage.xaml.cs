using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace MobCl;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
	}

	private string login = "";
	private string password = "";
	private string repassword = "";
    private bool isFilled = false;
    private static readonly byte[] key = Convert.FromBase64String("LPjR6pHBsx2VvuYNYAaRZfGKsomvqsh3vAODL46dENw=");
    private static readonly byte[] iv = Convert.FromBase64String("nXJhi/OyX83gULxJv1UARQ==");

    private void Login_Changed(object sender, TextChangedEventArgs e)
    {
        login = ((Entry)sender).Text;
        if (login != "" && password != "" && repassword != "")
            isFilled = true;
        else
            isFilled = false;
    }
    private void Password_Changed(object sender, TextChangedEventArgs e)
    {
        password = ((Entry)sender).Text;
        if (login != "" && password != "" && repassword != "")
            isFilled = true;
        else
            isFilled = false;
    }
    private void RePassword_Changed(object sender, TextChangedEventArgs e)
    {
        repassword = ((Entry)sender).Text;
        if (login != "" && password != "" && repassword != "")
            isFilled = true;
        else
            isFilled = false;
    }
    private async void Register_Clicked(object sender, EventArgs e)
    {
        if (isFilled)
        {
            if (repassword != password)
            {
                await DisplayAlert("�������� ������������� ������", "������ �� ���������. ��������� �������.", "OK");
            }
            else
            {
                var port = 11333;
                var ip = "95.174.93.97";
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(ip, port);

                    var responseBytes = new byte[1024];
                    var bytes = await socket.ReceiveAsync(responseBytes);
                    string response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
                    Console.WriteLine(response);

                    var passwdHash = BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).Replace("-", string.Empty);

                    var message = "register " + login + " " + passwdHash;
                    var messageBytes = Encoding.UTF8.GetBytes(EncryptString(message));
                    await socket.SendAsync(messageBytes);

                    bytes = await socket.ReceiveAsync(responseBytes);
                    response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
                    Console.WriteLine(response);
                    var answer = response.Split(' ');
                    var isSuccess = answer[0] == "1";
                    if (isSuccess)
                    {
                        await DisplayAlert("�������� �����������", "��������� ���� �� �������� ������", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("������", "������ ����� �����.", "OK");
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        else
            await DisplayAlert("������ �����", "���� ��� ��������� ����� �� ���������!", "OK");
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