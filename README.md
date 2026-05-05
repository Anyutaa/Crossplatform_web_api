# Booking System API

REST API для системы бронирования жилья (отель / краткосрочная аренда квартир) на **ASP.NET Core 9** с JWT-аутентификацией, ролевой моделью прав и нестандартной поддержкой авторизации через **Telegram**.

Проект демонстрирует не просто CRUD, а полноценную **бизнес-логику бронирования**: multi-room бронирования, контроль пересечений по датам, фиксацию цены на момент сделки, каскадные действия при блокировке/архивации и согласованные state-машины у трёх ключевых сущностей.

---

## Ключевые возможности

- **Аутентификация**: регистрация и вход по email/паролю, JWT-токены, дополнительный вход через Telegram ID
- **Multi-room бронирование**: одна бронь может содержать несколько комнат
- **Защита от двойного бронирования**: алгоритм проверки пересечений периодов (overlap detection) с учётом статуса брони
- **Историческая корректность цен**: цена комнаты фиксируется в момент бронирования (`PriceAtBooking`) и не меняется при последующих обновлениях прайса
- **Ролевая модель**: `Admin` / `User`, с разделением прав на уровне сервисов
- **State-машины** у трёх сущностей с автоматическими каскадными переходами
- **Soft-delete** через статус `Archived` — данные не теряются, история сохраняется
- **Документация API** через Swagger UI с поддержкой JWT-авторизации прямо в интерфейсе

---

## Стек технологий

| Слой              | Технология                                            |
|-------------------|-------------------------------------------------------|
| Платформа         | .NET 9, ASP.NET Core                                  |
| ORM               | Entity Framework Core (Code-First, Migrations)        |
| БД                | SQLite                                                |
| Аутентификация    | JWT Bearer (`System.IdentityModel.Tokens.Jwt`)        |
| Хеширование паролей | BCrypt.Net                                          |
| Документация API  | Swashbuckle (Swagger / OpenAPI)                       |
| Архитектура       | Layered: Controllers → Services → Data (DbContext)    |

---

## Доменная модель

### State-машины

```
User:     Active  ⇄  Blocked
            ↓
          Archived (terminal)

Room:     Available  ⇄  Maintenance / Blocked
            ↓
          Archived (terminal)

Booking:  Pending  →  Confirmed
            ↓             ↓
          Cancelled    Cancelled
```

### Каскадные правила

Изменение статуса пользователя автоматически распространяется на связанные сущности:

| Действие                  | Что происходит автоматически                                            |
|---------------------------|-------------------------------------------------------------------------|
| `User.Block()`            | Все комнаты пользователя → `Blocked`, активные брони → `Cancelled`     |
| `User.Unblock()`          | Заблокированные комнаты → `Available` (брони не восстанавливаются)     |
| `User.Archive()`          | Все его комнаты → `Archived`, активные брони → `Cancelled`             |
| `Room.Archive()`          | Активные брони этой комнаты → `Cancelled`                              |

### Права

```
Admin       — полный доступ ко всему
Room Owner  — управляет своими комнатами (кроме Blocked — только Admin)
User        — создаёт и отменяет свои брони, не видит чужие
```

---

## Структура проекта

```
.
├── Controller/                  # REST-эндпоинты
│   ├── AuthController.cs        # /api/auth — register, login, telegram
│   ├── UsersController.cs       # /api/users — управление пользователями
│   ├── RoomsController.cs       # /api/rooms — комнаты
│   └── BookingsController.cs    # /api/bookings — бронирования
│
├── Services/                    # Бизнес-логика
│   ├── UserService.cs           # Регистрация, аутентификация, блок/архив
│   ├── RoomService.cs           # CRUD комнат, проверка доступности
│   └── BookingService.cs        # Создание брони, overlap detection, отмена, подтверждение
│
├── Models/                      # Доменные сущности и enums
│   ├── User.cs                  # + UserRole, UserStatus
│   ├── Room.cs                  # + RoomStatus
│   ├── Booking.cs               # + BookingStatus
│   ├── BookingRoom.cs           # join-сущность Many-to-Many
│   └── AuthOption.cs            # POCO для биндинга секции Jwt из конфига
│
├── DTOs/Bookings/               # Data Transfer Objects
│   ├── BookingDto.cs
│   └── BookingRoomDto.cs
│
├── Data/
│   └── ApplicationDbContext.cs  # EF Core DbContext
│
├── Migrations/                  # EF Core миграции
├── Properties/
├── Program.cs                   # Конфигурация: DI, JWT, Swagger, CORS, миграции
├── appsettings.json             # Connection string, JWT, AdminSeed
└── Crossplatform_2_smirnova.sln
```

