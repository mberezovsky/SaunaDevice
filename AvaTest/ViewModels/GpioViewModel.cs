using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Avalonia.Threading;
using AvaTest.Services;
using ReactiveUI;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;

namespace AvaTest.ViewModels
{

    public class GpioViewModel : ViewModelBase
    {
        private readonly Dht11Service m_dht11Service = new Dht11Service();
        private enum TemperatureMode
        {
            Raw,
            Celsius,
            Fahrenheit
        }

        private readonly bool m_isFakeTemperature;

        public GpioViewModel()
        {
            var os = Environment.OSVersion;
            m_isFakeTemperature = os.Version.Major == 21;

            MeasureStepsCount = 100;

            if (!m_isFakeTemperature)
            {
                Pi.Init<BootstrapWiringPi>();
                SetPins(11, 9, 8, TemperatureMode.Celsius);
            }

            MeasureStepsCount = 100;
            m_dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Normal, TimerTick);
            m_dispatcherTimer.Start();

        }

        // public Collection<Item> Items
        // {
        //     get => m_items;
        //     set => this.RaiseAndSetIfChanged(ref m_items, value);
        // }

        // private static string GeneratePathGeometry(Collection<float> pointsData)
        // {
        //     int figureWidth = 400;
        //     int figureHeight = 300;
        //     int xOffset = -200;
        //     int yOffset = -200;
        //     float lowestTemperature = pointsData.Min();
        //     float highestTemperature = pointsData.Max();
        //     int stepsCount = pointsData.Count;
        //
        //     float xScale = (float) figureWidth / stepsCount;
        //     float yScale = (highestTemperature - lowestTemperature) / figureHeight;
        //
        //     var sb = new StringBuilder();
        //     bool isFirstData = true;
        //     for (int i = 0; i < pointsData.Count; i++)
        //     {
        //         if (pointsData[i] <= 0)
        //             continue;
        //
        //         var xValue = (int) (i * xScale) + xOffset;
        //         var yValue = figureHeight / 2 - ((int) ((pointsData[i] - lowestTemperature) / yScale) + yOffset);
        //         if (isFirstData)
        //         {
        //             isFirstData = false;
        //             sb.Append($"M {xValue},{yValue}");
        //         }
        //         else
        //             sb.Append($" L {xValue},{yValue}");
        //     }
        //
        //     return sb.ToString();
        // }

        private readonly Random m_rnd = new Random();

