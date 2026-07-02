namespace BudsCollab.Bridge
{
    public enum BudsBridgeJobStatus
    {
        Queued,
        Accepted,
        Downloading,
        Importing,
        Validating,
        Uploaded,
        Failed,
        Completed
    }

    public readonly struct BudsBridgeClientPresence
    {
        public BudsBridgeClientPresence(string clientId, string displayName, string spaceId, string roomId)
        {
            ClientId = clientId;
            DisplayName = displayName;
            SpaceId = spaceId;
            RoomId = roomId;
        }

        public string ClientId { get; }
        public string DisplayName { get; }
        public string SpaceId { get; }
        public string RoomId { get; }
    }
}
