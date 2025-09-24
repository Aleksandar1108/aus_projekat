using Common;
using System;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for periodic polling.
    /// </summary>
    public class Acquisitor : IDisposable
    {
        private AutoResetEvent acquisitionTrigger;
        private IProcessingManager processingManager;
        private Thread acquisitionWorker;
        private IStateUpdater stateUpdater;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Acquisitor"/> class.
        /// </summary>
        /// <param name="acquisitionTrigger">The acquisition trigger.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="stateUpdater">The state updater.</param>
        /// <param name="configuration">The configuration.</param>
        public Acquisitor(AutoResetEvent acquisitionTrigger, IProcessingManager processingManager, IStateUpdater stateUpdater, IConfiguration configuration)
        {
            this.stateUpdater = stateUpdater;
            this.acquisitionTrigger = acquisitionTrigger;
            this.processingManager = processingManager;
            this.configuration = configuration;
            this.InitializeAcquisitionThread();
            this.StartAcquisitionThread();
        }

        #region Private Methods

        /// <summary>
        /// Initializes the acquisition thread.
        /// </summary>
        private void InitializeAcquisitionThread()
        {
            this.acquisitionWorker = new Thread(Acquisition_DoWork);
            this.acquisitionWorker.Name = "Acquisition thread";
        }

        /// <summary>
        /// Starts the acquisition thread.
        /// </summary>
        private void StartAcquisitionThread()
        {
            acquisitionWorker.Start();
        }

        /// <summary>
        /// Acquisitor thread logic.
        /// Periodically issues read commands for each configured point based on its
        /// acquisition interval.
        /// </summary>
        private void Acquisition_DoWork()
        {
            try
            {
                while (true)
                {

                    acquisitionTrigger.WaitOne();

                    foreach (var configItem in configuration.GetConfigurationItems())
                    {

                        configItem.SecondsPassedSinceLastPoll++;


                        if (configItem.AcquisitionInterval <= 0)
                            continue;

                        if (configItem.SecondsPassedSinceLastPoll >= configItem.AcquisitionInterval)
                        {

                            configItem.SecondsPassedSinceLastPoll = 0;

                            try
                            {

                                if (configItem.RegistryType == PointType.DIGITAL_INPUT ||
                                    configItem.RegistryType == PointType.ANALOG_INPUT)
                                {
                                    processingManager.ExecuteReadCommand(
                                        configItem,
                                        configuration.GetTransactionId(),
                                        configuration.UnitAddress,
                                        configItem.StartAddress,
                                        configItem.NumberOfRegisters
                                    );
                                }
                                else
                                {

                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                stateUpdater?.LogMessage($"[Acquisitor] {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {

            }
        }

        #endregion Private Methods

        /// <inheritdoc />
        public void Dispose()
        {
            acquisitionWorker.Abort();
        }
    }
}