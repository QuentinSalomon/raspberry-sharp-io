#region References

using System;
using Raspberry.Timers;

#endregion

namespace Raspberry.IO.SerialPeripheralInterface
{
    public class SpiConnection : IDisposable
    {
        #region Fields

        private readonly IOutputPin clock;
        private readonly IOutputPin ss;
        private readonly IInputPin miso;
        private readonly IOutputPin mosi;

        private readonly Endianness endianness = Endianness.LittleEndian;

        #endregion

        #region Instance Management

        public SpiConnection(IOutputPin clock, IOutputPin ss, IInputPin miso, IOutputPin mosi, Endianness endianness)
        {
            this.clock = clock;
            this.ss = ss;
            this.miso = miso;
            this.mosi = mosi;
            this.endianness = endianness;

            clock.Write(false);
            ss.Write(true);

            if (mosi != null)
                mosi.Write(false);
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        #endregion

        #region Methods

        public void Close()
        {
            clock.Dispose();
            ss.Dispose();
            if (mosi != null)
                mosi.Dispose();
            if (miso != null)
                miso.Dispose();
        }

        public SpiSlaveSelection SelectSlave()
        {
            ss.Write(false);
            return new SpiSlaveSelection(this);
        }
        
        internal void DeselectSlave()
        {
            ss.Write(true);
        }

        public void Synchronize()
        {
            clock.Write(true);
            Timer.Sleep(1);
            clock.Write(false);
        }

        public void Write(bool data)
        {
            if (mosi == null)
                throw new NotSupportedException("No MOSI pin has been provided");

            mosi.Write(data);
            Synchronize();
        }

        public void Write(byte data, int bitCount)
        {
            if (bitCount > 8)
                throw new ArgumentOutOfRangeException("bitCount", bitCount, "byte data cannot contain more than 8 bits");

            SafeWrite(data, bitCount);
        }

        public void Write(ushort data, int bitCount)
        {
            if (bitCount > 16)
                throw new ArgumentOutOfRangeException("bitCount", bitCount, "ushort data cannot contain more than 16 bits");

            SafeWrite(data, bitCount);
        }

        public void Write(uint data, int bitCount)
        {
            if (bitCount > 32)
                throw new ArgumentOutOfRangeException("bitCount", bitCount, "uint data cannot contain more than 32 bits");

            SafeWrite(data, bitCount);
        }

        public void Write(ulong data, int bitCount)
        {
            if (bitCount > 64)
                throw new ArgumentOutOfRangeException("bitCount", bitCount, "ulong data cannot contain more than 64 bits");

            SafeWrite(data, bitCount);
        }

        public bool Read()
        {
            if (miso == null)
                throw new NotSupportedException("No MISO pin has been provided");

            Synchronize();
            return miso.Read();
        }

        public ulong Read(int bitCount)
        {
            if (bitCount > 64)
                throw new ArgumentOutOfRangeException("bitCount", bitCount, "ulong data cannot contain more than 64 bits");

            ulong data = 0;
            for (var i = 0; i < bitCount; i++)
            {
                var index = endianness == Endianness.BigEndian
                                ? i
                                : bitCount - 1 - i;

                var bit = Read();
                if (bit)
                    data |= ((ulong)1 << index);
            }

            return data;
        }

        #endregion

        #region Private Helpers

        private void SafeWrite(ulong data, int bitCount)
        {
            for (var i = 0; i < bitCount; i++)
            {
                var index = endianness == Endianness.BigEndian
                                ? i
                                : bitCount - 1 - i;

                var bit = data & ((ulong) 1 << index);
                Write(bit != 0);
            }
        }

        #endregion
    }
}