using System;
using System.Linq;
using FluentAssertions;
using Game.Domain;
using MongoDB.Bson;
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
            indexes.Should().Contain(index => index["key"].AsBsonDocument.Equals(
                new BsonDocument { { "gameId", 1 }, { "turnNumber", -1 } }));
        }

        [Test]
        public void ReadSavedTurnUsingNewRepository()
        {
            var turn = repo.Insert(new GameTurnEntity
            {
                GameId = Guid.NewGuid(),
                TurnNumber = 1,
                FirstPlayerMove = Move.Scissors,
                SecondPlayerMove = Move.Paper,
                Result = TurnResult.FirstPlayerWon,
                CreatedAt = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc)
            });

            var reopened = new MongoGameTurnRepository(TestMongoDatabase.Create());

            reopened.GetLastTurns(turn.GameId, 5).Single().Should().BeEquivalentTo(turn);
        }

        [Test]
        public void UseIndexForLatestFiveWithoutScanningOtherGames()
        {
            var db = TestMongoDatabase.Create();
            var collection = db.GetCollection<GameTurnEntity>(MongoGameTurnRepository.CollectionName);
            var gameId = Guid.NewGuid();
            var otherGameId = Guid.NewGuid();
            collection.InsertMany(Enumerable.Range(1, 1000).Select(number => new GameTurnEntity
            {
                GameId = number % 2 == 0 ? gameId : otherGameId,
                TurnNumber = number,
                FirstPlayerMove = Move.Rock,
                SecondPlayerMove = Move.Rock,
                Result = TurnResult.Draw,
                CreatedAt = DateTime.UtcNow
            }));

            repo.GetLastTurns(gameId, 5).Select(turn => turn.TurnNumber)
                .Should().Equal(992, 994, 996, 998, 1000);

            var filter = Builders<GameTurnEntity>.Filter.Eq(turn => turn.GameId, gameId)
                .Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
            var explain = db.RunCommand<BsonDocument>(new BsonDocument
            {
                { "explain", new BsonDocument
                    {
                        { "find", MongoGameTurnRepository.CollectionName },
                        { "filter", filter },
                        { "sort", new BsonDocument("turnNumber", -1) },
                        { "limit", 5 }
                    }
                },
                { "verbosity", "executionStats" }
            });

            explain["executionStats"]["nReturned"].ToInt32().Should().Be(5);
            explain["executionStats"]["totalDocsExamined"].ToInt32().Should().Be(5);
            var plan = explain["queryPlanner"]["winningPlan"].ToJson();
            plan.Should().Contain("IXSCAN").And.NotContain("\"SORT\"");
        }

        [Test]
        public void ReturnNoTurnsWhenCountIsZero()
        {
            var turn = repo.Insert(new GameTurnEntity { GameId = Guid.NewGuid(), TurnNumber = 1 });
            repo.GetLastTurns(turn.GameId, 0).Should().BeEmpty();
        }
    }
}
