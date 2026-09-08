namespace FateDice
{
    public enum GameSceneRole { Title, Lobby, InGame }

    public interface IRunSceneNavigation
    {
        bool IsTransitioning { get; }
        bool RequestLobby();
        bool RequestInGame();
    }
}
