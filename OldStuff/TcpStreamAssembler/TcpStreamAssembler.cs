using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Tamir.IPLib.Packets;
using System.Collections;

namespace TcpStreamAssembler
{
    class TcpStreamAssembler
    {
        // Reorder and Reassemble a TcpStream out of SharpPcap (winpcap) Packets
        // see @http://de.wikipedia.org/wiki/Transmission_Control_Protocol
        // provide a Stream, maybe

        /*
         * seq + data.Lenght = next.ack
         * 
         * 
         */
        private Queue<MemoryStream> dataBuffer; // the Reassembled TCP Stream
        private MemoryStream actualStream;
        private MemoryStream tmpBuffer; // tmp buffer

        private Hashtable map = new Hashtable();

        private long lastSeqNr = 0;
        private long currentSeqNr = 0;

        private int sourcePort = 0;
        private int destinationPort = 0;

        private bool synRecived = false;
        private bool finRecived = false;

        private long totalRecivedBytes = 0;
        private int position = 0;

        /// <summary>
        /// Versucht TCP-Stream aus gesnifften SharpPcap Packeten wiederherzustellen.
        /// Sobald ein SYN, ACK von sourcePort empfangen wird, wird der TCP-Stream wiederhergestellt.
        /// Bei einem FIN, ACK ist das ende des streams erreicht.
        /// Stell den  Bytestream zur verf�gung
        /// </summary>
        /// <param name="sourcePort">Quellport, um den gew�nschten Stream zu erkennen</param>
        /// <exception cref="System.IO.EndOfStreamException">Tritt auf wenn �ber der Stream zu ende ist</exception>
        public TcpStreamAssembler(int sourcePort)
        {
            this.dataBuffer = new Queue<MemoryStream>();
            this.tmpBuffer = new MemoryStream();

            this.sourcePort = sourcePort;
        }

        public void AddPacket(Packet p)
        {
            // Stream zu ende, sofort return
            if (this.finRecived)
            {
                return;
            }

            // is es �berhaupt ein TCPPacket?
            if (!(p is TCPPacket))
            {
                return;
            }
            TCPPacket tcpP = (TCPPacket)p;

            // geh�rt das Packet zu unserem TCP-Stream??
            // wenn ja, und es ist ein SYN, ACK merken wir uns des Zielport und setzen synRecived auf true
            if (tcpP.SourcePort == this.sourcePort && !this.synRecived)
            {
                if (tcpP.Syn && tcpP.Ack)
                {
                    this.destinationPort = tcpP.DestinationPort;
                    this.synRecived = true;
                    this.lastSeqNr = tcpP.AcknowledgmentNumber; // merken
                }
                return;
            }

            // geh�rt das Packet zu unserem TCP-Stream??
            if (tcpP.SourcePort != this.sourcePort || tcpP.DestinationPort != this.destinationPort)
            {
                return;
            }

            // Stream zu ende? FIN, ACK vorhanden?
            if (tcpP.Fin && tcpP.Ack)
            {
                this.finRecived = true;
                return;
            }

            // haben wir �berhaupt Daten?
            if (tcpP.Data.Length == 0)
            {
                return;
            }

            // packet einsortieren, wenn schon vorhanden > verwerfen
            if (this.map.ContainsKey(tcpP.SequenceNumber))
            {
                return;
            }

            // wenn seqNr <= currentSeqnr, packet verwerfen, >> Retransmit
            if (this.currentSeqNr <= tcpP.SequenceNumber)
            {
                return;
            }

            this.map.Add(tcpP.SequenceNumber, tcpP);
            this.currentSeqNr = tcpP.SequenceNumber;

            // Count total size
            this.totalRecivedBytes += tcpP.Data.Length;
            
            // Wenn Ack && Psh > daten zusammensetzen und memorystream der Queue hinzuf�gen
            if (tcpP.Ack && tcpP.Psh)
            {
                long nextSeqNr = this.lastSeqNr;
                while (this.map.ContainsKey(nextSeqNr))
                {
                    TCPPacket packet = (TCPPacket)this.map[nextSeqNr];
                    foreach (byte b in packet.Data)
                    {
                        this.tmpBuffer.WriteByte(b);
                    }
                    nextSeqNr += packet.Data.Length;
                }
                this.dataBuffer.Enqueue(this.tmpBuffer);
                this.tmpBuffer = new MemoryStream();
                this.lastSeqNr = nextSeqNr;
            }

        }

        public int SourcePort
        {
            get { return this.sourcePort; }
        }

        public int DestinationPort
        {
            get { return this.destinationPort; }
        }

        public bool SynRecived
        {
            get { return this.synRecived; }
        }

        public bool FinRecived
        {
            get
            {
                return this.finRecived;
            }
        }
        
        public long TotalRecivedBytes
        {
            get { return this.totalRecivedBytes; }
        }
    }
}
