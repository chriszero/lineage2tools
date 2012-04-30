using System;
using System.Collections.Generic;
using System.Text;

namespace L2Proxy
{
    public interface IPacketFilter
    {
        /// <summary>
        /// Filtert oder ver�ndert die Packete
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        void FilterPacket(L2BasePacket packet);

        /// <summary>
        /// Liefert eine Beschreibung des Filters zur�ck
        /// </summary>
        /// <returns></returns>
        string GetDiscription();

        /// <summary>
        /// Wird aufgerufen um den Filter zu Konfigurieren
        /// </summary>
        void Configure();
    }
}
