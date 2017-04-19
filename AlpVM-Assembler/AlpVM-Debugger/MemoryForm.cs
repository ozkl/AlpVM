using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlpVM_Debugger
{
    public partial class MemoryForm : Form
    {
        Debugger mDebugger;


        public MemoryForm(Debugger debugger)
        {
            InitializeComponent();

            mDebugger = debugger;

            DataGridViewTextBoxColumn columnAddress = new DataGridViewTextBoxColumn();
            columnAddress.HeaderText = "Address";

            dataGridView1.Columns.Add(columnAddress);

            for (int i = 0; i < 16; ++i)
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.Width = 30;
                column.HeaderText = i.ToString();
                dataGridView1.Columns.Add(column);
            }

            mDebugger.MemoryUpdated += OnMemoryUpdated;
        }

        void OnMemoryUpdated(UInt32 offset, UInt32 size)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { OnMemoryUpdated(offset, size); }));
            }
            else
            {
                FillTable();
            }
        }

        void FillTable()
        {
            dataGridView1.Rows.Clear();

            UInt32 address = 0;
            if (UInt32.TryParse(tbAddress.Text, out address) == false)
            {
                return;
            }

            byte[] memory = mDebugger.Machine.MachineMemory;

            for (UInt32 a = address; a < address + 16 * 20; a += 16)
            {
                dataGridView1.Rows.Add(a,
                    memory[a + 0],
                    memory[a + 1],
                    memory[a + 2],
                    memory[a + 3],
                    memory[a + 4],
                    memory[a + 5],
                    memory[a + 6],
                    memory[a + 7],
                    memory[a + 8],
                    memory[a + 9],
                    memory[a + 11],
                    memory[a + 12],
                    memory[a + 13],
                    memory[a + 14],
                    memory[a + 15]
                    );
            }
        }

        private void bShow_Click(object sender, EventArgs e)
        {
            UInt32 address = 0;
            if (UInt32.TryParse(tbAddress.Text, out address) == false)
            {
                return;
            }

            mDebugger.GetMemory(address);
        }
    }
}
