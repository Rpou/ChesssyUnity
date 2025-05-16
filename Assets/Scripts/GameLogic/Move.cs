using System;

namespace GameLogic
{
    /// <summary>
    /// Represents a chess move in UCI coordinate notation, with optional promotion.
    /// </summary>
    public class Move : IEquatable<Move>
    {
        // 0–7 for files a–h, 0–7 for ranks 1–8
        public int FromFile { get; }
        public int FromRank { get; }
        public int ToFile   { get; }
        public int ToRank   { get; }

        /// <summary>
        /// Promotion piece in lowercase (e.g. "q"), or empty string if no promotion.
        /// </summary>
        public string Promotion { get; }

        /// <summary>
        /// UCI string for the move, e.g. "e2e4" or "e7e8q".
        /// </summary>
        public string Uci { get; }

        /// <summary>
        /// Constructs a Move with optional promotion string ("q" for queen, or empty for none).
        /// </summary>
        public Move(int fromFile, int fromRank, int toFile, int toRank, string promotion)
        {
            if (fromFile < 0 || fromFile > 7) throw new ArgumentOutOfRangeException(nameof(fromFile));
            if (fromRank < 0 || fromRank > 7) throw new ArgumentOutOfRangeException(nameof(fromRank));
            if (toFile   < 0 || toFile   > 7) throw new ArgumentOutOfRangeException(nameof(toFile));
            if (toRank   < 0 || toRank   > 7) throw new ArgumentOutOfRangeException(nameof(toRank));

            FromFile  = fromFile;
            FromRank  = fromRank;
            ToFile    = toFile;
            ToRank    = toRank;
            Promotion = promotion ?? string.Empty;

            // build the UCI string
            char f1 = (char)('a' + fromFile);
            char r1 = (char)('1' + fromRank);
            char f2 = (char)('a' + toFile);
            char r2 = (char)('1' + toRank);
            Uci = $"{f1}{r1}{f2}{r2}{Promotion}";
        }

        /// <summary>
        /// Parses a UCI string (length 4 or 5) into a Move.
        /// </summary>
        public static Move FromUci(string uci)
        {
            if (uci == null) throw new ArgumentNullException(nameof(uci));
            if (uci.Length < 4 || uci.Length > 5)
                throw new ArgumentException($"Invalid UCI string: '{uci}'");

            int fromFile = uci[0] - 'a';
            int fromRank = uci[1] - '1';
            int toFile   = uci[2] - 'a';
            int toRank   = uci[3] - '1';

            string promo = uci.Length == 5 ? uci[4].ToString() : string.Empty;

            return new Move(fromFile, fromRank, toFile, toRank, promo);
        }

        public override string ToString() => Uci;

        public bool Equals(Move other)
        {
            if (other == null) return false;
            return FromFile == other.FromFile
                && FromRank == other.FromRank
                && ToFile   == other.ToFile
                && ToRank   == other.ToRank
                && Promotion == other.Promotion;
        }

        public override bool Equals(object obj) => Equals(obj as Move);

        public override int GetHashCode()
            => HashCode.Combine(FromFile, FromRank, ToFile, ToRank, Promotion);
    }
}
