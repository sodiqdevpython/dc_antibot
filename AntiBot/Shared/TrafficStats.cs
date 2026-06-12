namespace dc_antibot.AntiBot.Shared
{
    public class TrafficStats
    {
        private readonly object _lock = new object();
        private ulong _sendBytes;
        private ulong _recvBytes;
        private ulong _packets;
        private int   _ratioHits;   
        private bool  _massiveExfilFlagged;

        public ulong SendBytes { get { lock (_lock) return _sendBytes; } }
        public ulong RecvBytes { get { lock (_lock) return _recvBytes; } }
        public ulong Packets   { get { lock (_lock) return _packets; } }
        public int   RatioHits { get { lock (_lock) return _ratioHits; } }
        public bool  MassiveExfilFlagged { get { lock (_lock) return _massiveExfilFlagged; } }

        public void OnSend(int size) { lock (_lock) { _sendBytes += (ulong)size; _packets++; } }
        public void OnRecv(int size) { lock (_lock) { _recvBytes += (ulong)size; _packets++; } }

        public void OnSendOrRecv(bool isSend, int size)
        {
            if (size < 0) size = 0;
            if (isSend) OnSend(size); else OnRecv(size);
        }

        public bool TryRatioHit()
        {
            lock (_lock)
            {
                if (_ratioHits >= 5) return false;
                if (_packets == 0 || _packets % 10 != 0) return false;
                ulong total = _sendBytes + _recvBytes;
                if (total == 0) return false;
                double ratio = (double)_sendBytes / total;
                if (ratio <= 0.90) return false;
                _ratioHits++;
                return true;
            }
        }

        public bool TryMassiveExfil()
        {
            lock (_lock)
            {
                if (_massiveExfilFlagged) return false;
                if (_sendBytes < 5368709120UL) return false;  
                _massiveExfilFlagged = true;
                return true;
            }
        }
    }
}
