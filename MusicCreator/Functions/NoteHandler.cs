using MusicCreator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MusicCreator.Functions
{
    public static class NoteHandler
    {
        public static bool IsAtBottom { get; set; } = false;

        // Grid: 24 kolumner (tid), 24 rader (tonhöjd). 1 kolumn = 1/8.
        private const int Cols = 24;
        private const int Rows = 24;
        private const int ColumnsPerQuarter = 2; // 1/4 = 2 kolumner (1/8 per kolumn)

        // Scroll-hjälp: 1 notch ≈ 3 rader (din empiri)
        private const int RowsPerNotch = 3;

        // Visuell kompensation i rader på nedersta sidan (hela gridden)
        private const int BottomOffsetRows = 3;

        private static readonly List<Point> NoteGrid = new(Cols * Rows);

        // --- Viktigt: kontinuerlig tidsposition mätt i 1/8-kolumner ---
        private static double _currentColPos = 0.0;

        // Vertikal "sida": topprad (0-baserad i FullNotes-listan)
        private static int _currentTopRow = 0; // 0, 24, 48, ...

        public static void ResetColumn() => _currentColPos = 0.0;
        public static void ResetTopRow() => _currentTopRow = 0;

        // Senaste / aktuellt menyval (för att undvika onödiga meny-klick)
        public static string CurrentNoteAttribute { get; private set; } = "";
        public static string LastNoteAttribute { get; private set; } = "1/8"; // default

        // FULL lista (B7 → C0) för absolut pitch-index (0 = B7)
        private static readonly List<string> FullNotes = new()
        {
            "B7","A#7","A7","G#7","G7","F#7","F7","E7","D#7","D7","C#7","C7",
            "B6","A#6","A6","G#6","G6","F#6","F6","E6","D#6","D6","C#6","C6",
            "B5","A#5","A5","G#5","G5","F#5","F5","E5","D#5","D5","C#5","C5",
            "B4","A#4","A4","G#4","G4","F#4","F4","E4","D#4","D4","C#4","C4",
            "B3","A#3","A3","G#3","G3","F#3","F3","E3","D#3","D3","C#3","C3",
            "B2","A#2","A2","G#2","G2","F#2","F2","E2","D#2","D2","C#2","C2",
            "B1","A#1","A1","G#1","G1","F#1","F1","E1","D#1","D1","C#1","C1",
            "B0","A#0","A0","G#0","G0","F#0","F0","E0","D#0","D0","C#0","C0",
        };

        // För bakåtkompat: 24 synliga (första vyn)
        private static readonly List<string> NotesList = FullNotes.Take(Rows).ToList();

        /// <summary>
        /// Bygg grid (576 punkter) från fyra hörn: LT, RT, RB, LB (centra av hörnrutorna).
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

            _currentColPos = 0.0;
            _currentTopRow = 0;
        }

        /// <summary>
        /// Returnerar klickpunkter för att växla notvärdes-meny (1/4, 1/8, 1/16, ...),
        /// eller null om inget byte behövs. Ändrar aldrig meny på vilor.
        /// </summary>
        public static List<Point>? SelectNoteAttribute(int divisionsPerQuarter, Note note)
        {
            if (note.IsRest) return null;
            if (divisionsPerQuarter < 1) divisionsPerQuarter = 1;

            double quarters = (double)note.Duration / divisionsPerQuarter;

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

            // Menypunkter (justera efter din UI)
            double notespaceY = NoteGrid[25].Y - NoteGrid[0].Y;

            Point openMenu = NoteGrid[13];
            openMenu.Y -= (notespaceY * 2.5); // öppna menyn

            Point pick = nextAttr switch
            {
                "1/4" => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespaceY),
                "1/8" => new Point(NoteGrid[13].X, NoteGrid[13].Y),
                "1/16" => new Point(NoteGrid[13].X, NoteGrid[13].Y + notespaceY),
                "1/32" => new Point(NoteGrid[13].X, NoteGrid[13].Y + notespaceY * 2.5),
                "1/64" => new Point(NoteGrid[13].X, NoteGrid[13].Y + notespaceY * 3),
                "1/2" => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespaceY * 2),  // TODO
                "1/1" => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespaceY * 3),  // TODO
                _ => new Point(NoteGrid[13].X, NoteGrid[13].Y - notespaceY)
            };

            return new List<Point> { openMenu, pick };
        }

        /// <summary>
        /// Beräkna klickpunkt i mitten av sloten vid nuvarande tid (utan advance).
        /// Använd denna för varje not i chord-gruppen innan advance.
        /// </summary>
        public static Point? GetClickPosition(Note note, int divisionsPerQuarter)
        {
            if (NoteGrid.Count != Cols * Rows)
                throw new InvalidOperationException("NoteGrid is not built.");

            int absRow = MapPitchToAbsoluteRowIndex(note);
            if (absRow < 0) return null;

            var relRow = MapToVisibleRow(absRow);
            if (relRow == null) return null;

            // slotWidth i 1/8-kolumner
            double slotWidthCols = SlotWidthInCols(note, divisionsPerQuarter);

            // center = vänsterkanten av sloten + halva sloten
            const double eps = 1e-4; // liten bias vänster för att inte hamna i “nästa” cell
            double centerColPos = _currentColPos + (slotWidthCols * 0.5) - eps;

            // Interpolera mellan gridkolumnerna på aktuell rad
            int col0 = (int)Math.Floor(centerColPos);
            double t = centerColPos - col0;                    // 0..1 i cellen
            int col1 = Math.Min(col0 + 1, Cols - 1);

            int idx0 = relRow.Value * Cols + Math.Clamp(col0, 0, Cols - 1);
            int idx1 = relRow.Value * Cols + Math.Clamp(col1, 0, Cols - 1);

            var p0 = NoteGrid[idx0];
            var p1 = NoteGrid[idx1];

            var pos = new Point(
                p0.X + (p1.X - p0.X) * t,
                p0.Y + (p1.Y - p0.Y) * t
            );

            // Nedersta sidan: skjut gridden 3 rader ned
            if (IsAtBottomPage())
            {
                IsAtBottom = true;
                int r0 = 0, r1 = Math.Min(1, Rows - 1);
                double rowStepY = NoteGrid[r1 * Cols + col0].Y - NoteGrid[r0 * Cols + col0].Y;
                pos.Y += rowStepY * BottomOffsetRows;
            }

            return pos;
        }

        /// <summary>
        /// Avancera tiden med notens längd (duration/divisions → fraktionella kolumner).
        /// Returnerar antal sidor (24 kolumner) att scrolla horisontellt.
        /// </summary>
        public static int AdvanceByDuration(Note note, int divisionsPerQuarter)
        {
            double steps = SlotWidthInCols(note, divisionsPerQuarter);
            return AdvanceColumns(steps);
        }

        /// <summary>
        /// Vila: avancerar bara tiden enligt notens duration.
        /// </summary>
        public static int AdvanceRest(Note restNote, int divisionsPerQuarter)
        {
            double steps = SlotWidthInCols(restNote, divisionsPerQuarter);
            return AdvanceColumns(steps);
        }

        /// <summary>
        /// Rå advance i (fraktionella) kolumner; wrappar och returnerar antal "sidor".
        /// </summary>
        public static int AdvanceColumns(double steps)
        {
            if (steps <= 0) steps = 0.0001; // undvik fastna

            _currentColPos += steps;

            if (_currentColPos < Cols) return 0;

            int pages = (int)Math.Floor(_currentColPos / Cols);
            _currentColPos = _currentColPos % Cols;
            return pages;
        }

        // ======= Intern hjälp =======

        /// <summary>
        /// duration/divisions → kvartar → kolumner (double).
        /// 1/4 -> 2.0, 1/8 -> 1.0, 1/16 -> 0.5, 1/32 -> 0.25, 1/64 -> 0.125
        /// </summary>
        private static double SlotWidthInCols(Note note, int divisionsPerQuarter)
        {
            if (divisionsPerQuarter < 1) divisionsPerQuarter = 1;
            double quarters = (double)note.Duration / divisionsPerQuarter;
            double steps = quarters * ColumnsPerQuarter;
            return Math.Max(0.0001, steps);
        }

        private static int MapPitchToAbsoluteRowIndex(Note note)
        {
            string step = note.Pitch.Step.ToUpperInvariant(); // C D E F G A B
            int alter = note.Pitch.Alter;                     // -1,0,+1
            int octave = note.Pitch.Octave;

            string sharpName = ToSharpName(step, alter);
            string token = $"{sharpName}{octave}";

            return FullNotes.IndexOf(token); // 0.., eller -1
        }

        private static int? MapToVisibleRow(int absoluteRow)
        {
            int rel = absoluteRow - _currentTopRow;
            if (rel < 0 || rel >= Rows) return null;
            return rel;
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

        private static bool IsAtBottomPage()
        {
            return _currentTopRow >= Math.Max(0, FullNotes.Count - Rows);
        }

        // (valfri) Om du i Compose vill justera “scroll upp från botten” manuellt i notches:
        public static int GetBottomScrollCompensationNotches(int rowsPerNotch = RowsPerNotch)
        {
            if (!IsAtBottomPage()) return 0;
            return (int)Math.Ceiling((double)BottomOffsetRows / Math.Max(1, rowsPerNotch)); // vanligtvis 1
        }

        // ===== Vertikal synlighets-hantering (oförändrat) =====
        public static bool EnsureVisibleForNotes(
            IEnumerable<Note> notes,
            Action<int> scrollUpOnePage,    // t.ex. (n) => IOHandler.ScrollVertical(+6) * n
            Action<int> scrollDownOnePage)  // t.ex. (n) => IOHandler.ScrollVertical(-6) * n
        {
            var rows = notes
                .Where(n => !n.IsRest)
                .Select(MapPitchToAbsoluteRowIndex)
                .Where(r => r >= 0)
                .ToList();

            if (rows.Count == 0) return false;

            int minAbs = rows.Min();
            int maxAbs = rows.Max();

            bool scrolled = false;

            if (minAbs < _currentTopRow && IsAtBottomPage())
            {
                ForceScrollToAbsoluteTop(notches =>
                {
                    for (int i = 0; i < notches; i++)
                        scrollUpOnePage(1);
                });
                scrolled = true;
            }

            while (minAbs < _currentTopRow)
            {
                scrollUpOnePage(1);
                _currentTopRow = Math.Max(0, _currentTopRow - Rows);
                scrolled = true;
            }

            while (maxAbs >= _currentTopRow + Rows)
            {
                scrollDownOnePage(1);
                int maxTop = Math.Max(0, FullNotes.Count - Rows);
                _currentTopRow = Math.Min(maxTop, _currentTopRow + Rows);
                scrolled = true;
            }

            return scrolled;
        }

        public static void ForceScrollToAbsoluteTop(Action<int> scrollUpNotches)
        {
            int notchesToTop = (_currentTopRow + RowsPerNotch - 1) / RowsPerNotch;
            int safety = 1;
            int total = Math.Max(0, notchesToTop + safety);
            if (total > 0) scrollUpNotches(total);
            _currentTopRow = 0;
            IsAtBottom = false;
        }
    }
}
