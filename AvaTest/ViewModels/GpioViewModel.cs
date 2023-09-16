using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using AvaTest.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ReactiveUI;

[assembly:InternalsVisibleTo("AvaUnitTests")]
namespace AvaTest.ViewModels
{

    public class GpioViewModel : ViewModelBase
    {
        private readonly Dht11Service m_dht11Service = new Dht11Service();
        private readonly DataSeries m_threeSecondsSeries = new DataSeries(TimeSpan.FromSeconds(3), 100);
        private readonly DataSeries m_thirtySecondsSeries = new DataSeries(TimeSpan.FromSeconds(30), 100);
        private readonly DataSeries m_threeMinutesSeries = new DataSeries(TimeSpan.FromMinutes(3), 100);
        private enum TemperatureMode
        {
            Raw,
            Celsius,
            Fahrenheit
        }

        enum EShowMode
        {
            ByThreeSeconds,
            ByThirtySeconds,
            ByThreeMinutes
        }

        private EShowMode m_showMode = EShowMode.ByThreeSeconds;

        private readonly bool m_isFakeTemperature;

        public GpioViewModel()
        {
            var os = Environment.OSVersion;
            m_isFakeTemperature = os.Version.Major == 21 || Environment.GetEnvironmentVariable("FAKE_TEMPERATURE") == "true";

            MeasureStepsCount = 100;

            MeasureStepsCount = 100;
            m_dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Normal, TimerTick);
            m_dispatcherTimer.Start();
            var lineSeries = new LineSeries<float?>();
            m_series.Add(lineSeries);
            lineSeries.EnableNullSplitting = true;
            lineSeries.Values = new ObservableCollection<float?>();

        }


        private readonly Random m_rnd = new Random();