---

## Запуск

### Требования
- .NET 9 SDK
- Visual Studio 2022 (17.12+) или JetBrains Rider, либо `dotnet` CLI

### Установка

```bash
git clone https://github.com/Anyutaa/Crossplatform_web_api.git
cd Crossplatform_web_api
dotnet restore
```

### Настройка

Откройте `appsettings.json`. Ключевые секции:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Jwt": {
    "Issuer": "BookingSystem",
    "Audience": "BookingClients",
    "LifetimeInHours": 24,
    "SigningKey": "<секретный ключ длиной не менее 32 символов>"
  },
  "AdminSeed": {
    "Email": "admin@example.com",
    "Password": "<пароль администратора>"
  }
}
```

При первом запуске приложение автоматически:
1. Применяет миграции и создаёт `app.db`
2. Создаёт администратора с email/паролем из секции `AdminSeed`

> ⚠️ Для production: вынесите `Jwt.SigningKey` и `AdminSeed.Password` в переменные окружения или secret-менеджер. Не коммитьте реальные секреты в `appsettings.json`.

### Запуск

```bash
dotnet run
```

API стартует на `http://localhost:5000`.

Swagger UI: **http://localhost:5000/swagger**

---

## Основные эндпоинты

| Метод   | Путь                              | Доступ          | Описание                              |
|---------|-----------------------------------|-----------------|---------------------------------------|
| `POST`  | `/api/auth/register`              | Anonymous       | Регистрация, возвращает JWT           |
| `POST`  | `/api/auth/login`                 | Anonymous       | Вход по email/паролю                  |
| `GET`   | `/api/auth/telegram/{tgId}`       | Anonymous       | Вход по Telegram ID                   |
| `GET`   | `/api/rooms`                      | Authenticated   | Доступные комнаты (Admin видит все)   |
| `POST`  | `/api/rooms`                      | Authenticated   | Создать комнату                       |
| `PATCH` | `/api/rooms/{id}`                 | Owner / Admin   | Частичное обновление комнаты          |
| `POST`  | `/api/bookings`                   | Authenticated   | Создать бронь на одну/несколько комнат|
| `POST`  | `/api/bookings/{id}/cancel`       | Owner / Admin   | Отменить бронь                        |
| `POST`  | `/api/bookings/{id}/confirm`      | Admin           | Подтвердить бронь                     |
| `GET`   | `/api/bookings`                   | Authenticated   | Свои брони (Admin — все)              |

Полный список с DTO и кодами ответов — в Swagger UI.

---

## Безопасность

- **Пароли** хешируются через **BCrypt** с генерацией соли. В БД хранится только хеш.
- **JWT-ключ** и **админские реквизиты** вынесены в `appsettings.json`, в коде не хардкодятся.
- **JWT-валидация** проверяет issuer, audience, signing key, lifetime, без `ClockSkew` (минимизация окна на просроченный токен).
- **Защита эндпоинтов** через `[Authorize]` и проверку ролей в сервисном слое.
- **CORS** ограничен заранее заданным origin'ом фронтенда (`http://localhost:3000`).

---

## Связанные репозитории

Проект состоит из трёх компонентов:

| Компонент       | Репозиторий                                                                  | Описание                                       |
|-----------------|------------------------------------------------------------------------------|------------------------------------------------|
| Backend API     | _текущий репозиторий_                                                        | REST API на ASP.NET Core                       |
| Web frontend    | [crossplatform-front](https://github.com/Anyutaa/crossplatform-front)        | Клиентское SPA на React                        |
| Telegram bot    | [crossplatform-bot](https://github.com/Anyutaa/crossplatform-bot)            | Бот для бронирования через Telegram (Python)   |

Эндпоинт `/api/auth/telegram/{tgId}` реализован специально для интеграции с Telegram-ботом: пользователь, написавший `/start` боту, опознаётся по своему Telegram ID и получает JWT-токен — после этого может действовать в системе через тот же API, что и веб-клиент.

---

## Лицензия

MIT
