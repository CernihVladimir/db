using System;
using System.Linq;
using Game.Domain;
using MongoDB.Driver;

namespace ConsoleApp
{
    class Program
    {
        private readonly IUserRepository userRepo;
        private readonly IGameRepository gameRepo;
        private readonly IGameTurnRepository gameTurnRepo;
        private readonly Random random = new Random();

        private Program(string[] args)
        {
            var mongoConnectionString = Environment.GetEnvironmentVariable("PROJECT5100_MONGO_CONNECTION_STRING")
                                        ?? "mongodb://localhost:27017?maxConnecting=100";
            var mongoClient = new MongoClient(mongoConnectionString);
            var database = mongoClient.GetDatabase("game");

            userRepo = new MongoUserRepository(database);
            gameRepo = new MongoGameRepository(database);
            gameTurnRepo = new MongoGameTurnRepository(database);
        }

        public static void Main(string[] args)
        {
            new Program(args).RunMenuLoop();
        }

        private void RunMenuLoop()
        {
            var humanUser = userRepo.GetOrCreateByLogin("Human");
            var aiUser = userRepo.GetOrCreateByLogin("AI");
            var game = FindCurrentGame(humanUser);
            FindCurrentGame(aiUser);
            humanUser = userRepo.FindById(humanUser.Id);
            aiUser = userRepo.FindById(aiUser.Id);
            game ??= StartNewGame(humanUser);
            if (!TryJoinToGame(game, aiUser))
            {
                Console.WriteLine("Can't add AI user to the game");
                return;
            }

            if (game.CurrentTurnIndex > 0)
            {
                Console.WriteLine($"Resuming game at turn {game.CurrentTurnIndex + 1} of {game.TurnsCount}.");
                ShowScore(game);
            }

            while (HandleOneGameTurn(humanUser.Id))
            {
            }

            var isPaused = userRepo.FindById(humanUser.Id).CurrentGameId.HasValue;
            Console.WriteLine(isPaused ? "Game paused. Run again to continue." : "Game is finished");
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        private GameEntity StartNewGame(UserEntity user)
        {
            Console.WriteLine("Enter desired number of turns in game:");
            if (!int.TryParse(Console.ReadLine(), out var turnsCount) || turnsCount <= 0)
            {
                turnsCount = 5;
                Console.WriteLine($"Bad input. Use default value for turns count: {turnsCount}");
            }

            var game = new GameEntity(turnsCount);
            game.AddPlayer(user);
            var savedGame = gameRepo.Insert(game);

            user.CurrentGameId = savedGame.Id;
            userRepo.Update(user);

            return savedGame;
        }

        private bool TryJoinToGame(GameEntity game, UserEntity user)
        {
            if (IsUserInGame(user, game))
                return true;

            if (user.CurrentGameId.HasValue && user.CurrentGameId.Value != game.Id)
                return false;

            if (game.Players.Any(player => player.UserId == user.Id))
            {
                user.CurrentGameId = game.Id;
                userRepo.Update(user);
                return true;
            }

            if (game.Status != GameStatus.WaitingToStart)
                return false;

            game.AddPlayer(user);
            if (!gameRepo.TryUpdateWaitingToStart(game))
                return false;

            user.CurrentGameId = game.Id;
            userRepo.Update(user);

            return true;
        }

        private static bool IsUserInGame(UserEntity user, GameEntity game)
        {
            return user.CurrentGameId.HasValue
                   && user.CurrentGameId.Value == game.Id
                   && game.Players.Any(p => p.UserId == user.Id);
        }

        private GameEntity FindCurrentGame(UserEntity humanUser)
        {
            if (humanUser.CurrentGameId == null) return null;
            var game = gameRepo.FindById(humanUser.CurrentGameId.Value);
            if (game == null)
            {
                humanUser.CurrentGameId = null;
                userRepo.Update(humanUser);
                return null;
            }
            switch (game.Status)
            {
                case GameStatus.WaitingToStart:
                case GameStatus.Playing:
                    return game;
                case GameStatus.Finished:
                case GameStatus.Canceled:
                    UpdatePlayersWhenGameFinished(game);
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool HandleOneGameTurn(Guid humanUserId)
        {
            var game = GetGameByUser(humanUserId);

            if (game.IsFinished())
            {
                UpdatePlayersWhenGameFinished(game);
                return false;
            }

            PlayerDecision? decision = AskHumanDecision();
            if (!decision.HasValue)
                return false;
            game.SetPlayerDecision(humanUserId, decision.Value);

            var aiPlayer = game.Players.First(p => p.UserId != humanUserId);
            game.SetPlayerDecision(aiPlayer.UserId, GetAiDecision());

            if (game.HaveDecisionOfEveryPlayer)
            {
                var turn = game.FinishTurn();
                gameTurnRepo.Insert(turn);
            }

            gameRepo.Update(game);
            ShowScore(game);
            return true;
        }

        private GameEntity GetGameByUser(Guid userId)
        {
            var user = userRepo.FindById(userId) ?? throw new Exception($"Unknown user with id {userId}");
            var userCurrentGameId = user.CurrentGameId ?? throw new Exception($"No current game for user: {user}");
            return gameRepo.FindById(userCurrentGameId);
        }

        private PlayerDecision GetAiDecision()
        {
            return (PlayerDecision)random.Next(1, 4);
        }

        private void UpdatePlayersWhenGameFinished(GameEntity game)
        {
            // Вместо этого кода можно написать специализированный метод в userRepo, который сделает все эти обновления за одну операцию UpdateMany.
            // Вместо 4 запросов к БД будет 1, но усложнится репозиторий. В данном случае, это редкая операция, поэтому нет смысла оптимизировать.
            foreach (var player in game.Players)
            {
                var playerUser = userRepo.FindById(player.UserId);
                if (playerUser == null || playerUser.CurrentGameId != game.Id) continue;
                playerUser.ExitGame();
                userRepo.Update(playerUser);
            }
        }

        private static PlayerDecision? AskHumanDecision()
        {
            Console.WriteLine();
            Console.WriteLine("Select your next decision:");
            Console.WriteLine("1 - Rock");
            Console.WriteLine("2 - Scissors");
            Console.WriteLine("3 - Paper");
            Console.WriteLine("Esc - pause and exit");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == '1') return PlayerDecision.Rock;
                if (key.KeyChar == '2') return PlayerDecision.Scissors;
                if (key.KeyChar == '3') return PlayerDecision.Paper;
                if (key.Key == ConsoleKey.Escape) return null;
            }
        }

        private void ShowScore(GameEntity game)
        {
            var players = game.Players;
            var lastTurns = gameTurnRepo.GetLastTurns(game.Id, 5);
            if (lastTurns.Count > 0)
            {
                Console.WriteLine("Last turns:");
                foreach (var turn in lastTurns)
                {
                    string firstMove = turn.FirstPlayerMove.ToString();
                    string secondMove = turn.SecondPlayerMove.ToString();
                    string result = turn.Result switch
                    {
                        TurnResult.FirstPlayerWon => $"Winner: {players[0].Name}",
                        TurnResult.SecondPlayerWon => $"Winner: {players[1].Name}",
                        _ => "Draw"
                    };
                    Console.WriteLine($"Turn {turn.TurnNumber}: {players[0].Name}={firstMove}, {players[1].Name}={secondMove}, Result={result}");
                }
            }
            Console.WriteLine($"Score: {players[0].Name} {players[0].Score} : {players[1].Score} {players[1].Name}");
        }
    }
}
