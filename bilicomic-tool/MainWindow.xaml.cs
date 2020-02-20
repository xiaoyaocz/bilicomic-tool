using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace bilicomic_tool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtUser.Text = ApiHelper.user.name;
            listBookshelf.ItemsSource = bookshelf;
            page = 1;
            //await LoadWallet();
            await LoadBookshelf();
        }

        private async Task LoadWallet()
        {
            IDictionary<string, string> header = new Dictionary<string, string>();
            header.Add("cookie", ApiHelper.user.cookie);
            var data = await HttpHelper.Post("https://manga.bilibili.com/twirp/user.v1.User/GetWallet?device=pc&platform=web", "{}", header);
            if (!data.status)
            {
                MessageBox.Show(data.message);
                return;
            }
            var obj = data.GetJObject();
            if (obj["code"].ToInt32() == 0)
            {
                //txtCoupon.Text = obj["data"]["remain_coupon"].ToString();
                //txtCoin.Text = obj["data"]["remain_gold"].ToString();
            }
            else
            {
                MessageBox.Show(obj["msg"].ToString());
            }
        }
        ObservableCollection<BookItem> bookshelf = new ObservableCollection<BookItem>();
        int page = 1;
        bool _loading = false;
        private async Task LoadBookshelf()
        {
            _loading = true;
            loading.Visibility = Visibility.Visible;
            IDictionary<string, string> header = new Dictionary<string, string>();
            header.Add("cookie", ApiHelper.user.cookie);
            var data = await HttpHelper.Post("https://manga.bilibili.com/twirp/bookshelf.v1.Bookshelf/ListFavorite?device=pc&platform=web", JsonConvert.SerializeObject(new
            {
                order = 2,
                page_num = page,
                page_size = 15,
                wait_free = 0,
            }), header);
            if (!data.status)
            {
                loading.Visibility = Visibility.Collapsed;
                _loading = false;
                MessageBox.Show(data.message);
                
                return;
            }
            var obj = data.GetJObject();
            if (obj["code"].ToInt32() == 0)
            {
                var ls = JsonConvert.DeserializeObject<ObservableCollection<BookItem>>(obj["data"].ToString());
                foreach (var item in ls)
                {
                    bookshelf.Add(item);
                }
                page++;
            }
            else
            {
                loading.Visibility = Visibility.Collapsed;
                MessageBox.Show(obj["msg"].ToString());
            }
            _loading = false;
            loading.Visibility = Visibility.Collapsed;
        }

        private async void btnLoadMore_Click(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
               await LoadBookshelf();
            }
        }

        private void listBookshelf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBookshelf.SelectedItem==null)
            {
                return;
            }
            var data = listBookshelf.SelectedItem as BookItem;
            DetailWindow detailWindow = new DetailWindow(data);
            detailWindow.Show();
            listBookshelf.SelectedItem = null;
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"开发:xiaoyaocz\r\n版本:{Application.ResourceAssembly.GetName().Version.ToString()}\r\n\r\n本程序仅供学习交流编程技术使用", "关于");
        }
    }

    public class BookItem
    {
        public bool allow_wait_free { get; set; }
        public int comic_id { get; set; }
        public string vcover { get; set; }

        public string cover
        {
            get
            {
                return vcover + "@120w.jpg";
            }
        }

        public string last_ord { get; set; }
        public string ord_count { get; set; }

        public string last_ep_publish_time { get; set; }
        public string last_ep_short_title { get; set; }
        public string latest_ep_short_title { get; set; }
        public string title { get; set; }
    }
}
