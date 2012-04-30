using System;
using System.Collections.Generic;
using System.Text;
using Tamir.IPLib;
using L2PacketDecrypt;
using L2PacketDecrypt.Packets;
using Tamir.IPLib.Packets;
using System.Windows.Forms;
using La2PacketSniffer.DataHolding;
using TcpRecon;

namespace La2PacketSniffer
{
    class SnifferControl
    {
        private PcapDevice device = null;
        private string tcpDumpFilter = null;
        private bool gotLock = false;
        private int port;

        private PacketContainer packetContainer;
        private int count = 0; // ReferenzZ�hler

        private L2GameSniffer gameSniffer = null;
        private L2PacketStream clientStr = null;
        private L2PacketStream serverStr = null;
        private TcpRecon.TcpRecon connection = null;

        private Dictionary<TCPConnection, TcpRecon.TcpRecon> sharpPcapDict = new Dictionary<TCPConnection, TcpRecon.TcpRecon>();


        // Delegate, Eventhandler
        public delegate void NewPacketHandler(object sender);
        /// <summary>
        /// Wird ausgel�st wenn neues Packet dem PacketContainer hinzugef�gt wurde
        /// </summary>
        public event NewPacketHandler NewPacketArrived;

        public delegate void SynRecivedEventHandler(object sender);
        public delegate void FinRecivedEventHandler(object sender);

        public event SynRecivedEventHandler OnSynRecived;
        public event FinRecivedEventHandler OnFinRecived;

        /// <summary>
        /// Stellt eine Klasse da die das Sniffen von Packets regelt
        /// </summary>
        /// <param name="pc">Der PacketContainer dem die Packete hinzugef�gt werden sollen</param>
        /// <param name="port">Der Port auf den der TCPStreamAssembler horchen soll</param>
        public SnifferControl(PacketContainer pc, int port)
        {
            this.port = port;
            this.packetContainer = pc;
            this.gameSniffer = new L2GameSniffer();
            this.clientStr = new L2PacketStream();
            this.serverStr = new L2PacketStream();
            // TODO: wenn keine devices gefunden wurden, Meldung
        }

        /// <summary>
        /// Initialisiert die SnifferControll
        /// </summary>
        /// <param name="device">Das PcapDevice mit dem gesnifft werden soll</param>
        /// <param name="filter">Der TCPDumpfilter der angewendet werden soll</param>
        public void Init(PcapDevice device, string filter)
        {
            this.device = device;
            this.tcpDumpFilter = filter;

            //Register our handler function to the 'packet arrival' event
            this.device.PcapOnPacketArrival +=
                new SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);

            //Open the device for capturing
            //true -- means promiscuous mode
            //1000 -- means a read wait of 1000ms
            this.device.PcapOpen(false, 100);

            //Associate the filter with this capture
            this.device.PcapSetFilter(this.tcpDumpFilter);
        }

        /// <summary>
        /// Startet den Sniffer
        /// </summary>
        public void Start()
        {
            this.device.PcapStartCapture();
        }

        /// <summary>
        /// Stoppt den Sniffer
        /// </summary>
        public void Stop()
        {
            if (this.device != null)
            {
                this.device.PcapStopCapture();
            }
            // Call a last time
            this.processPackets();
        }

        private void device_PcapOnPacketArrival(object sender, Packet packet)
        {
            if (!(packet is TCPPacket)) return;

            TCPPacket tcpPacket = (TCPPacket)packet;
            // Creates a key for the dictionary
            TCPConnection c = new TCPConnection(tcpPacket);

            // create a new entry if the key does not exists
            if (!sharpPcapDict.ContainsKey(c) && !this.gotLock)
            {
                TcpRecon.TcpRecon tcpRecon = new TcpRecon.TcpRecon(this.serverStr, this.clientStr);
                sharpPcapDict.Add(c, tcpRecon);
                this.gotLock = true;
                this.connection = tcpRecon;
            }

            // Use the TcpRecon class to reconstruct the session
            if (this.sharpPcapDict.ContainsKey(c))
            {
                // Events
                if (tcpPacket.Syn && tcpPacket.Ack)
                    this.OnSynRecived(this);
                else if (tcpPacket.Fin && tcpPacket.Ack)
                    this.OnFinRecived(this);
                sharpPcapDict[c].ReassemblePacket(tcpPacket);
            }

            this.processPackets();
        }

        public void processPackets()
        {
            L2Packet l2packet = null;

            
            while (this.clientStr.MorePackets())
            {
                l2packet = this.gameSniffer.handlePacket(this.clientStr.ReadPacket(), false);
                if (l2packet != null)
                {
                    this.packetContainer.AddPacket(l2packet);
                    l2packet.PacketNo = this.count++;
                    this.NewPacketArrived(this);
                }
            }
            while (this.serverStr.MorePackets())
            {
                l2packet = this.gameSniffer.handlePacket(this.serverStr.ReadPacket(), true);
                if (l2packet != null)
                {
                    this.packetContainer.AddPacket(l2packet);
                    l2packet.PacketNo = this.count++;
                    this.NewPacketArrived(this); //Raise Event
                }
            }
        }
    }
}
