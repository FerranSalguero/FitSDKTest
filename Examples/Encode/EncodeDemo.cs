#region Copyright
////////////////////////////////////////////////////////////////////////////////
// The following FIT Protocol software provided may be used with FIT protocol
// devices only and remains the copyrighted property of Dynastream Innovations Inc.
// The software is being provided on an "as-is" basis and as an accommodation,
// and therefore all warranties, representations, or guarantees of any kind
// (whether express, implied or statutory) including, without limitation,
// warranties of merchantability, non-infringement, or fitness for a particular
// purpose, are specifically disclaimed.
//
// Copyright 2012 Dynastream Innovations Inc.
////////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using Dynastream.Fit;
using fit = Dynastream.Fit;
using SharpGpx;
using System.Linq;

namespace EncodeDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Generate some FIT messages
            FileIdMesg fileIdMesg = new FileIdMesg();
            fileIdMesg.SetManufacturer(Manufacturer.Dynastream);  // Types defined in the profile are available
            fileIdMesg.SetProduct(1000);
            fileIdMesg.SetSerialNumber(12345);

            //UserProfileMesg myUserProfile = new UserProfileMesg();
            //myUserProfile.SetGender(Gender.Female);
            //float myWeight = 63.1F;
            //myUserProfile.SetWeight(myWeight);
            //myUserProfile.SetAge(99);
            //myUserProfile.SetFriendlyName(Encoding.UTF8.GetBytes("TestUser"));

            var route = GpxClass.FromFile("route.gpx");

            CourseMesg course = new CourseMesg();
            course.SetName(Encoding.UTF8.GetBytes("route from gpx"));
            course.SetSport(Sport.Cycling);

            var baseDate = route.metadata.timeSpecified ? route.metadata.time : System.DateTime.Now.AddDays(-1);

            FileStream fitDest = new FileStream("Test.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            // Create file encode object
            Encode encodeDemo = new Encode();
            // Write our header
            encodeDemo.Open(fitDest);
            // Encode each message, a definition message is automatically generated and output if necessary
            encodeDemo.Write(fileIdMesg);
            //encodeDemo.Write(myUserProfile);

            encodeDemo.Write(course);

            var lap = new LapMesg();
            encodeDemo.Write(lap);


            var firstTrk = route.trk.First();

            var firstTrkSeg = firstTrk.trkseg.First();

            var firstPoint = firstTrkSeg.trkpt.First();
            lap.SetTimestamp(new fit.DateTime(firstPoint.time));
            lap.SetStartPositionLat(firstPoint.lat.RawInt());
            lap.SetStartPositionLong(firstPoint.lon.RawInt());

            var lastPoint = firstTrkSeg.trkpt.Last();
            lap.SetEndPositionLat(lastPoint.lat.RawInt());
            lap.SetEndPositionLong(lastPoint.lon.RawInt());

            var e = new EventMesg();
            e.SetTimestamp(new fit.DateTime(firstPoint.time));
            e.SetEventType(EventType.Start);
            e.SetEventGroup(0);
            e.SetEvent(Event.Timer);
            e.SetData(null);
            encodeDemo.Write(e);

            foreach (var point in firstTrkSeg.trkpt)
            {
                var p = new RecordMesg();
                p.SetPositionLat(point.lat.RawInt());
                p.SetPositionLong(point.lon.RawInt());
                //p.SetDistance(point. 10665.65f);
                p.SetAltitude(Convert.ToSingle(point.ele));
                p.SetTimestamp(new fit.DateTime(baseDate));

                encodeDemo.Write(p);
            }

            var eventStop = new EventMesg();
            eventStop.SetData(null);
            eventStop.SetTimestamp(new fit.DateTime(lastPoint.time));
            eventStop.SetEvent(Event.Timer);
            eventStop.SetEventType(EventType.StopDisableAll);
            encodeDemo.Write(eventStop);

            // Update header datasize and file CRC
            encodeDemo.Close();

            fitDest.Close();

            Console.WriteLine("Encoded FIT file test.fit");
            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0:0.#}s", stopwatch.Elapsed.TotalSeconds);

            Console.ReadKey();
        }
    }
}
