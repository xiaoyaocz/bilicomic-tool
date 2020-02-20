using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Linq;
using RadioButton = System.Windows.Controls.RadioButton;
using System.Threading.Tasks;

namespace bilicomic_tool
{
    /// <summary>
    /// DownloadWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadWindow : Window
    {
        ObservableCollection<EpList> EpLists = new ObservableCollection<EpList>();
        ComicDetail detail;
        public DownloadWindow(ComicDetail comicDetail, List<EpList> epLists)
        {
            InitializeComponent();
            detail = comicDetail;
            foreach (var item in epLists)
            {
                EpLists.Add(item);
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            list.ItemsSource = EpLists;
        }
        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            grid.IsEnabled = false;
            DirectoryInfo directoryInfo = new DirectoryInfo(txtPath.Text);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            var file = System.IO.Path.Combine(txtPath.Text, detail.title);
            Directory.CreateDirectory(file);

            txtStatus.Text = "正在下载";
            var mode = GetEpubMode();
            var cover = CoverFile.IsChecked.Value;
            var q=GetQuality();
            List<Task> tasks = new List<Task>();
            var threadNum = ThreadNum.Value;
            //开始下载
            var num = Convert.ToInt32(Math.Ceiling(EpLists.Count / threadNum));
            for (int i = 0; i < threadNum; i++)
            {
                tasks.Add(NewThread(EpLists.Skip(i * num).Take(num).ToList(), file, cover, q));
            }
            await Task.WhenAll(tasks);
            if (mode == 0)
            {
                txtStatus.Text = "全部下载完成";
                grid.IsEnabled = true;
                return;
            }
            if (mode == 1)
            {
                txtStatus.Text = "正在打包EPUB";
                List<ImageItem> imgs = new List<ImageItem>();
                var ls = EpLists.Where(x => x.log == "已下载").OrderBy(x=>x.ord);
                foreach (var item in ls)
                {
                    var dir =new DirectoryInfo(System.IO.Path.Combine(file, (item.short_title + " " + item.title).Trim()));
                    foreach (var dirFile in dir.GetFiles("*.jpg"))
                    {
                        imgs.Add(new ImageItem() { 
                            ComicID=detail.id,
                            ChapterID=item.id,
                            Name= dirFile.Name,
                            Path= dirFile.FullName
                        });
                    }
                }
                var fileTilte = $"{detail.title}-{ls.FirstOrDefault().short_title}-{ls.LastOrDefault().short_title}";
                EpubHelper epubHelper = new EpubHelper(System.IO.Path.Combine(txtPath.Text, fileTilte+".epub"));
                var id = Guid.NewGuid().ToString();
                bool result = false;
                await Task.Run(() =>
                {
                   result= epubHelper.CreateEpubFile(id, fileTilte, detail.author, imgs);
                });
                if (result)
                {
                    txtStatus.Text = "全部下载完成";
                }
                else
                {
                    txtStatus.Text = "EPUB创建失败";
                }
                grid.IsEnabled = true;
                return;
            }

            if (mode == 2)
            {
                txtStatus.Text = "正在打包EPUB";
               
                var ls = EpLists.Where(x => x.log == "已下载").OrderBy(x => x.ord);
                foreach (var item in ls)
                {
                    item.log = "正在创建EPUB";
                    List<ImageItem> imgs = new List<ImageItem>();
                    var dir = new DirectoryInfo(System.IO.Path.Combine(file, (item.short_title + " " + item.title).Trim()));
                    foreach (var dirFile in dir.GetFiles("*.jpg"))
                    {
                        imgs.Add(new ImageItem()
                        {
                            ComicID = detail.id,
                            ChapterID = item.id,
                            Name = dirFile.Name,
                            Path = dirFile.FullName
                        });
                    }
                    var fileTilte = $"{detail.title}-{item.short_title}";
                    EpubHelper epubHelper = new EpubHelper(System.IO.Path.Combine(txtPath.Text, fileTilte + ".epub"));
                    bool result = false;
                    await Task.Run(() =>
                    {
                        result = epubHelper.CreateEpubFile(detail.id+item.id.ToString(), fileTilte, detail.author, imgs);
                    });
                    if (!result)
                    {
                        item.log = "EPUB创建失败";
                    }
                }

                txtStatus.Text = "全部下载完成";
                grid.IsEnabled = true;
                return;
            }

        }
      
        private async Task NewThread(List<EpList> items, string path, bool cover,string q)
        {
            foreach (var item in items)
            {
                try
                {
                    var folder = System.IO.Path.Combine(path, (item.short_title + " " + item.title).Trim());
                    Directory.CreateDirectory(folder);
                    this.Dispatcher.Invoke(() =>
                    {
                        item.log = "正在读取";
                    });
                    var data = await ApiHelper.GetImages(detail.id, item.id,q);
                    if (data.status)
                    {
                        int i = 1;
                        foreach (var img in data.data)
                        {
                            try
                            {
                                var file_path = System.IO.Path.Combine(folder, i.ToString("000") + ".jpg");
                                if (File.Exists(file_path) && !cover)
                                {
                                    continue;
                                }
                                var img_bytes = await HttpHelper.GetBytes(img.img_url);
                                await File.WriteAllBytesAsync(file_path, img_bytes);
                            }
                            catch (Exception)
                            {
                            }
                            this.Dispatcher.Invoke(() =>
                            {
                                item.log = $"{i}/{data.data.Count}";
                            });
                            i++;
                        }
                        this.Dispatcher.Invoke(() =>
                        {
                            item.log = "已下载";
                        });
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            item.log = data.msg;
                        });

                    }
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        item.log = ex.Message;
                    });

                }
            }
        }

        private void btnSetPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = dialog.SelectedPath;
            }
        }

        private int GetEpubMode()
        {
            var flag = 0;
            foreach (RadioButton item in (needEpub as StackPanel).Children)
            {
                if (item.IsChecked.Value)
                {
                    flag = Convert.ToInt32(item.Tag);
                    break;
                }
            }
            return flag;
        }
        private string GetQuality()
        {
            var flag = "";
            foreach (RadioButton item in (quality as StackPanel).Children)
            {
                if (item.IsChecked.Value)
                {
                    flag = item.Tag.ToString();
                    break;
                }
            }
            return flag;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EpLists.Clear();
        }
    
        
    
    }
}
