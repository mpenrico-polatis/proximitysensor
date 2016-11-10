using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using Configuration;
using Oberon.HttpServers;
using Oberon.HttpResources.Netmf;

namespace ProximitySensor
{
    public class Program
    {
        //These are to keep the current values of the measurements and measurement parameters such that they are in scope for all methods.
        //Not sure that the use of the Buffers is really needed. (Strictly speaking they should be used to pass the values between the two
        //threads in this application but I don't see how to get them in scope in the handler functions defined below.

        static long currentValue, previousValue;
        static int currentPeriod;
        static long currentLowTrigLevel;
        static int currentLowTrigDur;
        static int currentHoldOffTime;
        static bool belowThreshold;

        public static void Main()
        {
            var bufferMeasPeriod = new Buffer { };
            var bufferLowTriggerLevel = new Buffer { };
            var bufferLowTriggerDuration = new Buffer { };
            var bufferHoldOffTime = new Buffer { };
            var bufferCurrentValue = new Buffer { };

            Parameters.Setup(); //Call Gsiot Configuration.Parameters.Setup to set Parameters.StreamProvider to null (i.e. we don't want to use a YALER relay)

            currentPeriod = 2000;
            currentLowTrigLevel = 500L;
            currentLowTrigDur = 6000;
            currentHoldOffTime = 6000;
            belowThreshold = false;

            var rangeFinder = new RangeFinder(currentPeriod, currentLowTrigLevel, currentLowTrigDur, currentHoldOffTime)
            {
                sourceBufferMeasPeriod = bufferMeasPeriod,
                sourceBufferLowTrigLevel = bufferLowTriggerLevel,
                sourceBufferLowTrigDur = bufferLowTriggerDuration,
                sourceBufferHoldOffTime = bufferHoldOffTime,
                sourceBufferCurrentValue = bufferCurrentValue
            };

            var webServer = new HttpServer()
            {
                StreamProvider = Parameters.StreamProvider,
                RequestRouting =
                {
                    {
                        "GET /hello",
                        HandleGetHello
                    },
                    {
                        "GET /about*",
                        HandleGetAbout
                    },

                    {
                        "PUT /measurementPeriod",
                        new ManipulatedVariable
                        {
                            FromHttpRequest =
                                CSharpRepresentation.TryDeserializeInt,
                            ToActuator = bufferMeasPeriod.HandlePut
                        }.HandleRequest
                    },
                    {
                        "GET /measurementPeriod.html",
                        HandleGetMeasPeriod
                    },
                    {
                        "PUT /lowTrigLevel",
                        new ManipulatedVariable
                        {
                            FromHttpRequest =
                                CSharpRepresentation.TryDeserializeInt,
                            ToActuator = bufferLowTriggerLevel.HandlePut
                        }.HandleRequest
                    },
                    {
                        "GET /lowTrigLevel.html",
                        HandleGetLowTrigLevel
                    },
                    {
                        "PUT /lowTrigDuration",
                        new ManipulatedVariable
                        {
                            FromHttpRequest =
                                CSharpRepresentation.TryDeserializeInt,
                            ToActuator = bufferLowTriggerDuration.HandlePut
                        }.HandleRequest
                    },
                    {
                        "PUT /holdOffTime",
                        new ManipulatedVariable
                        {
                            FromHttpRequest =
                                CSharpRepresentation.TryDeserializeInt,
                            ToActuator = bufferHoldOffTime.HandlePut
                        }.HandleRequest
                    },
                    {
                        "GET /lowTrigDuration.html",
                        HandleGetLowTrigDuration
                    },
                    {
                        "GET /holdOffTime.html",
                        HandleGetHoldOffTime
                    },
                    {
                        "GET /measurement/current",
                        HandleGetCurrentValue
                    },
                    {
                        "GET /settings.html",
                        HandleGetSettings
                    }
                }
            };

            //Create and start threads
            var rangeFinderThread = new Thread(rangeFinder.Run);
            rangeFinderThread.Start();
            webServer.Run();
        }

