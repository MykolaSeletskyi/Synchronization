using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Синхронизация
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    class ReportFile
    {
        public int NumberOfNumbers { get; set; }
        public long FileLength { get; set; }
        public List<int> Numbers { get; set; } = new List<int>();
    }
    public partial class MainWindow : Window
    {
        void GenerateNumbers(object obj)
        {
            Mutex tmpMutex = (Mutex)obj;
            Random rnd = new Random();
            tmpMutex.WaitOne();
            using (StreamWriter sw = new StreamWriter("test1.txt"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    sw.WriteLine(rnd.Next(1000));
                }
            }
            tmpMutex.ReleaseMutex();
        }
        static bool IsPrimeNumber(int n)
        {
            var result = true;
            if (n > 1)
            {
                for (var i = 2u; i < n; i++)
                {
                    if (n % i == 0)
                    {
                        result = false;
                        break;
                    }
                }
            }
            else
            {
                result = false;
            }
            return result;
        }
        void PrimeNumbers(object obj)
        {
            Mutex mutex = obj as Mutex;
            mutex.WaitOne();
            using (StreamReader sr = new StreamReader("test1.txt"))
            {
                using (StreamWriter sw = new StreamWriter("test2.txt"))
                {
                    string strTmp;
                    while ((strTmp = sr.ReadLine()) != null)
                    {
                        int tmp = int.Parse(strTmp);
                        if (IsPrimeNumber(tmp))
                        {
                            sw.WriteLine(tmp);
                        }
                    }
                }
            }
            mutex.ReleaseMutex();
        }
        void FilteringPrimeNumbers(object obj)
        {
            Mutex mutex = obj as Mutex;
            mutex.WaitOne();
            using (StreamReader sr = new StreamReader("test2.txt"))
            {
                using (StreamWriter sw = new StreamWriter("test3.txt"))
                {
                    string strTmp;
                    while ((strTmp = sr.ReadLine()) != null)
                    {
                        if (strTmp[strTmp.Length - 1] == '7')
                        {
                            sw.WriteLine(strTmp);
                        }
                    }
                }
            }
            mutex.ReleaseMutex();
        }

        void Report(object obj)
        {
            Mutex mutex = obj as Mutex;
            mutex.WaitOne();

            List<ReportFile> reportFiles = new List<ReportFile>();
            string[] Paths = { "test1.txt", "test2.txt", "test3.txt" };

            foreach (string path in Paths)
            {
                ReportFile tmpReportFile = new ReportFile();
                using (StreamReader sr = new StreamReader(path))
                {
                    string strTmp;
                    while ((strTmp = sr.ReadLine()) != null)
                    {
                        tmpReportFile.Numbers.Add(int.Parse(strTmp));
                    }
                }
                tmpReportFile.NumberOfNumbers = tmpReportFile.Numbers.Count;
                tmpReportFile.FileLength = new FileInfo(path).Length;
                reportFiles.Add(tmpReportFile);
            }

            string jsonString = JsonSerializer.Serialize(reportFiles);
            File.WriteAllText("report.json", jsonString);

            mutex.ReleaseMutex();
        }
        public MainWindow()
        {
            InitializeComponent();
            Mutex mutex = new Mutex(false, "");
            Thread GenerateNumbersAndSaveFile = new Thread(GenerateNumbers);
            Thread SearchPrimeNumbers = new Thread(PrimeNumbers);
            Thread FilteringPrimeNumbers_ = new Thread(FilteringPrimeNumbers);
            Thread Reports = new Thread(Report);

            GenerateNumbersAndSaveFile.Start(mutex);
            SearchPrimeNumbers.Start(mutex);
            FilteringPrimeNumbers_.Start(mutex);
            Reports.Start(mutex);

            mutex.WaitOne();

            string jsonString = File.ReadAllText("report.json");
            DataGrid.ItemsSource = JsonSerializer.Deserialize<List<ReportFile>>(jsonString);
        }
    }
}
