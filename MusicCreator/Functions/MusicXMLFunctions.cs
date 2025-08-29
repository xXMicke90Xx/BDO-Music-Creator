using MusicCreator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Encoding = MusicCreator.Models.Encoding;

namespace MusicCreator.Functions
{
    public static class MusicXMLFunctions
    {
        public static MusicXML LoadFromFile(string path)
        {
            XDocument doc = XDocument.Load(path);

            var musicXML = new MusicXML();

            // --- Work (titel) ---
            var workEl = doc.Root.Element("work");
            if (workEl != null)
            {
                musicXML.Work = new Work
                {
                    WorkTitle = (string)workEl.Element("work-title")
                };
            }

            // --- Identification ---
            var identificationEl = doc.Root.Element("identification");
            if (identificationEl != null)
            {
                musicXML.Identification = new Identification
                {
                    Encoding = new Encoding
                    {
                        Software = (string)identificationEl.Element("encoding")?.Element("software"),
                        EncodingDate = DateTime.TryParse((string)identificationEl.Element("encoding")?.Element("encoding-date"), out DateTime dt) ? dt : DateTime.MinValue
                    }
                };
            }

            // --- Part-list (instrument) ---
            var partListEl = doc.Root.Element("part-list");
            if (partListEl != null)
            {
                foreach (var scorePartEl in partListEl.Elements("score-part"))
                {
                    var scorePart = new ScorePart
                    {
                        Id = (string)scorePartEl.Attribute("id"),
                        PartName = (string)scorePartEl.Element("part-name"),
                        Instrument = new ScoreInstrument
                        {
                            InstrumentName = (string)scorePartEl.Element("score-instrument")?.Element("instrument-name")
                        },
                        Midi = new MidiInstrument
                        {
                            MidiChannel = (int?)scorePartEl.Element("midi-instrument")?.Element("midi-channel") ?? 1,
                            MidiProgram = (int?)scorePartEl.Element("midi-instrument")?.Element("midi-program") ?? 1
                        }
                    };

                    musicXML.PartList.Add(scorePart);
                }
            }

            // --- Parts med measures och notes ---
            foreach (var partEl in doc.Root.Elements("part"))
            {
                var part = new Part
                {
                    Id = (string)partEl.Attribute("id")
                };

                foreach (var measureEl in partEl.Elements("measure"))
                {
                    var measure = new Measure
                    {
                        Number = (int)measureEl.Attribute("number")
                    };

                    // Attributes
                    var attrEl = measureEl.Element("attributes");
                    if (attrEl != null)
                    {
                        measure.Attributes = new Attributes
                        {
                            Divisions = (int?)attrEl.Element("divisions") ?? 1,
                            Key = new Key
                            {
                                Fifths = (int?)attrEl.Element("key")?.Element("fifths") ?? 0
                            },
                            Time = new Time
                            {
                                Beats = (int?)attrEl.Element("time")?.Element("beats") ?? 4,
                                BeatType = (int?)attrEl.Element("time")?.Element("beat-type") ?? 4
                            },
                            Clef = new Clef
                            {
                                Sign = (string)attrEl.Element("clef")?.Element("sign"),
                                Line = (int?)attrEl.Element("clef")?.Element("line") ?? 2
                            }
                        };
                    }

                    // Notes
                    foreach (var noteEl in measureEl.Elements("note"))
                    {
                        var note = new Note();

                        // Pitch
                        var pitchEl = noteEl.Element("pitch");
                        if (pitchEl != null)
                        {
                            note.Pitch = new Pitch
                            {
                                Step = (string)pitchEl.Element("step"),
                                Octave = (int?)pitchEl.Element("octave") ?? 4,
                                Alter = (int?)pitchEl.Element("alter") ?? 0
                            };
                        }

                        note.Duration = (int?)noteEl.Element("duration") ?? 1;
                        note.Type = (string)noteEl.Element("type");
                        note.Chord = noteEl.Element("chord") != null;
                        note.Voice = (int?)noteEl.Element("voice") ?? 1;

                        // Lyrics
                        foreach (var lyricEl in noteEl.Elements("lyric"))
                        {
                            note.Lyrics.Add(new Lyric
                            {
                                Number = (int?)lyricEl.Attribute("number") ?? 1,
                                Syllabic = (string)lyricEl.Element("syllabic"),
                                Text = (string)lyricEl.Element("text")
                            });
                        }

                        // Notations (slur, tie, articulations)
                        foreach (var notationEl in noteEl.Elements("notations"))
                        {
                            var n = new Notation
                            {
                                SlurStart = notationEl.Elements("slur").Any(s => (string)s.Attribute("type") == "start"),
                                SlurStop = notationEl.Elements("slur").Any(s => (string)s.Attribute("type") == "stop"),
                                Accent = notationEl.Elements("articulations").Elements("accent").Any(),
                                Staccato = notationEl.Elements("articulations").Elements("staccato").Any()
                            };
                            note.Notations.Add(n);
                        }

                        // Ties
                        foreach (var tieEl in noteEl.Elements("tie"))
                        {
                            var type = (string)tieEl.Attribute("type");
                            if (type == "start") note.TieStart = true;
                            if (type == "stop") note.TieStop = true;
                        }

                        // Dynamics
                        foreach (var dynEl in noteEl.Elements("direction"))
                        {
                            var dyn = dynEl.Element("direction-type")?.Element("dynamics")?.Elements().FirstOrDefault();
                            if (dyn != null)
                            {
                                note.Dynamics.Add(new Dynamics { Type = dyn.Name.LocalName });
                            }
                        }

                        measure.Notes.Add(note);
                    }

                    part.Measures.Add(measure);
                }

                musicXML.Parts.Add(part);
            }

            return musicXML;
        }
    }

}
