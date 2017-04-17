using System;
using RT.Servers;
using RT.Util.Serialization;

namespace KtaneWeb
{
    sealed class KtaneWebSession : Session, ISessionEquatable<KtaneWebSession>
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

        KtaneWebSession ISessionEquatable<KtaneWebSession>.DeepClone() => new KtaneWebSession(_config) { Username = Username };
        bool IEquatable<KtaneWebSession>.Equals(KtaneWebSession other) => other != null && string.Equals(Username, other.Username);

        [ClassifyIgnore]
        private KtaneWebConfig _config;
        public KtaneWebSession(KtaneWebConfig config) { _config = config; }
    }
}