        static void HandleGetMeasPeriod(RequestHandlerContext context)
        {
            string requestUri =
                context.BuildRequestUri("/measurementPeriod");
            var script =
                @"<html>
                    <head>
                      <script type=""text/javascript"">
                        var r;
                        try {
                          r = new XMLHttpRequest();
                        } catch (e) {
                          r = new ActiveXObject('Microsoft.XMLHTTP');
                        }
                        function put (content) {
                          r.open('PUT', '" + requestUri + @"');
                          r.setRequestHeader(""Content-Type"", ""text/plain"");
                          r.send(document.getElementById(""period"").value);
                        }
                      </script>
                    </head>
                    <body>
                      <p><h1>Michael's Proximity Sensor Black Box</h1><br /></p>
                      <p><h2>Measurement Period (ms):</h2></p>
                      <p>
                        <input type=""text"" value=""" + currentPeriod.ToString() + @""" id=""period""/>
                        <input type=""button"" value=""Set"" onclick=""put()""/>
                      </p>
                    </body>
                  </html>";
            context.SetResponse(script, "text/html");
        }

        static void HandleGetLowTrigLevel(RequestHandlerContext context)
        {
            string requestUri =
                context.BuildRequestUri("/lowTrigLevel");
            var script =
                @"<html>
                    <head>
                      <script type=""text/javascript"">
                        var r;
                        try {
                          r = new XMLHttpRequest();
                        } catch (e) {
                          r = new ActiveXObject('Microsoft.XMLHTTP');
                        }
                        function put (content) {
                          r.open('PUT', '" + requestUri + @"');
                          r.setRequestHeader(""Content-Type"", ""text/plain"");
                          r.send(document.getElementById(""lowTrigLevel"").value);
                        }
                      </script>
                    </head>
                    <body>
                      <p><h1>Michael's Proximity Sensor Black Box</h1><br /></p>
                      <p><h2>Low Trigger Level (mm):</h2></p>
                      <p>
                        <input type=""text"" value=""" + currentLowTrigLevel.ToString() + @""" id=""lowTrigLevel""/>
                        <input type=""button"" value=""Set"" onclick=""put()""/>
                      </p>
                    </body>
                  </html>";
            context.SetResponse(script, "text/html");
        }

        static void HandleGetLowTrigDuration(RequestHandlerContext context)
        {
            string requestUri =
                context.BuildRequestUri("/lowTrigDuration");
            var script =
                @"<html>
                    <head>
                      <script type=""text/javascript"">
                        var r;
                        try {
                          r = new XMLHttpRequest();
                        } catch (e) {
                          r = new ActiveXObject('Microsoft.XMLHTTP');
                        }
                        function put (content) {
                          r.open('PUT', '" + requestUri + @"');
                          r.setRequestHeader(""Content-Type"", ""text/plain"");
                          r.send(document.getElementById(""lowTrigDuration"").value);
                        }
                      </script>
                    </head>
                    <body>
                      <p><h1>Michael's Proximity Sensor Black Box</h1><br /></p>
                      <p><h2>Low trigger duration (ms):</h2></p>
                      <p>
                        <input type=""text"" value=""" + currentLowTrigDur.ToString() + @""" id=""lowTrigDuration""/>
                        <input type=""button"" value=""Set"" onclick=""put()""/>
                      </p>
                      <p>(This is the duration during which the current measurment must remain below the Low Level Trigger Threshold before proximity event is triggered.)</p>
                    </body>
                  </html>";
            context.SetResponse(script, "text/html");
        }

