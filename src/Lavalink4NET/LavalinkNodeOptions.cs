namespace Lavalink4NET;

using Rest;

/// <summary>
///     The required options used to connect to a lavalink node.
/// </summary>
public sealed class LavalinkNodeOptions : LavalinkRestOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether session resuming should be used when the
    ///     connection to the node is aborted.
    /// </summary>
    /// <remarks>This property defaults to <see langword="true"/>.</remarks>
    public bool AllowResuming { get; set; } = true;

    /// <summary>
    ///     Gets or sets the buffer size when receiving payloads from a lavalink node.
    /// </summary>
    /// <remarks>This property defaults to <c>65535</c> (64 KiB)</remarks>
    public int BufferSize { get; set; } = 64 * 1024;

    /// <summary>
    ///     Gets or sets a value indicating whether the player should disconnect from the voice
    ///     channel its connected to after the track ended.
    /// </summary>
    /// <remarks>
    ///     This property defaults to <see langword="true"/>. This can be useful to set to <see
    ///     langword="false"/>, for example when using the InactivityTrackingService.
    /// </remarks>
    public bool DisconnectOnStop { get; set; } = true;

    /// <summary>
    ///     Gets or sets the node label.
    /// </summary>
    /// <remarks>
    ///     This property defaults to <see langword="null"/> and is used for identifying nodes.
    /// </remarks>
    public string? Label { get; set; }

    /// <summary>
    ///     Gets or sets the reconnect strategy for reconnection.
    /// </summary>
    /// <remarks>This property defaults to <see cref="ReconnectStrategies.DefaultStrategy"/>.</remarks>
    public ReconnectStrategy ReconnectStrategy { get; set; } = ReconnectStrategies.DefaultStrategy;

    /// <summary>
    ///     The number of seconds a session is valid after the connection aborts.
    /// </summary>
    /// <remarks>This property defaults to <c>60</c>.</remarks>
    public int SessionTimeout { get; set; } = 60;

    /// <summary>
    ///     The resume key to use when trying to initially connect to the node.
    /// </summary>
    /// <remarks>
    ///     The resume key to use when trying to initially connect to the node. This property
    ///     defaults to <see langword="null"/>.
    /// </remarks>
    public string? ResumeKey { get; set; }

    /// <summary>
    ///     Gets or sets the Lavalink Node WebSocket host(name).
    /// </summary>
    /// <remarks>This property defaults to <c>ws://localhost:8080/</c>.</remarks>
    public string WebSocketUri { get; set; } = "ws://localhost:8080/";

    /// <summary>
    ///     Gets or sets value indicating whether all duplicate reconnect attempts shouldn't be logged
    /// </summary>
    /// <remarks>
    ///     If <see langword="true"/> then all "Connection to Lavalink Node failed" will be hidden from log
    /// </remarks>
    public bool SuppressReconnectionEntries { get; set; } = false;
}
