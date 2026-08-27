namespace Launcher;

/// <summary>Any failure that must stop the launcher before it starts ClassicUO.</summary>
internal sealed class UpdateException(string message) : Exception(message);
