using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDKeyMiner
{
    public interface IMiner
    {
        void Start(Credentials credentials);
        void Stop();
        event EventHandler OnAuthorized;
        event EventHandler OnMining;
        event EventHandler OnShare;
        event EventHandler<MinerError> OnError;
    }

    public enum MinerError
    {
        ExeNotFound,
        CannotStart,
        HasStopped,
        ConnectionError,
        UnknownError
    }
}
