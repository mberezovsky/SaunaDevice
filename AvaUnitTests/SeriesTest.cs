using AvaTest.ViewModels;

namespace AvaUnitTests;

[TestFixture]
internal class SeriesTest
{
    [Test]
    public void SimpleSeriesTest()
    {
        DataSeries series = new DataSeries(TimeSpan.FromSeconds(0), 5);
        List<MeasureData> res = series.ToArray();
        Assert.That(res.Count, Is.EqualTo(5));
        foreach (var measureData in res)
        {
            Assert.That(measureData, Is.Null);
        }

        DateTime startTime = DateTime.Now;

        series.RegisterData(0, startTime);
        series.RegisterData(1, startTime+TimeSpan.FromSeconds(1));
        series.RegisterData(2, startTime+TimeSpan.FromSeconds(2));
        series.RegisterData(3, startTime+TimeSpan.FromSeconds(3));
        series.RegisterData(4, startTime+TimeSpan.FromSeconds(4));
        series.RegisterData(5, startTime+TimeSpan.FromSeconds(5));

        res = series.ToArray();
        Assert.That(res[0].Data, Is.EqualTo(0.0f));
        Assert.That(res[1].Data, Is.EqualTo(1.0f));
        Assert.That(res[2].Data, Is.EqualTo(2.0f));
        
        series.RegisterData(6, startTime+TimeSpan.FromSeconds(6));
        res = series.ToArray();
        Assert.That(res[0].Data, Is.EqualTo(1.0f));
        Assert.That(res[1].Data, Is.EqualTo(2.0f));
        Assert.That(res[2].Data, Is.EqualTo(3.0f));
    }

    [Test]
    public void TripleSecondsSeriesTest()
    {
        DataSeries series = new DataSeries(TimeSpan.FromSeconds(3), 4);
        DateTime startTime = DateTime.Now;

        series.RegisterData(0, startTime);
        series.RegisterData(1, startTime+TimeSpan.FromSeconds(1));
        series.RegisterData(2, startTime+TimeSpan.FromSeconds(2));

        series.RegisterData(3, startTime+TimeSpan.FromSeconds(3));
        series.RegisterData(4, startTime+TimeSpan.FromSeconds(4));
        series.RegisterData(5, startTime+TimeSpan.FromSeconds(5));

        List<MeasureData> res = new List<MeasureData>();
        series.ToArray(res);

        Assert.That(res[3].Data, Is.EqualTo(1.0f));
        Assert.That(res[2], Is.Null);
        Assert.That(res[1], Is.Null);
        Assert.That(res[0], Is.Null);

        series.RegisterData(6, startTime+TimeSpan.FromSeconds(6));
        series.RegisterData(7, startTime+TimeSpan.FromSeconds(7));
        series.RegisterData(8, startTime+TimeSpan.FromSeconds(8));
        series.ToArray(res);
        Assert.That(res[3].Data, Is.EqualTo(4.0f));
        Assert.That(res[2].Data, Is.EqualTo(1.0f));
        Assert.That(res[1], Is.Null);
        Assert.That(res[0], Is.Null);

        series.RegisterData(9, startTime+TimeSpan.FromSeconds(9));
        series.RegisterData(10, startTime+TimeSpan.FromSeconds(10));
        series.RegisterData(11, startTime+TimeSpan.FromSeconds(11));
        series.ToArray(res);
        Assert.That(res[3].Data, Is.EqualTo(7.0f));
        Assert.That(res[2].Data, Is.EqualTo(4.0f));
        Assert.That(res[1].Data, Is.EqualTo(1.0f));
        Assert.That(res[0], Is.Null);

    }
}