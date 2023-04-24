using ArchiveApp.Properties;
using Aspose.Zip.Rar;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics.Metrics;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ArchiveApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            checkBox1.Checked = true;
            listBox1.Enabled = false;
            listBox2.Enabled = false;
            listBox1.MouseClick += ListBox1_MouseClick;
            listBox2.MouseClick += ListBox2_MouseClick;
            button2.Enabled = false;
            label5.AutoSize = false;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var defaultDirection = config.AppSettings.Settings["defaultDirectory"].Value;
            downloadPath = defaultDirection;
            textBox2.Text = downloadPath;
        }

        private string downloadPath = string.Empty;
        private string fileName = string.Empty;
        private string downloadFileName = Guid.NewGuid().ToString() + ".zip";
        private string zipFolderName = string.Empty;
        private List<string> links = new List<string>();
        private List<string> folderNames = new List<string>();

        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            label5.Visible = false;

            if (textBox1.Text == string.Empty || !textBox1.Text.Contains("https://"))
            {
                MessageBox.Show("Enter valid download link");
                return;
            }

            if (downloadPath == string.Empty)
            {
                MessageBox.Show("Choose download path");
                return;
            }

            var links = textBox1.Text.Replace(" ", "").Replace("\r", "").Replace("\n", "").Split("https://", StringSplitOptions.RemoveEmptyEntries);
            if (links.Length < 1)
            {
                MessageBox.Show("No valid download links");
                return;
            }

            foreach (var link in links)
            {
                this.links.Add("https://" + link);
            }

            if (this.links.Count > 0)
            {
                progressBar1.Maximum = this.links.Count;

                using (var webClient = new WebClient())
                {
                    webClient.UseDefaultCredentials = true;
                    webClient.DownloadProgressChanged += Client_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += Client_DownloadFileCompleted;
                    webClient.DownloadFileAsync(new Uri(this.links[0]), downloadPath + $"\\{downloadFileName}");

                    progressBar1.Value++;
                };
            }

            //
            button1.Enabled= false;
            textBox1.Enabled= false;
            button3.Enabled = false;
        }

        private void Client_DownloadFileCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                var s = (WebClient)sender;
                var contentDiscription = s.ResponseHeaders["Content-Disposition"];
                var startIndex = s.ResponseHeaders["Content-Disposition"].IndexOf("filename=") + 9;
                var endIndex = 0;
                if (contentDiscription.Contains(".rar"))
                {
                    endIndex = s.ResponseHeaders["Content-Disposition"].IndexOf(".rar") + 4;
                }
                else if (contentDiscription.Contains(".zip"))
                {
                    endIndex = s.ResponseHeaders["Content-Disposition"].IndexOf(".zip") + 4;
                }
                else
                {
                    MessageBox.Show("error: not archive");
                    return;
                }

                fileName = contentDiscription.Substring(startIndex, endIndex - startIndex).Replace("\"", "");

                try
                {
                    if (File.Exists(downloadPath + $"\\{downloadFileName}"))
                    {
                        File.Move(downloadPath + $"\\{downloadFileName}", downloadPath + $"\\{fileName}", true);
                    }
                }
                catch (Exception ex)
                {

                }

                zipFolderName = fileName.Substring(0, fileName.Length - 4);

                if (fileName.Contains(".rar"))
                {
                    RarArchive rarArchive = new RarArchive(downloadPath + $"\\{fileName}");
                    rarArchive.ExtractToDirectory(downloadPath);
                    rarArchive.Dispose();
                }

                if (fileName.Contains(".zip"))
                {
                    ZipFile.ExtractToDirectory(downloadPath + $"\\{fileName}", downloadPath + $"\\{zipFolderName}");
                }

                var files = Directory.GetFiles(downloadPath + $"\\{zipFolderName}");
                foreach (var file in files)
                {
                    var p = Path.GetExtension(file);

                    if (Path.GetExtension(file) == ".json" || Path.GetExtension(file) == ".session")
                    {
                        listBox2.Items.Add(file);
                    }
                    else
                    {
                        listBox1.Items.Add(file);
                    }
                }

                GetFilesFromDirectory(downloadPath + $"\\{zipFolderName}");
                folderNames.Add(zipFolderName);
                links.RemoveAt(0);

                if (checkBox1.Checked == true)
                {
                    File.Delete(downloadPath + $"\\{fileName}");
                }

                if (links.Count > 0)
                {
                    s.DownloadFileAsync(new Uri(links[0]), downloadPath + $"\\{downloadFileName}");
                    progressBar1.Value++;
                }
                //
                listBox1.Enabled = true;
                listBox2.Enabled = true;
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Download problem");
                return;
            }
        }

        private void GetFilesFromDirectory(string directory)
        {
            if (Directory.GetDirectories(directory).Any())
            {
                foreach (var dir in Directory.GetDirectories(directory))
                {
                    var files = Directory.GetFiles(dir);
                    listBox1.Items.AddRange(files);
                    GetFilesFromDirectory(dir);
                }
            }
            else
            {
                return;
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //progressBar1.Value = e.ProgressPercentage;
        }

        private void ListBox1_MouseClick(object sender, MouseEventArgs e)
        {
            var s = (ListBox)sender;

            if (s.SelectedItem != null)
            {
                listBox2.Items.Add(s.SelectedItem);
                listBox1.Items.Remove(s.SelectedItem);
            }
            else
            {
                return;
            }
        }

        private void ListBox2_MouseClick(object sender, MouseEventArgs e)
        {
            var s = (ListBox)sender;

            if (s.SelectedItem != null)
            {
                listBox1.Items.Add(s.SelectedItem);
                listBox2.Items.Remove(s.SelectedItem);
            }
            else
            {
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var item in listBox1.Items)
                {
                    File.Delete(item.ToString());
                }

                foreach (var folderName in folderNames)
                {
                    if (Directory.GetFiles(downloadPath + $"\\{folderName}").Any())
                    {
                        var files = Directory.GetFiles(downloadPath + $"\\{folderName}");
                        foreach (var file in files)
                        {
                            if (!File.Exists(downloadPath + $"\\{Path.GetFileName(file)}"))
                            {
                                File.Move(file, downloadPath + $"\\{Path.GetFileName(file)}", false);
                            }
                            else
                            {
                                MoveDuplicateFile(file);
                            }
                        }
                    }

                    MoveFilesFromDirectory(downloadPath + $"\\{folderName}");

                    Directory.Delete(downloadPath + $"\\{folderName}", true);
                }


                listBox1.Items.Clear();
                listBox2.Items.Clear();
                folderNames.Clear();
                links.Clear();
                textBox1.Text = string.Empty;
                fileName = string.Empty;
                //downloadPath = string.Empty;
                zipFolderName = string.Empty;
                progressBar1.Value = 0;
                //
                listBox1.Enabled = false;
                listBox2.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = true;
                button1.Enabled = true;
                textBox1.Enabled = true;

                label5.Visible = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        private int counter = 0;

        private void MoveDuplicateFile(string file)
        {
            counter++;

            if (!File.Exists(downloadPath + $"\\{Path.GetFileNameWithoutExtension(file)}" + counter.ToString() + Path.GetExtension(file)))
            {
                File.Move(file, downloadPath + $"\\{Path.GetFileNameWithoutExtension(file)}" + counter.ToString() + Path.GetExtension(file), false);
            }
            else
            {
                MoveDuplicateFile(file);
            }
        }

        private void MoveFilesFromDirectory(string directory)
        {
            if (Directory.GetDirectories(directory).Any())
            {
                foreach (var dir in Directory.GetDirectories(directory))
                {
                    var files = Directory.GetFiles(dir);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        if (!File.Exists(downloadPath + $"\\{fileName}"))
                        {
                            File.Move(file, downloadPath + $"\\{fileName}", false);
                        }
                        else
                        {
                            MoveDuplicateFile(file);
                        }
                    }
                    MoveFilesFromDirectory(dir);
                }
            }
            else
            {
                return;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                downloadPath = folderBrowserDialog1.SelectedPath;

                textBox2.Text = downloadPath;
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["defaultDirectory"].Value = downloadPath;
                config.Save();
                ConfigurationManager.RefreshSection("appSettings");
            }
        }
    }
}