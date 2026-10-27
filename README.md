# Работа с БД на примере MongoDB

Запуск готовой игры, всех тестов и проверка в Compass: [RUNNING.md](RUNNING.md).

`Game` - библиотека классов для реализации мультиплеерной игры Камень Ножницы Бумага.
`ConsoleApp` - консольное приложение для игры с компьютером.

В этой задаче нужно доработать приложение `ConsoleApp` так, чтобы оно хранило все данные в MongoDB.

К счастью, вся работа с БД уже изолирована за интерфейсами репозиториев `IUserRepository` и `IGameRepository`.
Сейчас у этих интерфейсов есть реализации для хранения всех данных в памяти.
Требуется создать новые реализации, для хранения данных в MongoDB. Ниже вся задача разбита на этапы:

## 0. Установка MongoDB

Для изучения нужно развернуть MongoDB локально. Есть два способа:
1. Установить MongoDB Community Edition. Скачать её можно [отсюда](https://www.mongodb.com/try/download/community).
2. Развернуть локально Docker-образ MongoDB по [инструкции](https://www.mongodb.com/docs/manual/tutorial/install-mongodb-community-with-docker/).

## 1. Bson сериализация

Сначала нужно подготовить сами классы сущностей к тому, чтобы их мог сохранять и загружать из базы драйвер MongoDB.
Драйвер делает это с помощью Bson сериализатора. Можно проверить работу сериализации отдельно от работы с сервером Mongo.

1. Изучи, а потом запусти тест `BsonSerializationTest`.
   `UserEntity` должен сериализоваться без ошибок, а `GameEntity` нет.
   Нужно настроить сериализацию `GameEntity`.
2. Добавь атрибут [BsonConstructor] над конструктором с полным набором аргументов.
3. Добавь атрибут [BsonElement] над всеми readonly-свойствами и readonly-полями, потому что по умолчанию Mongo их игнорирует.
   То же самое нужно сделать со всеми вложенными в сериализуемую сущность классами.
4. Запусти тест и проверь, что теперь класс `GameEntity` успешно сериализуется

Более подробно про настройку сериализации в Bson читай в документации:
http://mongodb.github.io/mongo-csharp-driver/2.10/reference/bson/mapping/


## 2. UserRepository

### MongoUserRepository

1. Реализуй в классе `MongoUserRepository` все методы.
   Тебе поможет документация.
   Операции чтения: http://mongodb.github.io/mongo-csharp-driver/2.10/reference/driver/crud/reading/
   Операции записи: http://mongodb.github.io/mongo-csharp-driver/2.10/reference/driver/crud/writing/
   Проверь работоспособность тестами на этот репозиторий.
2. С помощью MongoDB Compass убедись, что после выполнения тестов, в базе `game-tests` создаются коллекции и документы.


### Индекс по UserEntity.Login

1. Запусти тест `SearchByLoginFast`. Запомни, как долго он работал.
2. Ускорить операцию поиска по логину можно создав индекс.
   Так как логины у пользователей должны быть уникальными, стоит сразу сделать индекс со свойством "уникальный".
   При наличии такого индекса MongoDB будет самостоятельно проверять, что логины в сохраненных сущностях не повторяются.
   Для простоты добавь создание индекса в конструкторе репозитория. Не волнуйся, MongoDB игнорирует повторное создание индекса.
   Тебе поможет документация http://mongodb.github.io/mongo-csharp-driver/2.10/reference/driver/admin/#indexes
3. Повторно запусти тест `SearchByLoginFast` и сравни время работы с первоначальным.
4. С помощью MongoDB Compass убедись, что после выполнения тестов, у коллекции users появился индекс и он используется.
5. Раз ты создал уникальный индекс, то тест `LoginDuplicateNotAllowed` также должен проходить. Проверь это.


### Users в ConsoleApp

1. Поменяй `InMemoryUserRepository` на `MongoUserRepository` в `ConsoleApp`.
   Пример создания Mongo репозитория смотри в тестах на репозитории.
2. С помощью MongoDB Compass проверь, что после окончания игры у пользователя обновляется количество сыгранных игр в БД.


## 3. GameRepository

### MongoGameRepository

1. Реализуй в классе `MongoGameRepository` все методы.
   Тебе поможет документация.
   Операции чтения: http://mongodb.github.io/mongo-csharp-driver/2.10/reference/driver/crud/reading/
   Операции записи: http://mongodb.github.io/mongo-csharp-driver/2.10/reference/driver/crud/writing/
2. Проверь работоспособность тестами на этот репозиторий.
3. С помощью MongoDB Compass убедись, что после выполнения тестов, в базе game-tests создаются коллекции и документы.


### Games в ConsoleApp

1. Поменяй `InMemoryGameRepository` на `MongoGameRepository` в `ConsoleApp`.
2. Проверь, что прерванная на середине игра сохраняет состояние в БД и при перезапуске продолжается.


## 4. Бонус-задача «История игры»

Самостоятельно спроектируй ещё одну сущность: `GameTurnEntity` — это информация об одном завершённом туре игры.
В рамках тура каждый игрок называет Камень Ножницы или Бумага, и определяется исход тура — победитель тура или ничья.
Объект этого типа возвращает метод `GameEntity.FinishTurn()`.

Спроектируй репозиторий этих сущностей. Добавь его использование в `ConsoleApp` в двух местах, помеченных TODO-комментариями:

1. Сохраняй эту сущность в БД в конце каждого тура.
2. В методе `ShowScore` получай из репозитория последние 5 туров и показывай информацию о том,
   кто что назвал и кто в итоге выиграл тур.

Убедись, что у тебя есть правильный индекс, для того, чтобы запрос последних 5 туров работал быстро.
Можешь создать соответствующий тест, чтобы это проверить.


## Статьи и документация

[.NET MongoDB Driver Reference](http://mongodb.github.io/mongo-csharp-driver/2.10/)


## Статьи про проектирование в Монго

Вводные о моделировании данных: 

https://docs.mongodb.com/manual/core/data-modeling-introduction/
https://docs.mongodb.com/manual/core/data-model-design/

Ограничение на размер документа 16 Мб и другие ограничения. https://docs.mongodb.com/manual/reference/limits/#BSON-Document-Size 

О том, как принимать решения о проектировании БД:

* https://www.mongodb.com/blog/post/6-rules-of-thumb-for-mongodb-schema-design-part-1
* https://www.mongodb.com/blog/post/6-rules-of-thumb-for-mongodb-schema-design-part-2
* https://www.mongodb.com/blog/post/6-rules-of-thumb-for-mongodb-schema-design-part-3
