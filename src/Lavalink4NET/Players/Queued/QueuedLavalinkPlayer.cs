namespace Lavalink4NET.Players.Queued;

using System;
using System.Threading;
using System.Threading.Tasks;
using Lavalink4NET.Extensions;
using Lavalink4NET.Protocol;
using Lavalink4NET.Protocol.Payloads.Events;
using Lavalink4NET.Tracks;

/// <summary>
///     A lavalink player with a queuing system.
/// </summary>
public class QueuedLavalinkPlayer : LavalinkPlayer
{
    private readonly bool _disconnectOnStop;
    private readonly bool _clearQueueOnStop;
    private readonly bool _clearHistoryOnStop;
    private readonly bool _resetTrackRepeatOnStop;
    private readonly bool _resetShuffleOnStop;
    private readonly TrackRepeatMode _defaultTrackRepeatMode;

    /// <summary>
    ///     Initializes a new instance of the <see cref="QueuedLavalinkPlayer"/> class.
    /// </summary>
    public QueuedLavalinkPlayer(IPlayerProperties<QueuedLavalinkPlayer, QueuedLavalinkPlayerOptions> properties)
        : base(properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var options = properties.Options.Value;

        Queue = new TrackQueue(historyCapacity: options.HistoryCapacity);

        _disconnectOnStop = options.DisconnectOnStop;
        _clearQueueOnStop = options.ClearQueueOnStop;
        _resetTrackRepeatOnStop = options.ResetTrackRepeatOnStop;
        _resetShuffleOnStop = options.ResetShuffleOnStop;
        _defaultTrackRepeatMode = options.DefaultTrackRepeatMode;
        _clearHistoryOnStop = options.ClearHistoryOnStop;

        RepeatMode = _defaultTrackRepeatMode;
    }

    /// <summary>
    ///     Gets the track queue.
    /// </summary>
    public ITrackQueue Queue { get; }

    /// <summary>
    ///     Gets or sets the loop mode for this player.
    /// </summary>
    public TrackRepeatMode RepeatMode { get; set; }

    public bool Shuffle { get; set; }

    public async ValueTask<int> PlayAsync(ITrackQueueItem queueItem, bool enqueue = true, TrackPlayProperties properties = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(queueItem);
        EnsureNotDestroyed();

        // check if the track should be enqueued (if a track is already playing)
        if (enqueue && (Queue.Count > 0 || State == PlayerState.Playing || State == PlayerState.Paused))
        {
            // add the track to the queue
            return await Queue
                .AddAsync(queueItem, cancellationToken)
                .ConfigureAwait(false);
        }

        // play the track immediately
        await base
            .PlayAsync(queueItem.Track, properties, cancellationToken)
            .ConfigureAwait(false);

        // 0 = now playing
        return 0;
    }

    public ValueTask<int> PlayAsync(LavalinkTrack track, bool enqueue = true, TrackPlayProperties properties = default, CancellationToken cancellationToken = default)
    {
        EnsureNotDestroyed();
        cancellationToken.ThrowIfCancellationRequested();

        return PlayAsync(new TrackReference(track), enqueue, properties, cancellationToken);
    }

    public ValueTask<int> PlayAsync(string identifier, bool enqueue = true, TrackPlayProperties properties = default, CancellationToken cancellationToken = default)
    {
        EnsureNotDestroyed();
        cancellationToken.ThrowIfCancellationRequested();

        return PlayAsync(new TrackReference(identifier), enqueue, properties, cancellationToken);
    }

    public ValueTask<int> PlayAsync(TrackReference trackReference, bool enqueue = true, TrackPlayProperties properties = default, CancellationToken cancellationToken = default)
    {
        EnsureNotDestroyed();
        cancellationToken.ThrowIfCancellationRequested();

        return PlayAsync(new TrackQueueItem(trackReference), enqueue, properties, cancellationToken);
    }

    public override async ValueTask PlayAsync(TrackReference trackReference, TrackPlayProperties properties = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await PlayAsync(trackReference, enqueue: true, properties, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Skips the current track asynchronously.
    /// </summary>
    /// <param name="count">the number of tracks to skip</param>
    /// <returns>a task that represents the asynchronous operation</returns>
    /// <exception cref="InvalidOperationException">thrown if the player is destroyed</exception>
    public virtual async ValueTask SkipAsync(int count = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotDestroyed();

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "The count must not be negative.");
        }

        var track = await GetNextTrackAsync(count, cancellationToken).ConfigureAwait(false);

        if (!track.IsPresent)
        {
            // Do nothing, stop
            await StopAsync(_disconnectOnStop, cancellationToken).ConfigureAwait(false);
            return;
        }

        await base
            .PlayAsync(track.Value.Track, properties: default, cancellationToken)
            .ConfigureAwait(false);
    }

    public override async ValueTask StopAsync(bool disconnect = false, CancellationToken cancellationToken = default)
    {
        EnsureNotDestroyed();
        cancellationToken.ThrowIfCancellationRequested();

        if (_clearQueueOnStop)
        {
            await Queue
                .ClearAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (_clearHistoryOnStop && Queue.HasHistory)
        {
            await Queue.History
                .ClearAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (_resetTrackRepeatOnStop)
        {
            RepeatMode = _defaultTrackRepeatMode;
        }

        if (_resetShuffleOnStop)
        {
            Shuffle = false;
        }

        await base
            .StopAsync(disconnect, cancellationToken)
            .ConfigureAwait(false);
    }

    protected override ValueTask OnTrackEndedAsync(LavalinkTrack track, TrackEndReason endReason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(track);

        if (endReason.MayStartNext())
        {
            return SkipAsync(count: 1, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask<Optional<ITrackQueueItem>> GetNextTrackAsync(int count = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var track = default(Optional<ITrackQueueItem>);

        if (RepeatMode is TrackRepeatMode.Track)
        {
            return CurrentTrack is null
                ? Optional<ITrackQueueItem>.Default
                : new Optional<ITrackQueueItem>(new TrackQueueItem(new TrackReference(CurrentTrack)));
        }

        var dequeueMode = Shuffle
            ? TrackDequeueMode.Shuffle
            : TrackDequeueMode.Normal;

        while (count-- > 1)
        {
            var peekedTrack = await Queue
                .TryDequeueAsync(dequeueMode, cancellationToken)
                .ConfigureAwait(false);

            if (peekedTrack is null)
            {
                break;
            }

            if (RepeatMode is TrackRepeatMode.Queue)
            {
                await Queue
                    .AddAsync(peekedTrack, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (count >= 0)
        {
            var peekedTrack = await Queue
                .TryDequeueAsync(dequeueMode, cancellationToken)
                .ConfigureAwait(false);

            if (peekedTrack is null)
            {
                return Optional<ITrackQueueItem>.Default; // do nothing
            }

            if (RepeatMode is TrackRepeatMode.Queue)
            {
                await Queue
                    .AddAsync(peekedTrack, cancellationToken)
                    .ConfigureAwait(false);
            }

            track = new Optional<ITrackQueueItem>(peekedTrack);
        }

        return track;
    }
}
