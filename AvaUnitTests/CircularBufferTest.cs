using System;
using System.Collections.Generic;
using System.Text;
using AvaTest.ViewModels;

namespace AvaUnitTests;

[TestFixture]
internal class CircularBufferTest
{
    [Test]
    public void SimpleBufferTest()
    {
        Assert.That(() => CircularBuffer<int?>.Create(0), Throws.Exception);
        var buffer = CircularBuffer<int?>.Create(1);
        Assert.That(buffer, Is.Not.Null);
        Assert.That(buffer.Count, Is.EqualTo(1));

        var res = buffer.ToArray();
        foreach (var t in res)
        {
            Assert.That(t, Is.Null);
        }
        buffer.Put(1);
        res = buffer.ToArray();
        Assert.That(res[0], Is.EqualTo(1));

        buffer.Put(2);
        res = buffer.ToArray();
        Assert.That(res[0], Is.EqualTo(2));
    }

    [Test]
    public void DoubleBufferTest()
    {
        var buffer = CircularBuffer<int?>.Create(2);
        var res = buffer.ToArray();
        foreach(var r in res)
            Assert.That(r, Is.Null);

        buffer.Put(1);
        res = buffer.ToArray();
        Assert.That(res[0], Is.Null);
        Assert.That(res[1], Is.EqualTo(1));

        buffer.Put(2);
        res = buffer.ToArray();
        Assert.That(res[0], Is.EqualTo(1));
        Assert.That(res[1], Is.EqualTo(2));

        buffer.Put(3);
        res = buffer.ToArray();
        Assert.That(res[0], Is.EqualTo(2));
        Assert.That(res[1], Is.EqualTo(3));
    }
}