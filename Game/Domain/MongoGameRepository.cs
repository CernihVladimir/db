using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoGameRepository : IGameRepository
    {
        private readonly IMongoCollection<GameEntity> gameCollection;
        public const string CollectionName = "games";

        public MongoGameRepository(IMongoDatabase db)
        {
            gameCollection = db.GetCollection<GameEntity>(CollectionName);
            gameCollection.Indexes.CreateOne(new CreateIndexModel<GameEntity>(
                Builders<GameEntity>.IndexKeys.Ascending(game => game.Status)));
        }

        public GameEntity Insert(GameEntity game)
        {
            if (game.Id != Guid.Empty)
                throw new InvalidOperationException("The game has already been assigned an id.");
            gameCollection.InsertOne(game);
            return game;
        }

        public GameEntity FindById(Guid gameId)
        {
            return gameCollection.Find(game => game.Id == gameId).FirstOrDefault();
        }

        public void Update(GameEntity game)
        {
            gameCollection.ReplaceOne(entity => entity.Id == game.Id, game);
        }

        // Возвращает не более чем limit игр со статусом GameStatus.WaitingToStart
        public IList<GameEntity> FindWaitingToStart(int limit)
        {
            if (limit < 0)
                throw new ArgumentOutOfRangeException(nameof(limit));
            if (limit == 0)
                return new List<GameEntity>();
            return gameCollection.Find(game => game.Status == GameStatus.WaitingToStart)
                .Limit(limit)
                .ToList();
        }

        // Обновляет игру, если она находится в статусе GameStatus.WaitingToStart
        public bool TryUpdateWaitingToStart(GameEntity game)
        {
            var result = gameCollection.ReplaceOne(
                entity => entity.Id == game.Id && entity.Status == GameStatus.WaitingToStart, game);
            return result.IsAcknowledged && result.ModifiedCount == 1;
        }
    }
}
