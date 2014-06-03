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
using NLog;


namespace DecodeDemo
{
    class Program
    {
        static Dictionary<ushort, int> mesgCounts = new Dictionary<ushort, int>();
        static FileStream fitSource;
        static Logger logger = LogManager.GetCurrentClassLogger();

        public Encode Encoder { get; set; }


        static void Main(string[] args)
        {
            new Program().Foo();
        }

        public void Foo()
        {
            using (var fitDest = new FileStream("Test2.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {

                Encoder = new Encode();
                // Write our header
                Encoder.Open(fitDest);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                logger.Trace("FIT Decode Example Application");

                //if (args.Length != 1)
                //{
                //   logger.Trace("Usage: decode.exe <filename>");
                //   return;
                //}            

                // Attempt to open .FIT file
                var fileName = "Test.fit";
                fitSource = new FileStream(fileName, FileMode.Open);
                logger.Trace("Opening {0}", fileName);

                Decode decodeDemo = new Decode();
                MesgBroadcaster mesgBroadcaster = new MesgBroadcaster();

                // Connect the Broadcaster to our event (message) source (in this case the Decoder)
                decodeDemo.MesgEvent += mesgBroadcaster.OnMesg;
                decodeDemo.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;

                // Subscribe to message events of interest by connecting to the Broadcaster
                mesgBroadcaster.MesgEvent += new MesgEventHandler(OnMesg);
                mesgBroadcaster.MesgDefinitionEvent += new MesgDefinitionEventHandler(OnMesgDefn);

                mesgBroadcaster.FileIdMesgEvent += new MesgEventHandler(OnFileIDMesg);
                mesgBroadcaster.UserProfileMesgEvent += new MesgEventHandler(OnUserProfileMesg);

                bool status = decodeDemo.IsFIT(fitSource);
                status &= decodeDemo.CheckIntegrity(fitSource);
                // Process the file
                if (status == true)
                {
                    logger.Trace("Decoding...");
                    decodeDemo.Read(fitSource);
                    logger.Trace("Decoded FIT file {0}", fileName);
                }
                else
                {
                    try
                    {
                        logger.Trace("Integrity Check Failed {0}", fileName);
                        logger.Trace("Attempting to decode...");
                        decodeDemo.Read(fitSource);
                    }
                    catch (FitException ex)
                    {
                        logger.Trace("DecodeDemo caught FitException: " + ex.Message);
                    }
                }
                fitSource.Close();

                logger.Trace("");
                logger.Trace("Summary:");
                int totalMesgs = 0;
                foreach (KeyValuePair<ushort, int> pair in mesgCounts)
                {
                    logger.Trace("MesgID {0,3} Count {1}", pair.Key, pair.Value);
                    totalMesgs += pair.Value;
                }

                logger.Trace("{0} Message Types {1} Total Messages", mesgCounts.Count, totalMesgs);

                stopwatch.Stop();
                logger.Trace("");
                logger.Trace("Time elapsed: {0:0.#}s", stopwatch.Elapsed.TotalSeconds);
                
            }

            Console.ReadKey();
        }

        #region Message Handlers
        // Client implements their handlers of interest and subscribes to MesgBroadcaster events
        static void OnMesgDefn(object sender, MesgDefinitionEventArgs e)
        {
            logger.Trace("OnMesgDef: Received Defn for local message #{0}, global num {1}", e.mesgDef.LocalMesgNum, e.mesgDef.GlobalMesgNum);
            logger.Trace("\tIt has {0} fields and is {1} bytes long", e.mesgDef.NumFields, e.mesgDef.GetMesgSize());
        }

        void OnMesg(object sender, MesgEventArgs e)
        {
            logger.Trace("OnMesg: Received Mesg with global ID#{0}, its name is {1}", e.mesg.Num, e.mesg.Name);

            var msg = new Mesg(e.mesg.Name, e.mesg.Num);

            for (byte i = 0; i < e.mesg.GetNumFields(); i++)
            {
                for (int j = 0; j < e.mesg.fields[i].GetNumValues(); j++)
                {
                    logger.Trace("\tField{0} Index{1} (\"{2}\" Field#{4}) Value: {3} (raw value {5})", i, j, e.mesg.fields[i].GetName(), e.mesg.fields[i].GetValue(j), e.mesg.fields[i].Num, e.mesg.fields[i].GetRawValue(j));
                    var field = e.mesg.fields[i];
                    msg.SetField(new Field(field.Name, field.Num, field.Type, field.Scale, field.Offset, field.Units));
                }
            }

            Encoder.Write(msg);

            if (mesgCounts.ContainsKey(e.mesg.Num) == true)
            {
                mesgCounts[e.mesg.Num]++;
            }
            else
            {
                mesgCounts.Add(e.mesg.Num, 1);
            }
        }

        void OnFileIDMesg(object sender, MesgEventArgs e)
        {
            logger.Trace("FileIdHandler: Received {1} Mesg with global ID#{0}", e.mesg.Num, e.mesg.Name);
            FileIdMesg myFileId = (FileIdMesg) e.mesg;
            try
            {
                logger.Trace("\tType: {0}", myFileId.GetType());
                logger.Trace("\tManufacturer: {0}", myFileId.GetManufacturer());
                logger.Trace("\tProduct: {0}", myFileId.GetProduct());
                logger.Trace("\tSerialNumber {0}", myFileId.GetSerialNumber());
                logger.Trace("\tNumber {0}", myFileId.GetNumber());
                Dynastream.Fit.DateTime dtTime = new Dynastream.Fit.DateTime(myFileId.GetTimeCreated().GetTimeStamp());

            }
            catch (FitException exception)
            {
                logger.Trace("\tOnFileIDMesg Error {0}", exception.Message);
                logger.Trace("\t{0}", exception.InnerException);
            }
        }

        void OnUserProfileMesg(object sender, MesgEventArgs e)
        {
            logger.Trace("UserProfileHandler: Received {1} Mesg, it has global ID#{0}", e.mesg.Num, e.mesg.Name);
            UserProfileMesg myUserProfile = (UserProfileMesg) e.mesg;
            try
            {
                logger.Trace("\tFriendlyName \"{0}\"", Encoding.UTF8.GetString(myUserProfile.GetFriendlyName()));
                logger.Trace("\tGender {0}", myUserProfile.GetGender().ToString());
                logger.Trace("\tAge {0}", myUserProfile.GetAge());
                logger.Trace("\tWeight  {0}", myUserProfile.GetWeight());
            }
            catch (FitException exception)
            {
                logger.Trace("\tOnUserProfileMesg Error {0}", exception.Message);
                logger.Trace("\t{0}", exception.InnerException);
            }
        }
        #endregion
    }
}
