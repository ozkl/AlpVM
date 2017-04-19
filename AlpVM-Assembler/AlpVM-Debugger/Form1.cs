using AlpVM_Assembler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlpVM_Debugger
{
    public partial class Form1 : Form
    {
        Debugger mDebugger;

        MemoryForm mMemoryForm;

        DataGridViewCell mCurrentCell = null;
        UInt32 mTableStartAddress = 0;

        public Form1()
        {
            InitializeComponent();

            mDebugger = new Debugger();
            mDebugger.MachineStateUpdated += OnMachineStateUpdated;
            mDebugger.MemoryUpdated += OnMemoryUpdated;
            mDebugger.ConnectionStateChanged += OnConnectionStateChanged;

            mMemoryForm = new MemoryForm(mDebugger);
            mMemoryForm.Show();
        }

        void OnMachineStateUpdated()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { OnMachineStateUpdated(); }));
            }
            else
            {
                UpdateRegisters(mDebugger.Machine.MachineState);
                
                SetCurrentInstructionIndicator();
            }
        }

        void OnConnectionStateChanged()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { OnConnectionStateChanged(); }));
            }
            else
            {
                Text = mDebugger.Connected ? "Connected" : "Disconnected";
            }
        }

        void OnMemoryUpdated(UInt32 offset, UInt32 size)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { OnMemoryUpdated(offset, size); }));
            }
            else
            {
                FillInstructionTable();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {        
        }

        void FillInstructionTable()
        {
            const UInt32 instructionCount = 22;
            const UInt32 instructionsTotalSize = Instruction.InstructionSize * instructionCount;

            mCurrentCell = null;

            this.dataGridView1.Rows.Clear();

            if (mDebugger.Machine.MachineState.mExecutionPointer < mTableStartAddress + Instruction.InstructionSize ||
                mDebugger.Machine.MachineState.mExecutionPointer > mTableStartAddress + instructionsTotalSize - 2 * Instruction.InstructionSize)
            {
                mTableStartAddress = mDebugger.Machine.MachineState.mExecutionPointer - Instruction.InstructionSize * 4;//lets start filling from 4 instructions earlier
            }

            for (UInt32 address = mTableStartAddress; address < mTableStartAddress + instructionsTotalSize; address += Instruction.InstructionSize)
            {
                Int32 op = BitConverter.ToInt32(mDebugger.Machine.MachineMemory, (int)address);
                Int16 mod1 = BitConverter.ToInt16(mDebugger.Machine.MachineMemory, (int)address + 4);
                Int16 mod2 = BitConverter.ToInt16(mDebugger.Machine.MachineMemory, (int)address + 6);
                Int32 param1 = BitConverter.ToInt32(mDebugger.Machine.MachineMemory, (int)address + 8);
                Int32 param2 = BitConverter.ToInt32(mDebugger.Machine.MachineMemory, (int)address + 12);

                OpModifier m1 = (OpModifier)mod1;
                OpModifier m2 = (OpModifier)mod2;
                Instruction instruction = new Instruction((OpCode)op, m1, m2, param1, param2);

                string param1Str = param1.ToString();
                string param2Str = param2.ToString();

                if ((m1 & (OpModifier.Register | OpModifier.MemoryAtRegister)) > 0)
                {
                    param1Str = ((Register)param1).ToString();
                }

                if ((m2 & (OpModifier.Register | OpModifier.MemoryAtRegister)) > 0)
                {
                    param2Str = ((Register)param2).ToString();
                }

                this.dataGridView1.Rows.Add(
                        address,
                        instruction.Operation.ToString(),
                        m1.ToString(),
                        m2.ToString(),
                        param1Str,
                        param2Str);
            }

            SetCurrentInstructionIndicator();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F10)
            {
                mDebugger.Step();
                return true;    // indicate that you handled this keystroke
            }
            else if (keyData == Keys.F5)
            {
                mDebugger.Resume();
                return true;    // indicate that you handled this keystroke
            }

            // Call the base class
            return base.ProcessCmdKey(ref msg, keyData);
        }

        

        void UpdateRegisters(MachineState state)
        {
            dataGridView2.Rows.Clear();

            foreach (var field in typeof(MachineState).GetFields(BindingFlags.Instance |
                                                 BindingFlags.Public))
            {
                this.dataGridView2.Rows.Add(field.Name, field.GetValue(state));
            }
        }

        void SetCurrentInstructionIndicator()
        {
            for (int i = 0; i < dataGridView1.Rows.Count; ++i)
            {
                if ((UInt32)dataGridView1.Rows[i].Cells[0].Value == mDebugger.Machine.MachineState.mExecutionPointer)
                {
                    mCurrentCell = dataGridView1.Rows[i].Cells[0];
                    dataGridView1.CurrentCell = mCurrentCell;
                    break;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (null == mCurrentCell)
            {
                return;
            }

            if (mCurrentCell != dataGridView1.CurrentCell)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    dataGridView1.CurrentCell = mCurrentCell;
                }));
            }
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mDebugger.Connect();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mDebugger.Resume();
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mDebugger.Pause();
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mDebugger.Step();
        }
    }
}
