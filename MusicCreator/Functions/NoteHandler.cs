using MusicCreator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MusicCreator.Functions
{
    public static class NoteHandler
    {
        static double MULTIPLIER = 0;
        public static string CurrentNoteAttribute { get; set; } = "";
        public static string LastNoteAttribute { get; set; } = "1/8"; // default since user will adjust screen
        // 24 rader (överst B7 ned till C6) – justera vid behov
        private static readonly List<string> NotesList = new()
        {
            "B7","A#7","A7","G#7","G7","F#7","F7","E7","D#7","D7","C#7","C7",
            "B6","A#6","A6","G#6","G6","F#6","F6","E6","D#6","D6","C#6","C6",
            // resten finns, men gridden visar bara första 24
        };

        private const int Cols = 24; // tid/steputrymme i rutan (horisontellt)
        private const int Rows = 24; // rader/tonhöjd i rutan (vertikalt)
        // 24 x 24 = 576 punkter

        private static readonly List<Point> NoteGrid = new(Cols * Rows);
        private static int _currentCol = 0; // vilken kolumn vi är på (0..23)
        static bool firstnote = true;
        /// <summary>
        /// Bygg rutan (576 punkter) från fyra hörn i ordning: LT, RT, RB, LB.
        /// Hörnpunkterna ska vara CENTER av hörnrutorna.
        /// </summary>
        public static void CreateNoteGrid(List<Point> borderpoints)
        {
            if (borderpoints == null || borderpoints.Count < 4)
                throw new ArgumentException("borderpoints must have LT, RT, RB, LB");

            var LT = borderpoints[0];
            var RT = borderpoints[1];
            var RB = borderpoints[2];
            var LB = borderpoints[3];

            NoteGrid.Clear();

            // Affin interpolering (parallellogram) – robust och snabb
            var dx = new Vector((RT.X - LT.X) / (Cols - 1.0), (RT.Y - LT.Y) / (Cols - 1.0));
            var dy = new Vector((LB.X - LT.X) / (Rows - 1.0), (LB.Y - LT.Y) / (Rows - 1.0));

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    double x = LT.X + col * dx.X + row * dy.X;
                    double y = LT.Y + col * dx.Y + row * dy.Y;
                    NoteGrid.Add(new Point(x, y)); // index = row*Cols + col
                }
            }

            _currentCol = 0; // reset när ny grid byggs
        }
        /// <summary>
        /// Ändrar i menyn för notvärde (1/4, 1/8, 1/16, etc) beroende på divisionsPerQuarter och note.Duration.
        /// </summary>
        /// <param name="divisionsPerQuarter"></param>
        /// <param name="note"></param>
        public static List<Point> SelectNoteAttribute(int divisionsPerQuarter, Note note)
        {
            List<Point> pointsToClick = new List<Point>();

            int noteDivisions = note.Duration; // ex: 1=åttondel, 2=kvart, 4=halv om divisions=2
            if (divisionsPerQuarter < 1) divisionsPerQuarter = 1;

            // räkna ut hur många "kvartar" noten varar
            double quarters = (double)noteDivisions / divisionsPerQuarter;



            if (quarters == 1) CurrentNoteAttribute = "1/4";
            else if (quarters == 0.5) CurrentNoteAttribute = "1/8";
            else if (quarters == 0.25) CurrentNoteAttribute = "1/16";
            else if (quarters == 0.125) CurrentNoteAttribute = "1/32";
            else if (quarters == 0.0625) CurrentNoteAttribute = "1/64";
            else if (quarters == 2)
            {
                // TODO: hantera halvnoter (2 kvart = 1/2) genom menyväxling
                CurrentNoteAttribute = "1/2";
            }
            else if (quarters == 4)
            {
                // TODO: hantera helnoter (4 kvart = 1/1) genom menyväxling
                CurrentNoteAttribute = "1/1";
            }
            else
            {
                // fallback: försök avrunda
                CurrentNoteAttribute = $"Unknown length ({quarters} quarters)";
            }

            if (CurrentNoteAttribute == LastNoteAttribute)
                return null; // ingen ändring

            LastNoteAttribute = CurrentNoteAttribute;

            double notespace = NoteGrid[25].Y - NoteGrid[0].Y;

            Point menulocation = NoteGrid[13];
            menulocation.Y -= (notespace * 2.5); //Row 13 
            pointsToClick.Add(menulocation); // Klicka för att öppna notvärdesmenyn
            Point noteattribute = CurrentNoteAttribute switch
            {

                "1/4" => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespace),
                "1/8" => new Point(NoteGrid[13].X, NoteGrid[13].Y),
                "1/16" => new Point(NoteGrid[13].X, NoteGrid[13].Y + notespace),
                "1/32" => new Point(NoteGrid[13].X, NoteGrid[13].Y + notespace * 2),
                _ => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespace)
            };
            pointsToClick.Add(noteattribute);
            return pointsToClick;

        }

        /// <summary>
        /// Returnerar positionen för en note i aktuell kolumn samt ev. scroll-signal.
        /// - Chord=true: stanna i samma kolumn (lägg fler toner i samma slag).
        /// - IsRest=true: hoppa fram kolumn(er) utan position.
        /// - advanceSteps anger hur många kolumner ett note/rest förbrukar (default 1).
        /// För nu: om kolumn > 23, sätt scroll=+1 (senare implementerar du faktiskt scroll).
        /// </summary>
        public static (Point? position, int scroll) GetNotePosition(
            Note note,
            int advanceSteps = 1)
        {
            
            double notespace = NoteGrid[1].X - NoteGrid[0].X;
            switch (CurrentNoteAttribute)
            {
                case "1/1": MULTIPLIER = 4; break;
                case "1/2": MULTIPLIER = 2; break;
                case "1/4": MULTIPLIER = 1; break;
                case "1/8": MULTIPLIER = 0; break;
                case "1/16": MULTIPLIER = 0.5; break;
                case "1/32": MULTIPLIER = 0.25; break;
                default: MULTIPLIER = 1; break;
            }
            ;

            // 1) Hantera rests: ingen position, bara avancering
            if (note.Rest)
            {
                var scrollRest = AdvanceAndComputeScroll(advanceSteps);
                return (null, scrollRest);
            }

            // 2) Mappa MusicXML-note → radindex i våra 24 synliga rader
            int row = MapPitchToRowIndex(note);
            if (row < 0 || row >= Rows)
            {
                // Noten ligger utanför de första 24 raderna (gridens vertikala utsnitt)
                // För nu: ingen position. Senare kan du signalera vertikal scroll.
                var scrollNone = note.Chord ? 0 : AdvanceAndComputeScroll(advanceSteps);
                return (null, scrollNone);
            }

            int scroll = 0;
            if (!note.Chord && !firstnote)
                scroll = AdvanceAndComputeScroll(advanceSteps);

            // 3) Hämta position i aktuell kolumn
            int col = _currentCol.Clamp(0, Cols - 1);
            var pos = NoteGrid[row * Cols + col];
            pos.X += notespace * MULTIPLIER; // justera horisontellt för notvärde




            firstnote = false;
            return (pos, scroll);
        }

        /// <summary>
        /// Ökar kolumnen och returnerar scroll-signal (1 = behöver scrolla höger en sida).
        /// </summary>
        private static int AdvanceAndComputeScroll(int steps)
        {
            if (steps < 1) steps = 1;
            _currentCol += steps + (int)MULTIPLIER;
            if (_currentCol <= Cols - 1) return 0; // fortfarande inom rutan

            // Vi har passerat gridens sista kolumn
            int pages = _currentCol / Cols; // hur många 24-stegssidor vi passerade
            _currentCol = _currentCol % Cols; // wrap inom sidan efter scroll
            return pages; // för nu: returnera antal “scroll-right”
        }

        /// <summary>
        /// Mappar MusicXML-step+alter+octave till en av våra 24 rader (0..23).
        /// Hanterar ♭→♯ så att ”Bb” → ”A#”.
        /// </summary>
        private static int MapPitchToRowIndex(Note note)
        {
            // Normalisera till skarpt namn
            string step = note.Pitch.Step.ToUpperInvariant(); // C D E F G A B
            int alter = note.Pitch.Alter; // -1 (flat), 0, +1 (sharp)
            int octave = note.Pitch.Octave;

            string sharpName = ToSharpName(step, alter); // t.ex. C, C#, D, D#, ... B
            string token = $"{sharpName}{octave}";       // t.ex. A#7

            // Vi arbetar bara med de första 24 i listan (B7..C6)
            int idx = NotesList.IndexOf(token);
            if (idx < 0) return -1;
            return idx; // 0..23 om token råkar finnas i de första 24
        }

        private static string ToSharpName(string step, int alter)
        {
            // C D E F G A B (0..6), map till semitonklass 0..11
            int baseSemitone = step switch
            {
                "C" => 0,
                "D" => 2,
                "E" => 4,
                "F" => 5,
                "G" => 7,
                "A" => 9,
                "B" => 11,
                _ => 0
            };
            int semitone = (baseSemitone + alter + 12) % 12;
            return SemitoneToSharp[semitone];
        }

        private static readonly string[] SemitoneToSharp =
        {
            "C","C#","D","D#","E","F","F#","G","G#","A","A#","B"
        };

        /// <summary>
        /// Anropa om du vill börja om från kolumn 0 (t.ex. efter att du faktiskt scrollat spelet).
        /// </summary>
        public static void ResetColumn() => _currentCol = 0;

        private static int Clamp(this int v, int min, int max) => v < min ? min : (v > max ? max : v);
    }
}
