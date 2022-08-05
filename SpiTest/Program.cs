// See https://aka.ms/new-console-template for more information

using Unosquare.RaspberryIO.Abstractions;

Console.WriteLine("Hello, World!");

class Max6675Reader
{
    private BcmPin my_sck;
    private BcmPin my_so;
    private BcmPin my_cs;

    public Max6675Reader(BcmPin sck, BcmPin cs, BcmPin so)
    {
        my_sck = sck;
        my_so = so;
        my_cs = cs;
    }
    
    
}