using System;
using System.Text.RegularExpressions;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services;

public static class ReplayDestinationValidator
{
    public const string ConfirmationRequired = "Confirm the destination before sending.";
    public const string KindRequired = "Choose a queue or a topic.";
    public const string DestinationRequired = "Enter a destination.";
    public const string DestinationInvalid = "Destination can only contain letters, numbers, dots, hyphens, and underscores.";
    public const string ConfirmationMismatch = "Type the destination name again so it matches.";
    public const string SameAsSourceQueue = "That is the queue these messages were taken from. Use Release selected to put them back there.";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex NamePattern = new(
        "^[A-Za-z0-9]$|^[A-Za-z0-9][A-Za-z0-9._-]{0,258}[A-Za-z0-9]$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        MatchTimeout);

    public static string Validate(ReplayDestinationRequest request)
    {
        if (request == null || !request.Confirmed)
        {
            return ConfirmationRequired;
        }

        if (request.Kind != ReplayDestinationKind.Queue && request.Kind != ReplayDestinationKind.Topic)
        {
            return KindRequired;
        }

        var destination = request.Destination?.Trim() ?? string.Empty;
        var confirmation = request.Confirmation?.Trim() ?? string.Empty;

        if (destination.Length == 0)
        {
            return DestinationRequired;
        }

        try
        {
            if (!NamePattern.IsMatch(destination))
            {
                return DestinationInvalid;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return DestinationInvalid;
        }

        if (!string.Equals(destination, confirmation, StringComparison.OrdinalIgnoreCase))
        {
            return ConfirmationMismatch;
        }

        var sourceQueue = request.SourceQueue?.Trim() ?? string.Empty;
        if (string.Equals(destination, sourceQueue, StringComparison.OrdinalIgnoreCase))
        {
            return SameAsSourceQueue;
        }

        return null;
    }
}
