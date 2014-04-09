using System;

namespace Chronos
{
    public interface IClock
    {
        DateTime Now { get; } 
    }

    public class SystemClock : IClock
    {
        public DateTime Now
        {
            get { return DateTime.Now; }
        }
    }
}