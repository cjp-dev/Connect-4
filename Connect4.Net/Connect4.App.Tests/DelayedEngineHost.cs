using Connect4.App.Services;
using Connect4.Engine;

namespace Connect4.App.Tests;

/// <summary>Waits <paramref name="delayMs"/> before asking the real engine, so a search can be made slow or instant.</summary>
internal sealed class DelayedEngineHost(IEngineHost inner, int delayMs) : IEngineHost
{
    public async Task<SearchResult> ChooseMoveAsync(
        IReadOnlyList<int> moves,
        SearchLimits limits,
        IProgress<SearchInfo>? progress,
        CancellationToken cancellationToken,
        CancellationToken moveNowToken)
    {
        await Task.Delay(delayMs, cancellationToken);
        return await inner.ChooseMoveAsync(moves, limits, progress, cancellationToken, moveNowToken);
    }

    public void NewGame() => inner.NewGame();
}
