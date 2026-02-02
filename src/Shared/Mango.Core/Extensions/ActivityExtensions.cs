using System.Diagnostics;

namespace Mango.Core.Extensions;

public static class ActivityExtensions
{
    extension(Activity activity)
    {
        public void SetExceptionTags(Exception ex)
        {
            if (activity is null)
            {
                return;
            }

            activity.AddTag("exception.message", ex.Message);
            activity.AddTag("exception.stacktrace", ex.ToString());
            activity.AddTag("exception.type", ex.GetType().FullName);
            activity.SetStatus(ActivityStatusCode.Error);

        }
    }
}
