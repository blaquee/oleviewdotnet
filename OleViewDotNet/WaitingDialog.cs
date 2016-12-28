﻿//    This file is part of OleViewDotNet.
//    Copyright (C) James Forshaw 2014
//
//    OleViewDotNet is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    OleViewDotNet is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with OleViewDotNet.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace OleViewDotNet
{
    partial class WaitingDialog : Form
    {
        private class ReportProgress : IProgress<string>
        {
            private CancellationToken _token;
            private Action<string> _report;
            private Func<string, string> _format;

            public void Report(string data)
            {
                _token.ThrowIfCancellationRequested();
                _report(_format(data));
            }

            public ReportProgress(Action<string> report, CancellationToken token, Func<string, string> format)
            {
                _token = token;
                _format = format;
                _report = report;
            }
        }

        private ReportProgress m_progress;
        private BackgroundWorker m_worker;
        private CancellationTokenSource m_cancellation;

        public WaitingDialog(Func<IProgress<string>, CancellationToken, object> worker_func, Func<string, string> format_label)
        {
            if (format_label == null)
            {
                format_label = s => String.Format("Currently Processing {0}. Please Wait.", s);
            }
            m_cancellation = new CancellationTokenSource();
            CancellationToken token = m_cancellation.Token;
            m_progress = new ReportProgress(SetLabel, token, format_label);
            m_worker = new BackgroundWorker();
            m_worker.DoWork += (sender, e) => Result = worker_func(m_progress, token);
            m_worker.RunWorkerCompleted += RunWorkerCompletedCallback;
            InitializeComponent();
            SetLabel(String.Empty);
        }

        public WaitingDialog(Func<IProgress<string>, CancellationToken, object> worker_func) 
            : this(worker_func, null)
        {
        }

        private void SetLabel(string value)
        {
            Action set = () => SetLabel(value);
            if (InvokeRequired)
            {
                Invoke(set);
            }
            else
            {
                lblProgress.Text = value;
            }
        }

        private void LoadingDialog_Load(object sender, EventArgs e)
        {                        
            m_worker.RunWorkerAsync();
        }

        private void RunWorkerCompletedCallback(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DialogResult = DialogResult.Cancel;
                Error = e.Error;
            }
            else
            {
                DialogResult = DialogResult.OK;
            }
            Close();
        }

        public Exception Error { get; private set; }
        public object Result { get; private set; }

        public bool CancelEnabled
        {
            get { return btnCancel.Enabled; }
            set { btnCancel.Enabled = value; }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            m_cancellation.Cancel();
        }
    }
}
