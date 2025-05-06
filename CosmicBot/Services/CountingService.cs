using CosmicBot.DAL;
using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace CosmicBot.Services
{
    public class CountingService
    {
        private readonly DataContext _context;

        public CountingService(DataContext context)
        {
            _context = context;
        }

        public async Task TryAddCount(ulong guildId, ulong countingChannelId, SocketUserMessage userMessage)
        {
            if (!Regex.IsMatch(userMessage.Content, @"^\d+$"))
                return;

            ulong number;
            if (!ulong.TryParse(userMessage.Content, out number))
                return;

            var currentCount = _context.Counts.FirstOrDefault(c => c.GuildId == guildId);
            if (currentCount == null)
            {
                if (number != 1)
                    return;

                currentCount = new Models.Counts()
                {
                    GuildId = guildId,
                    LastUserId = userMessage.Author.Id,
                    Count = 1
                };

                await _context.Counts.AddAsync(currentCount);
                await _context.SaveChangesAsync();
                await userMessage.AddReactionAsync(new Emoji("✅"));
                return;
            }

            if (currentCount.Count == 0 && number != 1) return;

            if (number == currentCount.Count + 1 && userMessage.Author.Id != currentCount.LastUserId)
            {
                currentCount.Count++;
                currentCount.LastUserId = userMessage.Author.Id;
                if(currentCount.Count > currentCount.Record)
                    await userMessage.AddReactionAsync(new Emoji("☑️"));
                else
                    await userMessage.AddReactionAsync(new Emoji("✅"));
            }
            else
            {
                await userMessage.AddReactionAsync(new Emoji("❌"));
                if (currentCount.Count > currentCount.Record)
                {
                    currentCount.Record = currentCount.Count;
                    var currentChannel = userMessage.Channel as ITextChannel;
                    if (currentChannel != null)
                    {
                        var pins = await currentChannel.GetPinnedMessagesAsync();
                        if (pins != null)
                        {
                            var pinsByBot = pins.Where(p => p.Author.IsBot);
                            foreach (var pin in pinsByBot)
                            {
                                if (pin is IUserMessage lastPinnedRecord)
                                    await lastPinnedRecord.UnpinAsync();
                            }
                        }
                    }

                    var newRecordMessage = await userMessage.Channel.SendMessageAsync($"Record: **{currentCount.Record}** by <@{currentCount.LastUserId}>");
                    await newRecordMessage.PinAsync();
                }
                if (userMessage.Author.Id == currentCount.LastUserId)
                    await userMessage.Channel.SendMessageAsync("❌ Oops! Count broken. Same user can't count twice in a row. Start again from **1**.");
                else
                    await userMessage.Channel.SendMessageAsync($"❌ Oops! Count broken. I was expecting **{currentCount.Count + 1}**. Start again from **1**.");

                currentCount.Count = 0;
                currentCount.LastUserId = null;
            }
            _context.Update(currentCount);
            await _context.SaveChangesAsync();
        }
    }
}
