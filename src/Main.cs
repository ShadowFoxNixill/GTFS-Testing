using System;
using System.IO;
using System.Linq;
using GTFS;
using GTFS.Entities;

namespace Nixill.GTFSTesting
{
  class Program
  {
    static void Bounds()
    {
      var reader = new GTFSReader<GTFSFeed>();
      var feed = reader.Read("gtfs/ddot_gtfs.zip");

      foreach (DayOfWeek day in (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)))
      {
        using StreamWriter file = new StreamWriter("output/" + day + ".txt");
        var listings =
          from routes in feed.Routes
          join trips in feed.Trips on routes.Id equals trips.RouteId
          join stop_times in feed.StopTimes on trips.Id equals stop_times.TripId
          join calendar in feed.Calendars on trips.ServiceId equals calendar.ServiceId
          where calendar[day]
          group new
          {
            ArrTime = stop_times.ArrivalTime,
            DepTime = stop_times.DepartureTime,
            RtSName = routes.ShortName,
            RtLName = routes.LongName,
            DestSgn = trips.Headsign
          } by new
          {
            trips.RouteId,
            trips.Direction
          } into result
          select new
          {
            StartTime = result.Min(x => x.ArrTime),
            EndTime = result.Max(x => x.DepTime),
            ShortName = result.First().RtSName,
            LongName = result.First().RtLName,
            Headsign = result.First().DestSgn
          };

        foreach (var listing in listings)
        {
          file.WriteLine($"Route {listing.ShortName} {listing.LongName} ({listing.Headsign}) starts at {listing.StartTime} and ends at {listing.EndTime}.");
        }
      }
    }
  }
}