        static void HandleGetHoldOffTime(RequestHandlerContext context)
        {
            string requestUri =
                context.BuildRequestUri("/holdOffTime");
            var script =
                @"<html>
                    <head>
                      <script type=""text/javascript"">
                        var r;
                        try {
                          r = new XMLHttpRequest();
                        } catch (e) {
                          r = new ActiveXObject('Microsoft.XMLHTTP');
                        }
                        function put (content) {
                          r.open('PUT', '" + requestUri + @"');
                          r.setRequestHeader(""Content-Type"", ""text/plain"");
                          r.send(document.getElementById(""holdOffTime"").value);
                        }
                      </script>
                    </head>
                    <body>
                      <p><h1>Michael's Proximity Sensor Black Box</h1><br /></p>
                      <p><h2>Hold-off time (ms):</h2></p>
                      <p>
                        <input type=""text"" value=""" + currentHoldOffTime.ToString() + @""" id=""lowTrigDuration""/>
                        <input type=""button"" value=""Set"" onclick=""put()""/>
                      </p>
                      <p>(This is the duration during which the current measurment must remain below the Low Level Trigger Threshold before proximity event is triggered.)</p>
                    </body>
                  </html>";
            context.SetResponse(script, "text/html");
        }

        static void HandleGetSettings(RequestHandlerContext context)
        {
            string requestUri =
                context.BuildRequestUri("/settings");
            var script =
                @"<html>
                    <head>
                      <script type=""text/javascript"">
                        var xhttp1, xhttp2, xhttp3;
                        try {
                          xhttp1 = new XMLHttpRequest();
                          xhttp2 = new XMLHttpRequest();
                          xhttp3 = new XMLHttpRequest();
                        } catch (e) {
                          xhttp1 = new ActiveXObject('Microsoft.XMLHTTP');
                          xhttp1 = new ActiveXObject('Microsoft.XMLHTTP');
                          xhttp1 = new ActiveXObject('Microsoft.XMLHTTP');
                        }
                        function putMeasPeriod (content) {
                          var xhttp;
                          try {
                            xhttp = new XMLHttpRequest();
                          } catch (error) {
                            xhttp = new ActiveXObject('Microsoft.XMLHTTP');
                          }
                          xhttp.open('PUT', '" + context.BuildRequestUri("/measurementPeriod") + @"');
                          xhttp.setRequestHeader(""Content-Type"", ""text/plain"");
                          xhttp.send(document.getElementById(""period"").value);
                        }
                        function putLowTrigLevel (content) {
                          var xhttp;
                          try {
                            xhttp = new XMLHttpRequest();
                          } catch (error) {
                            xhttp = new ActiveXObject('Microsoft.XMLHTTP');
                          }
                          xhttp.open('PUT', '" + context.BuildRequestUri("/lowTrigLevel") + @"');
                          xhttp.setRequestHeader(""Content-Type"", ""text/plain"");
                          xhttp.send(document.getElementById(""lowTrigLevel"").value);
                        }
                        function putLowTrigDuration (content) {
                          var xhttp;
                          try {
                            xhttp = new XMLHttpRequest();
                          } catch (error) {
                            xhttp = new ActiveXObject('Microsoft.XMLHTTP');
                          }
                          xhttp.open('PUT', '" + context.BuildRequestUri("/lowTrigDuration") + @"');
                          xhttp.setRequestHeader(""Content-Type"", ""text/plain"");
                          xhttp.send(document.getElementById(""lowTrigDuration"").value);
                        }
                        function putHoldOffTime (content) {
                          var xhttp;
                          try {
                            xhttp = new XMLHttpRequest();
                          } catch (error) {
                            xhttp = new ActiveXObject('Microsoft.XMLHTTP');
                          }
                          xhttp.open('PUT', '" + context.BuildRequestUri("/holdOffTime") + @"');
                          xhttp.setRequestHeader(""Content-Type"", ""text/plain"");
                          xhttp.send(document.getElementById(""holdOffTime"").value);
                        }
                        function putAll (content) {
                          putMeasPeriod();
                          putLowTrigLevel();
                          putLowTrigDuration();
                          putHoldOffTime();
                        }
                      </script>
                    </head>
                    <body>
                      <p><h1>Michael's Proximity Sensor Black Box</h1><br /></p>
                      <p><h2>Measurement Period (ms):</h2></p>
                      <p>
                        <input type=""text"" value=""" + currentPeriod.ToString() + @""" id=""period""/>
                        <input type=""button"" value=""Set"" onclick=""putMeasPeriod()""/>
                      </p>
                      <p><h2>Low Trigger Level (mm):</h2></p>
                      <p>
                        <input type=""text"" value=""" + currentLowTrigLevel.ToString() + @""" id=""lowTrigLevel""/>
                        <input type=""button"" value=""Set"" onclick=""putLowTrigLevel()""/>
                      </p>
                      <p><h2>Low trigger duration (ms):</h2></p>
                      <p>
                        <input type=""text"" value=""" + currentLowTrigDur.ToString() + @""" id=""lowTrigDuration""/>
                        <input type=""button"" value=""Set"" onclick=""putLowTrigDuration()""/>
                      </p>
                      <p>(This is the duration during which the current measurment must remain below the Low Level Trigger Threshold before proximity event is triggered.)</p>
                      <p>
                        <input type=""button"" value=""Set All"" onclick=""putAll()""/>
                      </p>
                      <p><h2>Hold-off time (ms):</h2></p>
                      <p>
                        <input type=""text"" value=""" + currentHoldOffTime.ToString() + @""" id=""holdOffTime""/>
                        <input type=""button"" value=""Set"" onclick=""putHoldOffTime()""/>
                      </p>
                      <p>(This is the duration during which the current measurment must be above the Low Level Trigger Threshold before the proximity event is cleared.)</p>
                      <p>
                        <input type=""button"" value=""Set All"" onclick=""putAll()""/>
                      </p>
                    </body>
                  </html>";
            context.SetResponse(script, "text/html");
        }

