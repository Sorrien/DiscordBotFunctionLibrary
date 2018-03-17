using Discord;
using Discord.WebSocket;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBotFunctionLibrary
{
    public class RoleManagementHelper
    {
        public static Task UpdateGuildRoles(SocketGuild guild, List<string> roleNames)
        {
            var guildRoles = guild.Roles.ToList();
            Parallel.ForEach(guild.Users, async (user) =>
            {
                await UpdateRolesWithRetry(user, guildRoles, roleNames);
            });

            return Task.FromResult(0);
        }

        public static async Task UpdateRolesWithRetry(SocketGuildUser user, List<SocketRole> guildRoles, List<string> roleNames)
        {
            await Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(20, retryAttempt =>
                   TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                   .ExecuteAsync(async () =>
                   {
                       await UpdateRoles(user, guildRoles, roleNames);
                   });
        }

        public static async Task UpdateRoles(SocketGuildUser user, List<SocketRole> guildRoles, List<string> roleNames)
        {
            var RolesToAdd = new List<IRole>();
            var RolesToRemove = new List<IRole>();

            foreach (var name in roleNames)
            {
                var role = guildRoles.FirstOrDefault(x => x.Name == name);
                var hasRole = user.Roles.Contains(role);
                var shouldHaveRole = false;

                switch (name)
                {
                    case "In Game":
                        shouldHaveRole = UserIsInGame(user);
                        break;
                    case "Streaming":
                        shouldHaveRole = UserIsStreaming(user);
                        break;
                    default:
                        shouldHaveRole = hasRole;
                        break;
                }
                if (shouldHaveRole && !hasRole)
                {
                    RolesToAdd.Add(role);
                }
                else if (!shouldHaveRole && hasRole)
                {
                    RolesToRemove.Add(role);
                }
            }
            foreach (var Role in RolesToAdd)
            {
                await user.AddRoleAsync(Role);
            }
            foreach (var Role in RolesToRemove)
            {
                await user.RemoveRoleAsync(Role);
            }
        }

        public static bool UserIsInGame(SocketGuildUser user)
        {
            return user.Game.HasValue;
        }

        public static bool UserIsStreaming(SocketGuildUser user)
        {
            return UserIsInGame(user) && user.Game.Value.StreamType != StreamType.NotStreaming;
        }
    }
}
