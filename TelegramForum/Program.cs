using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

Logger logger = LogManager.GetCurrentClassLogger();

Config config = new()
{
    Token = default,
    ChatId = default,
    Proxy = default
};
if (!File.Exists("config.json"))
{
    File.WriteAllText("config.json", JsonSerializer.Serialize(config));
}
config = JsonSerializer.Deserialize<Config>("config.json");

Dictionary<int, Datum> data = new();
if (!File.Exists("data.json"))
{
    File.WriteAllText("data.json", JsonSerializer.Serialize(data));
}
data = JsonSerializer.Deserialize<Dictionary<int, Datum>>("data.json");

TelegramBotClient botClient = new(config.Token, string.IsNullOrWhiteSpace(config.Proxy) ? new(new HttpClientHandler()
{
    Proxy = new WebProxy(config.Proxy)
}) : default);
botClient.StartReceiving((_, update, _) =>
{
    if (update.Type is not UpdateType.Message)
    {
        return;
    }
    Message forwardMessage = botClient.ForwardMessageAsync(config.ChatId, update.Message.SenderChat.Id, update.Message.MessageId).Result;
    data.Add(update.Message.MessageId, new()
    {
        MessageId = forwardMessage.MessageId,
        UserId = update.Message.SenderChat.Id
    });
    File.WriteAllText("data.json", JsonSerializer.Serialize(data));
}, (_, ex, _) =>
{
    logger.Error(ex);
});
while (true)
{
    _ = Console.Read();
}