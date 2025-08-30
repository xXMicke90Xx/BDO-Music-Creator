using MusicCreator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MusicCreator.Functions
{
    public static class NoteHandler
    {
        // Grid: 24 kolumner (tid), 24 rader (tonhöjd). 1 kolumn = 1/8-del.
        private const int Cols = 24;
        private const int Rows = 24;
        private const int ColumnsPerQuarter = 2; // 1/4 = 2 kolumner (1/8 per kolumn)

        private static readonly List<Point> NoteGrid = new(Cols * Rows);
        private static int _currentCol = 0;

        public static void ResetColumn() => _currentCol = 0;

        // Senaste / aktuellt menyval (för att undvika onödiga meny-klick)
        public static string CurrentNoteAttribute { get; private set; } = "";
        public static string LastNoteAttribute { get; private set; } = "1/8"; // default

        // 24 rader (överst B7 ned till C6)
        private static readonly List<string> NotesList = new()
        {
            "B7","A#7","A7","G#7","G7","F#7","F7","E7","D#7","D7","C#7","C7",
            "B6","A#6","A6","G#6","G6","F#6","F6","E6","D#6","D6","C#6","C6",
        };

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

            // Affin interpolering (parallellogram)
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
        /// Returnerar klickpunkter för att växla notvärdes-meny (1/4, 1/8, 1/16, ...),
        /// eller null om inget byte behövs. Ändrar aldrig meny på vilor.
        /// </summary>
        public static List<Point>? SelectNoteAttribute(int divisionsPerQuarter, Note note)
        {
            if (note.Rest) return null; // ändra inte meny på vilor
            if (divisionsPerQuarter < 1) divisionsPerQuarter = 1;

            double quarters = (double)note.Duration / divisionsPerQuarter;

            // välj meny baserat på längd (TODO: implementera 1/2 och 1/1 i din meny)
            string nextAttr =
                quarters == 1 ? "1/4" :
                quarters == 0.5 ? "1/8" :
                quarters == 0.25 ? "1/16" :
                quarters == 0.125 ? "1/32" :
                quarters == 0.0625 ? "1/64" :
                quarters == 2 ? "1/2" :   // TODO
                quarters == 4 ? "1/1" :   // TODO
                "1/4";

            if (nextAttr == LastNoteAttribute) return null;

            LastNoteAttribute = nextAttr;
            CurrentNoteAttribute = nextAttr;

            // Pekkoordinater för din meny (justera efter din UI)
            // Här använder vi NoteGrid för att utgå från en känd referenspunkt
            double notespaceY = NoteGrid[25].Y - NoteGrid[0].Y;

            Point openMenu = NoteGrid[13];
            openMenu.Y -= (notespaceY * 2.5); // öppna menyn

            Point pick = nextAttr switch
            {
                "1/4" => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespaceY),
                "1/8" => new Point(NoteGrid[13].X, NoteGrid[13].Y),
                "1/16" => new Point(NoteGrid[13].X, NoteGrid[13].Y + notespaceY),
                "1/32" => new Point(NoteGrid[13].X, NoteGrid[13].Y + notespaceY * 2),
                "1/64" => new Point(NoteGrid[13].X, NoteGrid[13].Y + notespaceY * 3), // om du har detta i menyn
                "1/2" => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespaceY * 2),  // TODO: justera till din meny
                "1/1" => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespaceY * 3),  // TODO: justera till din meny
                _ => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespaceY)
            };

            return new List<Point> { openMenu, pick };
        }

        /// <summary>
        /// Hämta position i NUVARANDE kolumn för en not (ingen tidsadvance).
        /// Returnerar null om notens pitch ligger utanför de 24 raderna.
        /// </summary>
        public static Point? GetPositionNoAdvance(Note note)
        {
            if (NoteGrid.Count != Cols * Rows)
                throw new InvalidOperationException("NoteGrid is not built.");

            int row = MapPitchToRowIndex(note);
            if (row < 0 || row >= Rows) return null;

            int col = _currentCol.Clamp(0, Cols - 1);
            return NoteGrid[row * Cols + col];
        }

        /// <summary>
        /// Avancera tiden med notens längd (duration/divisions → kolumner).
        /// Returnerar sidor att scrolla (0 om ingen).
        /// </summary>
        public static int AdvanceByDuration(Note note, int divisionsPerQuarter)
        {
            int steps = StepsFromDuration(note, divisionsPerQuarter);
            return AdvanceColumns(steps);
        }

        /// <summary>
        /// Vila: avancerar bara tiden enligt notens duration.
        /// </summary>
        public static int AdvanceRest(Note restNote, int divisionsPerQuarter)
        {
            int steps = StepsFromDuration(restNote, divisionsPerQuarter);
            return AdvanceColumns(steps);
        }

        /// <summary>
        /// Rå advance i kolumner; wrappar och returnerar antal "sidor" att scrolla.
        /// </summary>
        public static int AdvanceColumns(int steps)
        {
            if (steps < 1) steps = 1;

            _currentCol += steps;
            if (_currentCol <= Cols - 1) return 0;

            int pages = _currentCol / Cols;
            _currentCol = _currentCol % Cols;
            return pages;
        }

        // ======= Intern hjälp =======

        /// <summary>
        /// Beräkna hur många kolumner en note varar: duration/divisions → kvartar → kolumner.
        /// 1/8 per kolumn → ColumnsPerQuarter = 2.
        /// </summary>
        private static int StepsFromDuration(Note note, int divisionsPerQuarter)
        {
            if (divisionsPerQuarter < 1) divisionsPerQuarter = 1;
            double quarters = (double)note.Duration / divisionsPerQuarter;
            int steps = (int)Math.Round(quarters * ColumnsPerQuarter);
            return Math.Max(1, steps);
        }

        /// <summary>
        /// Mappar MusicXML-step+alter+octave till en av våra 24 rader (0..23).
        /// Hanterar ♭→♯ så att ”Bb” → ”A#”.
        /// </summary>
        private static int MapPitchToRowIndex(Note note)
        {
            string step = note.Pitch.Step.ToUpperInvariant(); // C D E F G A B
            int alter = note.Pitch.Alter; // -1 (flat), 0, +1 (sharp)
            int octave = note.Pitch.Octave;

            string sharpName = ToSharpName(step, alter); // C, C#, D, ...
            string token = $"{sharpName}{octave}";

            int idx = NotesList.IndexOf(token);
            if (idx < 0) return -1;
            return idx; // 0..23
        }

        private static string ToSharpName(string step, int alter)
        {
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

        private static int Clamp(this int v, int min, int max) => v < min ? min : (v > max ? max : v);
    }
}