        static void HandleGetCurrentValue(RequestHandlerContext context)
        {
            Debug.Print("GET currentValue...");
            long currentValue = 999;
            string s =
                "<html>\r\n" +
                "\t<body\r\n" +
                "\t\t" + currentValue.ToString() + "\r\n" +
                "\t</body>\r\n" +
                "</html>";
            context.SetResponse(s, "text/html");
        }

        static void HandleGetHello(RequestHandlerContext context)
        {
            Debug.Print("HELLO...");
            string s =
                "Hello (from Netduino) at " + DateTime.Now + "\r\n";
            context.SetResponse(s, "text/plain");
        }

        static void HandleGetAbout(RequestHandlerContext context)
        {
            if (context.RequestUri == "/about")
            {
                if (context.RequestMethod == "GET")
                {
                    Debug.Print("ABOUT...");
                    string s = "Michael's Proximity Sensor Box\n\n" +
                               "This is Michael's Proximity Sensor Box which is based on a Netduino Plus 3 microcontroller programmed in C# in the .NET Micro Framework\n\n" +
                               "Software version 1 (23/9/16)";
                    context.SetResponse(s, "text/plain");
                }
                else
                {
                    context.ResponseStatusCode = 405; //Method not allowed
                }
            }
            else if (context.RequestUri == "/about.html")
            {
                if (context.RequestMethod == "GET")
                {
                    Debug.Print("ABOUT...");
                    string s =
                        @"<html>
                            <body>
                              <h1>Michael's Proximity Sensor Box</h1>
                              <p><h3>This is Michael's Proximity Sensor Box which is based on a Netduino Plus 3 microcontroller
                              programmed in <strong>C#</strong> in the <strong>.NET Micro Framework</strong></h3></p>
                              <p>Software version 1 (23/9/16)</p>
                            </body>
                          </html>";
                    context.SetResponse(s, "text/html");
                }
                else
                {
                    context.ResponseStatusCode = 405; //MethodNotAllowed
                }
            }
        }

