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
using System.Linq;
using System.IO;
using System.IO.Compression;

namespace bilicomic_tool
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            var outTmp= @"C:\哔哩漫画下载\堀与宫村\epubTmp\page.1";
            Directory.CreateDirectory(outTmp);
          

            DirectoryInfo tplFiles = new DirectoryInfo("epub-files");
            var files = tplFiles.GetFileSystemInfos();
            foreach (var item in tplFiles.GetFiles())
            {
                    item.CopyTo(System.IO.Path.Combine(outTmp, item.Name),true);
            }
            foreach (var dir in tplFiles.GetDirectories())
            {
                foreach (var item in dir.GetFiles())
                {
                    var dirPath= System.IO.Path.Combine(outTmp, dir.Name);
                    Directory.CreateDirectory(dirPath);
                    item.CopyTo(System.IO.Path.Combine(outTmp,dir.Name, item.Name), true);
                }
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(@"C:\哔哩漫画下载\堀与宫村\page.1");
            var title = "堀与宫村 page.1";
            var author = "测试作者";

            var comic_id = 10086;
            var md5 = Utils.ToMD5(comic_id.ToString());


            var htmlFileTpl = File.ReadAllText(@"epub-files\html\tpl.html");
            var volmoeTpl = File.ReadAllText(@"epub-files\volmoe.opf");
            volmoeTpl = volmoeTpl.Replace("{BOOK_VOL_ID}", md5+"-"+comic_id);
            volmoeTpl = volmoeTpl.Replace("{BOOK_VOL_NAME}", title);
            volmoeTpl = volmoeTpl.Replace("{BOOK_AUTHOR}", author);

            var ncxTpl = File.ReadAllText(@"epub-files\xml\volmoe.ncx");
            ncxTpl = ncxTpl.Replace("{BOOK_VOL_ID}", md5 + "-" + comic_id);
            ncxTpl = ncxTpl.Replace("{BOOK_VOL_NAME}", title);
            ncxTpl = ncxTpl.Replace("{BOOK_AUTHOR}", author);
           
            int i = 1;
            var imgs = "";
            var pages = "";
            var img_no = "";
            var imgFiles = directoryInfo.GetFiles();
            ncxTpl = ncxTpl.Replace("{TOTAL_PAGE_COUNT}", imgFiles.Length.ToString());
            var ncx = "";
            var imgPath = System.IO.Path.Combine(outTmp, "image");
            Directory.CreateDirectory(imgPath);
            foreach (var item in imgFiles)
            {
                item.CopyTo(System.IO.Path.Combine(imgPath, item.Name), true);
                ncx += $"<navPoint class=\"other\" id=\"Page_{i}\" playOrder=\"{i}\"><navLabel><text>第 {i.ToString("000")} 页</text></navLabel><content src=\"../html/{i}.html\"/></navPoint>\r\n";
                imgs += $"<item id=\"Page_{i}\" href=\"html/{i}.html\" media-type=\"application/xhtml+xml\"/>\r\n";
                pages += $"<item id=\"img_{i}\" href=\"image/{item.Name}\" media-type=\"image/jpg\"/>\r\n";
                img_no += $" <itemref idref=\"Page_{i}\" />\r\n";
                var htmlContent = htmlFileTpl.Replace("{IMG_FILENAME}",item.Name);
                File.WriteAllText(System.IO.Path.Combine(outTmp,"html", i+".html"),htmlContent);
                i++;
            }
            volmoeTpl = volmoeTpl.Replace("{IMGS}", imgs);
            volmoeTpl = volmoeTpl.Replace("{PAGES}", pages);
            volmoeTpl = volmoeTpl.Replace("{PAGES_NO}", img_no);
            ncxTpl = ncxTpl.Replace("{NCX}", ncx);
            File.WriteAllText(System.IO.Path.Combine(outTmp, "volmoe.opf"), volmoeTpl);
            File.WriteAllText(System.IO.Path.Combine(outTmp,"xml", "volmoe.ncx"), ncxTpl);


            ZipFile.CreateFromDirectory(outTmp, System.IO.Path.Combine(@"C:\哔哩漫画下载\堀与宫村\page.1", "test.epub"));

            
        }
    }
}
