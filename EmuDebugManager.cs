using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace debugger
{
    public class EmuDebugManager
    {
        public EmuDebugManager()
        {
        }

        public bool HasBreakpoint(uint instructionAddress)
        {
            return _breakpoints.Contains(instructionAddress);
        }

        public void ToggleBreakpoint(uint bpAddress)
        {
            if (!_breakpoints.Remove(bpAddress))
            {
                _breakpoints.Add(bpAddress);
                NetHandler.SendAddBreakpoint(bpAddress);
            } else
            {
                NetHandler.SendRemoveBreakpoint(bpAddress);
            }
            BreakpointsChangedEvent.Invoke(this, new EventArgs());
        }

        public EmuMemoryView CreateMemoryView(ulong start, ulong end)
        {
            return new EmuMemoryView(start, end);
        }

        public DebugSymbolInfo FindSymbol(uint address)
        {
            if (_pauseInfo == null || _pauseInfo.symbols == null)
            {
                return null;
            }

            var symbols = _pauseInfo.symbols;
            for (var i = 0; i < symbols.Length; ++i)
            {
                if (symbols[i].address == address)
                {
                    return symbols[i];
                }
            }
            return null;
        }

        public DebugModuleInfo GetModule(uint moduleIdx)
        {
            return _pauseInfo.modules[moduleIdx];
        }

        public void UpdateData(DebugPauseInfo pauseInfo, DebugThreadInfo activeThread)
        {
            _pauseInfo = pauseInfo;
        }

        private DebugPauseInfo _pauseInfo = null;
        public List<uint> _breakpoints = new List<uint>();

        public event EventHandler<EventArgs> BreakpointsChangedEvent;

    }
}
