using System;
using System.Text;
using Newtonsoft.Json;

namespace Querier.Api.Models.Notifications.MQMessages
{
	public enum ToastType
	{
		Standard,
		Success,
		Danger
	}

	[Serializable]
	public class ToastMessage
	{
		public ToastType Type { get; set; } = ToastType.Standard;
		public string Recipient { get; set; } = "";
		public string TitleCode { get; set; } = "lbl-undefined";
		public string ContentCode { get; set; } = "lbl-undefined";
		public string ContentDownloadURL { get; set; } = "";
		public string ContentDownloadsFilename { get; set; } = "";
		public bool Closable { get; set; } = false;
		public bool Persistent { get; set; } = false;

		public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(this.ToJSONString());
        }

		public string ToJSONString()
        {
			try
			{
	            string result = JsonConvert.SerializeObject(this);
				return result;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return "";
			}
        }
	}
}