using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
// using Unosquare.RaspberryIO;
// using Unosquare.RaspberryIO.Abstractions;

namespace AvaTest.Services
{

    public class Dht11Service
    {
        // private const BcmPin DataPin = BcmPin.Gpio17;
        private const int DataPin = 17;

        public float Temperature { get; private set; }
        public float Humidity { get; private set; }
        
        public float HotTemperature { get; private set; }

        private bool m_isReady = true;
        
        public Dht11Service()
        {
        }

        private const int DhtPulses = 41;
        private const int DhtMaxCount = 30000;

        private const string URL = "http://192.168.1.211:5000/info";

        public class TemperatureData
        {
            public float t;
            public float h;
            public float tc;
        }
        
        public async Task<bool> ReadDataAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync(URL);

                if (string.IsNullOrEmpty(json))
                    return false;


                TemperatureData? d = JsonSerializer.Deserialize<TemperatureData>(json);
                if (d == null)
                    return false;

                this.Temperature = d.t;
                this.Humidity = d.h;
                this.HotTemperature = d.tc;
                return true;
            }        
        }
        
/*
        public bool ReadData2()
        {
            m_isReady = false;
            Console.WriteLine($"***** Pin: phys = {Pi.Gpio[DataPin].PhysicalPinNumber}, bcm={Pi.Gpio[DataPin].BcmPin}");
            float temperature = 0.0f;
            float humidity = 0.0f;
        
            // int[] pulseCounts = new int[DhtPulses * 2];
        
            
            Thread currentThread = Thread.CurrentThread;
            currentThread.Priority = ThreadPriority.AboveNormal;
            long preLowTicks = 0;
            long preHighTicks = 0;
            long[] myCounts = new long [DhtPulses];
            long[] interPulseTicks = new long[DhtPulses];
            try
            {
                Pi.Gpio[DataPin].PinMode = GpioPinDriveMode.Output;
                // Pi.Gpio[DataPin].Write(GpioPinValue.Low);
                // WaitForMkSec(300);

                Pi.Gpio[DataPin].Write(GpioPinValue.Low);
                WaitForMkSec(20000);
                
                Pi.Gpio[DataPin].Write(GpioPinValue.High);
                
                // Console.WriteLine("Switching to receive");
                
                Pi.Gpio[DataPin].PinMode = GpioPinDriveMode.Input;
                Pi.Gpio[DataPin].InputPullMode = GpioPinResistorPullMode.PullUp;
                // WaitForMkSec(30);

                WaitForMkSec(20);
                DateTime start = DateTime.Now;

                long loopCounter = 0;

                int t1 = 0;
                while (Pi.Gpio[DataPin].Read())
                {
                    t1++;
                    if (t1 > 1000)
                    {
                        throw new Exception($"High state time is {t1}");
                    }
                }
                
                while (!Pi.Gpio[DataPin].Read())
                {
                    preLowTicks++;
                    // var nowTick = DateTime.Now;
                    // long mksecs = (nowTick - start).Ticks / (TimeSpan.TicksPerMillisecond / 1000);
                    // if (mksecs > 100)
                    // {
                    //     Console.WriteLine("Reading - 1: {0}", mksecs);
                    //     return false;
                    // }
                }

                var signalLowTime = (DateTime.Now - start).Ticks; 
                start = DateTime.Now;
                while (Pi.Gpio[DataPin].Read())
                {
                    loopCounter++;
                    preHighTicks++;
                    var nowTick = DateTime.Now;
                    long mksecs = (nowTick - start).Ticks / (TimeSpan.TicksPerMillisecond / 1000);
                    
                    if (mksecs <= 100) continue;
                    Console.WriteLine("Reading - 2: preLoadTicks: {0}, PreLoadMkSec: {1}, PrePreTicks: {2}", 
                        preLowTicks, signalLowTime/(TimeSpan.TicksPerMillisecond/1000), t1
                        );
                    return false;
                }
                    
                
                for (int i = 0; i < DhtPulses; i++)
                { 
                    DateTime startTick = DateTime.Now;
                    while (!Pi.Gpio[DataPin].Value) interPulseTicks[i] = (DateTime.Now - startTick).Ticks;
                    DateTime infoStart = DateTime.Now;
                    while (Pi.Gpio[DataPin].Value) myCounts[i] = (DateTime.Now - infoStart).Ticks;
                }

                PrintData(myCounts, interPulseTicks, preLowTicks, preHighTicks);
                return false;
                //
                // // int count = 0;
                // if (!Pi.Gpio[DataPin].WaitForValue(GpioPinValue.Low, 700))
                //     throw new TimeoutException("Reading - 1");
                //
                //
                //
                //
                //
                // for (int i = 0; i < DhtPulses * 2; i += 2)
                // {
                //     pulseCounts[i] = 0;
                //     pulseCounts[i + 1] = 0;
                //     DateTime startTime = DateTime.Now;
                //     if (!Pi.Gpio[DataPin].WaitForValue(GpioPinValue.High, 50))
                //     {
                //         PrintData(pulseCounts);
                //         pulseCounts[i] = (int)(DateTime.Now - startTime).Ticks;
                //         pulseCounts[i + 1] = -1;
                //         throw new TimeoutException($"Reading - 2 ({i})");
                //     }
                //     DateTime midTime = DateTime.Now;
                //     // while (!Pi.Gpio[DataPin].Read())
                //     // {
                //     //     if (++pulseCounts[i] >= DhtMaxCount)
                //     //         throw new TimeoutException($"Reading - 2 ({i})");
                //     // }
                //     if (!Pi.Gpio[DataPin].WaitForValue(GpioPinValue.Low, 50))
                //     {
                //         PrintData(pulseCounts);
                //         pulseCounts[i] = (int)(midTime - startTime).Ticks;
                //         pulseCounts[i + 1] = (int) (DateTime.Now - midTime).Ticks;
                //         throw new TimeoutException($"Reading - 3 ({i}) timeout: {(DateTime.Now - midTime).TotalMilliseconds} ({pulseCounts[i+1]})!");
                //     }
                //     DateTime endTime = DateTime.Now;
                //
                //     pulseCounts[i] = (int)(midTime - startTime).Ticks;
                //     pulseCounts[i + 1] = (int) (endTime - midTime).Ticks;
                //     // while (Pi.Gpio[DataPin].Read())
                //     // {
                //     //     if (++pulseCounts[i + 1] >= DhtMaxCount)
                //     //         throw new TimeoutException($"Reading - 3 ({i+1})");
                //     // }
                // }

            }
            catch (Exception x)
            {
                Console.WriteLine(" XXXX: {0}", x.Message);
                m_isReady = true;
                return false;
            }
            finally
            {
                currentThread.Priority = ThreadPriority.Normal;
                m_isReady = true;
            }
            
            // // Done with timing critical code, now interpret the results.
            //
            // // Compute the average low pulse width to use as 50 microsecond reference threshold.
            // // Ignore the first two readings because they are a constant 80 microsecond pulse.
            // int threshold = 0;
            // for (int i = 2; i < DhtPulses * 2; i += 2)
            // {
            //     threshold += pulseCounts[i];
            // }
            // threshold /= DhtPulses - 1;
        
            
            // Interpret each high pulse as a 0 or 1 by comparing it to the 50us reference.
            // If the count is less than 50us it must be a ~28us 0 pulse, and if it's higher
            // then it must be a ~70us 1 pulse
            // int[] data = new int [5];
            // for (int i = 3; i < DhtPulses * 2; i += 2)
            // {
            //     int index = (i - 3) / 16;
            //     if (myCounts[i])
            //     data[index] <<= 1;
            //     if (pulseCounts[i] >= threshold)
            //         data[index] |= 1;
            // }
            //
            // // Verify checksum of received data.
            // var sum = data[0] + data[1] + data[2] + data[3];
            // var sum1 = sum & 0xFF;
            // var check = data[4];
            //
            // Console.WriteLine("---- {0}, {1}, {2}, {3}, {4} ----", data[0], data[1], data[2], data[3], data[4]);
            // Console.WriteLine("Check: {0} ({1}) =?= {2}", sum1, sum, check);
            // if (data[4] == ((data[0] + data[1] + data[2] + data[3]) & 0xFF))
            // {
            //     Humidity = (float) data[0];
            //     Temperature = (float) data[2];
            //     Console.WriteLine("YESSSS!!!!!!");
            // }
            // else
            //     Console.WriteLine("Wrong checksum: {0} != {1}+{2}+{3}+{4}", data[4], data[0], data[1], data[2], data[3]);
            //
            // Console.WriteLine("=====");
            // m_isReady = true;
            // return true;
        }
*/

        private void PrintData(long[] myCounts, long[] interPulseTicks, long preLowTicks, long preHighTicks)
        {   
            var sb = new StringBuilder();

            sb.AppendFormat("{0} - {1}",
                preLowTicks / TimeSpan.TicksPerMillisecond / 1000,
                preHighTicks / TimeSpan.TicksPerMillisecond / 1000);
            
            Console.WriteLine(sb.ToString());
            sb.Length = 0;
            
            foreach (var t in myCounts)
                sb.AppendFormat(" {0}", t);

            Console.WriteLine(sb.ToString());
            sb.Length = 0;

            foreach (var l in interPulseTicks)
                sb.AppendFormat(" {0}", l);
            Console.WriteLine(sb.ToString());
            
            Console.WriteLine("------------------------------------------");
        }

        void WaitForMkSec(long mksec)
        {
            DateTime startTick = DateTime.Now;
            while (ToMicroseconds(DateTime.Now - startTick) >= mksec) break;
            // Thread.Sleep(msec);
            // int endTick = Environment.TickCount + msec;
            // while (Environment.TickCount >= endTick)
            //     break;
        }

        static long ToMicroseconds(TimeSpan ts)
        {
            return ts.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
        }
        
/*
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
*/

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

/*
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
*/

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
/*
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
*/

        // private void SendAndSleep(GpioPinValue val, int msec)
        // {
        //     Pi.Gpio[DataPin].Write(val);
        //     Thread.Sleep(msec);
        // }

        public bool IsReady()
        {
            return m_isReady;
        }
    }
}