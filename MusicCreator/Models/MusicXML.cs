using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCreator.Models
{
    // Huvudobjekt
    public class MusicXML
    {
        public Work Work { get; set; }
        public Identification Identification { get; set; }
        public List<ScorePart> PartList { get; set; } = new List<ScorePart>();
        public List<Part> Parts { get; set; } = new List<Part>();
    }

    // Titelinformation
    public class Work
    {
        public string WorkTitle { get; set; }
    }

    // Metadata
    public class Identification
    {
        public Encoding Encoding { get; set; }
    }

    public class Encoding
    {
        public string Software { get; set; }
        public DateTime EncodingDate { get; set; }
    }

    // Instrumentlista
    public class ScorePart
    {
        public string Id { get; set; }
        public string PartName { get; set; }
        public ScoreInstrument Instrument { get; set; }
        public MidiInstrument Midi { get; set; }
    }

    public class ScoreInstrument
    {
        public string InstrumentName { get; set; }
    }

    public class MidiInstrument
    {
        public int MidiChannel { get; set; }
        public int MidiProgram { get; set; }
    }

    // Stämma med takter
    public class Part
    {
        public string Id { get; set; }
        public List<Measure> Measures { get; set; } = new List<Measure>();
    }

    public class Measure
    {
        public int Number { get; set; }
        public Attributes Attributes { get; set; }
        public List<Note> Notes { get; set; } = new List<Note>();
        public Barline Barline { get; set; }
        public Direction Direction { get; set; }
    }

    public class Attributes
    {
        public int Divisions { get; set; }
        public Key Key { get; set; }
        public Time Time { get; set; }
        public Clef Clef { get; set; }
    }

    public class Key
    {
        public int Fifths { get; set; } // antal kors eller b-förtecken
    }

    public class Time
    {
        public int Beats { get; set; }      // t.ex. 4
        public int BeatType { get; set; }   // t.ex. 4 för 4/4
    }

    public class Clef
    {
        public string Sign { get; set; } // G eller F
        public int Line { get; set; }    // linjeposition
    }

    public class Note
    {
        public bool IsRest { get; set; }
        public Pitch Pitch { get; set; }
        public int Duration { get; set; }
        public string Type { get; set; } // quarter, half, etc.
        public bool Chord { get; set; }

        // NYTT:
        public int Voice { get; set; } // för att särskilja flera stämmor
        public List<Lyric> Lyrics { get; set; } = new List<Lyric>();
        public List<Notation> Notations { get; set; } = new List<Notation>();
        public List<Dynamics> Dynamics { get; set; } = new List<Dynamics>();
        public bool TieStart { get; set; }
        public bool TieStop { get; set; }
    }

    public class Pitch
    {
        public string Step { get; set; } // C, D, E...
        public int Octave { get; set; }

        // NYTT:
        public int Alter { get; set; } // -1 = b, 1 = #, 0 = naturlig
    }

    public class Lyric
    {
        public int Number { get; set; }
        public string Syllabic { get; set; } // single, begin, middle, end
        public string Text { get; set; }
    }

    public class Notation
    {
        public bool SlurStart { get; set; }
        public bool SlurStop { get; set; }
        public bool Accent { get; set; }
        public bool Staccato { get; set; }
    }

    public class Dynamics
    {
        public string Type { get; set; } // p, f, mf, ff, cresc, decresc
    }

    public class Barline
    {
        public string Location { get; set; } // t.ex. right
        public string BarStyle { get; set; } // t.ex. light-heavy
    }

    public class Direction
    {
        public string Placement { get; set; } // above, below
        public Metronome Metronome { get; set; }
        public int Tempo { get; set; }
    }

    public class Metronome
    {
        public string BeatUnit { get; set; } // quarter, eighth
        public int PerMinute { get; set; }
    }
}