﻿// Copyright (c) 2016-2017 Alexander Ong
// See LICENSE in project root for license information.

using System;
using System.Collections.Generic;

namespace Moonscraper
{
    namespace ChartParser
    {
        public class Note : ChartObject
        {
            private readonly ID _classID = ID.Note;

            public override int classID { get { return (int)_classID; } }

            public uint sustain_length;
            public int rawNote;
            public Fret_Type fret_type
            {
                get
                {
                    return (Fret_Type)rawNote;
                }
                set
                {
                    rawNote = (int)value;
                }
            }

            public Drum_Fret_Type drum_fret_type
            {
                get
                {
                    return (Drum_Fret_Type)fret_type;
                }
            }

            public GHLive_Fret_Type ghlive_fret_type
            {
                get
                {
                    return (GHLive_Fret_Type)rawNote;
                }
                set
                {
                    rawNote = (int)value;
                }
            }

            /// <summary>
            /// Properties, such as forced or taps, are stored here in a bitwise format.
            /// </summary>
            public Flags flags;

            /// <summary>
            /// The previous note in the linked-list.
            /// </summary>
            public Note previous;
            /// <summary>
            /// The next note in the linked-list.
            /// </summary>
            public Note next;

            public Note(uint _position,
                        int _rawNote,
                        uint _sustain = 0,
                        Flags _flags = Flags.NONE) : base(_position)
            {
                sustain_length = _sustain;
                flags = _flags;
                rawNote = _rawNote;

                previous = null;
                next = null;
            }

            public Note(uint _position,
                        Fret_Type _fret_type,
                        uint _sustain = 0,
                        Flags _flags = Flags.NONE) : base(_position)
            {
                sustain_length = _sustain;
                flags = _flags;
                fret_type = _fret_type;

                previous = null;
                next = null;
            }

            public Note(Note note) : base(note.position)
            {
                position = note.position;
                sustain_length = note.sustain_length;
                flags = note.flags;
                rawNote = note.rawNote;
            }

            public enum Fret_Type
            {
                // Assign to the sprite array position
                GREEN = 0, RED = 1, YELLOW = 2, BLUE = 3, ORANGE = 4, OPEN = 5
            }

            public enum Drum_Fret_Type
            {
                // Wrapper to account for how the frets change colours between the drums and guitar tracks from the GH series
                KICK = Fret_Type.OPEN, RED = Fret_Type.GREEN, YELLOW = Fret_Type.RED, BLUE = Fret_Type.YELLOW, ORANGE = Fret_Type.BLUE, GREEN = Fret_Type.ORANGE
            }

            public enum GHLive_Fret_Type
            {
                // Assign to the sprite array position
                //WHITE_1, BLACK_1, WHITE_2, BLACK_2, WHITE_3, BLACK_3, OPEN
                BLACK_1, BLACK_2, BLACK_3, WHITE_1, WHITE_2, WHITE_3, OPEN
            }

            public enum Note_Type
            {
                Natural, Strum, Hopo, Tap
            }

            public enum Special_Type
            {
                NONE, STAR_POW, BATTLE
            }

            [Flags]
            public enum Flags
            {
                NONE = 0,
                FORCED = 1,
                TAP = 2
            }

            private Chart.GameMode gameMode
            {
                get
                {
                    if (chart != null)
                        return chart.gameMode;
                    else
                    {
#if APPLICATION_MOONSCRAPER
                        return ChartEditor.FindCurrentEditor().currentChart.gameMode;
#else
                        return Chart.GameMode.Unrecognised;
#endif
                    }
                }
            }

            public bool forced
            {
                get
                {
                    return (flags & Flags.FORCED) == Flags.FORCED;
                }
                set
                {
                    if (value)
                        flags = flags | Flags.FORCED;
                    else
                        flags = flags & ~Flags.FORCED;
                }
            }

            /// <summary>
            /// Gets the next note in the linked-list that's not part of this note's chord.
            /// </summary>
            public Note nextSeperateNote
            {
                get
                {
                    Note nextNote = next;
                    while (nextNote != null && nextNote.position == position)
                        nextNote = nextNote.next;
                    return nextNote;
                }
            }

