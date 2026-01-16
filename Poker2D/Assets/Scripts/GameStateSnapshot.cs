using System.Collections.Generic;

public class GameStateSnapshot
{
    public GameState GameState;
    public int PlayerPot;
    public int OpponentPot;
    public int PlayerChips;
    public int OpponentChips;
    public List<Card> CommunityCards;
}
