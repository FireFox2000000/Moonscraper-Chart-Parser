﻿// Copyright (c) 2016-2017 Alexander Ong
// See LICENSE in project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace Moonscraper
{
    namespace ChartParser.IO
    {
        public class ChartWriter
        {
            string path;

            public ChartWriter(string path)
            {
                this.path = path;
            }

            delegate string GetAudioStreamSaveString(Song.AudioInstrument audio);
            public void Write(Song song, ExportOptions exportOptions, out string errorList)
            {
                song.UpdateCache();
                errorList = string.Empty;
                string saveString = string.Empty;

                try
                {
                    string musicString = string.Empty;
                    string guitarString = string.Empty;
                    string bassString = string.Empty;
                    string rhythmString = string.Empty;
                    string drumString = string.Empty;

                    GetAudioStreamSaveString GetSaveAudioString = audio =>
                    {
                        string audioString;
                        int arrayIndex = (int)audio;
                        string audioLocation = song.GetAudioLocation(audio);

                        if (song.GetAudioIsLoaded(audio) && Path.GetDirectoryName(audioLocation).Replace("\\", "/") == Path.GetDirectoryName(path).Replace("\\", "/"))
                            audioString = Path.GetFileName(audioLocation);
                        else
                            audioString = audioLocation;

                        return audioString;
                    };

                    musicString = GetSaveAudioString(Song.AudioInstrument.Song);
                    guitarString = GetSaveAudioString(Song.AudioInstrument.Guitar);
                    bassString = GetSaveAudioString(Song.AudioInstrument.Bass);
                    rhythmString = GetSaveAudioString(Song.AudioInstrument.Rhythm);
                    drumString = GetSaveAudioString(Song.AudioInstrument.Drum);

                    // Song properties
                    Console.WriteLine("Writing song properties");
                    saveString += "[Song]" + Globals.LINE_ENDING + "{" + Globals.LINE_ENDING;
                    saveString += GetPropertiesStringWithoutAudio(song, exportOptions);

                    // Song audio
                    if (song.GetAudioIsLoaded(Song.AudioInstrument.Song) || (musicString != null && musicString != string.Empty))
                        saveString += Globals.TABSPACE + "MusicStream = \"" + musicString + "\"" + Globals.LINE_ENDING;

                    if (song.GetAudioIsLoaded(Song.AudioInstrument.Guitar) || (guitarString != null && guitarString != string.Empty))
                        saveString += Globals.TABSPACE + "GuitarStream = \"" + guitarString + "\"" + Globals.LINE_ENDING;

                    if (song.GetAudioIsLoaded(Song.AudioInstrument.Bass) || (bassString != null && bassString != string.Empty))
                        saveString += Globals.TABSPACE + "BassStream = \"" + bassString + "\"" + Globals.LINE_ENDING;

                    if (song.GetAudioIsLoaded(Song.AudioInstrument.Rhythm) || (rhythmString != null && rhythmString != string.Empty))
                        saveString += Globals.TABSPACE + "RhythmStream = \"" + rhythmString + "\"" + Globals.LINE_ENDING;

                    if (song.GetAudioIsLoaded(Song.AudioInstrument.Drum) || (drumString != null && drumString != string.Empty))
                        saveString += Globals.TABSPACE + "DrumStream = \"" + drumString + "\"" + Globals.LINE_ENDING;

                    saveString += "}" + Globals.LINE_ENDING;
                }
                catch (System.Exception e)
                {
                    System.Diagnostics.Debugger.Break();
                    string error = "Error with saving song properties: " + e.Message;
                    Console.WriteLine(error);
                    errorList += error + Globals.LINE_ENDING;

                    saveString = string.Empty;  // Clear all the song properties because we don't want braces left open, which will screw up the loading of the chart
                }

                // SyncTrack
                Console.WriteLine("Writing synctrack");
                saveString += "[SyncTrack]" + Globals.LINE_ENDING + "{" + Globals.LINE_ENDING;
                if (exportOptions.tickOffset > 0)
                {
                    saveString += new BPM().GetSaveString();
                    saveString += new TimeSignature().GetSaveString();
                }

                saveString += GetSaveString(song, song.syncTrack, exportOptions, ref errorList);
                saveString += "}" + Globals.LINE_ENDING;

                // Events
                Console.WriteLine("Writing events");
                saveString += "[Events]" + Globals.LINE_ENDING + "{" + Globals.LINE_ENDING;
                saveString += GetSaveString(song, song.eventsAndSections, exportOptions, ref errorList);
                saveString += "}" + Globals.LINE_ENDING;

                // Charts      
                foreach (Song.Instrument instrument in Enum.GetValues(typeof(Song.Instrument)))
                {
                    string instrumentSaveString = string.Empty;
                    switch (instrument)
                    {
                        case (Song.Instrument.Guitar):
                            instrumentSaveString = "Single";
                            break;
                        case (Song.Instrument.GuitarCoop):
                            instrumentSaveString = "DoubleGuitar";
                            break;
                        case (Song.Instrument.Bass):
                            instrumentSaveString = "DoubleBass";
                            break;
                        case (Song.Instrument.Rhythm):
                            instrumentSaveString = "DoubleRhythm";
                            break;
                        case (Song.Instrument.Drums):
                            instrumentSaveString = "Drums";
                            break;
                        case (Song.Instrument.Keys):
                            instrumentSaveString = "Keyboard";
                            break;
                        case (Song.Instrument.GHLiveGuitar):
                            instrumentSaveString = "GHLGuitar";
                            break;
                        case (Song.Instrument.GHLiveBass):
                            instrumentSaveString = "GHLBass";
                            break;
                        default:
                            continue;
                    }

                    foreach (Song.Difficulty difficulty in Enum.GetValues(typeof(Song.Difficulty)))
                    {
                        string difficultySaveString = difficulty.ToString();

                        string chartString = GetSaveString(song, song.GetChart(instrument, difficulty).chartObjects, exportOptions, ref errorList, instrument);

                        if (chartString == string.Empty)
                        {

                            if (exportOptions.copyDownEmptyDifficulty)
                            {
                                Song.Difficulty chartDiff = difficulty;
                                bool exit = false;
                                while (chartString == string.Empty)
                                {

                                    switch (chartDiff)
                                    {
                                        case (Song.Difficulty.Easy):
                                            chartDiff = Song.Difficulty.Medium;
                                            break;
                                        case (Song.Difficulty.Medium):
                                            chartDiff = Song.Difficulty.Hard;
                                            break;
                                        case (Song.Difficulty.Hard):
                                            chartDiff = Song.Difficulty.Expert;
                                            break;
                                        case (Song.Difficulty.Expert):
                                        default:
                                            exit = true;
                                            break;
                                    }

                                    chartString = GetSaveString(song, song.GetChart(instrument, chartDiff).chartObjects, exportOptions, ref errorList, instrument);

                                    if (exit)
                                        break;
                                }

                                if (exit)
                                    continue;
                            }
                            else
                                continue;

                        }

                        string seperator = "[" + difficultySaveString + instrumentSaveString + "]";
                        saveString += seperator + Globals.LINE_ENDING + "{" + Globals.LINE_ENDING;
                        saveString += chartString;
                        saveString += "}" + Globals.LINE_ENDING;
                    }
                }

                // Unrecognised charts
                foreach (Chart chart in song.unrecognisedCharts)
                {
                    string chartString = GetSaveString(song, chart.chartObjects, exportOptions, ref errorList, Song.Instrument.Unrecognised);

                    string seperator = "[" + chart.name + "]";
                    saveString += seperator + Globals.LINE_ENDING + "{" + Globals.LINE_ENDING;
                    saveString += chartString;
                    saveString += "}" + Globals.LINE_ENDING;
                }

                try
                {
                    // Save to file
                    File.WriteAllText(path, saveString);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            string GetPropertiesStringWithoutAudio(Song song, ExportOptions exportOptions)
            {
                string saveString = string.Empty;
                if (exportOptions.targetResolution <= 0)
                    exportOptions.targetResolution = song.resolution;

                Metadata metaData = song.metaData;

                // Song properties  
                if (metaData.name != string.Empty)
                    saveString += Globals.TABSPACE + "Name = \"" + metaData.name + "\"" + Globals.LINE_ENDING;
                if (metaData.artist != string.Empty)
                    saveString += Globals.TABSPACE + "Artist = \"" + metaData.artist + "\"" + Globals.LINE_ENDING;
                if (metaData.charter != string.Empty)
                    saveString += Globals.TABSPACE + "Charter = \"" + metaData.charter + "\"" + Globals.LINE_ENDING;
                if (metaData.album != string.Empty)
                    saveString += Globals.TABSPACE + "Album = \"" + metaData.album + "\"" + Globals.LINE_ENDING;
                if (metaData.year != string.Empty)
                    saveString += Globals.TABSPACE + "Year = \", " + metaData.year + "\"" + Globals.LINE_ENDING;
                saveString += Globals.TABSPACE + "Offset = " + song.offset + Globals.LINE_ENDING;

                saveString += Globals.TABSPACE + "Resolution = " + exportOptions.targetResolution + Globals.LINE_ENDING;
                if (metaData.player2 != string.Empty)
                    saveString += Globals.TABSPACE + "Player2 = " + metaData.player2.ToLower() + Globals.LINE_ENDING;
                saveString += Globals.TABSPACE + "Difficulty = " + metaData.difficulty + Globals.LINE_ENDING;
                if (song.manualLength)
                    saveString += Globals.TABSPACE + "Length = " + song.length + Globals.LINE_ENDING;
                saveString += Globals.TABSPACE + "PreviewStart = " + metaData.previewStart + Globals.LINE_ENDING;
                saveString += Globals.TABSPACE + "PreviewEnd = " + metaData.previewEnd + Globals.LINE_ENDING;
                if (metaData.genre != string.Empty)
                    saveString += Globals.TABSPACE + "Genre = \"" + metaData.genre + "\"" + Globals.LINE_ENDING;
                if (metaData.mediatype != string.Empty)
                    saveString += Globals.TABSPACE + "MediaType = \"" + metaData.mediatype + "\"" + Globals.LINE_ENDING;

                return saveString;
            }

            string GetSaveString<T>(Song song, T[] list, ExportOptions exportOptions, ref string out_errorList, Song.Instrument instrument = Song.Instrument.Guitar) where T : SongObject
            {
                System.Text.StringBuilder saveString = new System.Text.StringBuilder();

                //string saveString = string.Empty;

                float resolutionScaleRatio = song.ResolutionScaleRatio(exportOptions.targetResolution);

                for (int i = 0; i < list.Length; ++i)
                //foreach (SongObject songObject in list)
                {
                    SongObject songObject = list[i];
                    try
                    {
                        uint tick = (uint)System.Math.Round(songObject.position * resolutionScaleRatio) + exportOptions.tickOffset;
                        saveString.Append(Globals.TABSPACE + tick);

                        switch ((SongObject.ID)songObject.classID)
                        {
                            case (SongObject.ID.BPM):
                                BPM bpm = songObject as BPM;
                                if (bpm.anchor != null)
                                {
                                    saveString.Append(" = A " + (uint)((double)bpm.anchor * 1000000));
                                    saveString.Append(Globals.LINE_ENDING);
                                    saveString.Append(Globals.TABSPACE + tick);
                                }

                                saveString.Append(" = B " + bpm.value);
                                break;

                            case (SongObject.ID.TimeSignature):
                                TimeSignature ts = songObject as TimeSignature;
                                saveString.Append(" = TS " + ts.numerator);

                                if (ts.denominator != 4)
                                    saveString.Append(" " + (uint)System.Math.Log(ts.denominator, 2));
                                break;

                            case (SongObject.ID.Section):
                                Section section = songObject as Section;
                                saveString.Append(" = E \"section " + section.title + "\"");
                                break;

                            case (SongObject.ID.Event):
                                Event songEvent = songObject as Event;
                                saveString.Append(" = E \"" + songEvent.title + "\"");
                                break;

                            case (SongObject.ID.ChartEvent):
                                ChartEvent chartEvent = songObject as ChartEvent;
                                saveString.Append(" = E " + chartEvent.eventName);
                                break;

                            case (SongObject.ID.Starpower):
                                Starpower sp = songObject as Starpower;
                                saveString.Append(" = S 2 " + (uint)System.Math.Round(sp.length * resolutionScaleRatio));
                                break;

                            case (SongObject.ID.Note):
                                Note note = songObject as Note;
                                int fretNumber;

                                if (instrument != Song.Instrument.Unrecognised)
                                {
                                    if (instrument == Song.Instrument.Drums)
                                        fretNumber = GetDrumsSaveNoteNumber(note);

                                    else if (instrument == Song.Instrument.GHLiveGuitar || instrument == Song.Instrument.GHLiveBass)
                                        fretNumber = GetGHLSaveNoteNumber(note);

                                    else
                                        fretNumber = GetStandardSaveNoteNumber(note);
                                }
                                else
                                    fretNumber = note.rawNote;

                                saveString.Append(" = N " + fretNumber + " " + (uint)System.Math.Round(note.sustain_length * resolutionScaleRatio));

                                saveString.Append(Globals.LINE_ENDING);

                                // Only need to get the flags of one note of a chord
                                if (exportOptions.forced && (note.next == null || (note.next != null && note.next.position != note.position)))
                                {
                                    if ((note.flags & Note.Flags.FORCED) == Note.Flags.FORCED)
                                        saveString.Append(Globals.TABSPACE + tick + " = N 5 0 " + Globals.LINE_ENDING);

                                    // Save taps line if not an open note, as open note taps cause weird artifacts under sp
                                    if (!note.IsOpenNote() && (note.flags & Note.Flags.TAP) == Note.Flags.TAP)
                                        saveString.Append(Globals.TABSPACE + tick + " = N 6 0 " + Globals.LINE_ENDING);
                                }
                                continue;

                            default:
                                continue;
                        }
                        saveString.Append(Globals.LINE_ENDING);

                        //throw new System.Exception("Test error count: " + i);
                    }
                    catch (System.Exception e)
                    {
                        string error = "Error with saving object #" + i + " as " + songObject + ": " + e.Message;
                        Console.WriteLine(error);
                        out_errorList += error + Globals.LINE_ENDING;
                    }
                }

                return saveString.ToString();
            }

            int GetStandardSaveNoteNumber(Note note)
            {
                switch (note.fret_type)
                {
                    case (Note.Fret_Type.GREEN):
                        return 0;
                    case (Note.Fret_Type.RED):
                        return 1;
                    case (Note.Fret_Type.YELLOW):
                        return 2;
                    case (Note.Fret_Type.BLUE):
                        return 3;
                    case (Note.Fret_Type.ORANGE):
                        return 4;
                    case (Note.Fret_Type.OPEN):
                        return 7;                               // 5 and 6 are reserved for forced and taps properties
                    default: break;
                }

                return 0;
            }

            int GetDrumsSaveNoteNumber(Note note)
            {
                switch (note.drum_fret_type)
                {
                    case (Note.Drum_Fret_Type.KICK):
                        return 0;
                    case (Note.Drum_Fret_Type.RED):
                        return 1;
                    case (Note.Drum_Fret_Type.YELLOW):
                        return 2;
                    case (Note.Drum_Fret_Type.BLUE):
                        return 3;
                    case (Note.Drum_Fret_Type.ORANGE):
                        return 4;
                    case (Note.Drum_Fret_Type.GREEN):
                        return 5;

                    default: break;
                }

                return 0;
            }

            int GetGHLSaveNoteNumber(Note note)
            {
                switch (note.ghlive_fret_type)
                {
                    case (Note.GHLive_Fret_Type.WHITE_1):
                        return 0;
                    case (Note.GHLive_Fret_Type.WHITE_2):
                        return 1;
                    case (Note.GHLive_Fret_Type.WHITE_3):
                        return 2;
                    case (Note.GHLive_Fret_Type.BLACK_1):
                        return 3;
                    case (Note.GHLive_Fret_Type.BLACK_2):
                        return 4;

                    case (Note.GHLive_Fret_Type.OPEN):
                        return 7;
                    case (Note.GHLive_Fret_Type.BLACK_3):
                        return 8;

                    default: break;
                }

                return 0;
            }
        }
    }
}