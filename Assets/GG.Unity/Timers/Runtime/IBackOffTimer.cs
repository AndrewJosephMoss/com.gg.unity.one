namespace GG.Unity.Timers
{
    public interface IBackOffTimer
    { 
        int AttemptCount { get; } 
        void Schedule(); 
        void Reset(); 
        void Cancel(); 
    }
}

