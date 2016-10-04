using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace ArtikelCRMgoesIoT.MFRC522
{
    public class Init
    {
        //------------------MFRC522---------------
        public readonly byte PCD_IDLE = 0x00;
        public readonly byte PCD_MEM = 0x01;
        public readonly byte PCD_AUTHENT = 0x0E;
        public readonly byte PCD_RECEIVE = 0x08;
        public readonly byte PCD_TRANSMIT = 0x04;
        public readonly byte PCD_TRANSCEIVE = 0x0C;
        public readonly byte PCD_RESETPHASE = 0x0F;
        public readonly byte PCD_CALCCRC = 0x03;

        public readonly byte PICC_REQIDL = 0x26;
        public readonly byte PICC_REQALL = 0x52;
        public readonly byte PICC_ANTICOLL = 0x93;
        public readonly byte PICC_SElECTTAG = 0x93;
        public readonly byte PICC_AUTHENT1A = 0x60;
        public readonly byte PICC_AUTHENT1B = 0x61;
        public readonly byte PICC_READ = 0x30;
        public readonly byte PICC_WRITE = 0xA0;
        public readonly byte PICC_DECREMENT = 0xC0;
        public readonly byte PICC_INCREMENT = 0xC1;
        public readonly byte PICC_RESTORE = 0xC2;
        public readonly byte PICC_TRANSFER = 0xB0;
        public readonly byte PICC_HALT = 0x50;

        //Page 0:Command and Status
        public readonly byte Reserved00 = 0x00;
        public readonly byte CommandReg = 0x01;
        public readonly byte ComIEnReg = 0x02;
        public readonly byte DivlEnReg = 0x03;
        public readonly byte ComIrqReg = 0x04;
        public readonly byte DivIrqReg = 0x05;
        public readonly byte ErrorReg = 0x06;
        public readonly byte Status1Reg = 0x07;
        public readonly byte Status2Reg = 0x08;
        public readonly byte FIFODataReg = 0x09;
        public readonly byte FIFOLevelReg = 0x0A;
        public readonly byte WaterLevelReg = 0x0B;
        public readonly byte ControlReg = 0x0C;
        public readonly byte BitFramingReg = 0x0D;
        public readonly byte CollReg = 0x0E;
        public readonly byte Reserver01 = 0x0F;
        //Page 1:Command     
        public readonly byte Reserved10 = 0x10;
        public readonly byte ModeReg = 0x11;
        public readonly byte TxModeReg = 0x12;
        public readonly byte RxModeReg = 0x13;
        public readonly byte TxControlReg = 0x14;
        public readonly byte TxAutoReg = 0x15;
        public readonly byte TxSelReg = 0x16;
        public readonly byte RxSelReg = 0x17;
        public readonly byte RxThresholdReg = 0x18;
        public readonly byte DemodReg = 0x19;
        public readonly byte Reserved11 = 0x1A;
        public readonly byte Reserved12 = 0x1B;
        public readonly byte MifareReg = 0x1C;
        public readonly byte Reserved13 = 0x1D;
        public readonly byte Reserved14 = 0x1E;
        public readonly byte SerialSpeedReg = 0x1F;
        //Page 2:CFG    
        public readonly byte Reserved20 = 0x20;
        public readonly byte CRCResultRegM = 0x21;
        public readonly byte CRCResultRegL = 0x22;
        public readonly byte Reserved21 = 0x23;
        public readonly byte ModWidthReg = 0x24;
        public readonly byte Reserved22 = 0x25;
        public readonly byte RFCfgReg = 0x26;
        public readonly byte GsNReg = 0x27;
        public readonly byte CWGsPReg = 0x28;
        public readonly byte ModGsPReg = 0x29;
        public readonly byte TModeReg = 0x2A;
        public readonly byte TPrescalerReg = 0x2B;
        public readonly byte TReloadRegH = 0x2C;
        public readonly byte TReloadRegL = 0x2D;
        public readonly byte TCounterValueRegH = 0x2E;
        public readonly byte TCounterValueRegL = 0x2F;
        //Page 3:TestRegister     
        public readonly byte Reserved30 = 0x30;
        public readonly byte TestSel1Reg = 0x31;
        public readonly byte TestSel2Reg = 0x32;
        public readonly byte TestPinEnReg = 0x33;
        public readonly byte TestPinValueReg = 0x34;
        public readonly byte TestBusReg = 0x34;
        public readonly byte AutoTestReg = 0x36;
        public readonly byte VersionReg = 0x37;
        public readonly byte AnalogTestReg = 0x38;
        public readonly byte TestDAC1Reg = 0x39;
        public readonly byte TestDAC2Reg = 0x3A;
        public readonly byte TestADCReg = 0x3B;
        public readonly byte Reserved31 = 0x3C;
        public readonly byte Reserved32 = 0x3D;
        public readonly byte Reserved33 = 0x3E;
        public readonly byte Reserved34 = 0x3F;

        public readonly int OK = 0;
        public readonly int NOTAGERR = 1;
        public readonly int ERR = 2;
        //-----------------------------------------------
        private SpiDevice SpiDisplay;
        private int MAX_LEN = 16;
        private const string SPI_CONTROLLER_NAME = "SPI0";  /* For Raspberry Pi 2, use SPI0                             */
        private const Int32 SPI_CHIP_SELECT_LINE = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */
        private const Int32 DATA_COMMAND_PIN = 22;          /* We use GPIO 22 since it's conveniently near the SPI pins */
        private const Int32 RESET_PIN = 23;
        private int buffer16 = 16 + 1;
        private int buffer25 = 25 + 1;
        private int buffer64 = 64 + 1;
        private GpioController IoController;
        private GpioPin ResetPin;
        private GpioPin DataCommandPin;
        private bool onUpdate = false;
        private bool activated = false;
        private Timer cardReader = null;
        public delegate void CardReadHandler(object myObject,
                                             MFRArgs myArgs);

        public event CardReadHandler OnCardRead;

        //MFRC522.Init mfrc522 = new MFRC522.Init();
        public async Task ConfigureSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE); /* Create SPI initialization settings                               */
                settings.ClockFrequency = 1000000;                             /* Datasheet specifies maximum SPI clock frequency of 10MHz         */

                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);       /* Find the selector string for the SPI bus controller          */
                var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);         /* Find the SPI bus controller device with our selector string  */
                SpiDisplay = await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings);  /* Create an SpiDevice with our bus controller and SPI settings */
                if (SpiDisplay == null)
                {

                    return;
                }


            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
            //SPI.Configuration xSPIConfig;

            //xSPIConfig = new SPI.Configuration(Cpu.Pin.GPIO_NONE,    //Chip Select pin
            //                                    true,              //Chip Select Active State
            //                                    50,                  //Chip Select Setup Time
            //                                    0,                  //Chip Select Hold Time
            //                                    true,              //Clock Idle State
            //                                    true,               //Clock Edge
            //                                    1000,               //Clock Rate (kHz)
            //                                    SPI.SPI_module.SPI1);//SPI Module
            //spiDevice = new SPI(xSPIConfig);
            //MFRC522Init();

            MFRC522Init();

        }

        public void ConfigureTimer(bool activate)
        {
            if (activate)
            {
                Debug.Write("****Card reader started****");
                onUpdate = false;
                //mfrc522.MFRC522Init();
                cardReader = new Timer(StartMFRC522, this, 10, 500);

                activated = true;

            }
            else
            {
                Debug.Write("****Card reader stoped****");
                cardReader.Dispose();
                //mfrc522.MFRC522Stop();
                activated = false;
            }
        }


        private void StartMFRC522(Object timerInput)
        {
            if (!onUpdate)
            {
                onUpdate = true;
                String cardType = ReadTagTypeString(PICC_REQALL);
                if (!cardType.Equals("*"))
                {
                    CardDetected(cardType, ReadSerialNumberString());
                }
                onUpdate = false;
            }
        }

        private async void CardDetected(String cardType, String serialNumber)
        {
            /**Card type
            *			 	0x4400 = Mifare_UltraLight
            *				0x0400 = Mifare_One(S50)
            *				0x0200 = Mifare_One(S70)
            *				0x0800 = Mifare_Pro(X)
            *				0x4403 = Mifare_DESFire
            */

            cardType = cardType.Trim();
            switch (cardType)
            {
                case "4400":
                    cardType = "Mifare_UltraLight (" + cardType + ") ";
                    break;
                case "0400":
                    cardType = "Mifare_One(S50) (" + cardType + ") ";
                    break;
                case "0200":
                    cardType = "Mifare_One(S70) (" + cardType + ") ";
                    break;
                case "0800":
                    cardType = "Mifare_Pro(X) (" + cardType + ") ";
                    break;
                case "4403":
                    cardType = "Mifare_DESFire (" + cardType + ") ";
                    break;
            }
            if (cardType == "00")
            {

            }
            else
            {

                MFRArgs myArgs = new MFRArgs(serialNumber);

                // Tracking number is available, raise the event.
                OnCardRead(this, myArgs);

            }

        }

        private void ResetDevice()
        {
            WriteReg_MFRC522(CommandReg, PCD_RESETPHASE);
            //WriteReg(CommandReg, PCD_IDLE);
        }

        private byte FormCommand(byte cmd)
        {
            return (byte)(0x30 | (cmd & 0x0f));
        }

        /**
         * Sets a byte on the register
         * */
        private void SetBitMask(byte reg, byte mask)
        {
            byte temp = ReadReg_MFRC522(reg);
            WriteReg_MFRC522(reg, (byte)(temp | mask));
        }

        /**
         * Clears the byte from the register
         * */
        private void ClearBitMask(byte reg, byte mask)
        {
            byte temp = ReadReg_MFRC522(reg);
            WriteReg_MFRC522(reg, (byte)(temp & (~mask)));
        }


        /**
         * Write a data byte on the specified address
         * */
        private void WriteReg_MFRC522(byte addr, byte data)
        {
            byte[] writeBuffer = new byte[2];

            writeBuffer[0] = ((byte)(0x7E & (addr << 1)));
            writeBuffer[1] = data;

            SpiDisplay.Write(writeBuffer);
        }

        /**
         * Write a data byte[] on the specified address
         * */
        private void WriteData_MFRC522(byte addr, byte[] data)
        {
            foreach (byte d in data)
                WriteReg_MFRC522(addr, d);
            /*byte[] writeBuffer = new byte[data.Length+1];
            writeBuffer[0] = ((byte)(0x7E & (addr << 1)));
            for (int i = 0; i < data.Length; ++i)
                writeBuffer[i+1] = data[i];
            spiDevice.Write(writeBuffer);*/
        }

        /**
         * Read the value from a specified address
         * */
        public byte ReadReg_MFRC522(byte addr)
        {
            byte[] ReadBuffer = new byte[2];
            byte[] WriteBuffer = new byte[2];

            WriteBuffer[0] = ((byte)(0x80 | (0x7E & (addr << 1))));
            WriteBuffer[1] = 0;

            SpiDisplay.TransferFullDuplex(WriteBuffer, ReadBuffer);
            return ReadBuffer[1];
        }

        /**
         * Read the data from a specified address
         * */
        private byte[] ReadData_MFRC522(byte addr)
        {
            ArrayList result = new ArrayList();
            do
            {
                result.Add(ReadReg_MFRC522(FIFODataReg));
            } while (ReadReg_MFRC522(FIFOLevelReg) > 0);

            /*for(int i = 0 ; i < buffer64; ++i)
                result.Add(ReadReg_MFRC522(FIFODataReg));*/

            byte[] data = new byte[result.Count];
            data = (byte[])result.ToArray(typeof(byte));

            result.Clear();
            return data;
        }

        /**
         * Switch on the antenna
         * */
        private void AntennaOn()
        {
            byte temp = ReadReg_MFRC522(TxControlReg);

            if (temp != 0x03)
            {
                SetBitMask(TxControlReg, 0x03);
            }

            //SetBitMask(TxControlReg, 0x03);
        }

        /**
         * Switch off the antenna
         * */
        private void AntennaOff()
        {
            ClearBitMask(TxControlReg, 0x03);
        }

        /**
         * Initializes the MFRC522 unit
         **/
        public void MFRC522Init()
        {
            ResetDevice();

            WriteReg_MFRC522(TModeReg, 0x8D);
            WriteReg_MFRC522(TPrescalerReg, 0x3E);
            WriteReg_MFRC522(TReloadRegL, 30);
            WriteReg_MFRC522(TReloadRegH, 0);
            WriteReg_MFRC522(TxAutoReg, 0x40);
            WriteReg_MFRC522(ModeReg, 0x3D);

            AntennaOn();

        }

        /**
         * Stops the MFRC522 unit
         * */
        public void MFRC522Stop()
        {
            AntennaOff();
            ResetDevice();
        }

        /**
         * Read tag type from RFID Card
         *			 	0x4400 = Mifare_UltraLight
         *				0x0400 = Mifare_One(S50)
         *				0x0200 = Mifare_One(S70)
         *				0x0800 = Mifare_Pro(X)
         *				0x4403 = Mifare_DESFire
         */
        public byte[] ReadTagType(byte reqMode)
        {
            byte[] tagType = new byte[1];

            WriteReg_MFRC522(BitFramingReg, 0x07);

            tagType[0] = reqMode;
            return ToCard(PCD_TRANSCEIVE, tagType);
        }

        /**
         * Returns the card type string
         * */
        public String ReadTagTypeString(byte reqMode)
        {
            String s = "*";
            byte[] receivedData = ReadTagType(reqMode);
            if (receivedData != null && receivedData.Length > 1)
                s = receivedData.ToString();
            return BitConverter.ToString(receivedData).Replace("-", string.Empty);
        }

        /**
         * Read serial number from RFID Card
         * */
        public byte[] ReadSerialNumber()
        {
            byte[] serNumber = new byte[2];

            WriteReg_MFRC522(BitFramingReg, 0x00);

            serNumber[0] = PICC_ANTICOLL;
            serNumber[1] = 0x20;
            return ToCard(PCD_TRANSCEIVE, serNumber);
        }

        /**
         * Returns the serial number Card string
         * */
        public String ReadSerialNumberString()
        {
            String s = "*";
            byte[] receivedData = ReadSerialNumber();

            if (receivedData != null)
                s = receivedData.ToString();
            Debug.WriteLine(BitConverter.ToString(receivedData).Replace("-", string.Empty));

            return BitConverter.ToString(receivedData).Replace("-", string.Empty);
        }

        private byte[] ToCard(byte command, byte[] sendData)
        {
            byte irqEn = 0x00;
            byte waitIRq = 0x00;
            int n = 0;
            uint i = 0;

            byte[] readData = null;

            if (command == PCD_AUTHENT)
            {
                irqEn = 0x12;
                waitIRq = 0x10;
            }
            else if (command == PCD_TRANSCEIVE)
            {
                irqEn = 0x77;
                waitIRq = 0x30;
            }

            WriteReg_MFRC522(ComIEnReg, (byte)(irqEn | 0x80));
            ClearBitMask(ComIrqReg, 0x80);
            SetBitMask(FIFOLevelReg, 0x80);

            WriteReg_MFRC522(CommandReg, PCD_IDLE);

            WriteData_MFRC522(FIFODataReg, sendData);

            WriteReg_MFRC522(CommandReg, command);
            if (command == PCD_TRANSCEIVE)
            {
                SetBitMask(BitFramingReg, 0x80);
            }

            i = 2000;
            do
            {
                n = ReadReg_MFRC522(ComIrqReg);
                i--;
            }
            while ((i != 0) && (n == 0) && (n != waitIRq));

            ClearBitMask(BitFramingReg, 0x80);

            if (i != 0)
            {
                byte error = ReadReg_MFRC522(ErrorReg);
                if (error != 0x1B)	//BufferOvfl Collerr CRCErr ProtecolErr
                {
                    if (n == 0x01 && irqEn == 0x01)
                    {
                        //???
                    }

                    if (command == PCD_TRANSCEIVE)
                    {
                        readData = ReadData_MFRC522(FIFODataReg);
                    }
                }
            }

            WriteReg_MFRC522(CommandReg, PCD_IDLE);

            return readData;
        }

        public String TestMFRC522()
        {
            int n = 0;
            int i = 0;

            ResetDevice(); //SOFTRESET

            //**RESET internal mem
            byte[] writeBuffer = new byte[buffer25 - 1];
            for (i = 0; i < buffer25 - 1; ++i)
                writeBuffer[i] = 0;
            WriteData_MFRC522(FIFODataReg, writeBuffer);
            WriteReg_MFRC522(CommandReg, PCD_MEM);
            //**

            //**Enable self test
            WriteReg_MFRC522(AutoTestReg, 0x09);

            //**

            //**Write to FIFO buffer
            WriteReg_MFRC522(FIFODataReg, 0x00);
            WriteReg_MFRC522(FIFOLevelReg, 0x80);//Flush

            //**

            //**Start CalcCRC
            WriteReg_MFRC522(CommandReg, PCD_CALCCRC);

            //**

            //**Read result
            byte[] readData = new byte[buffer64];

            readData = ReadData_MFRC522(FIFODataReg);
            //**

            return readData.ToString();
        }

    }
}
