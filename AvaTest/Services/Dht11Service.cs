using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace AvaTest.Services
{

    public class Dht11Service
    {
        private const int DataPin = 17;

        public float Temperature { get; private set; }
        public float Humidity { get; private set; }

        public Dht11Service()
        {
        }

        public bool ReadData()
        {
            Console.WriteLine("ReadData - 0");
            Pi.Gpio[DataPin].PinMode = GpioPinDriveMode.Output;
            SendAndSleep(GpioPinValue.High, 500);
            SendAndSleep(GpioPinValue.Low, 20);
            Pi.Gpio[DataPin].PinMode = GpioPinDriveMode.Input;
            Pi.Gpio[DataPin].InputPullMode = GpioPinResistorPullMode.PullUp;
            Console.WriteLine("ReadData - 1");
            var data = CollectInput();
            Console.WriteLine("ReadData - 2");
            int[] pullUpLength = ParseDataPullUpLength(data);
            Console.WriteLine("ReadData - 3");
            if (pullUpLength.Length != 40)
                throw new ArgumentException($"Wrong pullup message length: {pullUpLength.Length}");

            byte[] bits = CalculateBits(pullUpLength);
            byte[] bytes = BitsToBytes(bits);

            Console.WriteLine("ReadData - 4");
            int checksum = CalculateChecksum(bytes);
            if (checksum != bytes[4])
                throw new ArgumentException("Wrong checksum");

            // # ok, we have valid data
            //
            // # The meaning of the return sensor values
            // # the_bytes[0]: humidity int
            // # the_bytes[1]: humidity decimal
            // # the_bytes[2]: temperature int
            // # the_bytes[3]: temperature decimal
            Temperature = bytes[2] + (float) bytes[3] / 10;
            Humidity = bytes[0] + (float) bytes[1] / 10;

            return true;
        }

        private int CalculateChecksum(byte[] bytes)
        {
            return bytes[0] + bytes[1] + bytes[2] + bytes[3] & 0xFF;
        }

        private byte[] BitsToBytes(byte[] bits)
        {
            var res = new List<byte>();
            var count = 0;
            byte curByte = 0;
            foreach (var bit in bits)
            {
                if (bit != 0) curByte <<= 1;
                count++;

                if (count == 8)
                {
                    res.Add(curByte);
                    curByte = 0;
                    count = 0;
                }
            }

            if (count > 0)
                res.Add(curByte);

            return res.ToArray();
        }

        private byte[] CalculateBits(int[] pullUpLength)
        {
            int shortestPullUp = 1000;
            int longestPullUp = 0;

            foreach (var length in pullUpLength)
            {
                if (length < shortestPullUp)
                    shortestPullUp = length;
                if (length > longestPullUp)
                    longestPullUp = length;
            }

            var halfWay = shortestPullUp + (longestPullUp - shortestPullUp) / 2;
            var stream = new MemoryStream();
            var buffer = new byte[1];
            foreach (var length in pullUpLength)
            {
                buffer[0] = (byte) (length > halfWay ? 1 : 0);
                stream.Write(buffer, 0, 1);
            }

            return stream.ToArray();
        }

        private enum States
        {
            InitPullDown,
            InitPullUp,
            DataFirstPullDown,
            DataPullUp,
            DataPullDown
        }

        private int[] ParseDataPullUpLength(byte[] data)
        {
            States state = States.InitPullDown;
            var length = new List<int>();
            int currentLength = 0;

            foreach (var b in data)
            {
                var current = b == 0 ? GpioPinValue.Low : GpioPinValue.High;
                currentLength++;

                switch (state)
                {
                    case States.InitPullDown:
                        if (current == GpioPinValue.Low)
                            state = States.InitPullUp;
                        break;
                    case States.InitPullUp:
                        if (current == GpioPinValue.High)
                            state = States.DataFirstPullDown;
                        break;
                    case States.DataFirstPullDown:
                        if (current == GpioPinValue.Low)
                            state = States.DataPullUp;
                        break;
                    case States.DataPullUp:
                        if (current == GpioPinValue.High)
                        {
                            currentLength = 0;
                            state = States.DataPullDown;
                        }

                        break;
                    case States.DataPullDown:
                        if (current == GpioPinValue.Low)
                        {
                            length.Add(currentLength);
                            state = States.DataPullUp;
                        }

                        break;
                }
            }

            return length.ToArray();
        }

        /*
        def __collect_input(self):
        # collect the data while unchanged found
        unchanged_count = 0

        # this is used to determine where is the end of the data
        max_unchanged_count = 100

        last = -1
        data = []
        while True:
            current = RPi.GPIO.input(self.__pin)
            data.append(current)
            if last != current:
                unchanged_count = 0
                last = current
            else:
                unchanged_count += 1
                if unchanged_count > max_unchanged_count:
                    break

        return data

         */
        private byte[] CollectInput()
        {
            int unchangedCount = 0;
            // int maxUnchangedCount = 100;
            int maxUnchangedCount = 100;
            int DHT_MAXCOUNT = 32000;

            bool last = false;
            bool isFirst = true;

            var stream = new MemoryStream();
            byte[] buffer = new byte[1];

            int initCount = 0;
            while (Pi.Gpio[DataPin].Read() && initCount++ < DHT_MAXCOUNT)
            { }

            if (initCount >= DHT_MAXCOUNT)
                throw new Exception("No pin initialization");
            
            while (true)
            {
                // Console.WriteLine("PIO Mode: {0} ({1})", Pi.Gpio[DataPin].PinMode, Pi.Gpio[DataPin].InputPullMode);
                var val = Pi.Gpio[DataPin].Read();

                Console.Write(val?"+":" ");
                buffer[0] = val ? (byte) 0 : (byte) 1;
                stream.Write(buffer, 0, 1);
                if (isFirst || last != val)
                {
                    unchangedCount = 0;
                    last = val;
                }
                else
                {
                    unchangedCount++;
                    if (unchangedCount > maxUnchangedCount)
                        break;
                }
                isFirst = false;
            }

            return stream.ToArray();
        }

        private void SendAndSleep(GpioPinValue val, int msec)
        {
            Pi.Gpio[DataPin].Write(val);
            Thread.Sleep(msec);
        }
    }
}