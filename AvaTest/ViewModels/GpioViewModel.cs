using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using AvaTest.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ReactiveUI;
// using Unosquare.RaspberryIO;
// using Unosquare.RaspberryIO.Abstractions;
// using Unosquare.WiringPi;

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

        enum EShowMode
        {
            ByThreeSeconds,
            ByThirtySeconds,
            ByThreeMinutes
        }

        private readonly bool m_isFakeTemperature;

        public GpioViewModel()
        {
            var os = Environment.OSVersion;
            m_isFakeTemperature = os.Version.Major == 21;

            if (Environment.GetEnvironmentVariable("FAKE_TEMPERATURE") == "true")
            {
                m_isFakeTemperature = true;
            }

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

        float m_thirtySecondsAccumulator = 0.0f;
        float m_threeMinutesAccumulator = 0.0f;

        DateTime m_thirtySecondsTimeStamp;
        DateTime m_threeMinutesTimeStamp;

        private void ShiftTemps(float temp)
        {
            LineSeries<float?> temperatureSeries = (LineSeries<float?>)m_series[0];//new LineSeries<float>();
            ObservableCollection<float?> dataValues = (ObservableCollection<float?>)temperatureSeries.Values;
            for (int i = 0; i < m_temperatureMeasures.Count - 1; i++)
            {
                m_temperatureMeasures[i].Data = m_temperatureMeasures[i + 1].Data;
                float data = m_temperatureMeasures[i].Data;
                if (dataValues.Count > i)
                    dataValues[i] = data < 0.01? null : data;
                else 
                    dataValues.Add(data < 0.01? null : data);
            }

            m_temperatureMeasures.Last().Data = temp;
            if (dataValues.Count == m_temperatureMeasures.Count)
                dataValues[m_temperatureMeasures.Count - 1] = temp < 0.01? null: temp;
            else
                dataValues.Add(temp < 0.01? null : temp);
            // temperatureSeries.Values = m_temperatureMeasures.Select(item => item.Data).ToArray();
            var tempState = AnalyzeDelta(m_temperatureMeasures);
            TemperatureState = tempState;
            // m_series.Clear();
            // m_series.Add(temperatureSeries);
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

    public class DataAccumulator
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
            if (m_lastCheckPoint == DateTime.MinValue)
            {
                m_lastCheckPoint = DateTime.Now;
                m_temperatureAccumulator = 0f;
                m_counter = 0;
            }
            if (DateTime.Now > m_lastCheckPoint+m_accumulatorSpan)
            {
                var res = new MeasureData() {
                    Data = m_temperatureAccumulator,
                    Time = m_lastCheckPoint
                };
                m_temperatureAccumulator = temperature;
                m_lastCheckPoint = DateTime.Now;
                m_counter = 1;
                return res;
            }

            m_temperatureAccumulator += (m_temperatureAccumulator*m_counter+temperature)/(m_counter+1);
            m_counter++;
            return null;
        }
    }

    public class MeasureData
    {
        public DateTime Time = DateTime.Now;
        public float DeltaTime { get; set; }
        public float Data { get; set; } = 0.0f;
    }
    
}