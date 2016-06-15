using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace debugger
{
    public partial class EmuMemoryControl : UserControl
    {
        private EmuDebugManager _dbgMgr = null;
        private EmuMemoryView _dataView = null;
        private uint _activeAddress = 0;
        private uint _selectedAddressStart = 0;
        private uint _selectedAddressEnd = 0;
        private uint _dragStartAddress = 0;
        private uint _addressAlignment = 4;
        private uint _sizePerLine = 4;

        private class jumpEntry {
            public uint targetAddress;
            public uint scrollAddress;
        }
        private Stack<jumpEntry> _jumpHistory = new Stack<jumpEntry>();

        public EmuMemoryControl()
        {
            InitializeComponent();
        }

        public uint AddressAlignment
        {
            get { return _addressAlignment; }
            set { _addressAlignment = value; }
        }

        public uint SizePerLine
        {
            get { return _sizePerLine; }
            set { _sizePerLine = value; }
        }

        public uint Address
        {
            get { return (uint)scrollBar.Value * _sizePerLine; }
            set {
                var newValue = (int)(value / _sizePerLine);
                if (newValue < scrollBar.Minimum)
                {
                    newValue = scrollBar.Minimum;
                } else if (newValue > scrollBar.Maximum)
                {
                    newValue = scrollBar.Maximum;
                }
                scrollBar.Value = newValue;
            }
        }

        public uint VisibleSize
        {
            get
            {
                return (uint)Math.Ceiling((float)ClientSize.Height / (float)Font.Height) * _sizePerLine;
            }
        }

        public uint ActiveAddress
        {
            get
            {
                return _activeAddress;
            }
            set
            {
                _activeAddress = value;
                Invalidate();
            }
        }

        public uint SelectedAddressStart
        {
            get
            {
                return _selectedAddressStart;
            }
            set
            {
                _selectedAddressStart = value;
                Invalidate();
            }
        }

        public uint SelectedAddressEnd
        {
            get
            {
                return _selectedAddressEnd;
            }
            set
            {
                _selectedAddressEnd = value;
                Invalidate();
            }
        }

        public uint SelectedAddress
        {
            set
            {
                _selectedAddressStart = value;
                _selectedAddressEnd = value;
                Invalidate();
            }
        }

        public uint MinAddress
        {
            get { return (uint)scrollBar.Minimum / _sizePerLine; }
        }

        public uint MaxAddress
        {
            get { return (uint)scrollBar.Maximum / _sizePerLine; }
        }

        public EmuDebugManager DebugManager
        {
            get
            {
                return _dbgMgr;
            }
            set
            {
                if (_dbgMgr != null)
                {
                    _dbgMgr.BreakpointsChangedEvent -= DbgMgr_BreakpointsChanged;
                }

                _dbgMgr = value;

                if (_dbgMgr != null)
                {
                    _dbgMgr.BreakpointsChangedEvent += DbgMgr_BreakpointsChanged;
                }
            }
        }

        public EmuMemoryView DataView
        {
            get
            {
                return _dataView;
            }
            set
            {
                if (_dataView != null)
                {
                    _dataView.DataChanged -= DataView_DataChanged;
                }
                _dataView = value;
                if (_dataView != null)
                {
                    _dataView.DataChanged += DataView_DataChanged;
                }

                _jumpHistory.Clear();
                Recache();
            }
        }

        private void Recache()
        {
            if (_dataView != null)
            {
                scrollBar.Enabled = true;
                scrollBar.Minimum = (int)(_dataView.Start / _sizePerLine);
                scrollBar.Maximum = (int)Math.Ceiling((float)(_dataView.End - _sizePerLine) / (float)_sizePerLine);
            } else
            {
                scrollBar.Enabled = false;
            }

            OnRecache(new EventArgs());
            this.Invalidate();
        }

        public uint PointToAddress(Point p)
        {
            uint offset = (uint)(p.Y / this.Font.Height) * _sizePerLine;
            return Address + offset;
        }

        public void ScrollToAddress(uint targetAddress, uint deadSpace = 1)
        {
            uint curStart = Address;
            uint curEnd = Address + VisibleSize;

            if (curStart == curEnd || targetAddress < curStart || targetAddress > curEnd)
            {
                Address = targetAddress - deadSpace * _sizePerLine;
            }
            else if (targetAddress > curEnd - (deadSpace + 2) * _sizePerLine)
            {
                Address = targetAddress - Address - ((deadSpace + 2) * _sizePerLine);
            }
            else if (targetAddress < curStart + deadSpace * _sizePerLine)
            {
                Address = targetAddress - deadSpace * _sizePerLine;
            }
        }

        public void pushJumpAddress(uint targetAddress)
        {
            if (targetAddress == 0)
            {
                return;
            }
            jumpEntry prevEntry = null;
            if (_jumpHistory.Count > 0)
            {
                prevEntry = _jumpHistory.Pop();
                if (prevEntry.targetAddress != targetAddress)
                {
                    // If this isn't a duplicate, just put it back on the stack
                    _jumpHistory.Push(prevEntry);
                    prevEntry = null;
                }
            }

            if (prevEntry == null)
            {
                prevEntry = new jumpEntry();
            }
            prevEntry.targetAddress = targetAddress;
            prevEntry.scrollAddress = Address;
            _jumpHistory.Push(prevEntry);
        }

        public void JumpToAddress(uint targetAddress)
        {
            pushJumpAddress(_selectedAddressEnd);

            SelectedAddress = targetAddress;
            ScrollToAddress(targetAddress, 5);

            pushJumpAddress(targetAddress);
        }

        public void JumpBack()
        {
            if (_jumpHistory.Count == 0)
            {
                // Can't jump back with no history
                return;
            }

            var lastEntry = _jumpHistory.Peek();
            if (_selectedAddressEnd == lastEntry.targetAddress && Address == lastEntry.scrollAddress)
            {
                if (_jumpHistory.Count >= 2)
                {
                    // If we are still at the same address, skip it and go back one more
                    _jumpHistory.Pop();
                    lastEntry = _jumpHistory.Peek();
                } else
                {
                    // If we are still at the same address but have no more entries,
                    //   just put it back on the stack and do nothing
                    return;
                }
            }

            
            SelectedAddress = lastEntry.targetAddress;
            Address = lastEntry.scrollAddress;
        }

        public void ToggleUserGoto()
        {
            gotoPanel.Left = this.ClientSize.Width / 2 - gotoPanel.Width / 2;
            gotoPanel.Top = this.ClientSize.Height / 2 - gotoPanel.Height / 2;
            gotoPanel.Visible = !gotoPanel.Visible;
            if (gotoPanel.Visible)
            {
                gotoTxt.Text = Convert.ToString(SelectedAddressEnd, 16);
                while (gotoTxt.Text.Length < 8)
                {
                    gotoTxt.Text = "0" + gotoTxt.Text;
                }
                gotoTxt.SelectionStart = 0;
                gotoTxt.SelectionLength = 8;
                gotoTxt.Focus();
            } else
            {
                scrollBar.Focus();
            }
            this.Invalidate();
        }

        private void gotoTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try {
                    uint targetAddress = Convert.ToUInt32(gotoTxt.Text, 16);
                    JumpToAddress(targetAddress);
                }
                catch(Exception) { }

                ToggleUserGoto();
            } else if (e.KeyCode == Keys.Escape)
            {
                ToggleUserGoto();
            }
        }

        private void DataView_DataChanged(object sender, EventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                Recache();
            });
        }

        private void scrollBar_ValueChanged(object sender, EventArgs e)
        {
            Recache();
        }

        protected virtual void OnRecache(EventArgs e)
        {
            // Do nothing in the default implementation
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (this.Focused)
            {
                scrollBar.SendMouseWheelEvent(e);
            }
            else
            {
                base.OnMouseWheel(e);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragStartAddress = PointToAddress(e.Location);
                _selectedAddressStart = _dragStartAddress;
                _selectedAddressEnd = _dragStartAddress;
                scrollBar.Focus();
                this.Invalidate();
            } else
            {
                base.OnMouseDown(e);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                uint newAddress = PointToAddress(e.Location);
                if (newAddress >= _dragStartAddress)
                {
                    _selectedAddressStart = _dragStartAddress;
                    _selectedAddressEnd = newAddress;
                } else
                {
                    _selectedAddressStart = newAddress;
                    _selectedAddressEnd = _dragStartAddress;
                }
                this.Invalidate();
            } else
            {
                base.OnMouseMove(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                _selectedAddressEnd += _sizePerLine;
                _selectedAddressStart = _selectedAddressEnd;
                this.ScrollToAddress(_selectedAddressEnd);
                this.Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                _selectedAddressEnd -= _sizePerLine;
                _selectedAddressStart = _selectedAddressEnd;
                this.ScrollToAddress(_selectedAddressEnd);
                this.Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.G) {
                ToggleUserGoto();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                JumpBack();
            } else
            {
                base.OnKeyDown(e);
            }
        }

        private void DbgMgr_BreakpointsChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void scrollBar_KeyDown(object sender, KeyEventArgs e)
        {
            this.OnKeyDown(e);
        }

        public override bool Focused
        {
            get
            {
                return this.scrollBar.Focused;
            }
        }

        protected override void OnEnter(EventArgs e)
        {
            this.Invalidate();
            base.OnEnter(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            this.Invalidate();
            base.OnLeave(e);
        }

        private void EmuMemoryControl_Enter(object sender, EventArgs e)
        {
            scrollBar.Focus();
        }
    }
}
