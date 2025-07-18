using UnityEngine;

public class OpponentAISimple : Player
{
    public (PlayerAction action, int amount) MakeDecision(GameStateSnapshot state)
    {
        (PlayerAction action, int amount) response;
        if (state.PlayerPot > state.OpponentPot)
        {
            response = (PlayerAction.CALL, state.PlayerPot - state.OpponentPot);
        }
        else
        {
            response = (PlayerAction.CHECK, 0);
        }

        // Add random raise and fold with 10% chance
        float chance = Random.value;
        if (chance <= 0.1)
        {
            int amount = Random.Range(state.PlayerPot - state.OpponentPot + 1, Mathf.Min((state.PlayerChips + (state.PlayerPot - state.OpponentPot)), state.OpponentChips) + 1);
            response = (PlayerAction.RAISE, amount);
        }
        else if (chance >= 0.9)
        {
            response = (PlayerAction.FOLD, 0);
        }

        return response;
    }
}
