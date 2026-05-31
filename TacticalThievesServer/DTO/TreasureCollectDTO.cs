namespace TacticalThievesServer.DTO
{
    /// <summary>
    /// DTO sent by the game when the player collects treasure. Contains the
    /// amount of gold collected during the event.
    /// </summary>
    [Serializable]
    public class TreasureCollectDTO
    {
        public uint Amount { get; set; }
    }
}
