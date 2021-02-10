using System.Collections.Generic;
using GTFS;
using GTFS.Entities;
using System.Linq;
using GTFS.Entities.Enumerations;
using System;

namespace Nixill.GTFS
{
  public static class Functions
  {
    public static List<Stop> GetStopOrder(GTFSFeed feed, string route, DirectionType? direction = null)
    {
      var routeStopTimes =
        from stopTimes in feed.StopTimes
        join trips in feed.Trips on stopTimes.TripId equals trips.Id
        where trips.RouteId == route
          && trips.Direction == direction
        select stopTimes;

      // If it's empty, return empty list. Though that shouldn't be possible in well-formed GTFS.
      if (routeStopTimes.Count() == 0)
      {
        return new List<Stop>();
      }

      // First, we'll need a listing of every trip, how many stops it has, and how many of those we've already listed.
      var stopsOnTrips =
        from rstListingUngrouped in routeStopTimes
        group rstListingUngrouped by rstListingUngrouped.TripId into rstListingGrouped
        let stopCount = rstListingGrouped.Count()
        orderby stopCount descending
        select new
        {
          rstListingGrouped.First().TripId,
          Stops = stopCount,
          FoundStops = 0
        };

      List<string> outStops = new List<string>();

      while (stopsOnTrips.Count() > 0)
      {
        // First take the first trip with "unlisted" stops
        string newTrip = stopsOnTrips.First().TripId;

        var newStops =
          (from rstListing in routeStopTimes
           where rstListing.TripId == newTrip
           orderby rstListing.StopSequence
           select rstListing.StopId).Distinct();

        // Handling for first trip searched
        if (outStops.Count == 0)
        {
          outStops = newStops.Distinct().ToList();
        }
        else
        {
          // Now merge the listings
          List<string> foundStops = new List<string>(outStops);
          List<string> addingStops = new List<string>();
          string lastFoundStop = null;

          foreach (string stop in newStops)
          {
            int foundIndex = foundStops.IndexOf(stop);
            if (foundIndex != -1)
            {
              // Remove the found stop and all preceding stops from the found-stops list
              foundStops.RemoveRange(0, foundIndex + 1);

              // If there were unfound stops, add them to the output too
              if (addingStops.Count > 0)
              {
                int outIndex = outStops.IndexOf(stop);
                outStops.InsertRange(outIndex, addingStops);
                addingStops.Clear();
              }

              // Also, set the last found stop to this one
              lastFoundStop = stop;
            }
            else
            {
              addingStops.Add(stop);
            }
          }

          // Add stray stops just after the last found stop
          if (lastFoundStop != null)
          {
            outStops.InsertRange(outStops.IndexOf(lastFoundStop) + 1, addingStops);
          }
          else
          {
            // Or on the end if the two segments are, somehow, not linked
            outStops.AddRange(addingStops);
          }

          // Make sure outStops only contains items once
          outStops = outStops.Distinct().ToList();
        }

        // Now we need to update stopsOnTrips
        stopsOnTrips =
          from rstListingUngrouped in routeStopTimes
          group rstListingUngrouped by rstListingUngrouped.TripId into rstListingGrouped
          let stopCount = rstListingGrouped.Count()
          let foundStopCount = rstListingGrouped.Count(x => outStops.Contains(x.StopId))
          where foundStopCount < stopCount
          orderby Math.Sign(stopCount) descending, stopCount - foundStopCount descending, stopCount
          select new
          {
            rstListingGrouped.First().TripId,
            Stops = rstListingGrouped.Count(),
            FoundStops = rstListingGrouped.Count(x => outStops.Contains(x.StopId))
          };
      }

      // One last thing: Convert stop IDs to actual Stops
      return
        (from stopIds in outStops
         join stops in feed.Stops on stopIds equals stops.Id
         select stops).ToList();
    }

    public static List<Stop> GetStopOrder(GTFSFeed feed, Route route, DirectionType? direction = null) => GetStopOrder(feed, route.Id, direction);
  }
}