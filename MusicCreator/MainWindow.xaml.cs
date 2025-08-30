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
                Opacity = 0.4
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
                await Compose();
            }

            CornersClicked++;
        }

        private async Task Compose()
        {
            NoteHandler.CreateNoteGrid(BorderPoints);
            var musicXML = MusicXMLFunctions.LoadFromFile(filePAth);
            
            foreach (var part in musicXML.Parts)
            {
                foreach (var measure in part.Measures)
                {
                    int divisions = measure.Attributes?.Divisions ?? 2;

                    var notes = measure.Notes;
                    for (int i = 0; i < notes.Count; i++)
                    {
                        var note = notes[i];

                        // 1) REST: ändra inte meny, bara avancera
                        if (note.Rest) // ← Använd EN konsekvent flagga! (se nedan)
                        {
                            var scrollR = NoteHandler.AdvanceRest(note, divisions);
                            if ((scrollR > 0))
                                await IOHandler.ShiftScrollAsync(-10);
                            // TODO: om scrollR > 0, scrolla grid & NoteHandler.ResetColumn()

                            continue;
                        }

                        // 2) MENYVAL för denna notlängd (ändra aldrig meny på rests)
                        var menuClicks = NoteHandler.SelectNoteAttribute(divisions, note);
                        if (menuClicks != null && menuClicks.Count >= 2)
                        {
                            IOHandler.SendAbsoluteClick(menuClicks[0]);
                            Task.Delay(120).Wait();
                            IOHandler.SendAbsoluteClick(menuClicks[1]);
                            Task.Delay(120).Wait();
                        }

                        // 3) Bygg chord-grupp: note + efterföljande <chord/>-noter
                        int j = i + 1;
                        while (j < notes.Count && notes[j].Chord) j++;

                        // Placera alla toner i samma kolumn
                        for (int k = i; k < j; k++)
                        {
                            var nk = notes[k];
                            var pos = NoteHandler.GetPositionNoAdvance(nk);
                            if (pos != null) IOHandler.SendAbsoluteClick(pos.Value);
                            Task.Delay(120).Wait();
                        }

                        // 4) Efter gruppen: avancera tiden en gång utifrån FÖRSTA notens duration
                        var scrollN = NoteHandler.AdvanceByDuration(note, divisions);
                        if(scrollN > 0)
                            await IOHandler.ShiftScrollAsync(-10);
                        

                        // hoppa fram i loopen till första noten efter chord-gruppen
                        i = j - 1;
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
