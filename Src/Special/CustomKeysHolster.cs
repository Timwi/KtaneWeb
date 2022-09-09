using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RT.Json;
using RT.Serialization;

namespace KtaneWeb.Special
{
    public static class CustomKeysHolster
    {
        private static readonly Dictionary<string, Hold> s_storage = new();

        private static readonly Random s_random = new();

        private static readonly char[] s_tokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890".ToCharArray();

        private static string CreateCode()
        {
            StringBuilder builder = new();
            for (int i = 0; i < 5; i++)
                builder.Append(s_random.Next(10));

            string code = builder.ToString();
            if (s_storage.ContainsKey(code))
                return CreateCode();

            return code;
        }

        private static string CreateToken()
        {
            StringBuilder builder = new();
            for (int i = 0; i < 15; i++)
                builder.Append(s_tokenChars[s_random.Next(s_tokenChars.Length)]);

            return builder.ToString();
        }

        public static JsonValue Push(string data)
        {
            Hold hold = new(data);
            s_storage.Add(hold.Code, hold);
            return ClassifyJson.Serialize(new Dictionary<string, string>
            {
                { "code", hold.Code },
                { "token", hold.Token }
            });
        }

        public static bool Has(string code) => s_storage.ContainsKey(code);

        public static string Pull(string code) => s_storage[code].Data;

        public static void Remove(string code)
        {
            if (s_storage.ContainsKey(code))
            {
                s_storage[code].RemovalCanceller.Cancel();
                s_storage.Remove(code);
            }
        }

        public static bool IsAuthorized(string code, string token) => s_storage.ContainsKey(code) && s_storage[code].Token == token;

        private class Hold
        {
            public string Code { get; }

            public string Data { get; }

            public string Token { get; }

            public CancellationTokenSource RemovalCanceller { get; }

            public Hold(string data)
            {
                Code = CreateCode();
                Data = data;
                Token = CreateToken();

                // Delete after a day
                RemovalCanceller = new CancellationTokenSource();
                Task.Delay(TimeSpan.FromDays(1), RemovalCanceller.Token).ContinueWith(t => Remove(Code));
            }
        }
    }
}
