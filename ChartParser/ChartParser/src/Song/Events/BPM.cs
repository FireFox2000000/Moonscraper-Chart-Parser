﻿using System.Text.RegularExpressions;

namespace Moonscraper
{
    namespace ChartParser
    {
        public class BPM : SyncTrack
        {
            private readonly ID _classID = ID.BPM;

            public override int classID { get { return (int)_classID; } }

            /// <summary>
            /// Stored as the bpm value * 1000. For example, a bpm of 120.075 would be stored as 120075.
            /// </summary>
            public uint value;
            public float? anchor = null;

            /// <summary>
            /// Basic constructor.
            /// </summary>
            /// <param name="_position">Tick position.</param>
            /// <param name="_value">Stored as the bpm value * 1000 to limit it to 3 decimal places. For example, a bpm of 120.075 would be stored as 120075.</param>
            public BPM(uint _position = 0, uint _value = 120000) : base(_position)
            {
                value = _value;
            }

            public BPM(BPM _bpm) : base(_bpm.position)
            {
                value = _bpm.value;
                anchor = _bpm.anchor;
            }

            internal override string GetSaveString()
            {
                //0 = B 140000
                string s = string.Empty;
                if (anchor != null)
                    s += Globals.TABSPACE + position + " = A " + (uint)(((double)anchor) * 1000000) + Globals.LINE_ENDING;
                s += Globals.TABSPACE + position + " = B " + value + Globals.LINE_ENDING;

                return s;
            }

            public float assignedTime = 0;

            public static bool regexMatch(string line)
            {
                return new Regex(@"\d+ = B \d+").IsMatch(line);
            }

            public override SongObject Clone()
            {
                return new BPM(this);
            }

            public override bool AllValuesCompare<T>(T songObject)
            {
                if (this == songObject && songObject as BPM != null && (songObject as BPM).value == value)
                    return true;
                else
                    return false;
            }
        }
    }
}
