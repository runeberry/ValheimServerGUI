using System;

namespace ValheimServerGUI.Tools
{
    public static class ExceptionExtensions
    {
        public static Exception GetPrimaryException(this Exception exception)
        {
            if (exception is AggregateException agg)
            {
                return agg.InnerException.GetPrimaryException() ?? agg;
            }

            return exception;
        }
    }
}