        static void HandleSettings(RequestHandlerContext context)
        {
            if (context.RequestUri == "/settings")
            {
                if (context.RequestMethod == "GET")
                {
                    Debug.Print("GET /settings...");
                    string s = "Michael's Proximity Sensor Box\n\n" +
                               "This is Michael's Proximity Sensor Box which is based on a Netduino Plus 3 microcontroller programmed in C# in the .NET Micro Framework\n\n" +
                               "Software version 1 (23/9/16)";
                    context.SetResponse(s, "text/plain");
                }
                else
                {
                    context.ResponseStatusCode = 405; //Method not allowed
                }
            }
            else if (context.RequestUri == "/settings.html")
            {
                if (context.RequestMethod == "GET")
                {
                    Debug.Print("ABOUT...");
                    string s =
                        @"<html>
                            <body>
                              <h1>Michael's Proximity Sensor Box</h1>
                              <p><h3>This is Michael's Proximity Sensor Box which is based on a Netduino Plus 3 microcontroller
                              programmed in <strong>C#</strong> in the <strong>.NET Micro Framework</strong></h3></p>
                              <p>Software version 1 (23/9/16)</p>
                            </body>
                          </html>";
                    context.SetResponse(s, "text/html");
                }
                else
                {
                    context.ResponseStatusCode = 405; //MethodNotAllowed
                }
            }
        }



        public class RangeFinder
        {
            public Buffer sourceBufferMeasPeriod { get; set; }
            public Buffer sourceBufferLowTrigLevel { get; set; }
            public Buffer sourceBufferLowTrigDur { get; set; }
            public Buffer sourceBufferHoldOffTime { get; set; }
            public Buffer sourceBufferCurrentValue { get; set; }

            static object monitor = new object();
            //long currentValue;
            int period = 500;
            int lowTrigDur = 2000;
            int holdOffTime = 2000;
            long lowTrigLevel = 500L;
            //private static TimerCallback proximityAlarmTimerCallback = null;
            //private static TimerCallback proximityAlarmClearHoldOffTimerCallback = null;
            Timer belowThresholdTimer = null;
            Timer proximityAlarmClearHoldOffTimer = null;
            private enum TimerState {belowThreshold, holdingOff};
            bool belowThresholdTimerActive = false;
            bool belowThresholdAlarmRaised = false;
            bool proximityAlarmClearHoldOffTimerActive = false;


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="period">Initial period (in ms) between measurements (pings)</param>
            /// <param name="lowTrigLevel">Low threshold level (in mm) to trigger proximity alarm</param>
            /// <param name="lowTrigDur">Time (in ms) during which ping results are less than <em>lowTrigLevel</em> to trigger proximity alarm</param>
            /// <param name="holdOffTime">Time (in ms) during which ping results must be greater than <em>lowTrigLevel</em> to clear proximity alarm</param>

            public RangeFinder(int period, long lowTrigLevel, int lowTrigDur, int holdOffTime)
            {
                this.period = period;
                //sourceBufferMeasPeriod.HandlePut((object)period);
                this.lowTrigLevel = lowTrigLevel;
                //sourceBufferLowTrigLevel.HandlePut((object)lowTrigLevel);
                this.lowTrigDur = lowTrigDur;
                //sourceBufferLowTrigDur.HandlePut((object)lowTrigDur);
                this.holdOffTime = holdOffTime;
                currentValue = 0;
            }

            private void ManageProximityAlarm(object state)
            {
                Debug.Print("Timer fired, ManageProximityAlarm called");
                //if (belowThresholdAlarmRaised == false &&  belowThreshold == true)
                if ((TimerState)state == TimerState.belowThreshold)
                {
                    Debug.Print("Proximity Alarm RAISED");
                    belowThresholdAlarmRaised = true;
                    belowThresholdTimer.Dispose();
                    belowThresholdTimerActive = false;
                    Debug.Print("belowThresholdTimer Cancelled (after firing)");
                    // Do more stuff here like send message to Xively, REST command to an external app, etc
                }
                //if (belowThresholdAlarmRaised == true && belowThreshold == false)
                if ((TimerState)state == TimerState.holdingOff)
                    {
                    Debug.Print("Proximity Alarm CLEARED");
                    belowThresholdAlarmRaised = false;
                    proximityAlarmClearHoldOffTimer.Dispose();
                    proximityAlarmClearHoldOffTimerActive = false;
                    Debug.Print("proximityAlarmClearHoldOffTimer Cancelled (after firing)");
                    // Do more stuff here too...
                }
            }

