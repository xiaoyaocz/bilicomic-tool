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
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;

namespace bilicomic_tool
{
    /// <summary>
    /// UnLockedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UnLockedWindow : Window
    {
        ObservableCollection<EpList> EpLists = new ObservableCollection<EpList>();
        int count = 0;
        int _coupon_count = 0;
        public UnLockedWindow(List<EpList> epLists,int coupon_count)
        {
            InitializeComponent();
            foreach (var item in epLists)
            {
                EpLists.Add(item);
            }
            count = epLists.Count;
            _coupon_count = coupon_count;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            list.ItemsSource = EpLists;
            progress.Text = "0/" + count;
        }

        private async Task<CanPay> GetEpisodeBuyInfo(int epid)
        {
            IDictionary<string, string> header = new Dictionary<string, string>();
            header.Add("cookie", ApiHelper.user.cookie);
            var data = await HttpHelper.Post("https://manga.bilibili.com/twirp/comic.v1.Comic/GetEpisodeBuyInfo?device=pc&platform=web", JsonConvert.SerializeObject(new
            {
                ep_id = epid
            }), header);
            if (!data.status)
            {
                return new CanPay() { 
                    status=false,
                    message=data.message
                };
            }
            var obj = data.GetJObject();
            if (obj["code"].ToInt32() == 0)
            {
                if ((bool)obj["data"]["allow_coupon"])
                {
                    return new CanPay()
                    {
                        status = true,
                        recommend_coupon_id = obj["data"]["recommend_coupon_id"].ToString(),
                        message = ""
                    };
                }
                else
                {
                    return new CanPay()
                    {
                        status = false,
                        message = "不支持使用漫读券"
                    };
                }
            }
            else
            {
                return new CanPay()
                {
                    status = false,
                    message = obj["msg"].ToString()
                };
            }

        }
        private async Task<CanPay> EpisodeBuyInfo(int epid,string coupon_id)
        {
            IDictionary<string, string> header = new Dictionary<string, string>();
            header.Add("cookie", ApiHelper.user.cookie);
            var data = await HttpHelper.Post("https://manga.bilibili.com/twirp/comic.v1.Comic/BuyEpisode?device=pc&platform=web", JsonConvert.SerializeObject(new
            {
                buy_method=2,
                coupon_id= coupon_id,
                ep_id = epid,
                auto_pay_gold_status=2
            }), header);
            if (!data.status)
            {
                return new CanPay()
                {
                    status = false,
                    message = data.message
                };
            }
            var obj = data.GetJObject();
            if (obj["code"].ToInt32() == 0)
            {
                return new CanPay() { 
                status=true
                };
            }
            else
            {
                return new CanPay()
                {
                    status = false,
                    message = obj["msg"].ToString()
                };
            }

        }
        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in EpLists.ToList())
            {
               var can=  await GetEpisodeBuyInfo(item.id);
                if (can.status)
                { 
                    var pay = await EpisodeBuyInfo(item.id, can.recommend_coupon_id);
                    if (pay.status)
                    {
                        EpLists.Remove(item);
                    }
                    else
                    {
                        item.log = can.message;
                    }
                }
                else
                {
                    item.log = can.message;
                }
                
            }
            MessageBox.Show($"完成，已解锁:{count - EpLists.Count}章,漫读券剩余:{_coupon_count-(count - EpLists.Count)}张");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EpLists.Clear();
        }
    }
    public class CanPay
    {
        public bool status { get; set; }
        public string message { get; set; }
        public string recommend_coupon_id { get; set; }
    }
}
