using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder;
using System.Linq;
using Microsoft.Bot.Connector.Authentication;

namespace MSTeamsEventsNotifyer
{
    public static class MessagesTrigger
    {

        private static readonly BotFrameworkAdapter adapter;

        static MessagesTrigger()
        {
            var appId = Environment.GetEnvironmentVariable(@"MS_APP_ID");
            var password = Environment.GetEnvironmentVariable(@"MS_APP_PASSWORD");

           adapter = new BotFrameworkAdapter(new SimpleCredentialProvider(appId, password));
        }

        [FunctionName("Messages")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            CancellationToken hostCancellationToken)
        {
            log.LogInformation("Messages function triggered");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            log.LogTrace($@"Bot got: {requestBody}");

            var activity = JsonConvert.DeserializeObject<Activity>(requestBody);

            try
            {
                await adapter.ProcessActivityAsync(req.Headers[@"Authorization"], activity, BotLogic, hostCancellationToken);
                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error processing incoming activity");
                return new ObjectResult(ex) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        private static async Task BotLogic(ITurnContext context, CancellationToken cancellationToken)
        {
            switch (context.Activity.Type)
            {
                case ActivityTypes.Message:
                    var message = context.Activity.AsMessageActivity();
                    await context.SendActivityAsync($@"You said: {message.Text}", cancellationToken: cancellationToken);
                    break;
                case ActivityTypes.ConversationUpdate:
                    var conversationUpdate = context.Activity.AsConversationUpdateActivity();
                    var botId = context.Activity.Recipient.Id;
                    foreach (var member in conversationUpdate.MembersAdded)
                    {
                        if (member.Id != botId)
                        {
                            await context.SendActivityAsync($@"Welcome to Echobot {member.Name}! Write me something!", cancellationToken: cancellationToken); 
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