            public void Run()
            {
                Debug.Print("Starting RangeFinder in a separate thread...");
                var rangeFinderProcess = new HC_SR04(Pins.GPIO_PIN_D0, Pins.GPIO_PIN_D1);
                OutputPort blueLED = new OutputPort(Pins.ONBOARD_LED, false);
                OutputPort greenLED = new OutputPort(Pins.GPIO_PIN_D13, false);
                TimerCallback proximityAlarmTimerCallback = new TimerCallback(ManageProximityAlarm);
                TimerCallback proximityAlarmClearHoldOffTimerCallback = new TimerCallback(ManageProximityAlarm);
                //Timer belowThresholdTimer = null;
                //Timer proximityAlarmClearHoldOffTimer = null;
                int belowThresholdCrossingCounter = 0;
                bool ledState = true;
                long pingResult;
                object setpoint;

                while (true)
                {
                    belowThreshold = false;
                    lock (monitor)
                    {
                        pingResult = rangeFinderProcess.Ping();
                    }
                    Debug.Print("Reading = " + pingResult.ToString());
                    previousValue = currentValue;
                    currentValue = pingResult;
                    sourceBufferCurrentValue.HandlePut((object)currentValue);
                    if (currentValue < currentLowTrigLevel)
                    {
                        Debug.Print("BELOW THRESHOLD!");
                        belowThreshold = true;
                        if (previousValue > currentLowTrigLevel && belowThresholdTimerActive == false)
                        {
                            TimerState belowThresholdState = TimerState.belowThreshold;
                            belowThresholdTimer = new Timer(proximityAlarmTimerCallback, belowThresholdState, lowTrigDur, (int) 1.5*lowTrigDur);
                            belowThresholdTimerActive = true;
                            belowThresholdCrossingCounter+=1;
                            Debug.Print("Low Level Threshold Crossed (count=" + belowThresholdCrossingCounter.ToString() + ")");
                            Debug.Print("belowThresholdTimer Started (fires in " + lowTrigDur.ToString() + "ms)");
                        }
                        if (proximityAlarmClearHoldOffTimerActive == true)
                        {
                            proximityAlarmClearHoldOffTimer.Dispose();
                            proximityAlarmClearHoldOffTimerActive = false;
                            Debug.Print("proximityAlarmClearHoldOffTimer Cancelled");
                        }

                    }
                    else
                    {
                        belowThreshold = false;
                        if (previousValue < currentLowTrigLevel && belowThresholdTimerActive == true)
                        {
                            belowThresholdTimer.Dispose();
                            belowThresholdTimerActive = false;
                            Debug.Print("belowThresholdTimer Cancelled");
                        }
                        if (previousValue < currentLowTrigLevel && proximityAlarmClearHoldOffTimerActive == false && belowThresholdCrossingCounter > 0)
                        {
                            proximityAlarmClearHoldOffTimerActive = true;
                            TimerState holdingOffState = TimerState.holdingOff;
                            proximityAlarmClearHoldOffTimer = new Timer(proximityAlarmClearHoldOffTimerCallback, holdingOffState, holdOffTime, (int) 1.5*holdOffTime);
                            Debug.Print("proximityAlarmClearHoldOffTimer Started (fires in " + holdOffTime.ToString() + "ms)");
                        }
                    }
                    Thread.Sleep(period);
                    setpoint = sourceBufferMeasPeriod.HandleGet();
                    if (setpoint != null)
                    {
                        period = (int)setpoint;
                        period = period > 10000 ? 10000 : period;
                        period = period < 100 ? 100 : period;
                        if (period != currentPeriod)
                        {
                            Debug.Print("Period changed to " + period.ToString() + "ms");
                        }
                        currentPeriod = period;
                    }
                    
                    setpoint = sourceBufferLowTrigLevel.HandleGet();
                    if (setpoint != null)
                    {
                        int i = (int)setpoint;
                        lowTrigLevel = (long)i;
                        lowTrigLevel = lowTrigLevel > 3000L ? 3000L : lowTrigLevel;
                        lowTrigLevel = lowTrigLevel < 100L ? 100L : lowTrigLevel;
                        if (lowTrigLevel != currentLowTrigLevel)
                        {
                            Debug.Print("Low Trigger Level changed to " + lowTrigLevel.ToString() + "mm");
                        }
                        currentLowTrigLevel = lowTrigLevel;
                    }

                    setpoint = sourceBufferLowTrigDur.HandleGet();
                    if (setpoint != null)
                    {
                        lowTrigDur = (int)setpoint;
                        lowTrigDur = lowTrigDur > 10000 ? 10000 : lowTrigDur;
                        lowTrigDur = lowTrigDur < 1000 ? 1000 : lowTrigDur;
                        if (lowTrigDur != currentLowTrigDur)
                        {
                            Debug.Print("Low Trigger Duration changed to " + lowTrigDur.ToString() + "ms");
                        }
                        currentLowTrigDur = lowTrigDur;
                    }

                    ledState = !ledState;
                    blueLED.Write(ledState);
                    greenLED.Write(ledState);
                }

            }
        }

