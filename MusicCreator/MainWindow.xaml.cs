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
using MusicCreator.Models;

namespace MusicCreator
{
    public partial class MainWindow : Window
    {
        public bool DoneResizing { get; set; } = false;
        public int CornersClicked { get; set; } = 0;
        public List<Point> BorderPoints { get; set; } = new();

        private IntPtr _hwnd;
        string filePAth;
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

            filePAth = "C:\\Users\\Mikae\\Desktop\\TEst.musicxml";
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

            //Scroll steps to bottom from top: 18
            //Scroll Steps from left to right: 10
            //Note blocks per side step: 3 (after 10 steps)
            //Notes per block on 1/8 left to right: 8 st (24 per "Big Step")
            //Notes per block Up to down: 12st   8 block 
            //Note attribute above note 14, about 2.5 notes above
            //Meter 6 notes in, about 2.5 notes above


            Point relativePos = e.GetPosition(this);
            Point screenPos = PointToScreen(relativePos);

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
                Compose();
            }

            CornersClicked++;
        }

        private void Compose()
        {
            const int ColumnsPerQuarter = 2;
            NoteHandler.CreateNoteGrid(BorderPoints);

            MusicXML musicXML = MusicXMLFunctions.LoadFromFile(filePAth);

            foreach (var part in musicXML.Parts)
            {
                foreach (var measure in part.Measures)
                {
                    int division = measure.Attributes?.Divisions ?? 2;
                    foreach (var note in measure.Notes)
                    {
                        List<Point> points = NoteHandler.SelectNoteAttribute(division, note);
                        if(points != null && points.Count >= 2)
                        {
                            // Klicka på notvärdesmenyn
                            IOHandler.SendAbsoluteClick(points[0]);
                            Task.Delay(100).Wait(); // kort paus för att menyn ska hinna öppnas
                            // Klicka på rätt notvärde
                            IOHandler.SendAbsoluteClick(points[1]);
                            Task.Delay(100).Wait(); // kort paus för att menyn ska hinna stängas
                        }   


                        var (point, scroll) = NoteHandler.GetNotePosition(note);

                        if (point != null)
                            IOHandler.SendAbsoluteClick(point.Value);


                    }

                }
            }

            this.Close();

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