        private void TimerTick(object? sender, EventArgs e)
        {
            Console.WriteLine("TimerTick");
            float temp;
            if (m_isFakeTemperature)
            {
                Console.WriteLine("Fake!");
                temp = 25.0f + m_rnd.Next(-5, 5);
            }
            else
            {
                temp = ReadTemp();
                Console.WriteLine("Temp: {0}", temp);
                try
                {
                    if (m_dht11Service.ReadData())
                    {
                        InternalTemperature = (int) m_dht11Service.Temperature;
                        Humidity = (int) m_dht11Service.Humidity;
                        Console.WriteLine("ITemp: {0}, Hum: {1}", InternalTemperature, Humidity);
                    }
                    else
                        Console.WriteLine("Can't read Internal Temperature");

                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            TemperatureInCelsius = temp;
            ShiftTemps(temp);
            MinTime = m_temperatureMeasures.Min(item => item.Time);
            MaxTime = m_temperatureMeasures.Max(item => item.Time);

            // FigurePath = GeneratePathGeometry(m_temperatureMeasures);
        }

        private void ShiftTemps(float temp)
        {
            for (int i = 0; i < m_temperatureMeasures.Count - 1; i++)
            {
                m_temperatureMeasures[i].Data = m_temperatureMeasures[i + 1].Data;
            }

            m_temperatureMeasures.Last().Data = temp;
            var tempState = AnalyzeDelta(m_temperatureMeasures);
            TemperatureState = tempState;
        }

        private ETemperatureStates AnalyzeDelta(ObservableCollection<MeasureData> temperatureMeasures)
        {
            var increasingDeltas = 0;
            var decreasingDeltas = 0;

            for (int i = temperatureMeasures.Count - MedianAperture; i < temperatureMeasures.Count; i++)
            {
                var delta = temperatureMeasures[i].Data - temperatureMeasures[i - 1].Data; 
                if (delta > 0f) // Rising
                    increasingDeltas++;
                else if (delta < 0f)
                    decreasingDeltas++;
            }

            if (increasingDeltas <= 1)
                if (decreasingDeltas <= 1)
                    return ETemperatureStates.Holding;
                else
                    return ETemperatureStates.Cooling;
            return decreasingDeltas <= 1 ? ETemperatureStates.Heating : ETemperatureStates.Holding;
        }

        private int m_mySck;
        private int m_mySo;
        private int m_myCs;
        private TemperatureMode m_myMode;
        private readonly DispatcherTimer m_dispatcherTimer;
        private float m_temperatureInCelsius;
        // private string m_figurePath;
        // private Geometry m_figureGeometry;
        private int m_temperatureStepsCount;

        private readonly ObservableCollection<MeasureData> m_temperatureMeasures = new ObservableCollection<MeasureData>();

        private DateTime m_minTime;

        private DateTime m_maxTime;
        // private Collection<Item> m_items;

        private void SetPins(int sck, int so, int cs, TemperatureMode mode)
        {
            m_mySck = sck;
            m_mySo = so;
            m_myCs = cs;
            m_myMode = mode;

            Pi.Gpio[m_mySck].PinMode = GpioPinDriveMode.Output;
            Pi.Gpio[m_myCs].PinMode = GpioPinDriveMode.Output;
            Pi.Gpio[m_mySo].PinMode = GpioPinDriveMode.Input;
        }

        private float ReadTemp()
        {
            Pi.Gpio[m_myCs].Write(GpioPinValue.Low);
            Thread.Sleep(2);
            Pi.Gpio[m_myCs].Write(GpioPinValue.High);
            Thread.Sleep(220);

            Pi.Gpio[m_myCs].Write(GpioPinValue.Low);
            // Skipping the dummy 15-th bit...
            Pi.Gpio[m_mySck].Write(GpioPinValue.High);
            Thread.Sleep(1);
            Pi.Gpio[m_mySck].Write(GpioPinValue.Low);

            // Reading for 14 to 3 bits for info.
            var value = 0;
            for (int i = 11; i >= 0; i--)
            {
                Pi.Gpio[m_mySck].Write(GpioPinValue.High);
                value += (Pi.Gpio[m_mySo].Read() ? 1 : 0) * (1 << i);
                Pi.Gpio[m_mySck].Write(GpioPinValue.Low);
            }

            // Reading the 3 bit for error
            Pi.Gpio[m_mySck].Write(GpioPinValue.High);
            var errorTc = Pi.Gpio[m_mySo].Read();
            Pi.Gpio[m_mySck].Write(GpioPinValue.Low);

            for (int i = 1; i <= 2; i++)
            {
                Pi.Gpio[m_mySck].Write(GpioPinValue.High);
                Thread.Sleep(1);
                Pi.Gpio[m_mySck].Write(GpioPinValue.Low);
            }

            Pi.Gpio[m_myCs].Write(GpioPinValue.High);

            if (errorTc)
                return -1.0f;

            return m_myMode switch
            {
                TemperatureMode.Raw => value,
                TemperatureMode.Celsius => value * 0.25f,
                TemperatureMode.Fahrenheit => value * 0.25f * 0.9f / 5.0f + 32.0f,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public float TemperatureInCelsius
        {
            get => m_temperatureInCelsius;
            set => this.RaiseAndSetIfChanged(ref m_temperatureInCelsius, value);
        }

        // public string FigurePath
        // {
        //     get => m_figurePath;
        //     set
        //     {
        //         this.RaiseAndSetIfChanged(ref m_figurePath, value);
        //         try
        //         {
        //             var geometry = Geometry.Parse(m_figurePath);
        //             FigureGeometry = geometry;
        //         }
        //         catch (Exception e)
        //         {
        //         }
        //     }
        // }

        // public Geometry FigureGeometry
        // {
        //     get => m_figureGeometry;
        //     set => this.RaiseAndSetIfChanged(ref m_figureGeometry, value);
        // }

        public int MedianAperture
        {
            get => m_medianAperture;
            set => this.RaiseAndSetIfChanged(ref m_medianAperture, value);
        }

        public int MeasureStepsCount
        {
            get => m_temperatureStepsCount;
            set
            {
                if (m_temperatureStepsCount == value)
                    return;

                if (value < m_temperatureMeasures.Count) // Уменьшаем коллекцию
                    while (m_temperatureMeasures.Count > value)
                        m_temperatureMeasures.RemoveAt(0);
                
                float maxDelta = 0.0f;
                if (m_temperatureMeasures.Any())
                    maxDelta = m_temperatureMeasures.Max(item => item.DeltaTime);
                
                float curDelta = maxDelta+0.1f;
                DateTime currentDt = DateTime.Now - TimeSpan.FromSeconds(value);
                while (m_temperatureMeasures.Count < value) // Увеличивем коллекцию
                {
                    m_temperatureMeasures.Add(new MeasureData()
                    {
                        DeltaTime = curDelta,
                        Time = currentDt
                    });
                    curDelta += 0.1f;
                    currentDt += TimeSpan.FromSeconds(1);
                }
                this.RaiseAndSetIfChanged(ref m_temperatureStepsCount, value);
            }
        }

        public DateTime MinTime
        {
            get => m_minTime;
            set => this.RaiseAndSetIfChanged(ref m_minTime, value);
        }

        public DateTime MaxTime
        {
            get => m_maxTime;
            set => this.RaiseAndSetIfChanged(ref m_maxTime, value);
        }

        public ObservableCollection<MeasureData> Measures => m_temperatureMeasures;

        public enum ETemperatureStates
        {
            Heating,
            Holding,
            Cooling
        }

        private ETemperatureStates m_temperatureState;
        private bool m_isTemperatureRising;
        private bool m_isTemperatureCooling;
        private bool m_isTemperatureKeeping;
        private int m_internalTemperature;
        private int m_humidity;
        private int m_medianAperture = 5;

        public ETemperatureStates TemperatureState
        {
            get => m_temperatureState;
            set
            {
                IsTemperatureCooling = value == ETemperatureStates.Cooling;
                IsTemperatureRising = value == ETemperatureStates.Heating;
                IsTemperatureKeeping = value == ETemperatureStates.Holding;
            }
        }

        public bool IsTemperatureRising
        {
            get => m_isTemperatureRising;
            set => this.RaiseAndSetIfChanged(ref m_isTemperatureRising,  value);
        }

        public bool IsTemperatureCooling
        {
            get => m_isTemperatureCooling;
            set => this.RaiseAndSetIfChanged(ref m_isTemperatureCooling, value);
        }

        public bool IsTemperatureKeeping
        {
            get => m_isTemperatureKeeping;
            set => this.RaiseAndSetIfChanged(ref m_isTemperatureKeeping, value);
        }

        public int InternalTemperature
        {
            get => m_internalTemperature;
            set => this.RaiseAndSetIfChanged(ref m_internalTemperature, value);
        }

        public int Humidity
        {
            get => m_humidity;
            set => this.RaiseAndSetIfChanged(ref m_humidity, value);
        }
    }

    public class MeasureData
    {
        public DateTime Time = DateTime.Now;
        public float DeltaTime { get; set; }
        public float Data { get; set; } = 0.0f;
    }
    
}