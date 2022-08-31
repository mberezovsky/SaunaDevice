using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace AvaTestProject
{
    [TestFixture]
    public class Tests
    {
        
        private enum States
        {
            InitPullDown,
            InitPullUp,
            DataFirstPullDown,
            DataPullUp,
            DataPullDown
        }

        struct GpioPinValue
        {
            public const int Low = 0;
            public const int High = 1;
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
 
        
        [Test]
        public void Test1()
        {
            var stream = new MemoryStream();
            byte[] buf = new[] {(byte)1}; 
            for (int i=0; i<100; i++)
                stream.Write(buf, 0, 1);
            ParseDataPullUpLength(stream.ToArray());
            Assert.True(true);
        }
    }
}