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
        //static object monitor = new object();
        //var bufferCurrentValue = new Buffer { };
        public long currentValue;

        public static void Main()
        {
            var bufferMeasPeriod = new Buffer { };
            var bufferLowTriggerLevel = new Buffer { };
            var bufferLowTriggerDuration = new Buffer { };
            var bufferCurrentValue = new Buffer { };

            //long currentValue;

            var rangeFinder = new RangeFinder { SourceBuffer1 = bufferMeasPeriod,
                                                SourceBuffer2 = bufferLowTriggerLevel,
                                                SourceBuffer3 = bufferLowTriggerDuration,
                                                SourceBuffer4 = bufferCurrentValue};
           
            var webServer = new HttpServer
            {
                StreamProvider = Parameters.StreamProvider,
                RequestRouting =
                {

                    {
                        "GET /hello",
                        HandleGetHelloHtml
                    },
                    {
                        "PUT /measurementPeriod/target",
                        new ManipulatedVariable
                        {
                            FromHttpRequest =
                                CSharpRepresentation.TryDeserializeInt,
                            ToActuator = bufferMeasPeriod.HandlePut
                        }.HandleRequest
                    },
                    {
                        "GET /measurementPeriod/target.html",
                        HandleMeasPeriodTargetHtml
                    },
                                        {
                        "PUT /lowTrigLevel/target",
                        new ManipulatedVariable
                        {
                            FromHttpRequest =
                                CSharpRepresentation.TryDeserializeInt,
                            ToActuator = bufferLowTriggerLevel.HandlePut
                        }.HandleRequest
                    },
                    {
                        "GET /lowTrigLevel/target.html",
                        HandleLowTrigLevelTargetHtml
                    },
                                        {
                        "PUT /lowTrigDuration/target",
                        new ManipulatedVariable
                        {
                            FromHttpRequest =
                                CSharpRepresentation.TryDeserializeInt,
                            ToActuator = bufferLowTriggerDuration.HandlePut
                        }.HandleRequest
                    },
                    {
                        "GET /lowTrigDuration/target.html",
                        HandleLowTrigDurationTargetHtml
                    },
                    {
                        "GET /measurement/current",
                        HandleCurrentValueQuery
                    }
                }
            };

            var rangeFinderThread = new Thread(rangeFinder.Run);
            rangeFinderThread.Start();
            webServer.Run();
        }

        static void HandleMeasPeriodTargetHtml(RequestHandlerContext context)
        {
            string requestUri =
                context.BuildRequestUri("/measurementPeriod/target");
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
                  <p>
                    <input type=""text"" value=""500"" id=""period"">
                    <input
                      type=""button"" value=""Set"" onclick=""put()""/>
                  </p>
                </body>
              </html>";
            context.SetResponse(script, "text/html");
        }

        static void HandleLowTrigLevelTargetHtml(RequestHandlerContext context)
        {
            string requestUri =
                context.BuildRequestUri("/lowTrigLevel/target");
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
                  <p>
                    <input type=""text"" value=""500"" id=""lowTrigLevel"">
                    <input
                      type=""button"" value=""Set"" onclick=""put()""/>
                  </p>
                </body>
              </html>";
            context.SetResponse(script, "text/html");
        }

        static void HandleLowTrigDurationTargetHtml(RequestHandlerContext context)
        {
            string requestUri =
                context.BuildRequestUri("/lowTrigDuration/target");
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
                  <p>
                    <input type=""text"" value=""3000"" id=""lowTrigDuration"">
                    <input
                      type=""button"" value=""Set"" onclick=""put()""/>
                  </p>
                </body>
              </html>";
            context.SetResponse(script, "text/html");
        }

        static void HandleCurrentValueQuery(RequestHandlerContext context)
        {
            Debug.Print("GET currentValue...");
            long currentValue = 999;
            string s =
                "<html>\r\n" +
                "\t<body\r\n" +
                "\t\t"+currentValue.ToString()+"\r\n" +
                "\t</body>\r\n" +
                "</html>";
            context.SetResponse(s, "text/html");
        }

        static void HandleGetHelloHtml(RequestHandlerContext context)
        {
            Debug.Print("HELLO...");
            string s =
                "<html>\r\n" +
                "\t<body\r\n" +
                "\t\tHello at " + DateTime.Now + "\r\n" +
                "\t</body>\r\n" +
                "</html>";
            context.SetResponse(s, "text/html");
        }
    }

    public class RangeFinder
    {
        public Buffer SourceBuffer1 { get; set; }
        public Buffer SourceBuffer2 { get; set; }
        public Buffer SourceBuffer3 { get; set; }
        public Buffer SourceBuffer4 { get; set; }

        static object monitor = new object();
        //long currentValue;

        public void Run()
        {
            Debug.Print("Starting RangeFinder in a thread...");
            var rangeFinderProcess = new HC_SR04(Pins.GPIO_PIN_D0, Pins.GPIO_PIN_D1);
            OutputPort blueLED = new OutputPort(Pins.ONBOARD_LED, false);
            var period = 500;
            var ledState = true;
            long pingResult;

            while (true)
            {
                lock (monitor)
                {
                    pingResult = rangeFinderProcess.Ping();
                }
                Debug.Print("Reading = " + pingResult.ToString());
                currentValue = pingResult;
                SourceBuffer4.HandlePut((object)currentValue);
                Thread.Sleep(period);
                object setpoint = SourceBuffer1.HandleGet();
                if (setpoint != null)
                {
                    period = (int)setpoint;
                    period = period > 10000 ? 10000 : period;
                    period = period < 100 ? 100 : period;
                }
                ledState = !ledState;
                blueLED.Write(ledState);
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


