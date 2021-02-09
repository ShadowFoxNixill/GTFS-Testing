using System.Linq;
using System;
using GTFS;
using GTFS.Entities;

namespace Nixill.GTFSTesting
{
  public class Test
  {
    static void Main(string[] args)
    {
      foreach (int i in Enumerable.Range(1, 127))
      {
        Console.WriteLine("\"" + DayMasks.Get(i) + "\",");
      }
    }
  }
}