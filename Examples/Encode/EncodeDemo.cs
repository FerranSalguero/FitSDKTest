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
            course.SetName(Encoding.UTF8.GetBytes(route.metadata.name));
            course.SetSport(Sport.Cycling);

            var baseDate = route.metadata.timeSpecified ? route.metadata.time : System.DateTime.Now.AddDays(-1);

            var lap = new LapMesg();
            lap.SetTimestamp(new fit.DateTime(baseDate));
            lap.SetStartPositionLat(491829325);
            lap.SetStartPositionLong(12838824);
            lap.SetEndPositionLat(491829325);
            lap.SetEndPositionLong(12838824);

            var e = new EventMesg();
            e.SetTimestamp(new fit.DateTime(baseDate));
            e.SetEventType(EventType.Start);
            e.SetEventGroup(0);
            e.SetEvent(Event.Timer);
            e.SetData(4294967295);

            var eventStop = new EventMesg();
            eventStop.SetData(4294967295);
            eventStop.SetTimestamp(new fit.DateTime(baseDate.AddSeconds(30)));
            eventStop.SetEvent(Event.Timer);
            eventStop.SetEventType(EventType.StopDisableAll);

            var point = new RecordMesg();
            point.SetPositionLat(491829325);
            point.SetPositionLong(12838824);
            point.SetDistance(10665.65f);
            point.SetAltitude(479.4f);
            point.SetTimestamp(new fit.DateTime(baseDate));


            FileStream fitDest = new FileStream("Test.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            // Create file encode object
            Encode encodeDemo = new Encode();
            // Write our header
            encodeDemo.Open(fitDest);
            // Encode each message, a definition message is automatically generated and output if necessary
            encodeDemo.Write(fileIdMesg);
            //encodeDemo.Write(myUserProfile);

            encodeDemo.Write(course);

            encodeDemo.Write(lap);

            encodeDemo.Write(e);

            encodeDemo.Write(point);

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
