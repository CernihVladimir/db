using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Domain
{
    public class GameEntity
    {
        [BsonElement]
        private readonly List<Player> players;

        public GameEntity(int turnsCount)
            : this(Guid.Empty, GameStatus.WaitingToStart, turnsCount, 0, new List<Player>())
        {
        }

        [BsonConstructor]
        public GameEntity(Guid id, GameStatus status, int turnsCount, int currentTurnIndex, List<Player> players)
        {
            Id = id;
            Status = status;
            TurnsCount = turnsCount;
            CurrentTurnIndex = currentTurnIndex;
            this.players = players ?? new List<Player>();
        }

        public Guid Id
        {
            get;
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local For MongoDB
            private set;
        }

        public IReadOnlyList<Player> Players => players.AsReadOnly();

        [BsonElement]
        public int TurnsCount { get; }

        public int CurrentTurnIndex { get; private set; }

        public GameStatus Status { get; private set; }

        public void AddPlayer(UserEntity user)
        {
            if (Status != GameStatus.WaitingToStart)
                throw new ArgumentException(Status.ToString());
            players.Add(new Player(user.Id, user.Login));
            if (Players.Count == 2)
                Status = GameStatus.Playing;
        }

        public bool IsFinished()
        {
            return CurrentTurnIndex >= TurnsCount
                   || Status == GameStatus.Finished
                   || Status == GameStatus.Canceled;
        }

        public void Cancel()
        {
            if (!IsFinished())
                Status = GameStatus.Canceled;
        }

        public bool HaveDecisionOfEveryPlayer => Players.All(p => p.Decision.HasValue);

        public void SetPlayerDecision(Guid userId, PlayerDecision decision)
        {
            if (Status != GameStatus.Playing)
                throw new InvalidOperationException(Status.ToString());
            foreach (var player in Players.Where(p => p.UserId == userId))
            {
                if (player.Decision.HasValue)
                    throw new InvalidOperationException(player.Decision.ToString());
                player.Decision = decision;
            }
        }

        public GameTurnEntity FinishTurn()
        {
            if (Players.Count < 2)
                throw new InvalidOperationException();
            if (!Players[0].Decision.HasValue || !Players[1].Decision.HasValue)
                throw new InvalidOperationException();

            var winnerId = Guid.Empty;
            var player0Won = Players[0].Decision.Value.Beats(Players[1].Decision.Value);
            var player1Won = Players[1].Decision.Value.Beats(Players[0].Decision.Value);
            if (player0Won && !player1Won)
            {
                Players[0].Score++;
                winnerId = Players[0].UserId;
            }
            else if (player1Won && !player0Won)
            {
                Players[1].Score++;
                winnerId = Players[1].UserId;
            }
            else
            {
                winnerId = Guid.Empty;
            }

            TurnResult turnResult = TurnResult.Draw;
            if (winnerId == Players[0].UserId)
                turnResult = TurnResult.FirstPlayerWon;
            else if (winnerId == Players[1].UserId)
                turnResult = TurnResult.SecondPlayerWon;
            else
                turnResult = TurnResult.Draw;

            var result = new GameTurnEntity
            {
                GameId = Id,
                TurnNumber = CurrentTurnIndex + 1,
                FirstPlayerMove = (Move)(int)Players[0].Decision.Value,
                SecondPlayerMove = (Move)(int)Players[1].Decision.Value,
                Result = turnResult,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var player in Players)
                player.Decision = null;
            CurrentTurnIndex++;
            if (CurrentTurnIndex == TurnsCount)
                Status = GameStatus.Finished;
            return result;
        }
    }
}