            /// <summary>
            /// Gets the previous note in the linked-list that's not part of this note's chord.
            /// </summary>
            public Note previousSeperateNote
            {
                get
                {
                    Note previousNote = previous;
                    while (previousNote != null && previousNote.position == position)
                        previousNote = previousNote.previous;
                    return previousNote;
                }
            }

            // Deprecated
            internal override string GetSaveString()
            {
                int fretNumber = (int)fret_type;

                if (fret_type == Fret_Type.OPEN)
                    fretNumber = 7;

                return Globals.TABSPACE + position + " = N " + fretNumber + " " + sustain_length + Globals.LINE_ENDING;          // 48 = N 2 0
            }

            public override SongObject Clone()
            {
                return new Note(this);
            }

            public override bool AllValuesCompare<T>(T songObject)
            {
                if (this == songObject && (songObject as Note).sustain_length == sustain_length && (songObject as Note).rawNote == rawNote && (songObject as Note).flags == flags)
                    return true;
                else
                    return false;
            }

            public string GetFlagsSaveString()
            {
                string saveString = string.Empty;

                if ((flags & Flags.FORCED) == Flags.FORCED)
                    saveString += Globals.TABSPACE + position + " = N 5 0 " + Globals.LINE_ENDING;

                if ((flags & Flags.TAP) == Flags.TAP)
                    saveString += Globals.TABSPACE + position + " = N 6 0 " + Globals.LINE_ENDING;

                return saveString;
            }

            protected override bool Equals(SongObject b)
            {
                if (b.GetType() == typeof(Note))
                {
                    Note realB = b as Note;
                    if (position == realB.position && rawNote == realB.rawNote)
                        return true;
                    else
                        return false;
                }
                else
                    return base.Equals(b);
            }

            protected override bool LessThan(SongObject b)
            {
                if (b.GetType() == typeof(Note))
                {
                    Note realB = b as Note;
                    if (position < b.position)
                        return true;
                    else if (position == b.position)
                    {
                        if (rawNote < realB.rawNote)
                            return true;
                    }

                    return false;
                }
                else
                    return base.LessThan(b);
            }

            public static void groupAddFlags(Note[] notes, Flags flag)
            {
                for (int i = 0; i < notes.Length; ++i)
                {
                    notes[i].flags = notes[i].flags | flag;
                }
            }

            public bool IsChord
            {
                get
                {
                    return ((previous != null && previous.position == position) || (next != null && next.position == position));
                }
            }

            /// <summary>
            /// Ignores the note's forced flag when determining whether it would be a hopo or not
            /// </summary>
            public bool IsNaturalHopo
            {
                get
                {
                    bool HOPO = false;

                    if (!IsChord && previous != null)
                    {
                        bool prevIsChord = previous.IsChord;
                        // Need to consider whether the previous note was a chord, and if they are the same type of note
                        if (prevIsChord || (!prevIsChord && rawNote != previous.rawNote))
                        {
                            // Check distance from previous note 
                            int HOPODistance = (int)(65 * song.resolution / Song.STANDARD_BEAT_RESOLUTION);

                            if (position - previous.position <= HOPODistance)
                                HOPO = true;
                        }
                    }

                    return HOPO;
                }
            }

            /// <summary>
            /// Would this note be a hopo or not? (Ignores whether the note's tap flag is set or not.)
            /// </summary>
            bool IsHopo
            {
                get
                {
                    bool HOPO = IsNaturalHopo;

                    // Check if forced
                    if (forced)
                        HOPO = !HOPO;

                    return HOPO;
                }
            }

            /// <summary>
            /// Returns a bit mask representing the whole note's chord. For example, a green, red and blue chord would have a mask of 0000 1011. A yellow and orange chord would have a mask of 0001 0100. 
            /// Shifting occurs accoring the values of the Fret_Type enum, so open notes currently output with a mask of 0010 0000.
            /// </summary>
            public int mask
            {
                get
                {
                    Note[] chord = GetChord();
                    int mask = 0;

                    foreach (Note note in chord)
                        mask |= (1 << note.rawNote);

                    return mask;
                }
            }

            /// <summary>
            /// Live calculation of what Note_Type this note would currently be. 
            /// </summary>
            public Note_Type type
            {
                get
                {
                    if (!IsOpenNote() && (flags & Flags.TAP) == Flags.TAP)
                    {
                        return Note_Type.Tap;
                    }
                    else
                    {
                        if (IsHopo)
                            return Note_Type.Hopo;
                        else
                            return Note_Type.Strum;
                    }
                }
            }

