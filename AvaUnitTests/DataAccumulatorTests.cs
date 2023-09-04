using AvaTest.ViewModels;

namespace AvaUnitTests;

public class DataAccumulatorTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void SimpleDataAccumulatorTest()
    {
        DataAccumulator accumulator = new DataAccumulator(TimeSpan.FromSeconds(0));

        MeasureData? res = accumulator.RegisterData(0.0f);
        Assert.IsNotNull(res);
        Assert.That(res.Data, Is.EqualTo(0.0f));

        res = accumulator.RegisterData(1.0f);
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Data, Is.EqualTo(0.0f));

        res = accumulator.RegisterData(2.0f);
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Data, Is.EqualTo(1.0f));
        
        Assert.Pass();
    }

    [Test]
    public void ShortTimedTest()
    {
        DateTime nowTime = DateTime.Now;
        DataAccumulator dataAccumulator = new DataAccumulator(TimeSpan.FromSeconds(5));
        MeasureData? res = dataAccumulator.RegisterData(0.0f, nowTime + TimeSpan.FromMilliseconds(100));
        Assert.That(res, Is.Null);
        res = dataAccumulator.RegisterData(1.0f, nowTime + TimeSpan.FromSeconds(1));
        Assert.That(res, Is.Null);
        res = dataAccumulator.RegisterData(2.0f, nowTime + TimeSpan.FromSeconds(2));
        Assert.That(res, Is.Null);
        res = dataAccumulator.RegisterData(3.0f, nowTime + TimeSpan.FromSeconds(6));
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Data, Is.EqualTo(1.0f));
        Assert.That(res.Time, Is.EqualTo(nowTime));

        DateTime checkpoint = nowTime + TimeSpan.FromSeconds(6);
        
        res = dataAccumulator.RegisterData(3.0f, checkpoint);
        Assert.That(res, Is.Null);
        res = dataAccumulator.RegisterData(3.0f, nowTime + TimeSpan.FromSeconds(9));
        Assert.That(res, Is.Null);
        res = dataAccumulator.RegisterData(4.0f, nowTime + TimeSpan.FromSeconds(11));
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Data, Is.EqualTo(3.0f));
        Assert.That(res.Time, Is.EqualTo(checkpoint));
    }
}