using System.Text.RegularExpressions;

namespace Chronos.Configuration
{
    public class RabbitMqConnectionString
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        private const string Pattern =
            @"^rabbitMq://?(<Host>[^:]+):(?<Port>[^:]+):(?<UserName>[^@]+)@(?<Password>.*))?$";

        public static RabbitMqConnectionString Parse(string s)
        {
            var match = Regex.Match(s, Pattern);

            return new RabbitMqConnectionString
            {
                Host = match.Groups["Host"].Value,
                Port = int.Parse(match.Groups["Port"].Value),
                Password = match.Groups["Password"].Value,
                Username = match.Groups["Username"].Value
            };
        }

        public static RabbitMqConnectionString Empty = new RabbitMqConnectionString
        {
            Host = "",
            Password = "",
            Username = ""
        };
        public static bool TryParse(string s, out RabbitMqConnectionString connStr)
        {
            var match = Regex.Match(s, Pattern);
            if (match.Success)
            {
                connStr = Parse(s);
                return true;
            }
            connStr = Empty;
            return false;
        }
    }
}