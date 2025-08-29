using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using MusicCreator.Functions;

namespace MusicCreator
{
    public partial class MainWindow : Window
    {
        public bool DoneResizing { get; set; } = false;
        public int CornersClicked { get; set; } = 0;
        public List<Point> BorderPoints { get; set; } = new();
       
        private IntPtr _hwnd;

        public MainWindow()
        {
            InitializeComponent();

            // sätt bakgrund (valfritt)
            var imageBrush = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(@"C:\Users\Mikae\source\repos\MusicCreator\MusicCreator\Images\FullB2-A0#.png")),
                Stretch = Stretch.UniformToFill,
                Opacity = 0.7
            };
            Form.Background = imageBrush;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _hwnd = new WindowInteropHelper(this).Handle;
            IOHandler._hwnd = _hwnd;
            IOHandler.SetAlwaysOnTop(true);
        }

        private void Form_Loaded(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Resize the window to fit the background. Press the \"I'm Done\" button when done");
        }

        private async void Form_MouseDown(object sender, MouseButtonEventArgs e)
        {
           


            if (!DoneResizing) return;
            if (CornersClicked >= 4) return;
           

            Point relativePos = e.GetPosition(this);
            Point screenPos = PointToScreen(relativePos);

            ShowWindow(_hwnd, 0);
            await ClickIntoGameAsync(screenPos, dropTopMost: true);           
            //ScrollVertical(-3);
            //ScrollHorizontal(3);
            await IOHandler.ShiftScrollAsync(-10); //-4 = Scroll Höger
            if (CornersClicked == 0)
            {
                BorderPoints.Add(screenPos);
                MessageBox.Show("Now click on the most top-right tile right in the middle");
            }
            else if (CornersClicked == 1)
            {
                BorderPoints.Add(screenPos);
                MessageBox.Show("Now click on the most bottom-right tile right in the middle");
            }
            else if (CornersClicked == 2)
            {
                BorderPoints.Add(screenPos);
                MessageBox.Show("Now click on the most bottom-left tile right in the middle");
            }
            else if (CornersClicked == 3)
            {
                BorderPoints.Add(screenPos);
                MessageBox.Show("Done");
                ShowWindow(_hwnd, 0);
              
                foreach (var item in BorderPoints)
                    // NEW: mer robust spelklick
                    await ClickIntoGameAsync(item, dropTopMost: true);
            }

            CornersClicked++;
        }
       

        // NEW: robust fokus + click-through + ABSOLUTE click för spel
        private async Task ClickIntoGameAsync(Point p, bool dropTopMost)
        {
            IntPtr gameHwnd = IOHandler.GetForegroundWindow(); // utgå från att spelet är aktivt

            if (dropTopMost) IOHandler.SetAlwaysOnTop(false);

            IOHandler.EnableClickThrough();
            IOHandler.ApplyStyleChanges();

            // Dölj overlay så spelet garanterat får capture/fokus
            //ShowWindow(_hwnd, SW_HIDE);
            await Task.Delay(80);

            if (gameHwnd != IntPtr.Zero)
            {
                //FocusWindowRobust(gameHwnd); // NEW: stark fokusering (se helper nedan)
            }

            await Task.Delay(60);

            IOHandler.SendAbsoluteClick(p); // ABSOLUTE move + click
            await Task.Delay(60);

            // Visa overlay igen utan att sno fokus
            //ShowWindow(_hwnd, SW_SHOWNOACTIVATE);

            // Låt spelet behålla capture ett ögonblick innan vi stänger click-through
            await Task.Delay(100);

            IOHandler.DisableClickThrough();
            IOHandler.ApplyStyleChanges();

            if (dropTopMost) IOHandler.SetAlwaysOnTop(true);
        }

       

       

        private void Done_btn_Click(object sender, RoutedEventArgs e)
        {
            Done_btn.Visibility = Visibility.Collapsed;
            DoneResizing = true;
            MessageBox.Show("Now click on the most top-left tile right in the middle");
        }

        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
