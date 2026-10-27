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
            try
            {
                turnCollection.Indexes.CreateOne(indexModel);
            }
            catch
            {
            }
        }

        public GameTurnEntity Insert(GameTurnEntity turn)
        {
            turnCollection.InsertOne(turn);
            return turn;
        }

        public IList<GameTurnEntity> GetLastTurns(Guid gameId, int count)
        {
            var result = turnCollection.Find(t => t.GameId == gameId)
                .SortByDescending(t => t.TurnNumber)
                .Limit(count)
                .ToList();
            result.Reverse();
            return result;
        }
    }
}