        private async void TimerTick(object? sender, EventArgs e)
        {
            Console.WriteLine("TimerTick");
            float temp;
            if (m_isFakeTemperature)
            {
                Console.WriteLine("Fake!");
                InternalTemperature = (int)(25.0f + m_rnd.Next(-5, 5));
                Humidity = 0;
                TemperatureInCelsius = (int)(90.0f + m_rnd.Next(-20, 20));
            }
            else
            {
                // temp = ReadTemp();
                // Console.WriteLine("Temp: {0}", temp);
                try
                {
                    if (!m_dht11Service.IsReady())
                        return;
                    if (await m_dht11Service.ReadDataAsync())
                    {
                        InternalTemperature = (int) m_dht11Service.Temperature;
                        Humidity = (int) m_dht11Service.Humidity;
                        TemperatureInCelsius = (int) m_dht11Service.HotTemperature;
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

            // TemperatureInCelsius = temp;
            ShiftTemps(TemperatureInCelsius);
            MinTime = m_temperatureMeasures.First().Time;//.Min(item => item.Time);
            MaxTime = m_temperatureMeasures.Last().Time; //Max(item => item.Time);

            // FigurePath = GeneratePathGeometry(m_temperatureMeasures);
        }

        DataSeries GetCurrentDataSeries()
        {
            switch (m_showMode)
            {
                case EShowMode.ByThreeSeconds : return m_threeSecondsSeries;
                case EShowMode.ByThirtySeconds : return m_thirtySecondsSeries;
                default: return m_threeMinutesSeries;
            }
        }

        private void UpdateTemps(float temp)
        {
            DateTime now = DateTime.Now;
            m_threeSecondsSeries.RegisterData(temp, now);
            m_thirtySecondsSeries.RegisterData(temp, now);
            m_threeMinutesSeries.RegisterData(temp, now);

            var temperatureSeries = (LineSeries<float?>)m_series[0];//new LineSeries<float>();
            if (temperatureSeries.Values == null)
                return;

            var dataValues = (ObservableCollection<float?>)temperatureSeries.Values;
            var dataSeries = GetCurrentDataSeries();

            var measureData = dataSeries.ToArray();

            if (measureData.Count > dataValues.Count)
                dataValues.Clear();

            float minTemp = float.MaxValue;
            float maxTemp = float.MinValue;

            for (int i = 0; i < measureData.Count; i++)
            {
                if (dataValues.Count <= i)
                {
                    dataValues.Add(measureData[i] == null ? null : measureData[i].Data);
                }
                else
                    dataValues[i] = measureData[i] == null? null : measureData[i].Data;

                if (measureData[i] != null)
                {
                    if (measureData[i].Data > maxTemp) maxTemp = measureData[i].Data;
                    if (measureData[i].Data < minTemp) minTemp = measureData[i].Data;
                }
            }

            MinTemperature = minTemp;
            MaxTemperature = maxTemp;
        }

        public void On3SecondsSelected()
        {
            this.m_showMode = EShowMode.ByThreeSeconds;
        }

        public void On30SecondsSelected()
        {
            m_showMode = EShowMode.ByThirtySeconds;
        }

        public void On3MinutesSelected()
        {
            m_showMode = EShowMode.ByThreeMinutes;
        }

        private void ShiftTemps(float temp)
        {
            UpdateTemps(temp);
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
        private int m_temperatureStepsCount;

        private readonly ObservableCollection<MeasureData> m_temperatureMeasures = new();
//        private readonly List<float> m_thirtySecondesMeasures = new List<float>();
//        private readonly List<>
        private readonly ObservableCollection<ISeries> m_series = new ObservableCollection<ISeries>() ;

        private DateTime m_minTime;

        private DateTime m_maxTime;

        private float m_minTemperature;
        private float m_maxTemperature;

        public float MinTemperature
        {
            get => m_minTemperature;
            set => this.RaiseAndSetIfChanged(ref m_minTemperature, value);
        }

        public float MaxTemperature
        {
            get => m_maxTemperature;
            set => this.RaiseAndSetIfChanged(ref m_maxTemperature, value);
        }


        public float TemperatureInCelsius
        {
            get => m_temperatureInCelsius;
            set => this.RaiseAndSetIfChanged(ref m_temperatureInCelsius, value);
        }

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

        public ObservableCollection<ISeries> Series => m_series;
        
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

    internal class DataAccumulator
    {
        private readonly TimeSpan m_accumulatorSpan;
        private float m_temperatureAccumulator;
        private int m_counter = 0;
        
        private DateTime m_lastCheckPoint = DateTime.MinValue;

        public DataAccumulator(TimeSpan accumulatorSpan)
        {
            m_accumulatorSpan = accumulatorSpan;
        }

        public MeasureData? RegisterData(float temperature)
        {
            return RegisterData(temperature, DateTime.Now);
        }

        public MeasureData? RegisterData(float temperature, DateTime currentDateTime)
        {
            if (m_lastCheckPoint == DateTime.MinValue)
            {
                m_lastCheckPoint = currentDateTime;
                m_temperatureAccumulator = 0;
                m_counter = 0;
            }
            if (currentDateTime >= m_lastCheckPoint+m_accumulatorSpan)
            {
                var res = new MeasureData() {
                    Data = m_temperatureAccumulator,
                    Time = m_lastCheckPoint
                };
                m_temperatureAccumulator = temperature;
                m_lastCheckPoint += m_accumulatorSpan;
                m_counter = 1;
                return res;
            }

            m_temperatureAccumulator = (m_temperatureAccumulator*m_counter+temperature)/(m_counter+1);
            m_counter++;
            return null;
        }
    }

    internal class CircularBuffer<T>
    {
        private readonly T[] m_buffer;
        private int m_endIdx;

        private CircularBuffer(int bufferSize)
        {
            m_buffer = new T[bufferSize];
            m_endIdx = 0;
        }

        public int Count => m_buffer.Length;

        public void Put(T data)
        {
            m_buffer[m_endIdx++] = data;
            if (m_endIdx == m_buffer.Length)
            {
                m_endIdx = 0;
            }
        }

        public List<T> ToArray()
        {
            var res = new List<T>();
            int endIdx = m_endIdx;
            foreach (var t in m_buffer)
            {
                if (endIdx >= m_buffer.Length)
                    endIdx = 0;

                res.Add(m_buffer[endIdx++]);
            }

            return res;
        }

        public void ToArray(List<T> buffer)
        {
            if (buffer.Count > m_buffer.Length) 
                buffer.Clear();

            int endIdx = m_endIdx;
            for (int i = 0; i < m_buffer.Length; i++)
            {
                if (endIdx >= m_buffer.Length)
                    endIdx = 0;

                if (i >= buffer.Count)
                    buffer.Add(m_buffer[endIdx++]);
                else
                    buffer[i] = m_buffer[endIdx++];
            }
        }

       public static CircularBuffer<T> Create(int bufferSize) 
        {
            if (bufferSize <= 0) throw new ArgumentException(
                string.Format("Circular buffer size must be more than 0 size, but passed value is {0}", bufferSize));

            return new CircularBuffer<T>(bufferSize);
        }
    }

    internal class DataSeries
    {
        private readonly DataAccumulator m_accumulator;
        private readonly CircularBuffer<MeasureData> m_buffer;
        
        public DataSeries(TimeSpan span, int seriesLength)
        {
            m_accumulator = new DataAccumulator(span);
            m_buffer = CircularBuffer<MeasureData>.Create(seriesLength);
        }

        public void RegisterData(float data, DateTime currentTimeStamp)
        {
            MeasureData? current = m_accumulator.RegisterData(data, currentTimeStamp);
            if (current != null)
            {
                m_buffer.Put(current);
            }
        }

        public List<MeasureData> ToArray()
        {
            return m_buffer.ToArray();
        }

        public void ToArray(List<MeasureData> buffer)
        {
            m_buffer.ToArray(buffer);
        }
    }
    
    public class MeasureData
    {
        public DateTime Time = DateTime.Now;
        public float DeltaTime { get; set; }
        public float Data { get; set; } = 0.0f;
    }
    
}