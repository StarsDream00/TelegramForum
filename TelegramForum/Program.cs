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
config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));

Dictionary<long, Dictionary<int, int>> data = new();
if (!File.Exists("data.json"))
{
    File.WriteAllText("data.json", JsonSerializer.Serialize(data));
}
data = JsonSerializer.Deserialize<Dictionary<long, Dictionary<int, int>>>(File.ReadAllText("data.json"));

TelegramBotClient botClient = new(config.Token, string.IsNullOrWhiteSpace(config.Proxy) ? default : new(new HttpClientHandler()
{
    Proxy = new WebProxy(config.Proxy)
}));
botClient.StartReceiving((_, update, _) =>
{
    if (update.Type is not UpdateType.Message || update.Message.From.Id != update.Message.Chat.Id)
    {
        return;
    }
    if (update.Message.Type is MessageType.Text && update.Message.Text.StartsWith('/'))
    {
        if (update.Message.Text is "/delete")
        {
            if (update.Message.ReplyToMessage is not null && data.ContainsKey(update.Message.From.Id) && data[update.Message.From.Id].ContainsKey(update.Message.ReplyToMessage.MessageId))
            {
                _ = botClient.DeleteMessageAsync(config.ChatId, data[update.Message.From.Id][update.Message.ReplyToMessage.MessageId]);
                _ = data[update.Message.From.Id].Remove(update.Message.ReplyToMessage.MessageId);
                File.WriteAllText("data.json", JsonSerializer.Serialize(data));
                _ = botClient.SendTextMessageAsync(update.Message.From.Id, "帖子删除成功", default, default, default, default, default, update.Message.ReplyToMessage.MessageId);
                return;
            }
            _ = botClient.SendTextMessageAsync(update.Message.From.Id, "帖子删除失败：帖子不存在", default, default, default, default, default, update.Message.MessageId);
        }
        return;
    }
    Message forwardMessage = botClient.ForwardMessageAsync(config.ChatId, update.Message.From.Id, update.Message.MessageId).Result;
    if (!data.ContainsKey(update.Message.From.Id))
    {
        data.Add(update.Message.From.Id, new());
    }
    data[update.Message.From.Id].Add(forwardMessage.MessageId, update.Message.MessageId);
    File.WriteAllText("data.json", JsonSerializer.Serialize(data));
    _ = botClient.SendTextMessageAsync(update.Message.From.Id, "帖子发布成功", default, default, default, default, default, update.Message.MessageId);
}, (_, ex, _) =>
{
    logger.Error(ex.Message);
});

while (true)
{
    _ = Console.Read();
}
