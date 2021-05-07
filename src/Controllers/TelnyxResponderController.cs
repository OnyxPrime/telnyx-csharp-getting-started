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
        public async Task<IActionResult> Post(object messageBody)
        {
            // Deserialize incoming message
            dynamic message = JsonConvert.DeserializeObject(messageBody.ToString());
            EventType eventType = ToEnum<EventType>(message.data.event_type.ToString());            

            // Respond only to received messages, not status updates
            if (eventType == EventType.MessageReceived)
            {
                // Determing how to repond to sender
                string responseText;
                switch (message.data.payload.text.ToString().ToLower())
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

                // Create return message
                string to = message.data.payload.to[0].phone_number;
                string from = message.data.payload.from.phone_number;
                TelnyxConfiguration.SetApiKey(TELNYX_API_KEY);
                NewMessagingSenderId options = new NewMessagingSenderId
                {
                    From = to,
                    To = from,
                    Text = responseText
                };
                
                // Send message back to sender
                MessagingSenderIdService service = new MessagingSenderIdService();
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
