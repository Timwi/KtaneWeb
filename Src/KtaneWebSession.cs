using RT.Servers;

namespace KtaneWeb
{
    sealed class KtaneWebSession : FileSession
    {
        private string _username;

        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                SessionModified = true;
            }
        }
    }
}
