using System.Runtime.Serialization;

namespace ThirdPartyLibraries.GitHub;

[Serializable]
public class ApiRateLimitExceededException : ApplicationException
{
    public ApiRateLimitExceededException()
    {
    }

    public ApiRateLimitExceededException(string message)
        : base(message)
    {
    }

    public ApiRateLimitExceededException(string message, long limit, long remaining, DateTime reset)
        : base(message)
    {
        Limit = limit;
        Remaining = remaining;
        Reset = reset;
    }

    public ApiRateLimitExceededException(string message, long limit, long remaining, DateTime reset, Exception inner)
        : base(message, inner)
    {
        Limit = limit;
        Remaining = remaining;
        Reset = reset;
    }

    protected ApiRateLimitExceededException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Limit = info.GetInt32(nameof(Limit));
        Remaining = info.GetInt32(nameof(Remaining));
        Reset = info.GetDateTime(nameof(Reset));
    }

    public long Limit { get; }

    public long Remaining { get; }

    public DateTime Reset { get; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue(nameof(Limit), Limit);
        info.AddValue(nameof(Remaining), Remaining);
        info.AddValue(nameof(Reset), Reset);
    }
}