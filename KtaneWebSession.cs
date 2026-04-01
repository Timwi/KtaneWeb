using System;
using RT.Serialization;
using RT.Servers;

namespace KtaneWeb
{
    sealed class KtaneWebSession(KtaneWebConfig config) : Session, ISessionEquatable<KtaneWebSession>
    {
        public string Username;

        protected override void DeleteSession()
        {
            lock (_config)
                _config.Sessions.Remove(SessionID);
        }

        protected override bool ReadSession()
        {
            lock (_config)
                return _config.Sessions.TryGetValue(SessionID, out Username);
        }

        protected override void SaveSession()
        {
            lock (_config)
                _config.Sessions[SessionID] = Username;
        }

        KtaneWebSession ISessionEquatable<KtaneWebSession>.DeepClone() => new(_config) { Username = Username };
        bool IEquatable<KtaneWebSession>.Equals(KtaneWebSession other) => other != null && string.Equals(Username, other.Username);
        public override bool Equals(object obj) => obj is KtaneWebSession other && string.Equals(Username, other.Username);
        public override int GetHashCode() => Username.GetHashCode();

        [ClassifyIgnore]
        private readonly KtaneWebConfig _config = config;
    }
}
