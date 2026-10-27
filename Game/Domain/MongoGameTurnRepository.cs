using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoGameTurnRepository : IGameTurnRepository
    {
        private readonly IMongoCollection<GameTurnEntity> turnCollection;
        public const string CollectionName = "gameTurns";

        public MongoGameTurnRepository(IMongoDatabase database)
        {
            turnCollection = database.GetCollection<GameTurnEntity>(CollectionName);
            CreateIndex();
        }

        private void CreateIndex()
        {
            var indexKeysDefinition = Builders<GameTurnEntity>.IndexKeys
                .Ascending(t => t.GameId)
                .Descending(t => t.TurnNumber);
            var indexModel = new CreateIndexModel<GameTurnEntity>(indexKeysDefinition);
            turnCollection.Indexes.CreateOne(indexModel);
        }

        public GameTurnEntity Insert(GameTurnEntity turn)
        {
            if (turn.CreatedAt == default)
                turn.CreatedAt = DateTime.UtcNow;
            turnCollection.InsertOne(turn);
            return turn;
        }

        public IList<GameTurnEntity> GetLastTurns(Guid gameId, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return new List<GameTurnEntity>();
            var result = turnCollection.Find(t => t.GameId == gameId)
                .SortByDescending(t => t.TurnNumber)
                .Limit(count)
                .ToList();
            result.Reverse();
            return result;
        }
    }
}
