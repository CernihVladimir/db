using System;
using System.Linq;
using FluentAssertions;
using Game.Domain;
using MongoDB.Driver;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class MongoGameTurnRepository_Should
    {
        [SetUp]
        public void SetUp()
        {
            var db = TestMongoDatabase.Create();
            db.DropCollection(MongoGameTurnRepository.CollectionName);
            repo = new MongoGameTurnRepository(db);
        }

        private MongoGameTurnRepository repo;

        [Test]
        public void InsertAndGetLastTurns()
        {
            var gameId = Guid.NewGuid();
            var turn1 = new GameTurnEntity
            {
                GameId = gameId,
                TurnNumber = 1,
                FirstPlayerMove = Move.Rock,
                SecondPlayerMove = Move.Scissors,
                Result = TurnResult.FirstPlayerWon,
                CreatedAt = DateTime.UtcNow
            };
            var turn2 = new GameTurnEntity
            {
                GameId = gameId,
                TurnNumber = 2,
                FirstPlayerMove = Move.Paper,
                SecondPlayerMove = Move.Rock,
                Result = TurnResult.FirstPlayerWon,
                CreatedAt = DateTime.UtcNow
            };
            repo.Insert(turn1);
            repo.Insert(turn2);

            var lastTurns = repo.GetLastTurns(gameId, 5);
            lastTurns.Should().HaveCount(2);
            lastTurns[0].TurnNumber.Should().Be(1);
            lastTurns[1].TurnNumber.Should().Be(2);
        }

        [Test]
        public void GetLastTurnsReturnsAtMostFive()
        {
            var gameId = Guid.NewGuid();
            for (int i = 1; i <= 10; i++)
            {
                repo.Insert(new GameTurnEntity
                {
                    GameId = gameId,
                    TurnNumber = i,
                    FirstPlayerMove = Move.Rock,
                    SecondPlayerMove = Move.Paper,
                    Result = TurnResult.SecondPlayerWon,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var lastTurns = repo.GetLastTurns(gameId, 5);
            lastTurns.Should().HaveCount(5);
            lastTurns.Select(t => t.TurnNumber).Should().Equal(new[] { 6, 7, 8, 9, 10 });
        }

        [Test]
        public void HasIndexForFastLastTurnsQuery()
        {
            var db = TestMongoDatabase.Create();
            var collection = db.GetCollection<GameTurnEntity>(MongoGameTurnRepository.CollectionName);
            var indexes = collection.Indexes.List().ToList();
            indexes.Should().Contain(i => i.ToString().Contains("gameId"));
        }
    }
}
