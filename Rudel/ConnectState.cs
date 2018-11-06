namespace Rudel
{
    public enum ConnectState
    {
        WaitingForChallenge, // Client
        WaitingForChallengeResponse, // Server
        Connected, // Both
        Disconnected // Both,
    }
}
