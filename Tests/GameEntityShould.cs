using System;
using FluentAssertions;
using Game.Domain;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class GameEntityShould
    {
        [TestCase(PlayerDecision.Rock, PlayerDecision.Rock, TurnResult.Draw)]
        [TestCase(PlayerDecision.Rock, PlayerDecision.Scissors, TurnResult.FirstPlayerWon)]
        [TestCase(PlayerDecision.Rock, PlayerDecision.Paper, TurnResult.SecondPlayerWon)]
        [TestCase(PlayerDecision.Scissors, PlayerDecision.Rock, TurnResult.SecondPlayerWon)]
        [TestCase(PlayerDecision.Scissors, PlayerDecision.Scissors, TurnResult.Draw)]
        [TestCase(PlayerDecision.Scissors, PlayerDecision.Paper, TurnResult.FirstPlayerWon)]
        [TestCase(PlayerDecision.Paper, PlayerDecision.Rock, TurnResult.FirstPlayerWon)]
        [TestCase(PlayerDecision.Paper, PlayerDecision.Scissors, TurnResult.SecondPlayerWon)]
        [TestCase(PlayerDecision.Paper, PlayerDecision.Paper, TurnResult.Draw)]
        public void FinishTurnRecordsMovesAndWinner(PlayerDecision first, PlayerDecision second, TurnResult expected)
        {
            var game = CreateGame(1);
            game.Players[0].Decision = first;
            game.Players[1].Decision = second;

            var turn = game.FinishTurn();

            turn.GameId.Should().Be(game.Id);
            turn.TurnNumber.Should().Be(1);
            turn.FirstPlayerMove.Should().Be((Move)first);
            turn.SecondPlayerMove.Should().Be((Move)second);
            turn.Result.Should().Be(expected);
            turn.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
            game.Players[0].Score.Should().Be(expected == TurnResult.FirstPlayerWon ? 1 : 0);
            game.Players[1].Score.Should().Be(expected == TurnResult.SecondPlayerWon ? 1 : 0);
            game.Players.Should().OnlyContain(player => !player.Decision.HasValue);
            game.CurrentTurnIndex.Should().Be(1);
            game.Status.Should().Be(GameStatus.Finished);
        }

        [Test]
        public void PreservePreviousTurnWhenNextTurnIsPlayed()
        {
            var game = CreateGame(2);
            game.Players[0].Decision = PlayerDecision.Rock;
            game.Players[1].Decision = PlayerDecision.Scissors;
            var firstTurn = game.FinishTurn();

            game.Players[0].Decision = PlayerDecision.Paper;
            game.Players[1].Decision = PlayerDecision.Paper;
            var secondTurn = game.FinishTurn();

            firstTurn.FirstPlayerMove.Should().Be(Move.Rock);
            firstTurn.SecondPlayerMove.Should().Be(Move.Scissors);
            firstTurn.Result.Should().Be(TurnResult.FirstPlayerWon);
            secondTurn.TurnNumber.Should().Be(2);
            secondTurn.Result.Should().Be(TurnResult.Draw);
        }

        [Test]
        public void RejectIncompleteTurnWithoutChangingScore()
        {
            var game = CreateGame(2);
            game.Players[0].Decision = PlayerDecision.Rock;

            Action finish = () => game.FinishTurn();

            finish.Should().Throw<InvalidOperationException>();
            game.CurrentTurnIndex.Should().Be(0);
            game.Players.Should().OnlyContain(player => player.Score == 0);
        }

        [Test]
        public void RejectTurnOfCanceledGame()
        {
            var game = CreateGame(2);
            game.Players[0].Decision = PlayerDecision.Rock;
            game.Players[1].Decision = PlayerDecision.Paper;
            game.Cancel();

            Action finish = () => game.FinishTurn();

            finish.Should().Throw<InvalidOperationException>();
            game.CurrentTurnIndex.Should().Be(0);
            game.Players.Should().OnlyContain(player => player.Score == 0);
        }

        private static GameEntity CreateGame(int turns)
        {
            var game = new GameEntity(turns);
            // Empty ids also ensure draws aren't confused with a winning player's id.
            game.AddPlayer(new UserEntity { Login = "Human" });
            game.AddPlayer(new UserEntity { Login = "AI" });
            return game;
        }
    }
}
