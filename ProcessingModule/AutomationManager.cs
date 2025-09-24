using Common;
using System;
using System.Threading;

namespace ProcessingModule
{
    public class AutomationManager : IAutomationManager, IDisposable
    {
        private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
        private IProcessingManager processingManager;
        private int delayBetweenCommands;
        private IConfiguration configuration;

        private IStateUpdater stateUpdater;
        private AlarmProcessor alarmProcessor = new AlarmProcessor();
        private EGUConverter eguConverter = new EGUConverter();

        private bool disposedValue = false;

        public AutomationManager(
            IStorage storage,
            IProcessingManager processingManager,
            AutoResetEvent automationTrigger,
            IConfiguration configuration)
        {
            this.storage = storage;
            this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
            this.stateUpdater = storage as IStateUpdater;
        }

        private void InitializeAndStartThreads()
        {
            automationWorker = new Thread(AutomationWorker_DoWork);
            automationWorker.Name = "Automation Thread";
            automationWorker.Start();
        }

        private void AutomationWorker_DoWork()
        {
            bool fireActive = false;


            var levelPoints = storage.GetPoints(new System.Collections.Generic.List<PointIdentifier>
            {
                new PointIdentifier(PointType.ANALOG_OUTPUT, 1000)
            });
            var tempPoints = storage.GetPoints(new System.Collections.Generic.List<PointIdentifier>
            {
                new PointIdentifier(PointType.ANALOG_OUTPUT, 1001)
            });

            var levelPoint = levelPoints.Count > 0 ? levelPoints[0] as IAnalogPoint : null;
            var tempPoint = tempPoints.Count > 0 ? tempPoints[0] as IAnalogPoint : null;

            double currentLevel = levelPoint != null ? levelPoint.ConfigItem.DefaultValue : 650.0;
            double currentTemperature = tempPoint != null ? tempPoint.ConfigItem.DefaultValue : 22.0;


            ResetDigitalOutput(2000);
            ResetDigitalOutput(2002);

            while (!disposedValue)
            {
                try
                {
                    var valvePoints = storage.GetPoints(new System.Collections.Generic.List<PointIdentifier>
                    {
                        new PointIdentifier(PointType.DIGITAL_OUTPUT, 2000)
                    });
                    var heaterPoints = storage.GetPoints(new System.Collections.Generic.List<PointIdentifier>
                    {
                        new PointIdentifier(PointType.DIGITAL_OUTPUT, 2002)
                    });

                    var valvePoint = valvePoints.Count > 0 ? valvePoints[0] as IDigitalPoint : null;
                    var heaterPoint = heaterPoints.Count > 0 ? heaterPoints[0] as IDigitalPoint : null;

                    bool valveOpen = valvePoint != null && valvePoint.State == DState.ON;
                    bool heaterOn = heaterPoint != null && heaterPoint.State == DState.ON;


                    if (heaterOn && !fireActive)
                    {
                        if (currentTemperature < 30) currentTemperature += 2.0;
                        else if (currentTemperature <= 50) currentTemperature += 5.0;
                        else currentTemperature += 20.0;
                    }


                    if (currentTemperature >= 57.0)
                    {
                        fireActive = true;
                        if (valvePoint != null)
                            UpdateDigitalOutput(valvePoint, 1);
                        if (heaterPoint != null)
                            UpdateDigitalOutput(heaterPoint, 0);

                        stateUpdater?.LogMessage("🔥 Požar detektovan! Automatsko gašenje aktivirano.");
                    }

                    if (fireActive && currentTemperature < 40.0)
                    {
                        fireActive = false;
                        stateUpdater?.LogMessage("✅ Sistem se ohladio, požar resetovan.");
                    }

                    if (fireActive && valveOpen)
                    {
                        if (currentLevel > 0)
                        {
                            double waterUsed = Math.Min(10.0, currentLevel);
                            double cooling = (waterUsed / 10.0) * 4.0;

                            currentTemperature -= cooling;
                            currentLevel -= waterUsed;
                        }
                    }


                    if (!fireActive && valveOpen)
                    {
                        currentLevel += 2.0;
                        if (levelPoint != null && currentLevel > levelPoint.ConfigItem.EGU_Max)
                            currentLevel = levelPoint.ConfigItem.EGU_Max;
                    }


                    if (tempPoint != null)
                        UpdateAnalogPoint(tempPoint, currentTemperature);


                    if (levelPoint != null)
                        UpdateAnalogPoint(levelPoint, currentLevel);
                }
                catch (Exception ex)
                {
                    stateUpdater?.LogMessage($"[AutomationManager] Greška: {ex.Message}");
                }

                Thread.Sleep(delayBetweenCommands);
            }
        }

        private void UpdateDigitalOutput(IDigitalPoint point, int newValue)
        {
            point.State = (DState)newValue;
            point.RawValue = (ushort)newValue;
            point.Timestamp = DateTime.Now;
            point.Alarm = alarmProcessor.GetAlarmForDigitalPoint((ushort)newValue, point.ConfigItem);

            processingManager.ExecuteWriteCommand(
                point.ConfigItem,
                configuration.GetTransactionId(),
                configuration.UnitAddress,
                point.ConfigItem.StartAddress,
                newValue
            );
        }

        private void UpdateAnalogPoint(IAnalogPoint point, double eguValue)
        {
            if (eguValue > point.ConfigItem.EGU_Max) eguValue = point.ConfigItem.EGU_Max;
            if (eguValue < point.ConfigItem.EGU_Min) eguValue = point.ConfigItem.EGU_Min;

            ushort raw = eguConverter.ConvertToRaw(point.ConfigItem.ScaleFactor, point.ConfigItem.Deviation, eguValue);
            point.RawValue = raw;
            point.EguValue = eguValue;
            point.Timestamp = DateTime.Now;
            point.Alarm = alarmProcessor.GetAlarmForAnalogPoint(eguValue, point.ConfigItem);

            processingManager.ExecuteWriteCommand(
                point.ConfigItem,
                configuration.GetTransactionId(),
                configuration.UnitAddress,
                point.ConfigItem.StartAddress,
                raw
            );
        }

        private void ResetDigitalOutput(ushort address)
        {
            var points = storage.GetPoints(new System.Collections.Generic.List<PointIdentifier>
            {
                new PointIdentifier(PointType.DIGITAL_OUTPUT, address)
            });
            if (points.Count > 0)
            {
                var dp = points[0] as IDigitalPoint;
                if (dp != null)
                {
                    UpdateDigitalOutput(dp, 0);
                }
            }
        }

        public void Start(int delayBetweenCommands)
        {
            this.delayBetweenCommands = delayBetweenCommands * 1000;
            InitializeAndStartThreads();
        }

        public void Stop()
        {
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}