using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts.Kibana.Process
{
	public class KibanaMessage : ConsoleOut
	{
		private static readonly Regex StartedMessage =
			new Regex(@"Server running at https?:\/\/(?<host>[^:]+)(:(?<port>\d*))?");

		public string Message { get; }

		public KibanaMessage(string consoleLine) : base(false, consoleLine)
		{
			if (string.IsNullOrEmpty(consoleLine)) return;
			try
			{
				var message = SimpleJson.DeserializeObject<Dictionary<string,object>>(consoleLine);
				Message = message["message"].ToString();
			}
			catch (Exception)
			{
				throw new Exception($"Cannot deserialize ${consoleLine}");
			}
		}

		public bool TryGetStartedConfirmation(out string host, out int? port)
		{
			var match = StartedMessage.Match(this.Message);
			host = null;
			port = null;

			if (!match.Success) return false;

			host = match.Groups["host"].Value;
			var portValue = match.Groups["port"].Value;

			if (!string.IsNullOrEmpty(portValue))
				port = int.Parse(portValue);

			return true;
		}
	}
}