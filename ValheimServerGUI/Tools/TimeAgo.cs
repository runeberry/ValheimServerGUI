using Humanizer;
using System;

namespace ValheimServerGUI.Tools
{
    public readonly struct TimeAgo : IComparable, IComparable<TimeAgo>, IEquatable<TimeAgo>, IFormattable
    {
        public readonly DateTimeOffset Timestamp;

        public TimeAgo(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public static TimeAgo Parse(string str)
        {
            return new TimeAgo(DateTimeOffset.Parse(str));
        }

        #region Interface implementations

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (obj is TimeAgo timeAgo)
            {
                return this.CompareTo(timeAgo);
            }
            else if (obj is DateTimeOffset dateTimeOffset)
            {
                return this.Timestamp.CompareTo(dateTimeOffset);
            }
            else
            {
                throw new ArgumentException("Object must be a TimeAgo or a DateTimeOffset");
            }
        }

        public int CompareTo(TimeAgo other)
        {
            return this.Timestamp.CompareTo(other.Timestamp);
        }

        public bool Equals(TimeAgo other)
        {
            throw new NotImplementedException();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return this.Timestamp.ToString(formatProvider);
        }

        #endregion

        #region Object overrides

        public override string ToString()
        {
            return this.Timestamp.Humanize();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if (obj is TimeAgo timeAgo)
            {
                return this.CompareTo(timeAgo) == 0;
            }
            else if (obj is DateTimeOffset dateTimeOffset)
            {
                return this.Timestamp.CompareTo(dateTimeOffset) == 0;
            }
            else
            {
                throw new ArgumentException("Object must be a TimeAgo or a DateTimeOffset");
            }
        }

        public override int GetHashCode()
        {
            return Timestamp.GetHashCode();
        }

        #endregion

        #region Operators

        public static bool operator ==(TimeAgo left, TimeAgo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimeAgo left, TimeAgo right)
        {
            return !(left == right);
        }

        public static bool operator <(TimeAgo left, TimeAgo right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(TimeAgo left, TimeAgo right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(TimeAgo left, TimeAgo right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(TimeAgo left, TimeAgo right)
        {
            return left.CompareTo(right) >= 0;
        }

        #endregion
    }
}
