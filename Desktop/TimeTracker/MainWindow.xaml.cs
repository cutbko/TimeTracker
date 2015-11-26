using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace TimeTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataCommand _dataCommand;

        private bool _isPlaying;
        private Task _playingTask;
        private DateTime _recordTime;
        private TimeSpan trackedPerSession;
        private Dispatcher _dispatcher;

        private TimeSpan TrackedPerSession
        {
            get { return trackedPerSession; }
            set
            {
                trackedPerSession = value;
                _dispatcher.Invoke(() => { Tracked.Text = string.Format("{0:00}:{1:00}:{2:00}", TrackedPerSession.Hours, TrackedPerSession.Minutes, TrackedPerSession.Seconds); });
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _dispatcher = Dispatcher;

            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                Title += " v: " + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }

            _dataCommand = new DataCommand();
            if (!_dataCommand.DbExists())
            {
                try 
                { 
                    MessageBox.Show("Db doesn't exist, creating");

                    _dataCommand.CreateDbFile();

                    _dataCommand.ExecuteAsNonQuery(@"CREATE TABLE 'TimeRecords'
                                                    (
                                                        'Id' INTEGER PRIMARY KEY  AUTOINCREMENT  NOT NULL  UNIQUE , 
                                                        'CreatedAt' INTEGER NOT NULL, 
                                                        'CreatedAtDate' INTEGER NOT NULL, 
                                                        'TotalMinutes' DOUBLE NOT NULL
                                                    )");

                    var result = _dataCommand.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='TimeRecords';");

                    MessageBox.Show(result == 1 ? "Db and table TimeRecords is created" : "Error creating table TimeRecords");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error: " + e);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                if (_playingTask != null)
                {
                    _isPlaying = false;
                    _playingTask.Wait();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex);
            }

            base.OnClosing(e);
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            _isPlaying = !_isPlaying;

            PlayPause.Content = _isPlaying ? "Pause" : "Play";

            if (_isPlaying)
            {
                _recordTime = DateTime.Now;

                _playingTask = Task.Factory.StartNew(() =>
                {
                    while (_isPlaying)
                    {
                        TrackedPerSession += TimeSpan.FromSeconds(1);

                        if ((DateTime.Now - _recordTime).TotalMinutes > 10)
                        {
                            RecordTime();
                        }

                        Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                    }

                    RecordTime();

                    _playingTask = null;
                });
            }
        }

        private void RecordTime()
        {
            var now = DateTime.Now;
            double totalMinutes = (now - _recordTime).TotalMinutes;
            _recordTime = now;

            RecordTime(now, totalMinutes);
        }

        private void RecordTime(DateTime now, double totalMinutes)
        {
            _dataCommand.ExecuteAsNonQuery(string.Format("INSERT INTO 'TimeRecords' ('CreatedAt', 'CreatedAtDate', 'TotalMinutes') VALUES ({0},{1},{2})",
                                           now.ToUnixTime(),
                                           now.Date.ToUnixTime(),
                                           totalMinutes.ToString().Replace(",", ".")));

            using (Bitmap bmpScreenCapture = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                {
                    g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                        Screen.PrimaryScreen.Bounds.Y,
                        0, 0,
                        bmpScreenCapture.Size,
                        CopyPixelOperation.SourceCopy);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    bmpScreenCapture.Save(stream, ImageFormat.Png);
                    stream.Position = 0;
                    
                    string dir = Path.Combine(Const.AppDataPath, "Screenshots", DateTime.Now.ToString("yyyy-MM"));
                    Directory.CreateDirectory(dir);

                    using (var fileStream = File.Create(Path.Combine(dir, DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss") + ".png"))) 
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }

        }



        private void AddOffline_OnClick(object sender, RoutedEventArgs e)
        {
            int minutes;
            if (int.TryParse(OfflineBox.Text, out minutes))
            {
                RecordTime(DateTime.Now, minutes);
                MessageBox.Show("Added " + minutes + " minutes of offline work");
            }
            else
            {
                MessageBox.Show("Total Minutes is not integer");
            }

            OfflineBox.Text = "";
        }

        private void ViewReport_Click(object sender, RoutedEventArgs e)
        {
            int weekOffset;
            if (int.TryParse(WeekOffset.Text, out weekOffset))
            {
                var startOfWeek = DateTime.Now.AddDays(weekOffset * 7).StartOfWeek(DayOfWeek.Monday);
                var endOfWeek = startOfWeek.AddDays(7);

                DataTable dataTable = _dataCommand.GetTable(string.Format(@"SELECT CreatedAtDate, SUM(TotalMinutes)/60 as TotalHours FROM TimeRecords
                                                                          WHERE CreatedAtDate >= {0} AND CreatedAtDate < {1}
                                                                          GROUP BY CreatedAtDate
                                                                          ORDER BY CreatedAtDate", startOfWeek.ToUnixTime(), endOfWeek.ToUnixTime()));

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "*.txt|*.txt";
                var showDialog = saveFileDialog.ShowDialog();

                if (showDialog.HasValue && showDialog.Value)
                {
                    using (var writer = File.CreateText(saveFileDialog.FileName))
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            var date = DateTimeExtensions.FromUnixTime((long) dataTable.Rows[i]["CreatedAtDate"]).ToShortDateString();

                            writer.WriteLine("Date:" + date + "|Total Hours:" + dataTable.Rows[i]["TotalHours"]);    
                        }
                    }

                    MessageBox.Show("Saved report to " + saveFileDialog.FileName);
                }
            }
            else
            {
                MessageBox.Show("Week Offset is not integer");
            }
        }
    }
}
