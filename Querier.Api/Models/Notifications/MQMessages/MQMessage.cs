using System.Text;
using Newtonsoft.Json;
using Querier.Api.Models.Interfaces;

namespace Querier.Api.Models.Notifications.MQMessages
{
    public abstract class MQMessage : IMQMessage
    {
        public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }

        public string ToJSONString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static T FromBytes<T>(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}