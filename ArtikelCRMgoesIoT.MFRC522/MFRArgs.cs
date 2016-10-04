using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtikelCRMgoesIoT.MFRC522
{
    public class MFRArgs : EventArgs
    {
        private string message;

        public MFRArgs(string message)
        {
            this.message = message;
        }

        public string Message
        {
            get
            {
                return message;
            }
        }
    }
}