            /// <summary>
            /// Gets all the notes (including this one) that share the same tick position as this one.
            /// </summary>
            /// <returns>Returns an array of all the notes currently sharing the same tick position as this note.</returns>
            public Note[] GetChord()
            {
                List<Note> chord = new List<Note>();
                chord.Add(this);

                Note previous = this.previous;
                while (previous != null && previous.position == this.position)
                {
                    chord.Add(previous);
                    previous = previous.previous;
                }

                Note next = this.next;
                while (next != null && next.position == this.position)
                {
                    chord.Add(next);
                    next = next.next;
                }

                return chord.ToArray();
            }

            public void applyFlagsToChord()
            {
                Note[] chordNotes = GetChord();

                foreach (Note chordNote in chordNotes)
                {
                    chordNote.flags = flags;
                }
            }

            public bool CannotBeForcedCheck
            {
                get
                {
                    Note seperatePrevious = previousSeperateNote;

                    if ((seperatePrevious == null) || (seperatePrevious != null && mask == seperatePrevious.mask))
                        return true;

                    return false;
                }
            }

            public override void Delete(bool update = true)
            {
                base.Delete(update);
            }

            public bool IsOpenNote()
            {
                if (gameMode == Chart.GameMode.GHLGuitar)
                    return ghlive_fret_type == GHLive_Fret_Type.OPEN;
                else
                    return fret_type == Fret_Type.OPEN;
            }

            /*
            public Note FindNextSameFretWithinSustainExtendedCheck()
            {
                Note next = this.next;

                while (next != null)
                {
                    if (!GameSettings.extendedSustainsEnabled)
                    {
                        if ((next.IsOpenNote() || (position < next.position)) && position != next.position)
                            return next;
                    }
                    else
                    {
                        if ((!IsOpenNote() && next.IsOpenNote() && !(gameMode == Chart.GameMode.Drums)) || (next.rawNote == rawNote))
                            return next;
                    }

                    next = next.next;
                }

                return null;
            }

            /// <summary>
            /// Calculates and sets the sustain length based the tick position it should end at. Will be a length of 0 if the note position is greater than the specified position.
            /// </summary>
            /// <param name="pos">The end-point for the sustain.</param>
            public void SetSustainByPos(uint pos)
            {
                if (pos > position)
                    sustain_length = pos - position;
                else
                    sustain_length = 0;

                // Cap the sustain
                Note nextFret;
                nextFret = FindNextSameFretWithinSustainExtendedCheck();

                if (nextFret != null)
                {
                    CapSustain(nextFret);
                }
            }*/

            public void SetType(Note_Type type)
            {
                flags = Flags.NONE;
                switch (type)
                {
                    case (Note_Type.Strum):
                        if (IsChord)
                            flags &= ~Note.Flags.FORCED;
                        else
                        {
                            if (IsNaturalHopo)
                                flags |= Note.Flags.FORCED;
                            else
                                flags &= ~Note.Flags.FORCED;
                        }

                        break;

                    case (Note_Type.Hopo):
                        if (!CannotBeForcedCheck)
                        {
                            if (IsChord)
                                flags |= Note.Flags.FORCED;
                            else
                            {
                                if (!IsNaturalHopo)
                                    flags |= Note.Flags.FORCED;
                                else
                                    flags &= ~Note.Flags.FORCED;
                            }
                        }
                        break;

                    case (Note_Type.Tap):
                        if (!IsOpenNote())
                            flags |= Note.Flags.TAP;
                        break;

                    default:
                        break;
                }

                applyFlagsToChord();
            }

            public static Fret_Type SaveGuitarNoteToDrumNote(Fret_Type fret_type)
            {
                if (fret_type == Fret_Type.OPEN)
                    return Fret_Type.GREEN;
                else if (fret_type == Fret_Type.ORANGE)
                    return Fret_Type.OPEN;
                else
                    return fret_type + 1;
            }

            public static Fret_Type LoadDrumNoteToGuitarNote(Fret_Type fret_type)
            {
                if (fret_type == Fret_Type.OPEN)
                    return Fret_Type.ORANGE;
                else if (fret_type == Fret_Type.GREEN)
                    return Fret_Type.OPEN;
                else
                    return fret_type - 1;
            }
        }
    }
}