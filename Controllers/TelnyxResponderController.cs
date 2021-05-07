using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telnyx;
using Telnyx.net.Entities;
using Telnyx.net.Entities.Enum.Webhooks;

namespace telnyx_responder.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TelnyxResponderController : ControllerBase
    {
        

        private readonly string TELNYX_API_KEY;
        public ILogger<TelnyxResponderController> Logger { get; }

        public TelnyxResponderController(ILogger<TelnyxResponderController> logger, IConfiguration config)
        {
            Logger = logger;
            TELNYX_API_KEY = config["TELNYX_API_KEY"];
        }

        [HttpPost]
        public async Task<IActionResult> Post(object message)
        {
            dynamic webhook = JsonConvert.DeserializeObject(message.ToString());
            EventType eventType = ToEnum<EventType>(webhook.data.event_type.ToString());            

            if (eventType == EventType.MessageReceived)
            {
                string responseText;
                switch (webhook.data.payload.text.ToString().ToLower())
                {
                    case "pizza":
                        responseText = "Chicago pizza is the best";
                        break;
                    case "ice cream":
                        responseText = "I prefer gelato";
                        break;
                    default:
                        responseText = "Please send either the word ‘pizza’ or ‘ice cream’ for a different response";
                        break;
                }
                string to = webhook.data.payload.to[0].phone_number;
                string from = webhook.data.payload.from.phone_number;
                TelnyxConfiguration.SetApiKey(TELNYX_API_KEY);
                MessagingSenderIdService service = new MessagingSenderIdService();
                NewMessagingSenderId options = new NewMessagingSenderId
                {
                    From = to,
                    To = from,
                    Text = responseText
                };
                try
                {
                    MessagingSenderId messageResponse = await service.CreateAsync(options);
                    Logger.LogInformation($"Sent message with ID: {messageResponse.Id}");
                }
                catch (TelnyxException ex)
                {
                    Logger.LogInformation(ex.Message);
                }
            }

            return Ok();
        }

        public static T ToEnum<T>(string str)
        {
            var enumType = typeof(T);
            foreach (var name in Enum.GetNames(enumType))
            {
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                if (enumMemberAttribute.Value == str) return (T)Enum.Parse(enumType, name);
            }
            return default;
        }
    }
}
