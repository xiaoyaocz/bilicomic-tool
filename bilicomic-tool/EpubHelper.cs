using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.IO.Compression;

namespace bilicomic_tool
{
    public class EpubHelper
    {
        /// <summary>
        /// 临时文件目录，创建完自动删除
        /// </summary>
        public string TmpDirPath { get; set; } = "tmp";
        /// <summary>
        /// 输出EPUB文件
        /// </summary>
        public string OutFilePath { get; set; }
        public EpubHelper(string OutFilePath)
        {
            this.OutFilePath = OutFilePath;
        }
        public EpubHelper(string OutFilePath, string TmpDirPath)
        {
            this.OutFilePath = OutFilePath;
            this.TmpDirPath = TmpDirPath;
        }

        public bool CreateEpubFile(string id, string title, string author, List<ImageItem> files)
        {
            try
            {
                WriteBaseFile();
                WriteHtmlAndImage(files);
                WriteNcxFile(id, files.Count, title, author);
                WriteOpfFile(id, files, title, author);
                if (File.Exists(OutFilePath))
                {
                    File.Delete(OutFilePath);
                }
                ZipFile.CreateFromDirectory(TmpDirPath, OutFilePath);
                var tmpDir = Directory.CreateDirectory(TmpDirPath);
                tmpDir.Delete(true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void WriteBaseFile()
        {
            var tmpDir = Directory.CreateDirectory(TmpDirPath);
            //写出css
            var cssDir = Directory.CreateDirectory(Path.Combine(tmpDir.FullName, "css"));
            File.Copy("epub-files/css/style.css", Path.Combine(cssDir.FullName, "style.css"), true);
            //写出xml
            var metaDir = Directory.CreateDirectory(Path.Combine(tmpDir.FullName, "META-INF"));
            File.Copy("epub-files/META-INF/container.xml", Path.Combine(metaDir.FullName, "container.xml"), true);
            //写出mimetype
            File.WriteAllText(Path.Combine(tmpDir.FullName, "mimetype"), "application/epub+zip");

        }

        private void WriteNcxFile(string id, int page_count, string title, string author)
        {
            var tmpDir = Directory.CreateDirectory(TmpDirPath);
            //创建xml文件夹
            var xmlDir = Directory.CreateDirectory(Path.Combine(tmpDir.FullName, "xml"));
            var ncxTpl = File.ReadAllText("epub-files/xml/volmoe.ncx");
            ncxTpl = ncxTpl.Replace("{BOOK_VOL_ID}", id);
            ncxTpl = ncxTpl.Replace("{BOOK_VOL_NAME}", title);
            ncxTpl = ncxTpl.Replace("{BOOK_AUTHOR}", author);
            ncxTpl = ncxTpl.Replace("{TOTAL_PAGE_COUNT}", page_count.ToString());
            var ncxItemsStr = "";
            for (int i = 1; i < page_count + 1; i++)
            {
                ncxItemsStr += $"<navPoint class=\"other\" id=\"Page_{i}\" playOrder=\"{i}\"><navLabel><text>第 {i.ToString("000")} 页</text></navLabel><content src=\"../html/{i}.html\"/></navPoint>\r\n";
            }
            ncxTpl = ncxTpl.Replace("{NCX}", ncxItemsStr);
            File.WriteAllText(Path.Combine(xmlDir.FullName, "volmoe.ncx"), ncxTpl);


        }

        private void WriteOpfFile(string id, List<ImageItem> images, string title, string author)
        {
            var tmpDir = Directory.CreateDirectory(TmpDirPath);

            var opfTpl = File.ReadAllText("epub-files/volmoe.opf");
            opfTpl = opfTpl.Replace("{BOOK_VOL_ID}", id);
            opfTpl = opfTpl.Replace("{BOOK_VOL_NAME}", title);
            opfTpl = opfTpl.Replace("{BOOK_AUTHOR}", author);
            var imgs = "";
            var pages = "";
            var img_no = "";
            int i = 1;
            foreach (var item in images)
            {
                imgs += $"<item id=\"Page_{i}\" href=\"html/{i}.html\" media-type=\"application/xhtml+xml\"/>\r\n";
                pages += $"<item id=\"img_{i}\" href=\"image/{$"{item.ComicID}-{item.ChapterID}-{item.Name}"}\" media-type=\"image/jpg\"/>\r\n";
                img_no += $" <itemref idref=\"Page_{i}\" />\r\n";
                i++;
            }

            opfTpl = opfTpl.Replace("{IMGS}", imgs);
            opfTpl = opfTpl.Replace("{PAGES}", pages);
            opfTpl = opfTpl.Replace("{PAGES_NO}", img_no);
            File.WriteAllText(Path.Combine(tmpDir.FullName, "volmoe.opf"), opfTpl);
        }

        private void WriteHtmlAndImage(List<ImageItem> imgs_path)
        {
            var tmpDir = Directory.CreateDirectory(TmpDirPath);
            //创建html&image文件夹
            var htmlDir = Directory.CreateDirectory(Path.Combine(tmpDir.FullName, "html"));
            var imageDir = Directory.CreateDirectory(Path.Combine(tmpDir.FullName, "image"));
            int i = 1;
            var htmlFileTpl = File.ReadAllText("epub-files/html/tpl.html");
            foreach (var item in imgs_path)
            {
                //复制image文件
                FileInfo fileInfo = new FileInfo(item.Path);
                fileInfo.CopyTo(Path.Combine(imageDir.FullName, $"{item.ComicID}-{item.ChapterID}-{item.Name}"), true);
                //写出html文件
                var htmlContent = htmlFileTpl.Replace("{IMG_FILENAME}", $"{item.ComicID}-{item.ChapterID}-{item.Name}");
                File.WriteAllText(System.IO.Path.Combine(htmlDir.FullName, i + ".html"), htmlContent);

                i++;
            }


        }

    }

    public class ImageItem
    {
        public int ChapterID { get; set; }
        public int ComicID { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

}
