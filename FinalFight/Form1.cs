using System.Diagnostics;
using System.Net;
using System.Security.Policy;
using System.Xml.Linq;

namespace FinalFight
{
    public partial class Form1 : Form
    {
        private List<Task> downloadTasks = new List<Task>();
        public Form1()
        {
            InitializeComponent();
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            string url = textBox1.Text;
            if (!string.IsNullOrEmpty(url))
            {
                string fileName = Path.GetFileName(url);
                int fileSizeKb = await GetFileSize(url);
                string fileSizeMb = ConvertToMb(fileSizeKb);
                ListViewItem item = new ListViewItem((new[] { fileName, "0%", fileSizeMb, "00:00:00", "0" }));

                listView1.Items.Add(item);
            }
            else
            {
                MessageBox.Show("Введите url-адрес");
            }
        }
        private async Task<int> GetFileSize(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                return (int)response.ContentLength;
            }
        }
        private string ConvertToMb(int size)
        {
            const double mb = 1024.0 * 1024.0;
            double fileSizeMb = size / mb;
            return fileSizeMb.ToString("F1") + "MB";
        }
        private async Task DownloadFileAsync(string url, string fileName, ListViewItem item)
        {
            WebClient client = new WebClient();
            var stopwatch = Stopwatch.StartNew();//Start
            client.DownloadProgressChanged += (s, args) =>
            {
                double downloadSpeedKps = args.BytesReceived / 1024.0 / stopwatch.Elapsed.TotalSeconds;
                item.SubItems[4].Text = $"{downloadSpeedKps:F2}KB/s";

                TimeSpan remainingTime = DownloadTime((int)(args.TotalBytesToReceive - args.BytesReceived), downloadSpeedKps);

                item.SubItems[3].Text = remainingTime.ToString(@"hh\:mm\:ss");

                item.SubItems[1].Text = $"{args.ProgressPercentage}%";
            };
            client.DownloadFileCompleted += (s, args) =>
            {
                stopwatch.Stop();
                item.SubItems[1].Text = "Готово";
                MessageBox.Show("Файл успешно загружен");
            };

            await client.DownloadFileTaskAsync(new Uri(url), fileName);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "All files (*.*)|*.*",
                Title = "Выберите место для сохранения программы"
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    // Получаем выбранный элемент
                    ListViewItem selectedItem = listView1.SelectedItems[0];
                    string url = textBox1.Text; // Получаем URL файла из первого столбца элемента
                    string fileName = selectedItem.SubItems[2].Text; // Получаем имя файла из третьего столбца элемента

                    // Запускаем асинхронное скачивание файла
                    Task downloadTask = DownloadFileAsync(url, saveFileDialog.FileName, selectedItem);
                    downloadTasks.Add(downloadTask);
                }
                else
                {
                    MessageBox.Show("Выберите файл для скачивания из списка.");
                }

                await Task.WhenAll(downloadTasks);
            }
        }
        private TimeSpan DownloadTime(int fileSizeKB, double downloadSpeed)
        {
            double downloadSpeedFileSeconds = fileSizeKB / 1024.0 / downloadSpeed;
            return TimeSpan.FromSeconds(downloadSpeedFileSeconds);
        }
    }
}