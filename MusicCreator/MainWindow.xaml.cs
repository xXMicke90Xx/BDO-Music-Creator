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

            int currentDivisions = 0;   // bär vidare
            int currentBeats = 4, currentBeatType = 4; // om du vill använda senare

            foreach (var part in musicXML.Parts)
            {
                // Nollställ per PART så att state inte “läcker” mellan stämmor
                currentDivisions = 0;
                currentBeats = 4; currentBeatType = 4;
                foreach (var measure in part.Measures)
                {
                    // Uppdatera från <attributes> om finns
                    if (measure.Attributes != null)
                    {
                        if (measure.Attributes.Divisions > 0)
                            currentDivisions = measure.Attributes.Divisions;

                        if (measure.Attributes.Time != null)
                        {
                            currentBeats = measure.Attributes.Time.Beats;
                            currentBeatType = measure.Attributes.Time.BeatType;
                        }
                    }

                    // SANE fallback om första takten saknar divisions av ngn anledning
                    if (currentDivisions <= 0) currentDivisions = 8; // t.ex. quarter=8

                    var notes = measure.Notes;

                    for (int i = 0; i < notes.Count; i++)
                    {
                        var note = notes[i];

                        // REST
                        if (note.IsRest)
                        {
                            int pagesH = NoteHandler.AdvanceRest(note, currentDivisions);
                            if (pagesH > 0)
                            {
                                // ev. liten “säkring” mot korrupt data
                                pagesH = Math.Min(pagesH, 8); // t.ex. max 8 sidor åt gången
                                for (int p = 0; p < pagesH; p++)
                                {
                                    await IOHandler.ShiftScrollAsync(-10);
                                    await Task.Delay(80);
                                }
                                NoteHandler.ResetColumn();
                            }
                            await Task.Delay(120);
                            continue;
                        }

                        // CHORD-GRUPP
                        int j = i + 1;
                        while (j < notes.Count && notes[j].Chord) j++;

                        // Vertikal synlighet
                        NoteHandler.EnsureVisibleForNotes(
                            notes.Skip(i).Take(j - i),
                            scrollUpOnePage: (pages) => { for (int p = 0; p < pages; p++) { IOHandler.ScrollVertical(+6); System.Threading.Thread.Sleep(80); } },
                            scrollDownOnePage: (pages) => { for (int p = 0; p < pages; p++) { IOHandler.ScrollVertical(-6); System.Threading.Thread.Sleep(80); } }
                        );

                        // MENYBYTE (valfritt)
                        var menuClicks = NoteHandler.SelectNoteAttribute(currentDivisions, note);
                        if (menuClicks != null && menuClicks.Count >= 2)
                        {
                            IOHandler.SendAbsoluteClick(menuClicks[0]); await Task.Delay(120);
                            IOHandler.SendAbsoluteClick(menuClicks[1]); await Task.Delay(120);
                        }

                        // Klicka ackordet
                        for (int k = i; k < j; k++)
                        {
                            var pos = NoteHandler.GetClickPosition(notes[k], currentDivisions);
                            if (pos != null) IOHandler.SendAbsoluteClick(pos.Value);
                            await Task.Delay(120);
                        }

                        // Advance i tid (horisontellt)
                        int pagesAfter = NoteHandler.AdvanceByDuration(note, currentDivisions);
                        if (pagesAfter > 0)
                        {
                            pagesAfter = Math.Min(pagesAfter, 8); // liten säkring mot trasiga data
                            for (int p = 0; p < pagesAfter; p++)
                            {
                                await IOHandler.ShiftScrollAsync(-10);
                                await Task.Delay(80);
                            }
                            NoteHandler.ResetColumn();
                        }

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
