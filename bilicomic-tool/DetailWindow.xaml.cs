using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace bilicomic_tool
{
    /// <summary>
    /// DetailWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DetailWindow : Window
    {
        BookItem item;
        public DetailWindow(BookItem bookItem)
        {
            InitializeComponent();
            this.Title = bookItem.title;
            item = bookItem;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDetail(item.comic_id);
        }

        bool _loading = false;
        private async Task LoadDetail(int comicId)
        {
            try
            {
                _loading = true;
                loading.Visibility = Visibility.Visible;
                IDictionary<string, string> header = new Dictionary<string, string>();
                header.Add("cookie", ApiHelper.user.cookie);
                var data = await HttpHelper.Post("https://manga.bilibili.com/twirp/comic.v2.Comic/ComicDetail?device=pc&platform=web", JsonConvert.SerializeObject(new
                {
                    comic_id = comicId
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
                    var detail = JsonConvert.DeserializeObject<ComicDetail>(obj["data"].ToString());
                    this.DataContext = detail;
                }
                else
                {
                    loading.Visibility = Visibility.Collapsed;
                    MessageBox.Show(obj["msg"].ToString());
                }
                _loading = false;
                loading.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
           
        }

        private void ckLocked_Checked(object sender, RoutedEventArgs e)
        {
            var data = this.DataContext as ComicDetail;
            foreach (var item in data.ep_list.Where(x => x.is_locked))
            {
                item.check = true;
            }
        }

        private void ckLocked_Unchecked(object sender, RoutedEventArgs e)
        {
            var data = this.DataContext as ComicDetail;
            foreach (var item in data.ep_list.Where(x => x.is_locked))
            {
                item.check = false;
            }
        }

        private void ckUnLocked_Checked(object sender, RoutedEventArgs e)
        {
            var data = this.DataContext as ComicDetail;
            foreach (var item in data.ep_list.Where(x => !x.is_locked))
            {
                item.check = true;
            }
        }

        private void ckUnLocked_Unchecked(object sender, RoutedEventArgs e)
        {
            var data = this.DataContext as ComicDetail;
            foreach (var item in data.ep_list.Where(x => !x.is_locked))
            {
                item.check = false;
            }
        }

        private async void btnToUnLocked_Click(object sender, RoutedEventArgs e)
        {
            var data = (this.DataContext as ComicDetail).ep_list.Where(x => x.check && x.is_locked);
            if (data.Count() == 0)
            {
                MessageBox.Show("至少选中一章未解锁的章节");
                return;
            }
            var count = await LoadWalletCoupon();
            if (count == -1)
            {
                return;
            }
            if (count < data.Count())
            {
                MessageBox.Show($"漫读券不足!需要漫读券{data.Count()}张,拥有{count}张");
                return;
            }
            UnLockedWindow unLockedWindow = new UnLockedWindow(data.ToList(), count);
            unLockedWindow.ShowDialog();
            await LoadDetail(item.comic_id);
        }

        private async Task<int> LoadWalletCoupon()
        {
            IDictionary<string, string> header = new Dictionary<string, string>();
            header.Add("cookie", ApiHelper.user.cookie);
            var data = await HttpHelper.Post("https://manga.bilibili.com/twirp/user.v1.User/GetWallet?device=pc&platform=web", "{}", header);
            if (!data.status)
            {
                MessageBox.Show(data.message);
                return -1;
            }
            var obj = data.GetJObject();
            if (obj["code"].ToInt32() == 0)
            {
                return obj["data"]["remain_coupon"].ToInt32();
            }
            else
            {
                MessageBox.Show(obj["msg"].ToString());
                return -1;
            }
        }

        private void btnToDownload_Click(object sender, RoutedEventArgs e)
        {
            var data = (this.DataContext as ComicDetail).ep_list.Where(x => x.check && !x.is_locked);
            if (data.Count() == 0)
            {
                MessageBox.Show("至少选中一章已解锁的章节");
                return;
            }
            
            DownloadWindow downloadWindow = new DownloadWindow(this.DataContext as ComicDetail, data.ToList());
            downloadWindow.ShowDialog();

        }

        private void ToggleButton_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as ToggleButton;
            var data = btn.DataContext as EpList;
            //var result= await ApiHelper.GetImages(item.comic_id, data.id);
            
        }

       
       
    }


    public class EpList : INotifyPropertyChanged
    {
        public int id { get; set; }
        public double ord { get; set; }
        public int read { get; set; }
        public int pay_mode { get; set; }
        public bool is_locked { get; set; }
        public int pay_gold { get; set; }
        public int size { get; set; }
        public string short_title { get; set; }
        public bool is_in_free { get; set; }
        public string title { get; set; }
        public string cover { get; set; }
        public string pub_time { get; set; }
        public int comments { get; set; }
        public string unlock_expire_at { get; set; }
        public int unlock_type { get; set; }
        public bool allow_wait_free { get; set; }
        public string progress { get; set; }
        public int like_count { get; set; }
        public int chapter_id { get; set; }
        private bool _check = false;

        public bool check
        {
            get { return _check; }
            set { _check = value; DoPropertyChanged("check"); }
        }

        private string _log = "";
        public string log
        {
            get { return _log; }
            set { _log = value; DoPropertyChanged("log"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void DoPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    public class Styles2
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class FavComicInfo
    {
        public bool has_fav_activity { get; set; }
        public int fav_free_amount { get; set; }
        public int fav_coupon_type { get; set; }
    }

    public class ComicDetail : INotifyPropertyChanged
    {
        public int id { get; set; }
        public string title { get; set; }
        public int comic_type { get; set; }
        public int page_default { get; set; }
        public int page_allow { get; set; }
        public string horizontal_cover { get; set; }
        public string square_cover { get; set; }
        public string vertical_cover { get; set; }
        public string cover
        {
            get
            {
                return vertical_cover + "@300w.jpg";
            }
        }
        public string author
        {
            get
            {
                var str = "";
                foreach (var item in author_name)
                {
                    str += item + ",";
                }
                return str.TrimEnd(',');
            }
        }

        public List<string> author_name { get; set; }
        public List<string> styles { get; set; }
        public string last_ord { get; set; }
        public int is_finish { get; set; }
        public int status { get; set; }
        public int fav { get; set; }
        public int read_order { get; set; }
        public string evaluate { get; set; }
        public int total { get; set; }
        public List<EpList> ep_list { get; set; }
        public string release_time { get; set; }
        public int is_limit { get; set; }
        public int read_epid { get; set; }
        public string last_read_time { get; set; }
        public int is_download { get; set; }
        public string read_short_title { get; set; }
        public List<Styles2> styles2 { get; set; }
        public string style
        {
            get
            {
                var str = "";
                foreach (var item in styles2)
                {
                    str += item.name + ",";
                }
                return str.TrimEnd(',');
            }
        }
        public string renewal_time { get; set; }
        public string last_short_title { get; set; }
        public int discount_type { get; set; }
        public int discount { get; set; }
        public string discount_end { get; set; }
        public bool no_reward { get; set; }
        public int batch_discount_type { get; set; }
        public int ep_discount_type { get; set; }
        public bool has_fav_activity { get; set; }
        public int fav_free_amount { get; set; }
        public bool allow_wait_free { get; set; }
        public int wait_hour { get; set; }
        public string wait_free_at { get; set; }
        public int no_danmaku { get; set; }
        public int auto_pay_status { get; set; }
        public bool no_month_ticket { get; set; }
        public bool immersive { get; set; }
        public bool no_discount { get; set; }
        public int show_type { get; set; }
        public int pay_mode { get; set; }
        public List<object> chapters { get; set; }
        public string classic_lines { get; set; }
        public int pay_for_new { get; set; }
        public FavComicInfo fav_comic_info { get; set; }
        public string status_str
        {
            get
            {
                if (is_finish == 1)
                {
                    return "[已完结]共" + last_ord + "话";
                }
                else
                {
                    return "更新至" + last_short_title + "话";
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void DoPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


    }

}
