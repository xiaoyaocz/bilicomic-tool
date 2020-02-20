using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace bilicomic_tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void imgQR_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadQR();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //检查状态
            try
            {
                FileInfo file = new FileInfo("user.json");
                if (file.Exists)
                {
                    var user_json = File.ReadAllText("user.json");
                    var userInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<UserInfo>(user_json);
                    if (userInfo.expired >= DateTime.Now.AddHours(1))
                    {
                        ApiHelper.user = userInfo;
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();
                    }
                }
            }
            catch (Exception)
            {
            }
            LoadQR();
        }
        private string _url = "";
        private string _oauthKey = "";
        private DispatcherTimer timer;
        private async void LoadQR()
        {
            try
            {
                ShowQrLog("二维码加载中...");
                timer?.Stop();
                timer = null;
                var result = await HttpHelper.Get("https://passport.bilibili.com/qrcode/getLoginUrl");
                HideQrLog();
                if (!result.status)
                {
                    ShowQrLog("二维码加载失败，点击刷新");
                }
                var obj = result.GetJObject();
                if (obj["code"].ToInt32() == 0)
                {
                    _url = obj["data"]["url"].ToString();
                    _oauthKey = obj["data"]["oauthKey"].ToString();
                    QrImg.Source = new BitmapImage(new Uri("http://qr.topscan.com/api.php?text=" + Uri.EscapeDataString(_url)));
                    timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += Timer_Tick;
                    timer.Start();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("加载二维码失败，请重试");
            }

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckCode();
        }
        private async void CheckCode()
        {
            try
            {
                Dictionary<string, string> content = new Dictionary<string, string>();
                content.Add("oauthKey", _oauthKey);
                content.Add("gourl", "https%3A%2F%2Fwww.bilibili.com%2F");
                var result = await HttpHelper.Post("https://passport.bilibili.com/qrcode/getLoginInfo", content);
                if (result.status)
                {
                    var obj = result.GetJObject();
                    if ((bool)obj["status"])
                    {
                        ShowQrLog("正在登录");
                        var cookie_url = obj["data"]["url"].ToString();

                        if (await CookieLogin(cookie_url))
                        {
                            timer?.Stop();
                            MainWindow mainWindow = new MainWindow();
                            mainWindow.Show();
                            this.Close();
                        }
                    }
                    else if (obj["data"].ToInt32() == -2)
                    {
                        //二维码过期
                        LoadQR();
                    }
                    else if (obj["data"].ToInt32() == -5)
                    {
                        ShowQrLog("已经扫描，等待确认");
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task<bool> CookieLogin(string cookie_url)
        {
            try
            {
                HttpClientHandler httpClientHandler = new HttpClientHandler();
                httpClientHandler.AllowAutoRedirect = false;
                using (HttpClient httpClient = new HttpClient(httpClientHandler))
                {
                    var response = await httpClient.GetAsync(cookie_url);
                    var setcookie = response.Headers.GetValues("set-cookie");
                    string cookie = "";
                    foreach (var item in setcookie)
                    {
                        cookie += item.Split(';').FirstOrDefault() + ";";
                    }
                    Uri uri = new Uri(cookie_url);
                    cookie += uri.Query.Replace("?", "").Replace("&", ";");

                    IDictionary<string, string> header = new Dictionary<string, string>();
                    header.Add("cookie", cookie);
                    var data = await HttpHelper.Get("https://passport.bilibili.com/login/app/third?appkey=27eb53fc9058f8c3&api=http%3A%2F%2Flink.acg.tv%2Fforum.php&sign=67ec798004373253d60114caaad89a8c", header);
                    if (!data.status)
                    {
                        MessageBox.Show(data.message);
                        return false;
                    }
                    var obj = data.GetJObject();
                    if (obj["code"].ToInt32() != 0)
                    {
                        MessageBox.Show("登录失败，请重试");
                        return false;
                    }
                    httpClient.DefaultRequestHeaders.Add("cookie", cookie);
                    var result = await httpClient.GetAsync(obj["data"]["confirm_uri"].ToString());
                    var success_url = result.Headers.Location;
                    UserInfo userInfo = new UserInfo()
                    {
                        access_key = Regex.Match(success_url.AbsoluteUri, "access_key=(.*?)&").Groups[1].Value,
                        uid = Regex.Match(success_url.AbsoluteUri, "mid=(.*?)&").Groups[1].Value,
                        name = Uri.UnescapeDataString(Regex.Match(success_url.AbsoluteUri, "uname=(.*?)&").Groups[1].Value),
                        expired = DateTime.Now.AddDays(7),
                        cookie = cookie
                    };
                    ApiHelper.user = userInfo;
                    File.WriteAllText("user.json", Newtonsoft.Json.JsonConvert.SerializeObject(userInfo));
                    return true;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("登录失败，请重试");
                return false;
            }


        }

        private void ShowQrLog(string status)
        {
            QrText.Text = status;
            QrLog.Visibility = Visibility.Visible;
        }
        private void HideQrLog()
        {
            QrLog.Visibility = Visibility.Collapsed;
        }


    }

    public class UserInfo
    {
        public string name { get; set; }
        public string uid { get; set; }
        public string access_key { get; set; }
        public string cookie { get; set; }
        public DateTime expired { get; set; }
    }

}
