using System;
using System.Collections.Generic;
using System.Text;

namespace L2Proxy
{
    class Nullfilter : IPacketFilter
    {
        #region PacketFilter Member

        public void FilterPacket(L2BasePacket packet)
        {
            
        }

        public string GetDiscription()
        {
            return "NullFilter, l�sst alle Packete durch.";
        }

        public void Configure()
        {

        }

        #endregion

        public override string ToString()
        {
            return this.GetDiscription();
        }
    }
}
