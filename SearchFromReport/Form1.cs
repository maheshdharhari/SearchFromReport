using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace SearchFromReport
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = @"D:\",
                Title = @"Browse Report File",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "html",
                Filter = @"Html Files (*.html)|*.html",
                FilterIndex = 2,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var openFileDialog1 = new OpenFileDialog
            {
                Title = @"Browse csv File",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "csv",
                Filter = @"Html Files (*.csv)|*.csv",
                FilterIndex = 2,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = openFileDialog1.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private static List<string> FindIdInHtml(List<string> s, string htmlFilePath)
        {
           
                if (!File.Exists(htmlFilePath))
                    return null;
                var listOfItemId = new List<string>();
                var allListOfItemId = new List<HtmlNode>();
                var doc = new HtmlDocument();
                doc.Load(htmlFilePath);
                var tableRows = doc.DocumentNode.SelectNodes("//table[@id='myTable']//tr");
                foreach (var row in tableRows)
                {
                    try
                    {
                        var itemType = row.SelectNodes("td")[3].InnerText;
                        if (itemType != "Item") continue;
                        allListOfItemId.Add(row);
                    }
                    catch (Exception)
                    {
                        // throw;
                    }
                }

                var unused = allListOfItemId.Count();
                foreach (var item in s)
                {
                    var foundItem = false;
                    var itemError = "0";
                    foreach (var row in allListOfItemId)
                    {
                        try
                        {
                            var itemId = row.SelectNodes("td")[2].InnerText;
                            // Not found in report
                            if (itemId != item) continue;
                            // After finding in report
                            foundItem = true;
                            // Check error value
                            itemError = row.SelectNodes("td")[6].InnerText;
                            break;
                        }
                        catch (Exception)
                        {
                            // throw;
                        }
                    }

                    if (itemError == "1" || !foundItem)
                    {
                        // Failed items or error items
                        listOfItemId.Add(item);
                    }

                    if (s.Count + 1 - listOfItemId.Count == allListOfItemId.Count)
                        return listOfItemId;
                }

                return listOfItemId;
           
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var htmlFilePath = textBox1.Text;
            var csvItemsId = textBox2.Text;

            if (!File.Exists(csvItemsId) || !File.Exists(htmlFilePath))
                return;
            if (Path.GetExtension(csvItemsId).ToLower() != ".csv") return;
            var idFromFile = new List<string>();
            using (var reader = new StreamReader(csvItemsId))
            {
                while (!reader.EndOfStream)
                {
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        var s = values.FirstOrDefault();
                        idFromFile.Add(s);
                    }
                }
            }

            ;
            var failedItemsTask = FindIdInHtml(idFromFile, htmlFilePath);
            var failedItems = failedItemsTask;
            var fileName = Path.GetFileNameWithoutExtension(csvItemsId);
            string newFileWithExtension = fileName + " FailedItems.csv";
            var stringValue = string.Join(Environment.NewLine, failedItems.ToArray());

            using (var sw = new StreamWriter(newFileWithExtension))
            {
                sw.WriteLine(stringValue);
            }

            MessageBox.Show(@"Created csv file for the failed items.", this.Name, MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            //cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Close();
        }
    }
}