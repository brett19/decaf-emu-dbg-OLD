using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace debugger
{
    public class EmuMemoryView
    {
        private ulong _start = 0;
        private ulong _end = 0;
        private ulong _cur = 0;
        private NetHandler.EmuMemoryReader _memRead = null;
        private NetHandler.EmuInstrReader _instrRead = null;
        
        public EmuMemoryView(ulong start, ulong end)
        {
            _start = start;
            _end = end;
            Seek((uint)_start);
        }

        public void Seek(uint address)
        {
            _cur = address;
            _memRead = new NetHandler.EmuMemoryReader(address, NewReaderData);
            _instrRead = new NetHandler.EmuInstrReader(address, NewReaderData);
        }

        private void NewReaderData()
        {
            if (DataChanged != null)
            {
                DataChanged.Invoke(this, new EventArgs());
            }
        }

        public bool GetInstruction(out uint data, out string disasm)
        {
            _cur += 4;
            if (!_instrRead.GetInstr(out disasm))
            {
                disasm = "";
            }
            return _memRead.GetUInt32(out data);
        }

        public bool GetUint8(out byte data)
        {
            _cur += 1;
            return _memRead.GetUint8(out data);
        }

        public bool GetUint32(out uint data)
        {
            _cur += 4;
            return _memRead.GetUInt32(out data);
        }

        public bool Eof
        {
            get { return _cur >= _end; }
        }

        public ulong Start
        {
            get { return _start; }
        }

        public ulong End
        {
            get { return _end; }
        }

        public delegate void DataChangedHandler(object sender, EventArgs e);
        public event DataChangedHandler DataChanged;
    }
}
