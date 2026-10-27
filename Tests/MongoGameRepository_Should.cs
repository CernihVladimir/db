using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Domain;
using MongoDB.Driver.Core.WireProtocol.Messages;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class MongoGameRepository_Should
    {
        [SetUp]
        public void SetUp()
        {
            var db = TestMongoDatabase.Create();
            db.DropCollection(MongoGameRepository.CollectionName);
            repo = new MongoGameRepository(db);
        }

        private MongoGameRepository repo;

        [Test]
        public void CreateGame()
        {
            var gameEntity = repo.Insert(new GameEntity(10));
            Console.WriteLine(gameEntity.Id);
            gameEntity.Id.Should().NotBe(Guid.Empty);
        }

        [Test]
        public void FindGameById()
        {
            var gameEntity = repo.Insert(new GameEntity(10));
            repo.FindById(gameEntity.Id).Should().BeEquivalentTo(gameEntity);
        }

        [Test]
        public void UpdateGame()
        {
            var createdGame = repo.Insert(new GameEntity(10));
            var login = "someUserName";
            createdGame.AddPlayer(new UserEntity { Login = login });
            repo.Update(createdGame);
            var retrievedGame = repo.FindById(createdGame.Id);
            retrievedGame.Players.Should().HaveCount(1);
            retrievedGame.Players[0].Name.Should().Be(login);
        }

        [Test]
        public void FindWaitingToStart()
        {
            var playingGame = new GameEntity(10);
            playingGame.AddPlayer(new UserEntity());
            playingGame.AddPlayer(new UserEntity());
            repo.Insert(playingGame);

            var waitingToStartGame1 = new GameEntity(10);
            repo.Insert(waitingToStartGame1);
            var waitingToStartGame2 = new GameEntity(10);
            repo.Insert(waitingToStartGame2);

            var games = repo.FindWaitingToStart(1);
            games.Select(game => game.Id).Should().Equal(waitingToStartGame1.Id);
            games.First().Id.Should().Be(waitingToStartGame1.Id);
        }

        [Test]
        public void TryUpdateWaitingToStart()
        {
            var createdGame = repo.Insert(new GameEntity(10));
            createdGame.AddPlayer(new UserEntity());
            createdGame.AddPlayer(new UserEntity());

            repo.TryUpdateWaitingToStart(createdGame).Should().BeTrue();

            var retrievedGame = repo.FindById(createdGame.Id);
            retrievedGame.Status.Should().Be(GameStatus.Playing);
        }

        [Test]
        public void TryUpdateWaitingToStart_Fails_WhenGameIsNotWaitingToStart()
        {
            var createdGame = repo.Insert(new GameEntity(10));
            createdGame.AddPlayer(new UserEntity());
            createdGame.AddPlayer(new UserEntity());
            repo.Update(createdGame);

            createdGame.Cancel();
            repo.TryUpdateWaitingToStart(createdGame).Should().BeFalse();

            var retrievedGame = repo.FindById(createdGame.Id);
            retrievedGame.Status.Should().Be(GameStatus.Playing);
        }

        [Test]
        public void TryUpdateWaitingToStart_Fails_WhenNoGame()
        {
            repo.TryUpdateWaitingToStart(new GameEntity(10)).Should().BeFalse();
        }

        [Test]
        public void ContinueSavedGameUsingNewRepository()
        {
            var game = repo.Insert(new GameEntity(2));
            var first = new UserEntity(Guid.NewGuid()) { Login = "Human" };
            var second = new UserEntity(Guid.NewGuid()) { Login = "AI" };
            game.AddPlayer(first);
            game.AddPlayer(second);
            game.SetPlayerDecision(first.Id, PlayerDecision.Rock);
            game.SetPlayerDecision(second.Id, PlayerDecision.Scissors);
            game.FinishTurn();
            repo.Update(game);

            var reopenedRepo = new MongoGameRepository(TestMongoDatabase.Create());
            var restored = reopenedRepo.FindById(game.Id);
            restored.Should().BeEquivalentTo(game);
            restored.Should().NotBeSameAs(game);
            restored.SetPlayerDecision(first.Id, PlayerDecision.Paper);
            restored.SetPlayerDecision(second.Id, PlayerDecision.Paper);
            var lastTurn = restored.FinishTurn();
            reopenedRepo.Update(restored);

            lastTurn.TurnNumber.Should().Be(2);
            lastTurn.Result.Should().Be(TurnResult.Draw);
            var finished = repo.FindById(game.Id);
            finished.Status.Should().Be(GameStatus.Finished);
            finished.Players[0].Score.Should().Be(1);
            finished.Players[1].Score.Should().Be(0);
        }

        [Test]
        public void ReturnNoWaitingGamesWhenLimitIsZero()
        {
            repo.Insert(new GameEntity(2));
            repo.FindWaitingToStart(0).Should().BeEmpty();
        }

        [Test]
        public void ReturnNullForUnknownGame()
        {
            repo.FindById(Guid.NewGuid()).Should().BeNull();
        }
    }
}
