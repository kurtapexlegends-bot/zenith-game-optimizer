using System;

namespace ZenithOptimizer.Services.Tuners
{
    public interface IGameTuner
    {
        string GameId { get; }
        string GameName { get; }
        bool Apply(out string message);
        bool Restore(out string message);
        string GetStatus();
    }
}
