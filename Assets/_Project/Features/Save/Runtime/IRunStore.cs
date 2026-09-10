namespace FateDice
{
    // One local checkpoint slot. This is not an account or cloud persistence contract.
    public interface IRunStore
    {
        bool Exists { get; }
        RunState Load();
        void Save(RunState state);
        string Archive();
    }
}
