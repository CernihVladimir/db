using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace Game.Domain
{
    public class InMemoryGameTurnRepository : IGameTurnRepository
    {
        private readonly List<GameTurnEntity> turns = new List<GameTurnEntity>();

        public GameTurnEntity Insert(GameTurnEntity turn)
        {
            if (turn.Id == default)
            {
                turn.Id = ObjectId.GenerateNewId();
            }
            if (turn.CreatedAt == default)
                turn.CreatedAt = DateTime.UtcNow;
            turns.Add(turn);
            return turn;
        }

        public IList<GameTurnEntity> GetLastTurns(Guid gameId, int count)
        {
            return turns
                .Where(t => t.GameId == gameId)
                .OrderByDescending(t => t.TurnNumber)
                .Take(count)
                .OrderBy(t => t.TurnNumber)
                .ToList();
        }
    }
}
