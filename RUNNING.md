# Запуск и проверка

Нужны .NET SDK 8.0 или новее и работающая локальная MongoDB.
По умолчанию приложение подключается к `mongodb://localhost:27017`.
Другой адрес можно задать переменной окружения `PROJECT5100_MONGO_CONNECTION_STRING`.

## Игра

Открой терминал в папке с `db.sln` и выполни:

```powershell
dotnet run --project ConsoleApp/ConsoleApp.csproj
```

При первом запуске введи количество туров. Ходы: `1` — камень,
`2` — ножницы, `3` — бумага. После каждого тура показываются счёт,
последние пять туров, ходы обоих игроков и победитель либо ничья.

Чтобы прерваться, нажми `Esc`, затем `Enter`. Повторный запуск продолжает
сохранённую игру с прежним счётом и историей. После последнего тура
увеличивается `GamesPlayed` у обоих пользователей и очищается `CurrentGameId`.

## Тесты

В Windows дважды щёлкни `run-tests.cmd`: он запускает обычные тесты,
затем три дополнительные проверки индекса, уникальности и параллельных запросов.
Окно останется открытым с результатами.

Из терминала обычные тесты можно запустить отдельно:

```powershell
dotnet test Tests/Tests.csproj
```

Дополнительные тесты помечены `[Explicit]` и выбираются напрямую через NUnit:

```powershell
dotnet test Tests/Tests.csproj -- NUnit.Where="test == Tests.MongoUserRepositoryShould.SearchByLoginFast or test == Tests.MongoUserRepositoryShould.LoginDuplicateNotAllowed or test == Tests.MongoUserRepositoryShould.MassiveConcurrentCreateUser"
```

Тесты пересоздают свои коллекции в отдельной базе `game-tests`.
Данные консольной игры находятся в базе `game`.

## Проверка в MongoDB Compass

Подключись к `mongodb://localhost:27017` и открой базу `game`:

- `users` — пользователи, число законченных игр, ссылка на текущую игру;
- `games` — статус, число туров, номер текущего тура, игроки и счёт;
- `gameTurns` — ходы и результат каждого завершённого тура.

На вкладке **Indexes** доступны:

- уникальный `Login_1` в `users`;
- `Status_1` в `games` для поиска ожидающих игр;
- `gameId_1_turnNumber_-1` в `gameTurns` для последних туров конкретной игры.

Для проверки запроса истории задай фильтр по `gameId`, сортировку
`{ "turnNumber": -1 }` и limit `5`, затем открой план выполнения (**Explain**).
Индекс должен использоваться без отдельной сортировки. Автоматический тест
`UseIndexForLatestFiveWithoutScanningOtherGames` проверяет это на 1000 документах:
из них читаются только пять подходящих.