        public class HC_SR04
        {
            private OutputPort portOut;
            private InterruptPort interIn;
            private long beginTick;
            private long endTick;
            private long minTicks = 0;  // System latency, subtracted off ticks to find actual sound travel time

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="pinTrig">Netduino pin connected to the HC-SR04 Trig pin</param>
            /// <param name="pinEcho">Netduino pin connected to the HC-SR04 Echo pin</param>
            public HC_SR04(Cpu.Pin pinTrig, Cpu.Pin pinEcho)
            {
                portOut = new OutputPort(pinTrig, false);
                interIn = new InterruptPort(pinEcho, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
                interIn.OnInterrupt += new NativeEventHandler(interIn_OnInterrupt);
                minTicks = 4000L;

            }

            /// <summary>
            /// Trigger a sensor reading
            /// 
            /// </summary>
            /// <returns>Number of mm to the object</returns>
            public long Ping()
            {
                // Reset Sensor
                portOut.Write(true);
                Thread.Sleep(1);

                // Start Clock
                endTick = 0L;
                beginTick = System.DateTime.Now.Ticks;
                // Trigger Sonic Pulse
                portOut.Write(false);

                // Wait 1/20 second (this could be set as a variable instead of constant)
                Thread.Sleep(50);

                if (endTick > 0L)
                {
                    // Calculate Difference
                    long elapsed = endTick - beginTick;

                    // Subtract out fixed overhead (interrupt lag, etc.)
                    elapsed -= minTicks;
                    if (elapsed < 0L)
                    {
                        elapsed = 0L;
                    }

                    // Return elapsed ticks
                    return elapsed * 10 / 636;
                    ;
                }

                // Sonic pulse wasn't detected within 1/20 second
                return -1L;
            }

            /// <summary>
            /// This interrupt will trigger when detector receives back reflected sonic pulse       
            /// </summary>
            /// <param name="data1">Not used</param>
            /// <param name="data2">Not used</param>
            /// <param name="time">Transfer to endTick to calculated sound pulse travel time</param>
            void interIn_OnInterrupt(uint data1, uint data2, DateTime time)
            {
                // Save the ticks when pulse was received back
                endTick = time.Ticks;
            }

        }
    }
}




