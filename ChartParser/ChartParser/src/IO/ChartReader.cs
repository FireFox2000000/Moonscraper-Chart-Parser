﻿using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace Moonscraper
{
    namespace ChartParser.IO
    {
        public static class ChartReader
        {
            const string QUOTEVALIDATE = @"""[^""\\]*(?:\\.[^""\\]*)*""";
            const string QUOTESEARCH = "\"([^\"]*)\"";
            const string FLOATSEARCH = @"[\-\+]?\d+(\.\d+)?";

            struct Anchor
            {
                public uint position;
                public float anchorTime;
            }

            struct NoteFlag
            {
                public uint position;
                public Note.Flags flag;

                public NoteFlag(uint position, Note.Flags flag)
                {
                    this.position = position;
                    this.flag = flag;
                }
            }

            public static Song ReadChart(string filepath)
            {
                try
                {
                    if (!File.Exists(filepath))
                        throw new Exception("File does not exist");

                    if (Path.GetExtension(filepath) == ".chart")
                    {
                        Song song = new Song();

                        LoadChart(song, filepath);

                        return song;
                    }
                    else
                    {
                        throw new Exception("Bad file type");
                    }

                }
                catch
                {
                    throw new Exception("Could not open file");
                }
            }

            static void LoadChart(Song song, string filepath)
            {
                bool open = false;
                string dataName = string.Empty;

                List<string> dataStrings = new List<string>();
#if TIMING_DEBUG
        float time = Time.realtimeSinceStartup;
#endif
                StreamReader sr = File.OpenText(filepath);

                // Gather lines between {} brackets and submit data
                while (!sr.EndOfStream)
                {
                    string trimmedLine = sr.ReadLine().Trim();
                    if (trimmedLine.Length <= 0)
                        continue;

                    if (trimmedLine[0] == '[' && trimmedLine[trimmedLine.Length - 1] == ']')
                    {
                        dataName = trimmedLine;
                    }
                    else if (trimmedLine == "{")
                    {
                        open = true;
                    }
                    else if (trimmedLine == "}")
                    {
                        open = false;

                        // Submit data
                        SubmitChartData(song, dataName, dataStrings, filepath);

                        dataName = string.Empty;
                        dataStrings.Clear();
                    }
                    else
                    {
                        if (open)
                        {
                            // Add data into the array
                            dataStrings.Add(trimmedLine);
                        }
                        else if (dataStrings.Count > 0 && dataName != string.Empty)
                        {
                            // Submit data
                            SubmitChartData(song, dataName, dataStrings, filepath);

                            dataName = string.Empty;
                            dataStrings.Clear();
                        }
                    }
                }

                sr.Close();

#if TIMING_DEBUG
        Debug.Log("Chart file load time: " + (Time.realtimeSinceStartup - time));
        time = Time.realtimeSinceStartup;

        LoadAllAudioClips();
#endif

                song.UpdateCache();
            }

            static void SubmitChartData(Song song, string dataName, List<string> stringData, string filePath = "")
            {
                switch (dataName)
                {
                    case ("[Song]"):
#if SONG_DEBUG
                Debug.Log("Loading chart properties");
#endif
                        SubmitDataSong(song, stringData, new FileInfo(filePath).Directory.FullName);
                        break;
                    case ("[SyncTrack]"):
#if SONG_DEBUG
                Debug.Log("Loading sync data");
#endif
                    case ("[Events]"):
#if SONG_DEBUG
                Debug.Log("Loading events data");
#endif
                        SubmitDataGlobals(song, stringData);
                        break;
                    default:
                        Song.Difficulty chartDiff;
                        int instumentStringOffset = 1;
                        const string EASY = "Easy", MEDIUM = "Medium", HARD = "Hard", EXPERT = "Expert";

                        // Determine what difficulty
                        if (dataName.Substring(1, EASY.Length) == EASY)
                        {
                            chartDiff = Song.Difficulty.Easy;
                            instumentStringOffset += EASY.Length;
                        }
                        else if (dataName.Substring(1, MEDIUM.Length) == MEDIUM)
                        {
                            chartDiff = Song.Difficulty.Medium;
                            instumentStringOffset += MEDIUM.Length;
                        }
                        else if (dataName.Substring(1, HARD.Length) == HARD)
                        {
                            chartDiff = Song.Difficulty.Hard;
                            instumentStringOffset += HARD.Length;
                        }
                        else if (dataName.Substring(1, EXPERT.Length) == EXPERT)
                        {
                            chartDiff = Song.Difficulty.Expert;
                            instumentStringOffset += EXPERT.Length;
                        }
                        else
                        {
                            // Add to the unused chart list
                            LoadUnrecognisedChart(song, dataName, stringData);
                            return;
                        }

                        switch (dataName.Substring(instumentStringOffset, dataName.Length - instumentStringOffset - 1))
                        {
                            case ("Single"):
                                LoadChart(song.GetChart(Song.Instrument.Guitar, chartDiff), stringData);
                                break;
                            case ("DoubleGuitar"):
                                LoadChart(song.GetChart(Song.Instrument.GuitarCoop, chartDiff), stringData);
                                break;
                            case ("DoubleBass"):
                                LoadChart(song.GetChart(Song.Instrument.Bass, chartDiff), stringData);
                                break;
                            case ("DoubleRhythm"):
                                LoadChart(song.GetChart(Song.Instrument.Rhythm, chartDiff), stringData);
                                break;
                            case ("Drums"):
                                LoadChart(song.GetChart(Song.Instrument.Drums, chartDiff), stringData, Song.Instrument.Drums);
                                break;
                            case ("Keyboard"):
                                LoadChart(song.GetChart(Song.Instrument.Keys, chartDiff), stringData);
                                break;
                            case ("GHLGuitar"):
                                LoadChart(song.GetChart(Song.Instrument.GHLiveGuitar, chartDiff), stringData, Song.Instrument.GHLiveGuitar);
                                break;
                            case ("GHLBass"):
                                LoadChart(song.GetChart(Song.Instrument.GHLiveBass, chartDiff), stringData, Song.Instrument.GHLiveBass);
                                break;
                            default:
                                // Add to the unused chart list
                                LoadUnrecognisedChart(song, dataName, stringData);
                                return;
                        }
                        return;
                }
            }

            static void LoadUnrecognisedChart(Song song, string dataName, List<string> stringData)
            {
                dataName = dataName.TrimStart('[');
                dataName = dataName.TrimEnd(']');
                Chart unrecognisedChart = new Chart(song, Song.Instrument.Unrecognised, dataName);
                LoadChart(unrecognisedChart, stringData, Song.Instrument.Unrecognised);
                song.unrecognisedCharts.Add(unrecognisedChart);
            }

            static void SubmitDataSong(Song song, List<string> stringData, string audioDirectory = "")
            {
#if SONG_DEBUG
        Debug.Log("Loading song properties");
#endif
#if TIMING_DEBUG
        float time = Time.realtimeSinceStartup;
#endif

                Regex nameRegex = new Regex(@"Name = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex artistRegex = new Regex(@"Artist = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex charterRegex = new Regex(@"Charter = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex offsetRegex = new Regex(@"Offset = " + FLOATSEARCH, RegexOptions.Compiled);
                Regex resolutionRegex = new Regex(@"Resolution = " + FLOATSEARCH, RegexOptions.Compiled);
                Regex player2TypeRegex = new Regex(@"Player2 = \w+", RegexOptions.Compiled);
                Regex difficultyRegex = new Regex(@"Difficulty = \d+", RegexOptions.Compiled);
                Regex lengthRegex = new Regex(@"Length = " + FLOATSEARCH, RegexOptions.Compiled);
                Regex previewStartRegex = new Regex(@"PreviewStart = " + FLOATSEARCH, RegexOptions.Compiled);
                Regex previewEndRegex = new Regex(@"PreviewEnd = " + FLOATSEARCH, RegexOptions.Compiled);
                Regex genreRegex = new Regex(@"Genre = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex yearRegex = new Regex(@"Year = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex albumRegex = new Regex(@"Album = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex mediaTypeRegex = new Regex(@"MediaType = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex musicStreamRegex = new Regex(@"MusicStream = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex guitarStreamRegex = new Regex(@"GuitarStream = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex bassStreamRegex = new Regex(@"BassStream = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex rhythmStreamRegex = new Regex(@"RhythmStream = " + QUOTEVALIDATE, RegexOptions.Compiled);
                Regex drumStreamRegex = new Regex(@"DrumStream = " + QUOTEVALIDATE, RegexOptions.Compiled);

                Metadata metaData = song.metaData;

                try
                {
                    foreach (string line in stringData)
                    {
                        // Name = "5000 Robots"
                        if (nameRegex.IsMatch(line))
                        {
                            metaData.name = Regex.Matches(line, QUOTESEARCH)[0].ToString().Trim('"');
                        }

                        // Artist = "TheEruptionOffer"
                        else if (artistRegex.IsMatch(line))
                        {
                            metaData.artist = Regex.Matches(line, QUOTESEARCH)[0].ToString().Trim('"');
                        }

                        // Charter = "TheEruptionOffer"
                        else if (charterRegex.IsMatch(line))
                        {
                            metaData.charter = Regex.Matches(line, QUOTESEARCH)[0].ToString().Trim('"');
                        }

                        // Album = "Rockman Holic"
                        else if (albumRegex.IsMatch(line))
                        {
                            metaData.album = Regex.Matches(line, QUOTESEARCH)[0].ToString().Trim('"');
                        }

                        // Offset = 0
                        else if (offsetRegex.IsMatch(line))
                        {
                            song.offset = float.Parse(Regex.Matches(line, FLOATSEARCH)[0].ToString());
                        }

                        // Resolution = 192
                        else if (resolutionRegex.IsMatch(line))
                        {
                            song.resolution = short.Parse(Regex.Matches(line, FLOATSEARCH)[0].ToString());
                        }

                        // Player2 = bass
                        else if (player2TypeRegex.IsMatch(line))
                        {
                            string[] instrumentTypes = { "Bass", "Rhythm" };
                            string split = line.Split('=')[1].Trim();

                            foreach (string instrument in instrumentTypes)
                            {
                                if (split.Equals(instrument, System.StringComparison.InvariantCultureIgnoreCase))
                                {
                                    metaData.player2 = instrument;
                                    break;
                                }
                            }
                        }

                        // Difficulty = 0
                        else if (difficultyRegex.IsMatch(line))
                        {
                            metaData.difficulty = int.Parse(Regex.Matches(line, @"\d+")[0].ToString());
                        }

                        // Length = 300
                        else if (lengthRegex.IsMatch(line))
                        {
                            song.manualLength = true;
                            song.length = float.Parse(Regex.Matches(line, FLOATSEARCH)[0].ToString());
                        }

                        // PreviewStart = 0.00
                        else if (previewStartRegex.IsMatch(line))
                        {
                            metaData.previewStart = float.Parse(Regex.Matches(line, FLOATSEARCH)[0].ToString());
                        }

                        // PreviewEnd = 0.00
                        else if (previewEndRegex.IsMatch(line))
                        {
                            metaData.previewEnd = float.Parse(Regex.Matches(line, FLOATSEARCH)[0].ToString());
                        }

                        // Genre = "rock"
                        else if (genreRegex.IsMatch(line))
                        {
                            metaData.genre = Regex.Matches(line, QUOTESEARCH)[0].ToString().Trim('"');
                        }

                        // MediaType = "cd"
                        else if (mediaTypeRegex.IsMatch(line))
                        {
                            metaData.mediatype = Regex.Matches(line, QUOTESEARCH)[0].ToString().Trim('"');
                        }

                        else if (yearRegex.IsMatch(line))
                            metaData.year = Regex.Replace(Regex.Matches(line, QUOTESEARCH)[0].ToString().Trim('"'), @"\D", "");

                        // MusicStream = "ENDLESS REBIRTH.ogg"
                        else if (musicStreamRegex.IsMatch(line))
                        {
                            AudioLoadFromChart(song, Song.AudioInstrument.Song, line, audioDirectory);
                        }
                        else if (guitarStreamRegex.IsMatch(line))
                        {
                            AudioLoadFromChart(song, Song.AudioInstrument.Guitar, line, audioDirectory);
                        }
                        else if (bassStreamRegex.IsMatch(line))
                        {
                            AudioLoadFromChart(song, Song.AudioInstrument.Bass, line, audioDirectory);
                        }
                        else if (rhythmStreamRegex.IsMatch(line))
                        {
                            AudioLoadFromChart(song, Song.AudioInstrument.Rhythm, line, audioDirectory);
                        }
                        else if (drumStreamRegex.IsMatch(line))
                        {
                            AudioLoadFromChart(song, Song.AudioInstrument.Drum, line, audioDirectory);
                        }
                    }

#if TIMING_DEBUG
            Debug.Log("Song properties load time: " + (Time.realtimeSinceStartup - time));
#endif
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            static void AudioLoadFromChart(Song song, Song.AudioInstrument streamAudio, string line, string audioDirectory)
            {
                string audioFilepath = Regex.Matches(line, QUOTESEARCH)[0].ToString().Trim('"');

                // Check if it's already the full path. If not, make it relative to the chart file.
                if (!File.Exists(audioFilepath))
                    audioFilepath = audioDirectory + "\\" + audioFilepath;

                if (File.Exists(audioFilepath) && Globals.validateExtension(audioFilepath, Globals.validAudioExtensions))
                    song.SetAudioLocation(streamAudio, Path.GetFullPath(audioFilepath));
            }

            static void SubmitDataGlobals(Song song, List<string> stringData)
            {
                const int TEXT_POS_TICK = 0;
                const int TEXT_POS_EVENT_TYPE = 2;
                const int TEXT_POS_DATA_1 = 3;

#if TIMING_DEBUG
        float time = Time.realtimeSinceStartup;
#endif

                List<Anchor> anchorData = new List<Anchor>();

                foreach (string line in stringData)
                {
                    string[] stringSplit = line.Split(' ');
                    uint position;
                    string eventType;
                    if (stringSplit.Length > TEXT_POS_DATA_1 && uint.TryParse(stringSplit[TEXT_POS_TICK], out position))
                    {
                        eventType = stringSplit[TEXT_POS_EVENT_TYPE];
                        eventType = eventType.ToLower();
                    }
                    else
                        continue;

                    switch (eventType)
                    {
                        case ("ts"):
                            uint numerator;
                            uint denominator = 2;

                            if (!uint.TryParse(stringSplit[TEXT_POS_DATA_1], out numerator))
                                continue;

                            if (stringSplit.Length > TEXT_POS_DATA_1 + 1 && !uint.TryParse(stringSplit[TEXT_POS_DATA_1 + 1], out denominator))
                                continue;

                            song.Add(new TimeSignature(position, numerator, (uint)(System.Math.Pow(2, denominator))), false);
                            break;
                        case ("b"):
                            uint value;
                            if (!uint.TryParse(stringSplit[TEXT_POS_DATA_1], out value))
                                continue;

                            song.Add(new BPM(position, value), false);
                            break;
                        case ("e"):
                            if (stringSplit.Length > TEXT_POS_DATA_1 + 1 && stringSplit[TEXT_POS_DATA_1] == "\"section")
                            {
                                string title = string.Empty;// = stringSplit[TEXT_POS_DATA_1 + 1].Trim('"');
                                for (int i = TEXT_POS_DATA_1 + 1; i < stringSplit.Length; ++i)
                                {
                                    title += stringSplit[i].Trim('"');
                                    if (i < stringSplit.Length - 1)
                                        title += " ";
                                }
                                song.Add(new Section(title, position), false);
                            }
                            else
                            {
                                string title = stringSplit[TEXT_POS_DATA_1].Trim('"');
                                song.Add(new Event(title, position), false);
                            }
                            break;
                        case ("a"):
                            ulong anchorValue;
                            if (ulong.TryParse(stringSplit[TEXT_POS_DATA_1], out anchorValue))
                            {
                                Anchor a;
                                a.position = position;
                                a.anchorTime = (float)(anchorValue / 1000000.0d);
                                anchorData.Add(a);
                            }
                            break;
                        default:
                            break;
                    } 
                }

                BPM[] bpms = song.syncTrack.OfType<BPM>().ToArray();        // BPMs are currently uncached
                foreach (Anchor anchor in anchorData)
                {
                    int arrayPos = SongObjectHelper.FindClosestPosition(anchor.position, bpms);
                    if (bpms[arrayPos].position == anchor.position)
                    {
                        bpms[arrayPos].anchor = anchor.anchorTime;
                    }
                    else
                    {
                        // Create a new anchored bpm
                        uint value;
                        if (bpms[arrayPos].position > anchor.position)
                            value = bpms[arrayPos - 1].value;
                        else
                            value = bpms[arrayPos].value;

                        BPM anchoredBPM = new BPM(anchor.position, value);
                        anchoredBPM.anchor = anchor.anchorTime;
                    }
                }
#if TIMING_DEBUG
        Debug.Log("Synctrack load time: " + (Time.realtimeSinceStartup - time));
#endif
            }

            /*************************************************************************************
                Chart Loading
            **************************************************************************************/


            public static void LoadChart(Chart chart, List<string> data, Song.Instrument instrument = Song.Instrument.Guitar)
            {
                LoadChart(chart, data.ToArray(), instrument);
            }

            public static void LoadChart(Chart chart, string[] data, Song.Instrument instrument = Song.Instrument.Guitar)
            {
#if TIMING_DEBUG
        float time = Time.realtimeSinceStartup;
#endif
                List<NoteFlag> flags = new List<NoteFlag>();

                chart.SetCapacity(data.Length);

                const int SPLIT_POSITION = 0;
                const int SPLIT_EQUALITY = 1;
                const int SPLIT_TYPE = 2;
                const int SPLIT_VALUE = 3;
                const int SPLIT_LENGTH = 4;

                try
                {
                    // Load notes, collect flags
                    foreach (string line in data)
                    {
                        try
                        {
                            string[] splitString = line.Split(' ');
                            uint position = uint.Parse(splitString[SPLIT_POSITION]);
                            string type = splitString[SPLIT_TYPE].ToLower();

                            switch (type)
                            {
                                case ("n"):
                                    // Split string to get note information
                                    string[] digits = splitString;

                                    int fret_type = int.Parse(digits[SPLIT_VALUE]);
                                    uint length = uint.Parse(digits[SPLIT_LENGTH]);

                                    if (instrument == Song.Instrument.Unrecognised)
                                    {
                                        Note newNote = new Note(position, fret_type, length);
                                        chart.Add(newNote, false);
                                    }
                                    else if (instrument == Song.Instrument.Drums)
                                        LoadDrumNote(chart, position, fret_type, length);
                                    else if (instrument == Song.Instrument.GHLiveGuitar || instrument == Song.Instrument.GHLiveBass)
                                        LoadGHLiveNote(chart, position, fret_type, length, flags);
                                    else
                                        LoadStandardNote(chart, position, fret_type, length, flags);
                                    break;

                                case ("s"):
                                    fret_type = int.Parse(splitString[SPLIT_VALUE]);

                                    if (fret_type != 2)
                                        continue;

                                    length = uint.Parse(splitString[SPLIT_LENGTH]);

                                    chart.Add(new Starpower(position, length), false);
                                    break;

                                case ("e"):
                                    string[] strings = splitString;
                                    string eventName = strings[SPLIT_VALUE];
                                    chart.Add(new ChartEvent(position, eventName), false);
                                    break;
                                default:
                                    break;
                            }

                        }
                        catch (System.Exception e)
                        {
                            Console.WriteLine("Error parsing line \"" + line + "\": " + e);
                        }
                    }
                    chart.UpdateCache();

                    // Load flags
                    foreach (NoteFlag flag in flags)
                    {
                        Note[] notesToAddFlagTo = SongObjectHelper.FindObjectsAtPosition(flag.position, chart.notes);
                        if (notesToAddFlagTo.Length > 0)
                            Note.groupAddFlags(notesToAddFlagTo, flag.flag);
                    }
#if TIMING_DEBUG
            Debug.Log("Chart load time: " + (Time.realtimeSinceStartup - time));
#endif
                }
                catch (System.Exception e)
                {
                    // Bad load, most likely a parsing error
                    Console.WriteLine(e.Message);
                    chart.Clear();
                }
            }

            static void LoadStandardNote(Chart chart, uint position, int noteNumber, uint length, List<NoteFlag> flagsList)
            {
                Note.Fret_Type? noteFret = null;
                switch (noteNumber)
                {
                    case (0):
                        noteFret = Note.Fret_Type.GREEN;
                        break;
                    case (1):
                        noteFret = Note.Fret_Type.RED;
                        break;
                    case (2):
                        noteFret = Note.Fret_Type.YELLOW;
                        break;
                    case (3):
                        noteFret = Note.Fret_Type.BLUE;
                        break;
                    case (4):
                        noteFret = Note.Fret_Type.ORANGE;
                        break;
                    case (5):
                        NoteFlag forcedFlag = new NoteFlag(position, Note.Flags.FORCED);
                        flagsList.Add(forcedFlag);
                        break;
                    case (6):
                        NoteFlag tapFlag = new NoteFlag(position, Note.Flags.TAP);
                        flagsList.Add(tapFlag);
                        break;
                    case (7):
                        noteFret = Note.Fret_Type.OPEN;
                        break;
                    default:
                        return;
                }

                if (noteFret != null)
                {
                    Note newNote = new Note(position, (int)noteFret, length);
                    chart.Add(newNote, false);
                }
            }

            static void LoadDrumNote(Chart chart, uint position, int noteNumber, uint length)
            {
                Note.Drum_Fret_Type? noteFret = null;
                switch (noteNumber)
                {
                    case (0):
                        noteFret = Note.Drum_Fret_Type.KICK;
                        break;
                    case (1):
                        noteFret = Note.Drum_Fret_Type.RED;
                        break;
                    case (2):
                        noteFret = Note.Drum_Fret_Type.YELLOW;
                        break;
                    case (3):
                        noteFret = Note.Drum_Fret_Type.BLUE;
                        break;
                    case (4):
                        noteFret = Note.Drum_Fret_Type.ORANGE;
                        break;
                    case (5):
                        noteFret = Note.Drum_Fret_Type.GREEN;
                        break;
                    default:
                        return;
                }

                if (noteFret != null)
                {
                    Note newNote = new Note(position, (int)noteFret, length);
                    chart.Add(newNote, false);
                }
            }

            static void LoadGHLiveNote(Chart chart, uint position, int noteNumber, uint length, List<NoteFlag> flagsList)
            {
                Note.GHLive_Fret_Type? noteFret = null;
                switch (noteNumber)
                {
                    case (0):
                        noteFret = Note.GHLive_Fret_Type.WHITE_1;
                        break;
                    case (1):
                        noteFret = Note.GHLive_Fret_Type.WHITE_2;
                        break;
                    case (2):
                        noteFret = Note.GHLive_Fret_Type.WHITE_3;
                        break;
                    case (3):
                        noteFret = Note.GHLive_Fret_Type.BLACK_1;
                        break;
                    case (4):
                        noteFret = Note.GHLive_Fret_Type.BLACK_2;
                        break;
                    case (5):
                        flagsList.Add(new NoteFlag(position, Note.Flags.FORCED));
                        break;
                    case (6):
                        flagsList.Add(new NoteFlag(position, Note.Flags.TAP));
                        break;
                    case (7):
                        noteFret = Note.GHLive_Fret_Type.OPEN;
                        break;
                    case (8):
                        noteFret = Note.GHLive_Fret_Type.BLACK_3;
                        break;
                    default:
                        return;
                }

                if (noteFret != null)
                {
                    Note newNote = new Note(position, (int)noteFret, length);
                    chart.Add(newNote, false);
                }
            }
        }
    }
}